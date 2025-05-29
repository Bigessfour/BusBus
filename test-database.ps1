# Quick Database Connection Test
# Run this before starting the BusBus application

Write-Host "=== Quick Database Test ===" -ForegroundColor Green

$connectionString = "Server=localhost\SQLEXPRESS;Database=BusBusDB;Integrated Security=true;Connection Timeout=10"

try {
    Add-Type -AssemblyName "System.Data"

    Write-Host "Testing connection to SQL Server Express..." -ForegroundColor Yellow

    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    Write-Host "✅ Connection successful!" -ForegroundColor Green

    # Quick test query
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
    $tableCount = $command.ExecuteScalar()

    Write-Host "📊 Tables in database: $tableCount" -ForegroundColor Cyan

    if ($tableCount -eq 0) {
        Write-Host "⚠️ No tables found. Run setup-database.ps1 to create the database schema." -ForegroundColor Yellow
    }

    $connection.Close()

    Write-Host "🎉 Database is ready for BusBus application!" -ForegroundColor Green
}
catch {
    Write-Host "❌ Database test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Run: .\verify-sqlserver-express.ps1" -ForegroundColor White
    Write-Host "2. Run: .\setup-database.ps1" -ForegroundColor White
    Write-Host "3. Check if SQL Server Express is installed and running" -ForegroundColor White

    exit 1
}
