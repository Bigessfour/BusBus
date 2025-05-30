# Application Lifecycle Test - Full Startup to Shutdown
# Tests the complete application lifecycle including UI interaction

Write-Host "=== BusBus Application Lifecycle Test ===" -ForegroundColor Cyan
Write-Host "Testing complete startup, UI interaction, and shutdown flow" -ForegroundColor Yellow
Write-Host ""

# Function to get dotnet processes
function Get-DotnetProcesses {
    return Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -eq "dotnet" -and
        ($_.MainWindowTitle -like "*BusBus*" -or $_.CommandLine -like "*BusBus*" -or $_.Id -eq $global:AppProcessId)
    }
}

# Function to get detailed process info
function Get-ProcessDetails {
    param($processes)
    if ($processes) {
        foreach ($proc in $processes) {
            $cmdLine = ""
            try {
                $cmdLine = (Get-WmiObject Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
            }
            catch {
                $cmdLine = "Unable to get command line"
            }
            Write-Host "  PID: $($proc.Id), Name: $($proc.ProcessName), Window: '$($proc.MainWindowTitle)', CommandLine: $($cmdLine.Substring(0, [Math]::Min(100, $cmdLine.Length)))..." -ForegroundColor Gray
        }
    }
    else {
        Write-Host "  No dotnet processes found" -ForegroundColor Gray
    }
}

# Test Phase 1: Pre-startup state
Write-Host "Phase 1: Pre-startup Process Check" -ForegroundColor Green
$initialProcesses = Get-DotnetProcesses
$initialCount = if ($initialProcesses) { $initialProcesses.Count } else { 0 }
Write-Host "Initial dotnet processes: $initialCount"
Get-ProcessDetails $initialProcesses
Write-Host ""

# Test Phase 2: Application startup
Write-Host "Phase 2: Application Startup" -ForegroundColor Green
Write-Host "Starting BusBus application with verbose logging..."

try {
    # Start the application as a background job with detailed monitoring
    $job = Start-Job -ScriptBlock {
        param($workingDir)
        Set-Location $workingDir
        & dotnet run --verbose 2>&1
    } -ArgumentList (Get-Location).Path

    $global:AppProcessId = $null

    # Wait for startup with detailed monitoring
    Write-Host "Waiting for application startup (max 30 seconds)..." -ForegroundColor Yellow
    $startupTimeout = 30
    $started = $false

    for ($i = 1; $i -le $startupTimeout; $i++) {
        Start-Sleep -Seconds 1

        # Check for new dotnet processes
        $currentProcesses = Get-DotnetProcesses
        $currentCount = if ($currentProcesses) { $currentProcesses.Count } else { 0 }

        # Look for the application process
        $appProcess = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
            $_.ProcessName -eq "dotnet" -and $_.Id -notin ($initialProcesses | ForEach-Object { $_.Id })
        }

        if ($appProcess) {
            $global:AppProcessId = $appProcess.Id
            Write-Host "  $i/$startupTimeout seconds - Found new dotnet process: PID $($appProcess.Id)" -ForegroundColor Green
            $started = $true
            break
        }
        else {
            Write-Host "  $i/$startupTimeout seconds - Waiting for process to start..." -ForegroundColor Gray
        }

        # Check job output for errors
        $jobOutput = Receive-Job -Job $job -ErrorAction SilentlyContinue
        if ($jobOutput) {
            Write-Host "  Application output: $($jobOutput[-1])" -ForegroundColor Cyan
        }
    }

    if (-not $started) {
        Write-Host "ERROR: Application failed to start within $startupTimeout seconds" -ForegroundColor Red
        $jobOutput = Receive-Job -Job $job
        Write-Host "Job output:" -ForegroundColor Yellow
        $jobOutput | ForEach-Object { Write-Host "  $_" }
        Remove-Job -Job $job -Force
        exit 1
    }

    # Test Phase 3: Post-startup verification
    Write-Host ""
    Write-Host "Phase 3: Post-startup Verification" -ForegroundColor Green
    Start-Sleep -Seconds 3  # Allow full startup

    $postStartupProcesses = Get-DotnetProcesses
    $postStartupCount = if ($postStartupProcesses) { $postStartupProcesses.Count } else { 0 }
    Write-Host "Post-startup dotnet processes: $postStartupCount"
    Get-ProcessDetails $postStartupProcesses

    # Verify expected process count
    if ($postStartupCount -eq 1) {
        Write-Host "✓ Expected process count (1 process)" -ForegroundColor Green
    }
    else {
        Write-Host "⚠ Unexpected process count. Expected: 1, Actual: $postStartupCount" -ForegroundColor Yellow
    }

    # Test Phase 4: Runtime monitoring
    Write-Host ""
    Write-Host "Phase 4: Runtime Monitoring (10 seconds)" -ForegroundColor Green
    Write-Host "Monitoring application stability and resource usage..."

    for ($i = 1; $i -le 10; $i++) {
        Start-Sleep -Seconds 1
        $runtimeProcesses = Get-DotnetProcesses
        $runtimeCount = if ($runtimeProcesses) { $runtimeProcesses.Count } else { 0 }

        # Check if main process is still running
        $mainProcess = Get-Process -Id $global:AppProcessId -ErrorAction SilentlyContinue
        if (-not $mainProcess) {
            Write-Host "ERROR: Main application process terminated unexpectedly!" -ForegroundColor Red
            break
        }

        # Monitor memory usage
        $memoryMB = [Math]::Round($mainProcess.WorkingSet64 / 1MB, 2)
        Write-Host "  $i/10 seconds - Processes: $runtimeCount, Memory: ${memoryMB}MB, Status: Running" -ForegroundColor Gray

        # Check for new processes (potential leaks)
        if ($runtimeCount -gt $postStartupCount) {
            Write-Host "  ⚠ Process count increased! Possible process leak detected." -ForegroundColor Yellow
        }

        # Check job output for any errors
        $jobOutput = Receive-Job -Job $job -ErrorAction SilentlyContinue
        if ($jobOutput) {
            $jobOutput | Where-Object { $_ -match "error|exception|failed" } | ForEach-Object {
                Write-Host "  Error output: $_" -ForegroundColor Red
            }
        }
    }

    # Test Phase 5: Shutdown initiation
    Write-Host ""
    Write-Host "Phase 5: Shutdown Initiation" -ForegroundColor Green
    Write-Host "Initiating graceful shutdown..."

    # Try graceful shutdown first
    $shutdownSuccess = $false
    if ($global:AppProcessId) {
        $mainProcess = Get-Process -Id $global:AppProcessId -ErrorAction SilentlyContinue
        if ($mainProcess -and $mainProcess.MainWindowHandle -ne [IntPtr]::Zero) {
            Write-Host "Sending close message to main window..." -ForegroundColor Yellow
            try {
                Add-Type -AssemblyName System.Windows.Forms
                [System.Windows.Forms.SendKeys]::SendWait("%{F4}")  # Alt+F4
                $shutdownSuccess = $true
            }
            catch {
                Write-Host "Failed to send close message: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }

        if (-not $shutdownSuccess) {
            Write-Host "Sending SIGTERM to process..." -ForegroundColor Yellow
            try {
                Stop-Process -Id $global:AppProcessId -ErrorAction SilentlyContinue
                $shutdownSuccess = $true
            }
            catch {
                Write-Host "Failed to send SIGTERM: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    }

    # Test Phase 6: Shutdown monitoring
    Write-Host ""
    Write-Host "Phase 6: Shutdown Monitoring" -ForegroundColor Green
    Write-Host "Monitoring shutdown process (max 20 seconds)..."

    $shutdownTimeout = 20
    $cleanShutdown = $false

    for ($i = 1; $i -le $shutdownTimeout; $i++) {
        Start-Sleep -Seconds 1

        $shutdownProcesses = Get-DotnetProcesses
        $shutdownCount = if ($shutdownProcesses) { $shutdownProcesses.Count } else { 0 }

        Write-Host "  $i/$shutdownTimeout seconds - Remaining processes: $shutdownCount" -ForegroundColor Gray

        if ($shutdownCount -eq 0) {
            Write-Host "✓ Clean shutdown achieved in $i seconds!" -ForegroundColor Green
            $cleanShutdown = $true
            break
        }

        # Check job status
        if ($job.State -eq "Completed") {
            Write-Host "  Application job completed" -ForegroundColor Cyan
        }
    }

    # Final cleanup if processes still exist
    if (-not $cleanShutdown) {
        Write-Host "Forcing cleanup of remaining processes..." -ForegroundColor Yellow
        $remainingProcesses = Get-DotnetProcesses
        foreach ($proc in $remainingProcesses) {
            try {
                Stop-Process -Id $proc.Id -Force
                Write-Host "  Force killed process: $($proc.Id)" -ForegroundColor Red
            }
            catch {
                Write-Host "  Failed to kill process $($proc.Id): $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }

    # Clean up job
    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue

    # Test Phase 7: Final verification
    Write-Host ""
    Write-Host "Phase 7: Final Verification" -ForegroundColor Green
    Start-Sleep -Seconds 2

    $finalProcesses = Get-DotnetProcesses
    $finalCount = if ($finalProcesses) { $finalProcesses.Count } else { 0 }

    Write-Host "Final dotnet processes: $finalCount"
    Get-ProcessDetails $finalProcesses

    # Test Results Summary
    Write-Host ""
    Write-Host "=== Test Results Summary ===" -ForegroundColor Cyan
    Write-Host "Initial processes: $initialCount" -ForegroundColor White
    Write-Host "After startup: $postStartupCount" -ForegroundColor White
    Write-Host "After shutdown: $finalCount" -ForegroundColor White
    Write-Host "Clean shutdown: $cleanShutdown" -ForegroundColor White

    if ($finalCount -eq 0 -and $cleanShutdown) {
        Write-Host ""
        Write-Host "🎉 SUCCESS: Complete application lifecycle test passed!" -ForegroundColor Green
        Write-Host "   - Application started successfully" -ForegroundColor Green
        Write-Host "   - Runtime was stable" -ForegroundColor Green
        Write-Host "   - Shutdown was clean" -ForegroundColor Green
        Write-Host "   - No process leaks detected" -ForegroundColor Green
    }
    else {
        Write-Host ""
        Write-Host "❌ FAILURE: Application lifecycle test failed!" -ForegroundColor Red
        if ($finalCount -gt 0) {
            Write-Host "   - Process leak: $finalCount processes remaining" -ForegroundColor Red
        }
        if (-not $cleanShutdown) {
            Write-Host "   - Shutdown timeout: Required force termination" -ForegroundColor Red
        }
    }

}
catch {
    Write-Host "CRITICAL ERROR during lifecycle test: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.Exception.StackTrace)" -ForegroundColor Red

    # Emergency cleanup
    Write-Host "Performing emergency cleanup..." -ForegroundColor Yellow
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -eq "dotnet"
    } | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force
            Write-Host "Emergency killed process: $($_.Id)" -ForegroundColor Red
        }
        catch {
            Write-Host "Failed to emergency kill process $($_.Id)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Application lifecycle test completed." -ForegroundColor Cyan
