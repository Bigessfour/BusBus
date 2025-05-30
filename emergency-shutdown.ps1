# emergency-shutdown.ps1
# Comprehensive emergency shutdown script for BusBus application
# This script will forcefully terminate all related processes and clean up resources

param(
    [switch]$Force,
    [switch]$Silent,
    [switch]$Detailed
)

if (-not $Silent) {
    Write-Host "=== BusBus Emergency Shutdown ===" -ForegroundColor Red
    Write-Host "This script will forcefully terminate all BusBus and .NET processes" -ForegroundColor Yellow
    Write-Host ""
}

# Function to write status messages
function Write-Status {
    param($Message, $Color = "White")
    if (-not $Silent) {
        Write-Host $Message -ForegroundColor $Color
    }
}

# Function to get all related processes
function Get-BusBusRelatedProcesses {
    $processes = @()

    # Direct process names
    $processNames = @("BusBus", "dotnet", "testhost", "vstest.console", "MSBuild", "VBCSCompiler")

    foreach ($name in $processNames) {
        $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
        $processes += $procs
    }

    # Also check for processes with BusBus in command line
    $allProcesses = Get-Process -ErrorAction SilentlyContinue
    foreach ($process in $allProcesses) {
        try {
            $wmiProcess = Get-WmiObject -Class Win32_Process -Filter "ProcessId = $($process.Id)" -ErrorAction SilentlyContinue
            if ($wmiProcess -and $wmiProcess.CommandLine -like "*BusBus*" -and $process -notin $processes) {
                $processes += $process
            }
        }
        catch {
            # Ignore errors when checking command line
        }
    }

    return $processes | Sort-Object Id -Unique
}

# Get all related processes
$processes = Get-BusBusRelatedProcesses

if ($processes.Count -eq 0) {
    Write-Status "✓ No BusBus or .NET processes found running" "Green"
    exit 0
}

Write-Status "Found $($processes.Count) processes that may need termination:" "Yellow"

if ($Detailed) {
    foreach ($process in $processes) {
        try {
            $runtime = if ($process.StartTime) { (Get-Date) - $process.StartTime } else { "Unknown" }
            $memory = [math]::Round($process.WorkingSet / 1MB, 2)
            Write-Status "  PID: $($process.Id), Name: $($process.ProcessName), Runtime: $runtime, Memory: ${memory}MB" "Gray"
        }
        catch {
            Write-Status "  PID: $($process.Id), Name: $($process.ProcessName), Status: Unknown" "Gray"
        }
    }
    Write-Status ""
}

# Ask for confirmation unless Force is specified
if (-not $Force -and -not $Silent) {
    $confirmation = Read-Host "Proceed with termination? (Y/N)"
    if ($confirmation -ne "Y" -and $confirmation -ne "y") {
        Write-Status "Operation cancelled" "Yellow"
        exit 0
    }
}

Write-Status "Terminating processes..." "Red"

# Terminate processes with increasing force
$terminatedCount = 0
$errorCount = 0

foreach ($process in $processes) {
    $processId = $process.Id
    $processName = $process.ProcessName

    try {
        # Skip our own PowerShell process
        if ($processId -eq $PID) {
            continue
        }

        # Method 1: Graceful Stop-Process
        try {
            Stop-Process -Id $processId -ErrorAction Stop
            Write-Status "  ✓ Gracefully stopped $processName (PID: $processId)" "Green"
            $terminatedCount++
            Start-Sleep -Milliseconds 200
            continue
        }
        catch {
            # Continue to force method
        }

        # Method 2: Force Stop-Process
        try {
            Stop-Process -Id $processId -Force -ErrorAction Stop
            Write-Status "  ✓ Force stopped $processName (PID: $processId)" "Yellow"
            $terminatedCount++
            Start-Sleep -Milliseconds 200
            continue
        }
        catch {
            # Continue to taskkill method
        }

        # Method 3: taskkill with /T (tree)
        try {
            $result = cmd /c "taskkill /F /PID $processId /T 2>&1"
            if ($LASTEXITCODE -eq 0) {
                Write-Status "  ✓ Taskkill terminated $processName (PID: $processId)" "Orange"
                $terminatedCount++
            } else {
                Write-Status "  ✗ Failed to terminate $processName (PID: $processId): $result" "Red"
                $errorCount++
            }
        }
        catch {
            Write-Status "  ✗ All methods failed for $processName (PID: $processId)" "Red"
            $errorCount++
        }
    }
    catch {
        Write-Status "  ✗ Error processing $processName (PID: $processId): $_" "Red"
        $errorCount++
    }
}

# Wait and check for remaining processes
Start-Sleep -Seconds 2
$remainingProcesses = Get-BusBusRelatedProcesses

Write-Status ""
Write-Status "=== Shutdown Summary ===" "Cyan"
Write-Status "Processes terminated: $terminatedCount" "Green"
Write-Status "Errors encountered: $errorCount" "Red"
Write-Status "Processes remaining: $($remainingProcesses.Count)" $(if ($remainingProcesses.Count -eq 0) { "Green" } else { "Red" })

if ($remainingProcesses.Count -eq 0) {
    Write-Status "✓ All processes successfully terminated!" "Green"

    # Additional cleanup: Clear temp files and logs
    try {
        $tempPath = Join-Path $env:TEMP "BusBus*"
        $tempFiles = Get-ChildItem $tempPath -ErrorAction SilentlyContinue
        if ($tempFiles) {
            Remove-Item $tempPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Status "✓ Cleaned up temporary files" "Green"
        }
    }
    catch {
        # Ignore temp cleanup errors
    }

    Write-Status "System is now clean and ready for restart" "Green"
} else {
    Write-Status "⚠ Some processes remain. You may need to:" "Yellow"
    Write-Status "  - Restart your computer" "Yellow"
    Write-Status "  - Check for system-level processes" "Yellow"
    Write-Status "  - Run as Administrator" "Yellow"

    if ($Detailed) {
        Write-Status ""
        Write-Status "Remaining processes:" "Red"
        $remainingProcesses | Format-Table Id, ProcessName, StartTime -AutoSize
    }
}

Write-Status ""
Write-Status "=== Emergency Shutdown Complete ===" "Cyan"
