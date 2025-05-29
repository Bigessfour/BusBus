# Safe BusBus Test Runner with Timeout Protection
# This script runs specified tests with timeout protection to prevent hanging

param(
    [string]$TestFilter = "SimpleBasicTest", # Default to running only simple basic tests
    [int]$TimeoutSeconds = 30,              # Default timeout of 30 seconds
    [switch]$Verbose = $false,
    [string]$Configuration = "Debug",
    [switch]$NoBuild = $false               # Skip build if already built
)

$ErrorActionPreference = "Stop"

Write-Host "=== BusBus Safe Test Runner ===" -ForegroundColor Green
Write-Host "Running tests with filter: $TestFilter" -ForegroundColor Cyan
Write-Host "Timeout protection: $TimeoutSeconds seconds" -ForegroundColor Yellow
Write-Host ""

# Navigate to the project directory
$projectDir = "c:\Users\steve.mckitrick\Desktop\BusBus"
Set-Location $projectDir

try {
    # Build the project if needed
    if (-not $NoBuild) {
        Write-Host "Building solution..." -ForegroundColor Cyan
        dotnet build --configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
        Write-Host "Build completed successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "Skipping build (using existing binaries)" -ForegroundColor Yellow
    }

    # Build the dotnet test command
    $testArgs = @(
        "test",
        "--configuration", $Configuration,
        "--filter", $TestFilter,
        "--no-build"
    )

    if ($Verbose) {
        $testArgs += "--verbosity"
        $testArgs += "detailed"
    }
    else {
        $testArgs += "--verbosity"
        $testArgs += "minimal"
    }

    # Log the command we're about to run
    $testCommand = "dotnet $($testArgs -join ' ')"
    Write-Host "`nExecuting test command: $testCommand" -ForegroundColor Cyan
    Write-Host "(with $TimeoutSeconds second timeout protection)`n" -ForegroundColor Yellow

    # Run the test with timeout protection
    $job = Start-Job -ScriptBlock {
        param($projectDir, $testArgs)
        Set-Location $projectDir
        & dotnet $testArgs
        return $LASTEXITCODE
    } -ArgumentList $projectDir, $testArgs    # Wait for completion with timeout
    $completed = Wait-Job -Job $job -Timeout $TimeoutSeconds

    if ($null -eq $completed) {
        Write-Host "`nWARNING: Tests are taking too long (over $TimeoutSeconds seconds)!" -ForegroundColor Red
        Write-Host "Stopping runaway tests to prevent hanging..." -ForegroundColor Red

        # Stop the job and any child processes
        Stop-Job -Job $job
        Remove-Job -Job $job -Force

        # Find and kill any runaway dotnet test processes
        Write-Host "Killing any hanging dotnet test processes..." -ForegroundColor Red
        $testProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -like "*test*" -and $_.StartTime -gt (Get-Date).AddMinutes(-5) }

        if ($testProcesses) {
            foreach ($process in $testProcesses) {
                Write-Host "Killing process ID $($process.Id)" -ForegroundColor Yellow
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
        }

        Write-Host "`nTest execution was terminated due to timeout." -ForegroundColor Red
        exit 1
    }
    else {
        # Get the job output
        $result = Receive-Job -Job $job
        $exitCode = $job.ChildJobs[0].Output[-1]
        Remove-Job -Job $job

        Write-Host $result

        if ($exitCode -eq 0) {
            Write-Host "`nTests completed successfully!" -ForegroundColor Green
            exit 0
        }
        else {
            Write-Host "`nTests failed with exit code $exitCode" -ForegroundColor Red
            exit $exitCode
        }
    }
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red

    # Try to clean up any runaway processes
    Write-Host "Attempting to clean up any runaway processes..." -ForegroundColor Yellow
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like "*test*" -and $_.StartTime -gt (Get-Date).AddMinutes(-5) } |
        ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }

    exit 1
}
finally {
    # Always return to the original directory
    Set-Location $projectDir
}
