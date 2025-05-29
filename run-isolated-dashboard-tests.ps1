# Run only the isolated Dashboard UI tests
# This script avoids running other tests that may cause hanging issues

param(
    [switch]$Verbose = $false
)

Write-Host "=== Running Isolated Dashboard UI Tests ===" -ForegroundColor Green
Write-Host "This script runs only the dashboard UI tests to avoid hanging issues." -ForegroundColor Yellow
Write-Host ""

# Set location to test project directory
$testProjectPath = "c:\Users\steve.mckitrick\Desktop\BusBus\BusBus.Tests"
Set-Location $testProjectPath

try {
    # Build the test project first
    Write-Host "Building test project..." -ForegroundColor Cyan
    dotnet build --configuration Debug --verbosity minimal

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed! Cannot run tests." -ForegroundColor Red
        exit 1
    }

    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host ""

    # Run only the isolated Dashboard UI tests using category filter
    Write-Host "Running isolated Dashboard UI tests..." -ForegroundColor Cyan

    $testCommand = "dotnet test --configuration Debug --logger:console;verbosity=normal --filter `"TestCategory=DashboardUI`" --no-build"

    if ($Verbose) {
        $testCommand += " --verbosity diagnostic"
    }

    Write-Host "Executing: $testCommand" -ForegroundColor Gray
    Write-Host ""
    # Start the test with a timeout
    $process = Start-Process -FilePath "dotnet" -ArgumentList "test", "--configuration", "Debug", "--logger:console;verbosity=normal", "--filter", "TestCategory=DashboardUI", "--no-build" -NoNewWindow -PassThru

    # Wait for completion with timeout (30 seconds)
    $completed = $process.WaitForExit(30000)

    if (-not $completed) {
        Write-Host "Test execution timed out after 30 seconds. Killing process..." -ForegroundColor Red
        $process.Kill()
        Write-Host "Process killed. This indicates there may be hanging issues in the test infrastructure." -ForegroundColor Yellow
        exit 1
    }

    $exitCode = $process.ExitCode

    if ($exitCode -eq 0) {
        Write-Host ""
        Write-Host "=== Dashboard UI Tests Completed Successfully! ===" -ForegroundColor Green
    }
    else {
        Write-Host ""
        Write-Host "=== Dashboard UI Tests Failed ===" -ForegroundColor Red
        Write-Host "Exit code: $exitCode" -ForegroundColor Red
    }

    exit $exitCode
}
catch {
    Write-Host "Error running tests: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    # Return to original location
    Set-Location "c:\Users\steve.mckitrick\Desktop\BusBus"
}
