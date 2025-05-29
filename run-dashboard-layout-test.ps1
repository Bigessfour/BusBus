# run-dashboard-layout-test.ps1
# Runs the dashboard layout tests with timeout protection

Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "      RUNNING DASHBOARD LAYOUT TEST WITH TIMEOUT        " -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host ""

# Set timeout value (in seconds)
$timeoutSeconds = 30

# Start the test process with timeout
$testProcess = Start-Process -FilePath "dotnet" -ArgumentList "test", "BusBus.Tests/BusBus.Tests.csproj", "--filter", "FullyQualifiedName~DashboardLayoutTests" -NoNewWindow -PassThru

Write-Host "Test process started with ID: $($testProcess.Id)" -ForegroundColor Yellow
Write-Host "Waiting for test to complete (timeout: $timeoutSeconds seconds)..." -ForegroundColor Yellow

# Wait for process to complete or timeout
$completed = $testProcess.WaitForExit($timeoutSeconds * 1000)

if (-not $completed) {
    Write-Host "Test timed out after $timeoutSeconds seconds!" -ForegroundColor Red
    Write-Host "Terminating test process..." -ForegroundColor Red

    try {
        $testProcess.Kill()
        Write-Host "Test process terminated." -ForegroundColor Yellow
    }
    catch {
        Write-Host "Error terminating test process: $_" -ForegroundColor Red
    }

    exit 1
}
else {
    $exitCode = $testProcess.ExitCode

    if ($exitCode -eq 0) {
        Write-Host "Tests completed successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "Tests failed with exit code: $exitCode" -ForegroundColor Red
    }

    exit $exitCode
}
