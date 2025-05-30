# Database Schema Fix Script
# Executes the SQL script to add missing IsActive columns

Write-Host "=== BusBus Database Schema Fix ===" -ForegroundColor Cyan
Write-Host "Adding missing IsActive columns to fix schema mismatch" -ForegroundColor Yellow
Write-Host ""

# Check if sqlcmd is available
Write-Host "Checking sqlcmd availability..." -ForegroundColor Green
try {
    & sqlcmd -? 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd not found"
    }
    Write-Host "✓ sqlcmd is available" -ForegroundColor Green
}
catch {
    Write-Host "✗ sqlcmd is not available. Installing SQL Server Command Line Utilities..." -ForegroundColor Yellow
    Write-Host "Please install SQL Server Command Line Utilities from:" -ForegroundColor Red
    Write-Host "https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility" -ForegroundColor Red
    exit 1
}

# Test SQL Server connection
Write-Host ""
Write-Host "Testing SQL Server connection..." -ForegroundColor Green
$connectionString = "ST-LPTP9-23\SQLEXPRESS01"
$database = "BusBusDB"

try {
    $testResult = & sqlcmd -S $connectionString -d $database -Q "SELECT 1 as Test" -h -1 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Connection failed: $testResult"
    }
    Write-Host "✓ SQL Server connection successful" -ForegroundColor Green
}
catch {
    Write-Host "✗ SQL Server connection failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please ensure:" -ForegroundColor Yellow
    Write-Host "  1. SQL Server Express is running" -ForegroundColor White
    Write-Host "  2. Instance name is correct: $connectionString" -ForegroundColor White
    Write-Host "  3. Database exists: $database" -ForegroundColor White
    Write-Host "  4. Windows Authentication is enabled" -ForegroundColor White
    exit 1
}

# Execute the schema fix script
Write-Host ""
Write-Host "Executing database schema fix..." -ForegroundColor Green
try {
    $result = & sqlcmd -S $connectionString -d $database -i "fix-database-schema-isactive.sql" 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Schema fix script executed successfully!" -ForegroundColor Green

        # Display the output
        if ($result) {
            Write-Host ""
            Write-Host "Script output:" -ForegroundColor Cyan
            $result | Where-Object { $_ -and $_.Trim() -ne "" } | ForEach-Object {
                Write-Host "  $_" -ForegroundColor White
            }
        }
    }
    else {
        throw "SQL execution failed with exit code $LASTEXITCODE"
    }

}
catch {
    Write-Host "✗ Schema fix failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($result) {
        Write-Host "SQL Error output:" -ForegroundColor Red
        $result | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    }
    exit 1
}

Write-Host ""
Write-Host "🎉 Database schema fix completed!" -ForegroundColor Green
Write-Host "The application should now load without 'Invalid column name' errors." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run the application: dotnet run" -ForegroundColor White
Write-Host "2. Test navigation to Drivers and Vehicles views" -ForegroundColor White
Write-Host "3. Verify data loads properly in all views" -ForegroundColor White
