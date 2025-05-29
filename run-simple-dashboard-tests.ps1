# Run only the simplified Dashboard UI tests with process timeout protection
# This script prevents hanging by forcibly terminating any test that runs too long

param(
    [int]$TimeoutSeconds = 10,
    [switch]$Verbose = $false
)

Write-Host "=== Running Simple Dashboard UI Tests With Timeout Protection ===" -ForegroundColor Green
Write-Host "This script runs only simplified Dashboard UI tests with a ${TimeoutSeconds}-second timeout." -ForegroundColor Yellow
Write-Host ""

# Build the test project first
Write-Host "Building test project..." -ForegroundColor Cyan
$buildProcess = Start-Process -FilePath "dotnet" -ArgumentList "build", "--configuration", "Debug", "--verbosity", "minimal", "BusBus.Tests" -NoNewWindow -PassThru -Wait
if ($buildProcess.ExitCode -ne 0) {
    Write-Host "Build failed! Cannot run tests." -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Create a job to run the tests with timeout protection
Write-Host "Running simple Dashboard UI tests with ${TimeoutSeconds}s timeout protection..." -ForegroundColor Cyan

# Start the test process
$testProcess = Start-Process -FilePath "dotnet" -ArgumentList "test", "BusBus.Tests", "--filter", "`"TestCategory=SimpleDashboardUI`"", "--no-build" -NoNewWindow -PassThru

# Wait for completion with timeout
$testStartTime = Get-Date
$completed = $false

try {
    # Check if process completes before timeout
    while (-not $completed -and ((Get-Date) - $testStartTime).TotalSeconds -lt $TimeoutSeconds) {
        $completed = $testProcess.HasExited
        if (-not $completed) {
            Start-Sleep -Milliseconds 100
        }
    }

    if (-not $completed) {
        Write-Host "`nTest execution timed out after $TimeoutSeconds seconds." -ForegroundColor Red
        Write-Host "Forcibly terminating test process..." -ForegroundColor Yellow

        try {
            # Try to kill the main process
            $testProcess.Kill($true)  # Kill process tree
            Write-Host "Test process terminated successfully." -ForegroundColor Yellow
        }
        catch {
            Write-Host "Failed to terminate test process gracefully. Trying alternative method..." -ForegroundColor Red

            # Use more aggressive approach
            Start-Process -FilePath "taskkill" -ArgumentList "/F", "/T", "/PID", $testProcess.Id -NoNewWindow -Wait
        }

        # Check for any remaining dotnet processes that might be hung
        $hangingProcesses = Get-Process -Name "dotnet" | Where-Object { $_.MainWindowTitle -eq "" -and $_.StartTime -gt $testStartTime }

        if ($hangingProcesses -and $hangingProcesses.Count -gt 0) {
            Write-Host "Found $($hangingProcesses.Count) potentially hanging dotnet processes. Terminating..." -ForegroundColor Yellow
            foreach ($proc in $hangingProcesses) {
                try {
                    $proc.Kill()
                }
                catch {
                    Write-Host "Could not terminate process $($proc.Id)" -ForegroundColor Red
                }
            }
        }

        Write-Host "`nWARNING: Tests were forcibly terminated due to timeout." -ForegroundColor Red
        Write-Host "This indicates there may be hanging issues in the test infrastructure." -ForegroundColor Yellow
        exit 1
    }
    else {
        # Process completed naturally
        $exitCode = $testProcess.ExitCode

        if ($exitCode -eq 0) {
            Write-Host "`n=== Dashboard UI Tests Completed Successfully! ===" -ForegroundColor Green
        }
        else {
            Write-Host "`n=== Dashboard UI Tests Failed with exit code: $exitCode ===" -ForegroundColor Red
            Write-Host "Tests completed without hanging, but had errors or failures." -ForegroundColor Yellow
        }

        exit $exitCode
    }
}
catch {
    Write-Host "Error during test execution: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    # Ensure process is terminated if still running
    if (-not $testProcess.HasExited) {
        try { $testProcess.Kill() } catch { }
    }
}
