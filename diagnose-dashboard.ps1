# Dashboard Diagnostics Helper Script
# This script helps identify dashboard loading issues

Write-Host "BusBus Dashboard Diagnostics" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan

# Check if we're in the right directory
if (-not (Test-Path "BusBus.csproj")) {
    Write-Host "ERROR: Please run this script from the BusBus project directory." -ForegroundColor Red
    exit 1
}

Write-Host "Environment Information:" -ForegroundColor Yellow
Write-Host "- Current Directory: $PWD"
Write-Host "- .NET Version: $(dotnet --version)" 
Write-Host "- OS: $([System.Environment]::OSVersion.VersionString)"
Write-Host "- Date/Time: $(Get-Date)"

# Check for diagnostics output directory
$outputDir = ".\diagnostics"
if (-not (Test-Path $outputDir)) {
    Write-Host "Creating diagnostics directory..." -ForegroundColor Green
    New-Item -Path $outputDir -ItemType Directory | Out-Null
}

# Generate a unique log file name
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = "$outputDir\dashboard_diag_$timestamp.log"
$perfLogFile = "$outputDir\dashboard_perf_$timestamp.csv"

Write-Host "Running dashboard diagnostics..." -ForegroundColor Green
Write-Host "Log file: $logFile" -ForegroundColor Gray

# Create CSV header for performance data
"Timestamp,Event,ElapsedMs" | Out-File $perfLogFile

# Start timestamp
$startTime = Get-Date

# Write to both console and log file
function Write-Log {
    param([string]$message, [string]$color = "White")
    
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    "$timestamp - $message" | Out-File $logFile -Append
    Write-Host $message -ForegroundColor $color
}

# Log performance data
function Write-PerfData {
    param([string]$event, [int]$elapsedMs)
    
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    "$timestamp,$event,$elapsedMs" | Out-File $perfLogFile -Append
}

# Start the application with diagnostics flag
Write-Log "Starting BusBus with dashboard diagnostics..." "Yellow"

try {
    Write-PerfData "StartProcess" 0
    
    # Run the application with diagnostics flag
    $appStartTime = Get-Date
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run --diagnose-dashboard" -NoNewWindow -PassThru
    Write-PerfData "ProcessStarted" ((Get-Date) - $appStartTime).TotalMilliseconds
    
    # Wait for the process to start
    Write-Log "Waiting for application to initialize..." "Gray"
    Start-Sleep -Seconds 2
    
    # Check if process is running
    if ($process.HasExited) {
        Write-Log "ERROR: Application failed to start or exited too quickly (Exit code: $($process.ExitCode))" "Red"
        exit 1
    }

    Write-Log "Application launched successfully with PID: $($process.Id)" "Green"
    Write-Log "Monitoring for diagnostic output..." "Yellow"
    
    # Monitor for up to 60 seconds
    $timeoutSec = 60
    $elapsed = 0
    while (-not $process.HasExited -and $elapsed -lt $timeoutSec) {
        Write-Progress -Activity "Running Dashboard Diagnostics" -Status "Waiting for results..." -PercentComplete (($elapsed / $timeoutSec) * 100)
        Start-Sleep -Seconds 1
        $elapsed++
        
        # Check if diagnostic file was created
        $newDiagFiles = Get-ChildItem -Path "." -Filter "dashboard_diagnostics_*.log" | Where-Object { $_.LastWriteTime -gt $startTime }
        if ($newDiagFiles.Count -gt 0) {
            $latestDiag = $newDiagFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            Write-Log "Diagnostic file found: $($latestDiag.Name)" "Green"
            Write-Log "Copying diagnostic data..." "Gray"
            
            # Copy content to our log
            Get-Content $latestDiag.FullName | Out-File $logFile -Append
            
            # No need to wait further
            break
        }
    }
    
    # If process is still running, wait a bit more for user to view results
    if (-not $process.HasExited) {
        Write-Log "Diagnostics completed, waiting for application to close..." "Yellow"
        $process.WaitForExit(30000) | Out-Null
    }
    
    # Check final status
    $totalTime = ((Get-Date) - $startTime).TotalSeconds
    Write-Log "Diagnostics completed in $totalTime seconds" "Cyan"
    
    # Check for diagnostic file one last time
    $newDiagFiles = Get-ChildItem -Path "." -Filter "dashboard_diagnostics_*.log" | Where-Object { $_.LastWriteTime -gt $startTime }
    if ($newDiagFiles.Count -gt 0) {
        $latestDiag = $newDiagFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        Write-Log "Results saved to: $($latestDiag.FullName)" "Green"
    } else {
        Write-Log "WARNING: No diagnostic output file was found" "Yellow"
    }
    
    Write-Log "Complete log saved to: $logFile" "Green"
    Write-Log "Performance data saved to: $perfLogFile" "Green"
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)" "Red"
    exit 1
}

Write-Host "`nDiagnostics completed. Open log file? (Y/N)" -ForegroundColor Cyan
$response = Read-Host
if ($response -eq "Y" -or $response -eq "y") {
    Invoke-Item $logFile
}

Write-Log "Dashboard diagnostics script completed successfully" "Green"
