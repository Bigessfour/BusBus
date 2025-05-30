# Quick Application Lifecycle Test
Write-Host "=== Quick BusBus Lifecycle Test ===" -ForegroundColor Cyan

# Check initial state
$initial = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
Write-Host "Initial dotnet processes: $($initial.Count)" -ForegroundColor Yellow

# Start application
Write-Host "Starting application..." -ForegroundColor Green
$process = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -WindowStyle Normal

# Wait for startup
Write-Host "Waiting 10 seconds for startup..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check running state
$running = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
Write-Host "Running dotnet processes: $($running.Count)" -ForegroundColor Green
$running | ForEach-Object { Write-Host "  PID: $($_.Id), Memory: $([Math]::Round($_.WorkingSet64/1MB,2))MB" }

# Wait a bit more
Write-Host "Testing runtime stability (5 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Try to close
Write-Host "Attempting to close application..." -ForegroundColor Yellow
try {
    if ($process -and -not $process.HasExited) {
        $process.CloseMainWindow()
        Write-Host "Sent close signal to main window" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "Error closing: $($_.Exception.Message)" -ForegroundColor Red
}

# Monitor shutdown
Write-Host "Monitoring shutdown (max 15 seconds)..." -ForegroundColor Yellow
for ($i = 1; $i -le 15; $i++) {
    Start-Sleep -Seconds 1
    $current = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    Write-Host "  $i/15 - Remaining processes: $($current.Count)" -ForegroundColor Gray

    if ($current.Count -eq 0) {
        Write-Host "✓ Clean shutdown in $i seconds!" -ForegroundColor Green
        break
    }
}

# Final state
$final = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
Write-Host "Final dotnet processes: $($final.Count)" -ForegroundColor $(if ($final.Count -eq 0) { "Green" } else { "Red" })

if ($final.Count -eq 0) {
    Write-Host "🎉 SUCCESS: Clean lifecycle!" -ForegroundColor Green
}
else {
    Write-Host "❌ FAILURE: Processes remaining" -ForegroundColor Red
    $final | ForEach-Object {
        Write-Host "  Remaining: PID $($_.Id)" -ForegroundColor Red
        try { Stop-Process -Id $_.Id -Force } catch { }
    }
}

Write-Host "Test completed." -ForegroundColor Cyan
