# check-background-tasks.ps1
# This script runs the application with special monitoring to detect
# background tasks that might be preventing proper process termination

Write-Host "Running BusBus with background task monitoring..." -ForegroundColor Cyan

$env:BUSBUS_MONITOR_BACKGROUND_TASKS = "true"
$env:BUSBUS_DEBUG_SHUTDOWN = "true"

# Run the application with a timeout
$processStartInfo = New-Object System.Diagnostics.ProcessStartInfo
$processStartInfo.FileName = "dotnet"
$processStartInfo.Arguments = "run --project BusBus.csproj"
$processStartInfo.UseShellExecute = $false
$processStartInfo.RedirectStandardOutput = $true
$processStartInfo.RedirectStandardError = $true

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $processStartInfo
$process.Start() | Out-Null

# Create background jobs to read output streams
$outputJob = Start-Job -ScriptBlock {
    $process = $args[0]
    while (!$process.StandardOutput.EndOfStream) {
        $line = $process.StandardOutput.ReadLine()
        Write-Host $line
        if ($line -match "TASK_LEAK:") {
            $line | Out-File -Append "task_leaks.log"
        }
    }
} -ArgumentList $process

$errorJob = Start-Job -ScriptBlock {
    $process = $args[0]
    while (!$process.StandardError.EndOfStream) {
        $line = $process.StandardError.ReadLine()
        Write-Host $line -ForegroundColor Red
    }
} -ArgumentList $process

# Wait for user to press a key to stop
Write-Host "`nPress any key to stop the application and check for background tasks..." -ForegroundColor Yellow
$null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Stop the process
try {
    if (!$process.HasExited) {
        Write-Host "Stopping BusBus process..." -ForegroundColor Cyan
        $process.Kill()
    }
}
catch {
    Write-Host "Error stopping process: $_" -ForegroundColor Red
}

# Wait for output jobs to complete
Wait-Job -Job $outputJob, $errorJob -Timeout 5 | Out-Null
Stop-Job -Job $outputJob, $errorJob
Remove-Job -Job $outputJob, $errorJob -Force

# Check if there's a task_leaks.log file
if (Test-Path "task_leaks.log") {
    Write-Host "`nPotential task leaks detected:" -ForegroundColor Red
    Get-Content "task_leaks.log" | ForEach-Object {
        Write-Host $_ -ForegroundColor Red
    }

    Write-Host "`nTask leaks can prevent the application from terminating properly." -ForegroundColor Yellow
    Write-Host "Check these tasks to ensure they complete or are properly canceled." -ForegroundColor Yellow
}
else {
    Write-Host "`nNo task leaks detected." -ForegroundColor Green
}

# Reset environment variables
$env:BUSBUS_MONITOR_BACKGROUND_TASKS = ""
$env:BUSBUS_DEBUG_SHUTDOWN = ""
