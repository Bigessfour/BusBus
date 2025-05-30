try {
    $connectionString = "Server=.\SQLEXPRESS;Database=BusBusDB;Integrated Security=true;Connection Timeout=5;"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    Write-Host "Testing data access..." -ForegroundColor Yellow

    # Test Drivers table
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT COUNT(*) FROM Drivers"
    $driversCount = $command.ExecuteScalar()
    Write-Host "Drivers count: $driversCount" -ForegroundColor Green

    # Test Vehicles table
    $command.CommandText = "SELECT COUNT(*) FROM Vehicles"
    $vehiclesCount = $command.ExecuteScalar()
    Write-Host "Vehicles count: $vehiclesCount" -ForegroundColor Green

    # Test Routes table
    $command.CommandText = "SELECT COUNT(*) FROM Routes"
    $routesCount = $command.ExecuteScalar()
    Write-Host "Routes count: $routesCount" -ForegroundColor Green

    # Sample some driver data
    $command.CommandText = "SELECT TOP 3 Id, FirstName, LastName, LicenseType FROM Drivers"
    $reader = $command.ExecuteReader()
    Write-Host "`nSample Driver Data:" -ForegroundColor Cyan
    while ($reader.Read()) {
        Write-Host "  $($reader['FirstName']) $($reader['LastName']) - $($reader['LicenseType'])" -ForegroundColor White
    }
    $reader.Close()
    # Sample some vehicle data
    $command.CommandText = "SELECT TOP 3 Id, Number, Model, LicensePlate FROM Vehicles"
    $reader = $command.ExecuteReader()
    Write-Host "`nSample Vehicle Data:" -ForegroundColor Cyan
    while ($reader.Read()) {
        $model = if ($reader['Model'] -eq [DBNull]::Value) { 'Unknown Model' } else { $reader['Model'] }
        $plate = if ($reader['LicensePlate'] -eq [DBNull]::Value) { 'No Plate' } else { $reader['LicensePlate'] }
        Write-Host "  Vehicle #$($reader['Number']) - $model ($plate)" -ForegroundColor White
    }
    $reader.Close()

    $connection.Close()
    Write-Host "`nDatabase connectivity test completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
}
