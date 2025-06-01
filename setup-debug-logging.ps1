# Enable Enhanced Lifecycle Logging for VS Code Debugging
# Run this before pressing F5 in VS Code Insiders

Write-Host "🔧 Setting up enhanced logging environment..." -ForegroundColor Green

# Enable test lifecycle logging
$env:BUSBUS_TEST_LIFECYCLE = "true"
Write-Host "✅ BUSBUS_TEST_LIFECYCLE = true" -ForegroundColor Yellow

# Ensure Development environment for Debug level logging
$env:DOTNET_ENVIRONMENT = "Development"
Write-Host "✅ DOTNET_ENVIRONMENT = Development" -ForegroundColor Yellow

# Optional: Enable verbose console logging
$env:LOGGING__LOGLEVEL__DEFAULT = "Debug"
Write-Host "✅ LOGGING__LOGLEVEL__DEFAULT = Debug" -ForegroundColor Yellow

Write-Host ""
Write-Host "🚀 Ready for debugging! Press F5 in VS Code Insiders" -ForegroundColor Green
Write-Host "📺 Check Terminal and Debug Console for colorful test logs" -ForegroundColor Cyan
Write-Host ""
Write-Host "Expected logs:" -ForegroundColor White
Write-Host "  🔍 [DEBUG-TEST] BusBus Application Starting" -ForegroundColor Blue
Write-Host "  ⚠️ [DEBUG-TEST] This is a WARNING level test log" -ForegroundColor Yellow
Write-Host "  ❌ [DEBUG-TEST] This is an ERROR level test log" -ForegroundColor Red
Write-Host "  [LIFECYCLE] Application Startup - Active .NET instances" -ForegroundColor Green
