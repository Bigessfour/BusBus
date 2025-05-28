# terminate-hanging-processes.ps1
# Script to forcefully terminate hanging .NET processes

Write-Host "Searching for hanging .NET processes..." -ForegroundColor Cyan

# Get all dotnet processes
$dotnetProcesses = Get-Process | Where-Object { $_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "BusBus*" }

if ($dotnetProcesses.Count -eq 0) {
    Write-Host "No .NET processes found." -ForegroundColor Green
    exit
}

Write-Host "Found $($dotnetProcesses.Count) .NET processes:" -ForegroundColor Yellow

# Display all processes
$dotnetProcesses | Format-Table Id, ProcessName, StartTime, CPU, WorkingSet -AutoSize

# Confirm before terminating
$confirmation = Read-Host "Do you want to terminate these processes? (Y/N)"
if ($confirmation -ne "Y") {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit
}

# Terminate each process
foreach ($process in $dotnetProcesses) {
    try {
        Stop-Process -Id $process.Id -Force -ErrorAction Stop
        Write-Host "Process $($process.Id) ($($process.ProcessName)) terminated successfully." -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to terminate process $($process.Id): $_" -ForegroundColor Red
    }
}

Write-Host "Process termination complete." -ForegroundColor Cyan

# Check if any processes still remain
$remainingProcesses = Get-Process | Where-Object { $_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "BusBus*" }
if ($remainingProcesses.Count -gt 0) {
    Write-Host "Warning: $($remainingProcesses.Count) processes still remain!" -ForegroundColor Red
    $remainingProcesses | Format-Table Id, ProcessName -AutoSize
    
    $confirmation = Read-Host "Do you want to try terminating these with taskkill? (Y/N)"
    if ($confirmation -eq "Y") {
        foreach ($process in $remainingProcesses) {
            try {
                # Use taskkill as a last resort
                $output = cmd /c "taskkill /F /PID $($process.Id) /T 2>&1"
                Write-Host $output -ForegroundColor Gray
            }
            catch {
                Write-Host "Failed with taskkill: $_" -ForegroundColor Red
            }
        }
    }
}
else {
    Write-Host "All processes successfully terminated." -ForegroundColor Green
}
