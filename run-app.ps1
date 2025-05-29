Write-Host "Starting BusBus application..."
Set-Location "bin\Debug\net8.0-windows"
Start-Process -FilePath ".\BusBus.exe" -Wait
