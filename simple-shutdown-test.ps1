#!/usr/bin/env pwsh
# Simple Shutdown Test - Direct approach

Write-Host "=== BusBus Shutdown Test ===" -ForegroundColor Cyan

# Kill any existing dotnet processes first to start clean
Write-Host "Cleaning up any existing dotnet processes..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Wait for cleanup
Start-Sleep -Seconds 2

# Count initial processes
$initialCount = (Get-Process -Name "dotnet" -ErrorAction SilentlyContinue).Count
Write-Host "Initial dotnet processes: $initialCount" -ForegroundColor Green

# Start app and immediately test shutdown
Write-Host "Starting app for 5 seconds, then testing shutdown..." -ForegroundColor Yellow

# Use a job to run the app so we can control it
$job = Start-Job -ScriptBlock {
    Set-Location "c:\Users\steve.mckitrick\Desktop\BusBus"
    dotnet run
}

# Wait for startup
Start-Sleep -Seconds 5

# Check process count during run
$runningCount = (Get-Process -Name "dotnet" -ErrorAction SilentlyContinue).Count
Write-Host "Dotnet processes while running: $runningCount" -ForegroundColor Yellow

# Stop the job (simulates user closing app)
Write-Host "Stopping application..." -ForegroundColor Red
Stop-Job $job -PassThru | Remove-Job

# Wait for shutdown
Write-Host "Waiting for shutdown..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Check final count
$finalCount = (Get-Process -Name "dotnet" -ErrorAction SilentlyContinue).Count
Write-Host "Final dotnet processes: $finalCount" -ForegroundColor Green

# Results
if ($finalCount -gt $initialCount) {
    $orphaned = $finalCount - $initialCount
    Write-Host "FAILED: $orphaned orphaned processes!" -ForegroundColor Red

    # Show orphaned processes
    Write-Host "Orphaned processes:" -ForegroundColor Red
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "  PID: $($_.Id), StartTime: $($_.StartTime)" -ForegroundColor Red
    }

    # Clean up
    Write-Host "Cleaning up..." -ForegroundColor Yellow
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force
}
else {
    Write-Host "SUCCESS: Clean shutdown!" -ForegroundColor Green
}

Write-Host "Test completed." -ForegroundColor Cyan
