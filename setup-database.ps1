# BusBus Database Setup Script
# Following Microsoft SQL Server Express best practices

param(
    [switch]$Force,
    [switch]$Verbose
)

Write-Host "=== BusBus Database Setup ===" -ForegroundColor Green

# Check if SQL Server Express is available first
& ".\verify-sqlserver-express.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ SQL Server Express verification failed. Please install SQL Server Express first." -ForegroundColor Red
    exit 1
}

$connectionString = "Server=localhost\SQLEXPRESS;Integrated Security=true;Connection Timeout=30"
$dbConnectionString = "Server=localhost\SQLEXPRESS;Database=BusBusDB;Integrated Security=true;Connection Timeout=30"

Write-Host "`n=== Checking Database State ===" -ForegroundColor Green

try {
    Add-Type -AssemblyName "System.Data"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    # Check if database exists
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = 'BusBusDB'"
    $dbExists = $command.ExecuteScalar()

    if ($dbExists -eq 1) {
        Write-Host "✅ BusBusDB database exists." -ForegroundColor Green

        if ($Force) {
            Write-Host "⚠️ Force flag specified. Dropping existing database..." -ForegroundColor Yellow

            # Drop existing database
            $command.CommandText = @"
                ALTER DATABASE BusBusDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE BusBusDB;
"@
            $command.ExecuteNonQuery()
            Write-Host "✅ Existing database dropped." -ForegroundColor Green
            $dbExists = 0
        }
        else {
            Write-Host "ℹ️ Database already exists. Use -Force to recreate." -ForegroundColor Cyan
        }
    }

    if ($dbExists -eq 0) {
        Write-Host "📝 Creating BusBusDB database..." -ForegroundColor Yellow
          # Create database with best practices settings
        $dataPath = "${env:ProgramFiles}\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA"
        $command.CommandText = @"
            CREATE DATABASE BusBusDB
            ON (
                NAME = 'BusBusDB',
                FILENAME = '$dataPath\BusBusDB.mdf',
                SIZE = 100MB,
                MAXSIZE = 1GB,
                FILEGROWTH = 10MB
            )
            LOG ON (
                NAME = 'BusBusDB_Log',
                FILENAME = '$dataPath\BusBusDB_Log.ldf',
                SIZE = 10MB,
                MAXSIZE = 100MB,
                FILEGROWTH = 5MB
            );
"@

        try {
            $command.ExecuteNonQuery()
            Write-Host "✅ BusBusDB database created successfully." -ForegroundColor Green
        }
        catch {
            Write-Host "⚠️ Database creation with custom settings failed. Using simple creation..." -ForegroundColor Yellow

            # Fallback to simple database creation
            $command.CommandText = "CREATE DATABASE BusBusDB;"
            $command.ExecuteNonQuery()
            Write-Host "✅ BusBusDB database created (simple mode)." -ForegroundColor Green
        }
    }

    $connection.Close()
}
catch {
    Write-Host "❌ Database setup failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Running Entity Framework Migrations ===" -ForegroundColor Green

try {
    # Ensure we're in the project directory
    Set-Location $PSScriptRoot

    Write-Host "📝 Updating database with EF Core migrations..." -ForegroundColor Yellow

    $migrationOutput = & dotnet ef database update --verbose 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ EF Core migrations completed successfully." -ForegroundColor Green
        if ($Verbose) {
            Write-Host "Migration Output:" -ForegroundColor Cyan
            $migrationOutput | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
        }
    }
    else {
        Write-Host "❌ EF Core migrations failed:" -ForegroundColor Red
        $migrationOutput | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        exit 1
    }
}
catch {
    Write-Host "❌ EF Core migration execution failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Verifying Database Tables ===" -ForegroundColor Green

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($dbConnectionString)
    $connection.Open()

    $command = $connection.CreateCommand()
    $command.CommandText = @"
        SELECT TABLE_NAME
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_TYPE = 'BASE TABLE'
        ORDER BY TABLE_NAME
"@

    $reader = $command.ExecuteReader()
    $tableCount = 0

    Write-Host "📊 Database tables created:" -ForegroundColor Cyan
    while ($reader.Read()) {
        Write-Host "   - $($reader["TABLE_NAME"])" -ForegroundColor Gray
        $tableCount++
    }

    $reader.Close()
    $connection.Close()

    if ($tableCount -gt 0) {
        Write-Host "✅ Database setup complete with $tableCount tables." -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ No tables found. Check if migrations ran correctly." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "⚠️ Could not verify tables: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n=== Setup Complete ===" -ForegroundColor Green
Write-Host "🎉 BusBus database is ready for use!" -ForegroundColor Cyan
Write-Host "Connection string: Server=localhost\SQLEXPRESS;Database=BusBusDB;Integrated Security=true" -ForegroundColor Gray
