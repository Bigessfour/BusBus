try {
    $connectionString = "Server=.\SQLEXPRESS;Database=BusBusDB;Integrated Security=true;Connection Timeout=5;"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    Write-Host "Adding missing columns to Drivers table..." -ForegroundColor Yellow

    $command = $connection.CreateCommand()

    # Add LicenseType column
    Write-Host "Adding LicenseType column..." -ForegroundColor Cyan
    $command.CommandText = "ALTER TABLE Drivers ADD LicenseType nvarchar(50) NULL"
    $command.ExecuteNonQuery()
    Write-Host "SUCCESS: LicenseType column added" -ForegroundColor Green

    # Add PerformanceMetricsJson column
    Write-Host "Adding PerformanceMetricsJson column..." -ForegroundColor Cyan
    $command.CommandText = "ALTER TABLE Drivers ADD PerformanceMetricsJson nvarchar(max) NULL"
    $command.ExecuteNonQuery()
    Write-Host "SUCCESS: PerformanceMetricsJson column added" -ForegroundColor Green

    Write-Host ""
    Write-Host "Verifying all columns..." -ForegroundColor Yellow

    # Verify all columns exist
    $columns = @('IsActive', 'LicenseType', 'PerformanceMetricsJson')
    foreach ($columnName in $columns) {
        $command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = '$columnName'"
        $result = $command.ExecuteScalar()
        if ($result) {
            Write-Host "SUCCESS: Drivers.$columnName column exists" -ForegroundColor Green
        } else {
            Write-Host "FAILED: Drivers.$columnName column MISSING" -ForegroundColor Red
        }
    }

    $connection.Close()
    Write-Host ""
    Write-Host "Database schema update completed!" -ForegroundColor Green
} catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
}
