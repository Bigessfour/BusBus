# kill-hung-dotnet-processes.ps1
# This script finds and terminates any hung .NET processes related to BusBus
# Usage: .\kill-hung-dotnet-processes.ps1

Write-Host "Searching for hanging .NET processes related to BusBus..." -ForegroundColor Cyan

# Find all dotnet processes
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
$busBusProcesses = Get-Process -Name "BusBus" -ErrorAction SilentlyContinue

$allBusBusRelated = @()

# Check dotnet processes related to BusBus
foreach ($process in $dotnetProcesses) {
    try {
        # Check command line to find BusBus related processes
        $cmdLine = (Get-WmiObject -Class Win32_Process -Filter "ProcessId = $($process.Id)" -ErrorAction SilentlyContinue).CommandLine
        if ($cmdLine -like "*BusBus*") {
            $allBusBusRelated += $process
        }
    }
    catch {
        Write-Host "Error checking process $($process.Id): $_" -ForegroundColor Red
    }
}

# Add explicit BusBus processes
$allBusBusRelated += $busBusProcesses

# Report and kill
if ($allBusBusRelated.Count -gt 0) {
    Write-Host "Found $($allBusBusRelated.Count) BusBus-related processes:" -ForegroundColor Yellow

    foreach ($process in $allBusBusRelated) {
        $processInfo = "Process ID: $($process.Id), Name: $($process.ProcessName), StartTime: $($process.StartTime)"
        Write-Host $processInfo -ForegroundColor Yellow

        try {
            # Kill the process
            $process.Kill()
            Write-Host "Successfully terminated process $($process.Id)" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to terminate process $($process.Id): $_" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "No hanging BusBus processes found." -ForegroundColor Green
}

# Also look for any MSBuild or dotnet build processes that might be related
$buildProcesses = Get-Process -Name "MSBuild", "dotnet" -ErrorAction SilentlyContinue |
Where-Object { $_.StartTime -gt (Get-Date).AddHours(-1) }

if ($buildProcesses.Count -gt 0) {
    Write-Host "`nFound $($buildProcesses.Count) recent build-related processes:" -ForegroundColor Yellow
    foreach ($process in $buildProcesses) {
        Write-Host "Process ID: $($process.Id), Name: $($process.ProcessName), StartTime: $($process.StartTime)" -ForegroundColor Yellow
    }

    $killBuild = Read-Host "Do you want to kill these build processes too? (y/n)"
    if ($killBuild -eq "y") {
        foreach ($process in $buildProcesses) {
            try {
                $process.Kill()
                Write-Host "Successfully terminated build process $($process.Id)" -ForegroundColor Green
            }
            catch {
                Write-Host "Failed to terminate build process $($process.Id): $_" -ForegroundColor Red
            }
        }
    }
}

Write-Host "`nProcess cleanup complete." -ForegroundColor Cyan
