# Shutdown Test Script
# This script tests the application shutdown behavior

Write-Host "Starting BusBus Shutdown Test..." -ForegroundColor Cyan

# Function to get all dotnet processes
function Get-DotNetProcesses {
    return Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
}

# Function to get BusBus-related processes
function Get-BusBusProcesses {
    return Get-Process | Where-Object { $_.ProcessName -eq "dotnet" -and $_.MainWindowTitle -like "*BusBus*" } -ErrorAction SilentlyContinue
}

# Initial process count
$initialProcesses = Get-DotNetProcesses
Write-Host "Initial dotnet processes: $($initialProcesses.Count)" -ForegroundColor Yellow

# Start the application in the background
Write-Host "Starting BusBus application..." -ForegroundColor Green
$appProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -WindowStyle Normal

# Wait for application to fully start
Write-Host "Waiting for application to start (10 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check processes after startup
$afterStartupProcesses = Get-DotNetProcesses
Write-Host "Dotnet processes after startup: $($afterStartupProcesses.Count)" -ForegroundColor Yellow

# Show the process tree
Write-Host "`nProcess details:" -ForegroundColor Cyan
$afterStartupProcesses | ForEach-Object {
    Write-Host "  PID: $($_.Id), Name: $($_.ProcessName), Window: '$($_.MainWindowTitle)'"
}

# Wait a bit more for full initialization
Start-Sleep -Seconds 5

# Now test shutdown by sending close signal
Write-Host "`nTesting shutdown by sending close signal..." -ForegroundColor Red

# Try to close the main window gracefully
$busBusProcesses = Get-BusBusProcesses
if ($busBusProcesses) {
    Write-Host "Found BusBus processes: $($busBusProcesses.Count)" -ForegroundColor Yellow
    foreach ($proc in $busBusProcesses) {
        Write-Host "  Closing PID: $($proc.Id), Window: '$($proc.MainWindowTitle)'"
        try {
            $proc.CloseMainWindow()
        }
        catch {
            Write-Host "    Failed to close main window: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "No BusBus-specific processes found, trying main process..." -ForegroundColor Yellow
    try {
        $appProcess.CloseMainWindow()
    }
    catch {
        Write-Host "Failed to close main window: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Wait for graceful shutdown
Write-Host "Waiting for graceful shutdown (15 seconds)..." -ForegroundColor Yellow
$shutdownTimer = 0
$maxWaitTime = 15

while ($shutdownTimer -lt $maxWaitTime) {
    Start-Sleep -Seconds 1
    $shutdownTimer++

    $remainingProcesses = Get-DotNetProcesses
    $remainingCount = $remainingProcesses.Count

    Write-Host "  $shutdownTimer/$maxWaitTime seconds - Remaining dotnet processes: $remainingCount" -ForegroundColor Gray

    # Check if we're back to initial state
    if ($remainingCount -le $initialProcesses.Count) {
        Write-Host "SUCCESS: Application shut down cleanly!" -ForegroundColor Green
        break
    }
}

# Final process check
$finalProcesses = Get-DotNetProcesses
$finalCount = $finalProcesses.Count

Write-Host "`nFinal Results:" -ForegroundColor Cyan
Write-Host "  Initial processes: $($initialProcesses.Count)" -ForegroundColor Yellow
Write-Host "  After startup: $($afterStartupProcesses.Count)" -ForegroundColor Yellow
Write-Host "  After shutdown: $finalCount" -ForegroundColor Yellow

if ($finalCount -gt $initialProcesses.Count) {
    $orphanedCount = $finalCount - $initialProcesses.Count
    Write-Host "FAILED: $orphanedCount orphaned processes detected!" -ForegroundColor Red

    Write-Host "`nOrphaned processes:" -ForegroundColor Red
    $finalProcesses | ForEach-Object {
        $isOrphaned = $_.Id -notin $initialProcesses.Id
        if ($isOrphaned) {
            Write-Host "  PID: $($_.Id), Name: $($_.ProcessName), Window: '$($_.MainWindowTitle)'" -ForegroundColor Red
        }
    }

    # Kill orphaned processes for cleanup
    Write-Host "`nCleaning up orphaned processes..." -ForegroundColor Yellow
    $finalProcesses | ForEach-Object {
        $isOrphaned = $_.Id -notin $initialProcesses.Id
        if ($isOrphaned) {
            try {
                Write-Host "  Killing PID: $($_.Id)"
                $_.Kill()
            }
            catch {
                Write-Host "    Failed to kill PID $($_.Id): $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}
else {
    Write-Host "SUCCESS: Clean shutdown achieved!" -ForegroundColor Green
}

Write-Host "`nShutdown test completed." -ForegroundColor Cyan
