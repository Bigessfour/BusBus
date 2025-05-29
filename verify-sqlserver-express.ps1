# SQL Server Express Installation Verification Script
# Based on Microsoft documentation recommendations

Write-Host "=== SQL Server Express Installation Verification ===" -ForegroundColor Green

# Check if SQL Server Express is installed
$sqlServerServices = Get-Service -Name "*SQL*" -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "*SQLEXPRESS*" }

if ($sqlServerServices.Count -eq 0) {
    Write-Host "❌ SQL Server Express not found. Please install it first." -ForegroundColor Red
    Write-Host "Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads" -ForegroundColor Yellow
    Write-Host "Choose 'Express' edition for development." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ SQL Server Express services found:" -ForegroundColor Green
$sqlServerServices | ForEach-Object {
    Write-Host "   - $($_.Name): $($_.Status)" -ForegroundColor Cyan
}

# Check if SQL Server Express is running
$runningServices = $sqlServerServices | Where-Object { $_.Status -eq "Running" }
if ($runningServices.Count -eq 0) {
    Write-Host "⚠️ SQL Server Express is installed but not running. Starting services..." -ForegroundColor Yellow

    try {
        Start-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction Stop
        Write-Host "✅ SQL Server Express started successfully." -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to start SQL Server Express: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Try running as Administrator or check SQL Server Configuration Manager." -ForegroundColor Yellow
        exit 1
    }
}
else {
    Write-Host "✅ SQL Server Express is running." -ForegroundColor Green
}

# Test connection using sqlcmd
Write-Host "`n=== Testing Database Connection ===" -ForegroundColor Green

try {
    $connectionString = "Server=localhost\SQLEXPRESS;Integrated Security=true;Connection Timeout=30"

    # Use .NET SqlConnection to test connectivity
    Add-Type -AssemblyName "System.Data"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT @@VERSION"
    $version = $command.ExecuteScalar()

    Write-Host "✅ Connection successful!" -ForegroundColor Green
    Write-Host "   SQL Server Version: $($version.Split("`n")[0])" -ForegroundColor Cyan

    $connection.Close()
}
catch {
    Write-Host "❌ Connection failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check if:" -ForegroundColor Yellow
    Write-Host "   1. SQL Server Express is properly installed" -ForegroundColor Yellow
    Write-Host "   2. SQL Server Browser service is running" -ForegroundColor Yellow
    Write-Host "   3. TCP/IP protocol is enabled in SQL Server Configuration Manager" -ForegroundColor Yellow
    Write-Host "   4. Windows Firewall allows SQL Server Express connections" -ForegroundColor Yellow
    exit 1
}

# Check if BusBusDB database exists
try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = 'BusBusDB'"
    $dbExists = $command.ExecuteScalar()

    if ($dbExists -eq 1) {
        Write-Host "✅ BusBusDB database exists." -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ BusBusDB database does not exist. Will be created by EF migrations." -ForegroundColor Yellow
    }

    $connection.Close()
}
catch {
    Write-Host "⚠️ Could not check database existence: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n=== SQL Server Express Configuration Check ===" -ForegroundColor Green

# Check SQL Server Browser service
$browserService = Get-Service -Name "SQLBrowser" -ErrorAction SilentlyContinue
if ($browserService) {
    if ($browserService.Status -eq "Running") {
        Write-Host "✅ SQL Server Browser service is running." -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ SQL Server Browser service is not running. Starting..." -ForegroundColor Yellow
        try {
            Start-Service -Name "SQLBrowser" -ErrorAction Stop
            Write-Host "✅ SQL Server Browser service started." -ForegroundColor Green
        }
        catch {
            Write-Host "❌ Failed to start SQL Server Browser: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "This may cause connection issues with named instances." -ForegroundColor Yellow
        }
    }
}
else {
    Write-Host "⚠️ SQL Server Browser service not found." -ForegroundColor Yellow
}

Write-Host "`n=== Summary ===" -ForegroundColor Green
Write-Host "SQL Server Express verification complete." -ForegroundColor Cyan
Write-Host "If you encounter issues:" -ForegroundColor Yellow
Write-Host "1. Ensure SQL Server Express is installed with default instance name 'SQLEXPRESS'" -ForegroundColor Yellow
Write-Host "2. Enable TCP/IP protocol in SQL Server Configuration Manager" -ForegroundColor Yellow
Write-Host "3. Start SQL Server Browser service for named instance discovery" -ForegroundColor Yellow
Write-Host "4. Check Windows Firewall settings" -ForegroundColor Yellow
Write-Host "5. Run this script as Administrator if needed" -ForegroundColor Yellow
