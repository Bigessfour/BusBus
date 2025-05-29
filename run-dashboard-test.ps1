#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs the BusBus Dashboard Startup Test
.DESCRIPTION
    This script compiles and runs the standalone dashboard test to verify startup procedures and Route view functionality.
.PARAMETER Wait
    Wait for user input before exiting
.PARAMETER Verbose
    Enable verbose output
#>

param(
    [switch]$Wait,
    [switch]$Verbose
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "=== BusBus Dashboard Test Runner ===" -ForegroundColor Cyan
Write-Host "Starting at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host

try {
    # Check if .NET is available
    Write-Host "Checking .NET installation..." -ForegroundColor Yellow
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw ".NET SDK not found. Please install .NET 8.0 or later."
    }
    Write-Host "✓ .NET version: $dotnetVersion" -ForegroundColor Green

    # Check if SQL Server Express is running
    Write-Host "Checking SQL Server Express..." -ForegroundColor Yellow
    $sqlService = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue
    if ($sqlService -and $sqlService.Status -eq "Running") {
        Write-Host "✓ SQL Server Express is running" -ForegroundColor Green
    }
    else {
        Write-Host "⚠ SQL Server Express is not running - some tests may fail" -ForegroundColor Yellow
    }

    # Build the solution first
    Write-Host "Building BusBus solution..." -ForegroundColor Yellow
    dotnet build BusBus.sln --configuration Debug --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "✓ Build successful" -ForegroundColor Green

    # Compile the standalone test
    Write-Host "Compiling standalone test..." -ForegroundColor Yellow
    $testArgs = @(
        "DashboardStartupTest.cs"
        "-reference:bin\Debug\net8.0-windows\BusBus.dll"
        "-reference:bin\Debug\net8.0-windows\Microsoft.Extensions.DependencyInjection.dll"
        "-reference:bin\Debug\net8.0-windows\Microsoft.Extensions.Logging.dll"
        "-reference:bin\Debug\net8.0-windows\Microsoft.Extensions.Configuration.dll"
        "-reference:bin\Debug\net8.0-windows\Microsoft.Extensions.Configuration.Json.dll"
        "-reference:bin\Debug\net8.0-windows\Microsoft.EntityFrameworkCore.dll"
        "-reference:bin\Debug\net8.0-windows\Microsoft.EntityFrameworkCore.SqlServer.dll"
        "-out:DashboardStartupTest.exe"
        "-target:exe"
        "-platform:anycpu"
    )

    csc @testArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Test compilation failed"
    }
    Write-Host "✓ Test compiled successfully" -ForegroundColor Green

    # Run the standalone test
    Write-Host "Running standalone dashboard test..." -ForegroundColor Yellow
    Write-Host

    $testArgs = @()
    if ($Wait) { $testArgs += "--wait" }
    if ($Verbose) { $testArgs += "--verbose" }

    & ".\DashboardStartupTest.exe" @testArgs
    $testExitCode = $LASTEXITCODE

    Write-Host
    if ($testExitCode -eq 0) {
        Write-Host "✓ Standalone test completed successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "✗ Standalone test failed with exit code: $testExitCode" -ForegroundColor Red
    }

    # Also run MSTest if available
    Write-Host
    Write-Host "Running MSTest dashboard tests..." -ForegroundColor Yellow

    # Change to test directory
    Push-Location "BusBus.Tests"

    try {
        dotnet test --filter "TestCategory=UI" --logger "console;verbosity=detailed" --configuration Debug
        $msTestExitCode = $LASTEXITCODE

        if ($msTestExitCode -eq 0) {
            Write-Host "✓ MSTest dashboard tests passed!" -ForegroundColor Green
        }
        else {
            Write-Host "⚠ MSTest dashboard tests had issues (exit code: $msTestExitCode)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "⚠ MSTest execution failed: $($_.Exception.Message)" -ForegroundColor Yellow
        $msTestExitCode = 1
    }
    finally {
        Pop-Location
    }

    # Summary
    Write-Host
    Write-Host "=== Test Summary ===" -ForegroundColor Cyan
    Write-Host "Standalone Test: $(if ($testExitCode -eq 0) { "PASSED" } else { "FAILED" })" -ForegroundColor $(if ($testExitCode -eq 0) { "Green" } else { "Red" })
    Write-Host "MSTest Suite: $(if ($msTestExitCode -eq 0) { "PASSED" } else { "WARNING" })" -ForegroundColor $(if ($msTestExitCode -eq 0) { "Green" } else { "Yellow" })

    # Overall result
    $overallSuccess = ($testExitCode -eq 0)
    Write-Host "Overall Result: $(if ($overallSuccess) { "SUCCESS" } else { "FAILURE" })" -ForegroundColor $(if ($overallSuccess) { "Green" } else { "Red" })

    exit $(if ($overallSuccess) { 0 } else { 1 })
}
catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($Verbose) {
        Write-Host "Stack trace:" -ForegroundColor Gray
        Write-Host $_.Exception.StackTrace -ForegroundColor Gray
    }
    exit 1
}
finally {
    # Cleanup
    if (Test-Path "DashboardStartupTest.exe") {
        Remove-Item "DashboardStartupTest.exe" -Force -ErrorAction SilentlyContinue
    }

    Write-Host
    Write-Host "Completed at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

    if ($Wait) {
        Write-Host "Press any key to exit..." -ForegroundColor Gray
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    }
}
