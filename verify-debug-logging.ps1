# Enhanced Debug Logging Verification Script for BusBus
# This script helps verify Microsoft.Extensions.Logging output in VS Code Insiders

Write-Host "🔍 BusBus Debug Logging Verification Script" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Set environment variables
$env:DOTNET_ENVIRONMENT = "Development"
$env:BUSBUS_TEST_LIFECYCLE = "true"
$env:LOGGING__LOGLEVEL__DEFAULT = "Debug"

Write-Host "✅ Environment variables set:" -ForegroundColor Green
Write-Host "   DOTNET_ENVIRONMENT = $env:DOTNET_ENVIRONMENT" -ForegroundColor Yellow
Write-Host "   BUSBUS_TEST_LIFECYCLE = $env:BUSBUS_TEST_LIFECYCLE" -ForegroundColor Yellow
Write-Host "   LOGGING__LOGLEVEL__DEFAULT = $env:LOGGING__LOGLEVEL__DEFAULT" -ForegroundColor Yellow
Write-Host ""

Write-Host "📋 OUTPUT LOCATION GUIDE:" -ForegroundColor Magenta
Write-Host "  1. Terminal (THIS WINDOW) - Console.WriteLine, AddConsole logs" -ForegroundColor White
Write-Host "  2. Debug Console - AddDebug logs, System.Diagnostics.Debug" -ForegroundColor White
Write-Host "  3. Output Panel - Trace, some .NET Core logs" -ForegroundColor White
Write-Host ""

Write-Host "🚀 Starting BusBus..." -ForegroundColor Green
Write-Host "Watch for these test logs in VS Code Insiders:" -ForegroundColor Yellow
Write-Host "  🔍 [DEBUG-TEST] - Should appear in Terminal AND Debug Console" -ForegroundColor Cyan
Write-Host "  📺 [CONSOLE] - Should appear in Terminal" -ForegroundColor Cyan
Write-Host "  🐛 [DEBUG] - Should appear in Debug Console" -ForegroundColor Cyan
Write-Host "  🧪 [TEST-CATEGORY] - Should appear in both locations" -ForegroundColor Cyan
Write-Host ""

# Build first to ensure we have latest changes
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build BusBus.sln --configuration Debug

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful" -ForegroundColor Green
    Write-Host ""

    Write-Host "🎯 NEXT STEPS FOR VS CODE INSIDERS VERIFICATION:" -ForegroundColor Magenta
    Write-Host "1. Open VS Code Insiders in this directory" -ForegroundColor White
    Write-Host "2. Press F5 to start debugging (or use 'Debug BusBus App' configuration)" -ForegroundColor White
    Write-Host "3. Check these locations for test logs:" -ForegroundColor White
    Write-Host "   • Terminal (Ctrl+`) - Look for [CONSOLE] and [DEBUG-TEST] logs" -ForegroundColor Cyan
    Write-Host "   • Debug Console (Ctrl+Shift+D, then Debug Console tab) - Look for [DEBUG] logs" -ForegroundColor Cyan
    Write-Host "   • Output Panel (Ctrl+Shift+U, select 'Debug Console' dropdown) - Look for additional logs" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "🔧 If logs are missing:" -ForegroundColor Red
    Write-Host "   • Verify launch.json has 'console': 'integratedTerminal'" -ForegroundColor White
    Write-Host "   • Check if AddDebug() is working in ConfigureServices" -ForegroundColor White
    Write-Host "   • Look in Output Panel > '.NET Core' or 'Log (Main)' channels" -ForegroundColor White
    Write-Host ""

    # Start the application briefly to show terminal output
    Write-Host "Starting BusBus briefly to demonstrate Terminal output..." -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor DarkGray

    # Start and quickly stop to show terminal logs
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", "Debug" -NoNewWindow -PassThru
    Start-Sleep -Seconds 3

    if (!$process.HasExited) {
        Write-Host "⏹️  Stopping application after brief run..." -ForegroundColor Yellow
        $process.Kill()
        $process.WaitForExit()
    }

    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "✅ Terminal output verification complete!" -ForegroundColor Green
    Write-Host "Now use F5 in VS Code Insiders to verify Debug Console output." -ForegroundColor Yellow

}
else {
    Write-Host "❌ Build failed. Please fix build errors first." -ForegroundColor Red
}

Write-Host ""
Write-Host "📖 For detailed troubleshooting, see:" -ForegroundColor Cyan
Write-Host "   • DEBUG_CONSOLE_VERIFICATION.md" -ForegroundColor White
Write-Host "   • LIFECYCLE_LOGGING_IMPLEMENTATION.md" -ForegroundColor White
