# BusBus Logging Output Verification Script
# This script helps verify Microsoft.Extensions.Logging output in VS Code Insiders
# when experiencing project system issues

param(
    [switch]$QuickTest,
    [switch]$FullDiagnostics,
    [string]$LogLevel = "Debug"
)

Write-Host "🔍 BusBus Logging Output Verification" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Set environment variables for enhanced logging
$env:DOTNET_ENVIRONMENT = "Development"
$env:BUSBUS_TEST_LIFECYCLE = "true"
$env:LOGGING__LOGLEVEL__DEFAULT = $LogLevel
$env:LOGGING__LOGLEVEL__BUSBUS = "Debug"

Write-Host "Environment Configuration:" -ForegroundColor Yellow
Write-Host "• DOTNET_ENVIRONMENT: $env:DOTNET_ENVIRONMENT"
Write-Host "• BUSBUS_TEST_LIFECYCLE: $env:BUSBUS_TEST_LIFECYCLE"
Write-Host "• LOG_LEVEL: $LogLevel"
Write-Host ""

# Function to run with output capture
function Test-LoggingOutput {
    param([string]$TestName, [scriptblock]$TestBlock)

    Write-Host "🧪 Testing: $TestName" -ForegroundColor Green
    Write-Host "─────────────────────────────────────" -ForegroundColor Gray

    try {
        & $TestBlock
        Write-Host "✅ $TestName completed" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ $TestName failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

# Test 1: Build verification
Test-LoggingOutput "Build Verification" {
    Write-Host "Building BusBus solution..."
    $buildResult = dotnet build BusBus.sln --verbosity minimal
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Build failed" -ForegroundColor Red
        Write-Host $buildResult
    }
}

# Test 2: Quick run test (if requested)
if ($QuickTest) {
    Test-LoggingOutput "Quick Application Start Test" {
        Write-Host "Starting BusBus for 10 seconds to capture initial logs..."

        # Start the application in background
        $process = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "BusBus.csproj", "--configuration", "Debug" -PassThru -RedirectStandardOutput "quick-test-output.log" -RedirectStandardError "quick-test-error.log"

        # Wait 10 seconds
        Start-Sleep 10

        # Stop the application
        if (!$process.HasExited) {
            $process.Kill()
            $process.WaitForExit(5000)
        }

        Write-Host "Application run completed. Checking outputs..."

        # Check standard output
        if (Test-Path "quick-test-output.log") {
            $output = Get-Content "quick-test-output.log" -ErrorAction SilentlyContinue
            if ($output) {
                Write-Host "📺 Standard Output Captured:" -ForegroundColor Cyan
                $output | ForEach-Object { Write-Host "   $_" }
            }
        }

        # Check error output
        if (Test-Path "quick-test-error.log") {
            $errorOutput = Get-Content "quick-test-error.log" -ErrorAction SilentlyContinue
            if ($errorOutput) {
                Write-Host "⚠️ Error Output Captured:" -ForegroundColor Yellow
                $errorOutput | ForEach-Object { Write-Host "   $_" }
            }
        }
    }
}

# Test 3: VS Code Configuration Check
Test-LoggingOutput "VS Code Configuration Check" {
    Write-Host "Checking VS Code launch.json configuration..."

    if (Test-Path ".vscode/launch.json") {
        $launchConfig = Get-Content ".vscode/launch.json" -Raw

        # Check for console setting
        if ($launchConfig -match '"console":\s*"integratedTerminal"') {
            Write-Host "✅ Console set to 'integratedTerminal'" -ForegroundColor Green
        }
        else {
            Write-Host "⚠️ Console not set to 'integratedTerminal'" -ForegroundColor Yellow
        }

        # Check for environment variables
        if ($launchConfig -match '"DOTNET_ENVIRONMENT":\s*"Development"') {
            Write-Host "✅ DOTNET_ENVIRONMENT set to Development" -ForegroundColor Green
        }
        else {
            Write-Host "ℹ️ DOTNET_ENVIRONMENT not explicitly set" -ForegroundColor Blue
        }
    }
    else {
        Write-Host "❌ .vscode/launch.json not found" -ForegroundColor Red
    }
}

# Test 4: Logging Configuration Analysis
Test-LoggingOutput "Logging Configuration Analysis" {
    Write-Host "Analyzing Program.cs logging setup..."

    if (Test-Path "Program.cs") {
        $programContent = Get-Content "Program.cs" -Raw

        $checks = @(
            @{ Pattern = "AddConsole\(\)"; Name = "AddConsole()" },
            @{ Pattern = "AddDebug\(\)"; Name = "AddDebug()" },
            @{ Pattern = "AddSimpleConsole\("; Name = "AddSimpleConsole()" },
            @{ Pattern = "DEBUG-TEST"; Name = "Test Logs" },
            @{ Pattern = "LogInstanceCount"; Name = "Lifecycle Logging" }
        )

        foreach ($check in $checks) {
            if ($programContent -match $check.Pattern) {
                Write-Host "✅ $($check.Name) found" -ForegroundColor Green
            }
            else {
                Write-Host "❌ $($check.Name) not found" -ForegroundColor Red
            }
        }
    }
}

# Test 5: Output Location Guide
Write-Host "📍 VS Code Insiders Output Location Guide:" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "To view logs in VS Code Insiders:" -ForegroundColor White
Write-Host "1. 🔍 Debug Console:" -ForegroundColor Yellow
Write-Host "   • Press Ctrl+Shift+D (Debug View)"
Write-Host "   • Start debugging with F5"
Write-Host "   • View 'Debug Console' tab"
Write-Host "   • Look for: [DEBUG-TEST], [LIFECYCLE], System.ObjectDisposedException"
Write-Host ""
Write-Host "2. 📺 Terminal:" -ForegroundColor Yellow
Write-Host "   • Press Ctrl+` (Terminal View)"
Write-Host "   • Console.WriteLine output appears here"
Write-Host "   • Look for: [CONSOLE], timestamped logs"
Write-Host ""
Write-Host "3. 📋 Output Panel:" -ForegroundColor Yellow
Write-Host "   • Press Ctrl+Shift+U (Output View)"
Write-Host "   • Select dropdown: 'Log (Main)', '.NET Core', or 'C#'"
Write-Host "   • Look for: Build output, runtime logs"
Write-Host ""

if ($FullDiagnostics) {
    Write-Host "4. 🔧 Troubleshooting VS Code Issues:" -ForegroundColor Yellow
    Write-Host "   • Close VS Code Insiders completely"
    Write-Host "   • Delete .vscode/settings.json temporarily"
    Write-Host "   • Restart VS Code Insiders"
    Write-Host "   • Try: 'Developer: Reset Debug Console' from Command Palette"
    Write-Host "   • Check: View > Output > 'Extension Host' for extension errors"
    Write-Host ""
}

Write-Host "🎯 Next Steps:" -ForegroundColor Magenta
Write-Host "1. Start debugging in VS Code with F5"
Write-Host "2. Check Debug Console for test logs"
Write-Host "3. Check Terminal for console output"
Write-Host "4. Close Dashboard form to trigger disposal logs"
Write-Host "5. Look for System.ObjectDisposedException patterns"
Write-Host ""

# Cleanup
if (Test-Path "quick-test-output.log") { Remove-Item "quick-test-output.log" -ErrorAction SilentlyContinue }
if (Test-Path "quick-test-error.log") { Remove-Item "quick-test-error.log" -ErrorAction SilentlyContinue }

Write-Host "Verification script completed! 🎉" -ForegroundColor Green
