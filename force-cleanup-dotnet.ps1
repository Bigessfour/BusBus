# force-cleanup-dotnet.ps1
# Enhanced script to forcefully clean up all .NET processes and prevent hanging

Write-Host "=== Enhanced .NET Process Cleanup ===" -ForegroundColor Cyan
Write-Host "This script will clean up all .NET processes and prevent future hanging." -ForegroundColor Yellow
Write-Host ""

# Function to get detailed process information
function Get-DetailedProcessInfo {
    param($ProcessId)
    try {
        $wmiProcess = Get-WmiObject -Class Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction SilentlyContinue
        return @{
            CommandLine     = $wmiProcess.CommandLine
            ParentProcessId = $wmiProcess.ParentProcessId
        }
    }
    catch {
        return @{
            CommandLine     = "Unknown"
            ParentProcessId = "Unknown"
        }
    }
}

# Find all .NET related processes
$processNames = @("dotnet", "BusBus", "testhost", "vstest.console", "MSBuild")
$allProcesses = @()

foreach ($name in $processNames) {
    $processes = Get-Process -Name $name -ErrorAction SilentlyContinue
    $allProcesses += $processes
}

# Also find any processes with .NET in the command line
$allRunningProcesses = Get-Process
foreach ($process in $allRunningProcesses) {
    $details = Get-DetailedProcessInfo -ProcessId $process.Id
    if ($details.CommandLine -like "*dotnet*" -or $details.CommandLine -like "*BusBus*") {
        if ($process -notin $allProcesses) {
            $allProcesses += $process
        }
    }
}

if ($allProcesses.Count -eq 0) {
    Write-Host "No .NET processes found. System is clean." -ForegroundColor Green
    exit 0
}

Write-Host "Found $($allProcesses.Count) .NET-related processes:" -ForegroundColor Yellow
Write-Host ""

# Display detailed information about each process
foreach ($process in $allProcesses) {
    $details = Get-DetailedProcessInfo -ProcessId $process.Id
    $runtime = if ($process.StartTime) { (Get-Date) - $process.StartTime } else { "Unknown" }

    Write-Host "Process ID: $($process.Id)" -ForegroundColor White
    Write-Host "  Name: $($process.ProcessName)" -ForegroundColor Gray
    Write-Host "  Start Time: $($process.StartTime)" -ForegroundColor Gray
    Write-Host "  Runtime: $runtime" -ForegroundColor Gray
    Write-Host "  Memory: $([math]::Round($process.WorkingSet / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host "  Command Line: $($details.CommandLine)" -ForegroundColor Gray
    Write-Host "  Parent PID: $($details.ParentProcessId)" -ForegroundColor Gray
    Write-Host ""
}

# Ask for confirmation
$confirmation = Read-Host "Do you want to terminate ALL these processes? (Y/N)"
if ($confirmation -ne "Y" -and $confirmation -ne "y") {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Terminating processes..." -ForegroundColor Red

# Kill processes in order of priority (children first, then parents)
$processIds = $allProcesses | Sort-Object Id -Descending | Select-Object -ExpandProperty Id

foreach ($processId in $processIds) {
    try {
        # Try graceful termination first
        Stop-Process -Id $processId -ErrorAction Stop
        Write-Host "✓ Gracefully terminated process $processId" -ForegroundColor Green
        Start-Sleep -Milliseconds 500
    }
    catch {
        try {
            # Force termination if graceful fails
            Stop-Process -Id $processId -Force -ErrorAction Stop
            Write-Host "✓ Force terminated process $processId" -ForegroundColor Yellow
        }
        catch {
            try {
                # Use taskkill as last resort
                $result = cmd /c "taskkill /F /PID $processId /T 2>&1"
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ Taskkill terminated process $processId" -ForegroundColor Orange
                }
                else {
                    Write-Host "✗ Failed to terminate process $processId : $result" -ForegroundColor Red
                }
            }
            catch {
                Write-Host "✗ All termination methods failed for process $processId" -ForegroundColor Red
            }
        }
    }
}

# Wait a moment and check for remaining processes
Start-Sleep -Seconds 2
Write-Host ""
Write-Host "Checking for remaining processes..." -ForegroundColor Cyan

$remainingProcesses = @()
foreach ($name in $processNames) {
    $processes = Get-Process -Name $name -ErrorAction SilentlyContinue
    $remainingProcesses += $processes
}

if ($remainingProcesses.Count -eq 0) {
    Write-Host "✓ All .NET processes successfully terminated!" -ForegroundColor Green
}
else {
    Write-Host "⚠ Warning: $($remainingProcesses.Count) processes still remain:" -ForegroundColor Red
    $remainingProcesses | Format-Table Id, ProcessName, StartTime -AutoSize

    Write-Host ""
    Write-Host "You may need to restart your system or check for system-level .NET processes." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Cleanup Complete ===" -ForegroundColor Cyan
