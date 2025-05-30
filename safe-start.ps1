# safe-start.ps1
# Safe startup script for BusBus with automatic cleanup and monitoring

param(
    [switch]$Force,
    [switch]$Debug,
    [switch]$SkipCleanup
)

Write-Host "=== BusBus Safe Startup ===" -ForegroundColor Cyan
Write-Host "Starting BusBus with enhanced process monitoring..." -ForegroundColor Green
Write-Host ""

# Step 1: Pre-cleanup (unless skipped)
if (-not $SkipCleanup) {
    Write-Host "Step 1: Running pre-startup cleanup..." -ForegroundColor Yellow

    if (Test-Path ".\pre-start-cleanup.ps1") {
        & ".\pre-start-cleanup.ps1"
    } else {
        Write-Host "⚠ pre-start-cleanup.ps1 not found, running basic cleanup..." -ForegroundColor Yellow

        # Basic cleanup
        $processes = Get-Process -Name "BusBus", "dotnet" -ErrorAction SilentlyContinue
        if ($processes) {
            Write-Host "Found $($processes.Count) existing processes, terminating..." -ForegroundColor Yellow
            $processes | Stop-Process -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
        }
    }
} else {
    Write-Host "Step 1: Skipping cleanup (--SkipCleanup specified)" -ForegroundColor Gray
}

# Step 2: Setup monitoring
Write-Host ""
Write-Host "Step 2: Setting up process monitoring..." -ForegroundColor Yellow

# Create a monitoring job that will forcefully terminate after 5 minutes if app hangs
$monitoringJob = Start-Job -ScriptBlock {
    param($ParentPid)

    Start-Sleep -Seconds 300  # Wait 5 minutes

    # Check if parent process is still alive
    try {
        $parentProcess = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
        if ($parentProcess) {
            Write-Host "[MONITOR] Application running longer than 5 minutes, checking for hanging..." -ForegroundColor Yellow

            # Look for BusBus processes
            $busBusProcesses = Get-Process -Name "*BusBus*", "*dotnet*" -ErrorAction SilentlyContinue
            if ($busBusProcesses) {
                Write-Host "[MONITOR] Found potentially hanging processes, initiating emergency shutdown..." -ForegroundColor Red

                # Run emergency shutdown
                $scriptPath = Join-Path $using:PWD "emergency-shutdown.ps1"
                if (Test-Path $scriptPath) {
                    & $scriptPath -Force -Silent
                } else {
                    # Fallback - kill processes directly
                    $busBusProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }
    catch {
        # Parent process ended normally, nothing to do
    }
} -ArgumentList $PID

Write-Host "✓ Background monitoring started (PID: $($monitoringJob.Id))" -ForegroundColor Green

# Step 3: Build the application
Write-Host ""
Write-Host "Step 3: Building application..." -ForegroundColor Yellow

try {
    $buildResult = dotnet build BusBus.sln --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Build failed!" -ForegroundColor Red
        Write-Host $buildResult
        Stop-Job $monitoringJob -ErrorAction SilentlyContinue
        Remove-Job $monitoringJob -ErrorAction SilentlyContinue
        exit 1
    }
    Write-Host "✓ Build successful" -ForegroundColor Green
}
catch {
    Write-Host "✗ Build error: $($_.Exception.Message)" -ForegroundColor Red
    Stop-Job $monitoringJob -ErrorAction SilentlyContinue
    Remove-Job $monitoringJob -ErrorAction SilentlyContinue
    exit 1
}

# Step 4: Setup Ctrl+C handler for emergency shutdown
Write-Host ""
Write-Host "Step 4: Setting up emergency handlers..." -ForegroundColor Yellow

# Register cleanup handler
Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action {
    Write-Host "[CLEANUP] PowerShell exiting, running emergency cleanup..." -ForegroundColor Red

    if (Test-Path ".\emergency-shutdown.ps1") {
        & ".\emergency-shutdown.ps1" -Force -Silent
    }

    # Stop monitoring job
    Stop-Job $using:monitoringJob -ErrorAction SilentlyContinue
    Remove-Job $using:monitoringJob -ErrorAction SilentlyContinue
}

Write-Host "✓ Emergency handlers registered" -ForegroundColor Green

# Step 5: Start the application with timeout
Write-Host ""
Write-Host "Step 5: Starting BusBus application..." -ForegroundColor Green
Write-Host "Press Ctrl+C at any time for emergency shutdown" -ForegroundColor Yellow
Write-Host ""

try {
    # Set environment variables for better debugging
    $env:BUSBUS_MONITOR_BACKGROUND_TASKS = "true"
    $env:BUSBUS_AGGRESSIVE_CLEANUP = "true"

    # Add debug flags if requested
    $debugArgs = @()
    if ($Debug) {
        $debugArgs += "--verbose"
        Write-Host "Debug mode enabled" -ForegroundColor Cyan
    }

    # Start the application
    if ($debugArgs.Count -gt 0) {
        dotnet run -- $debugArgs
    } else {
        dotnet run
    }
}
catch {
    Write-Host "✗ Application error: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    # Cleanup
    Write-Host ""
    Write-Host "Performing post-run cleanup..." -ForegroundColor Yellow

    # Stop monitoring job
    Stop-Job $monitoringJob -ErrorAction SilentlyContinue
    Remove-Job $monitoringJob -ErrorAction SilentlyContinue

    # Run emergency shutdown to ensure no processes are left
    if (Test-Path ".\emergency-shutdown.ps1") {
        Write-Host "Running final cleanup..." -ForegroundColor Yellow
        & ".\emergency-shutdown.ps1" -Force -Silent
    }

    Write-Host "✓ Cleanup complete" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== BusBus Session Complete ===" -ForegroundColor Cyan
