# verify-busbus-logging.ps1
# Quick verification script for BusBus Microsoft.Extensions.Logging output
# Checks all output locations: Debug Console, Terminal, Output Panel, and log file

param(
    [switch]$Detailed,
    [int]$TimeoutSeconds = 30
)

$projectPath = "c:\Users\steve.mckitrick\Desktop\BusBus"
$logFile = "debug-session.log"
$buildLog = "build-verification.log"

Write-Host "🔍 BusBus Logging Verification Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Set environment variables
$env:DOTNET_ENVIRONMENT = "Development"
$env:BUSBUS_TEST_LIFECYCLE = "true"

Write-Host "📁 Project Path: $projectPath" -ForegroundColor Yellow
Write-Host "🌍 Environment: $env:DOTNET_ENVIRONMENT" -ForegroundColor Yellow
Write-Host "🔄 Lifecycle Testing: $env:BUSBUS_TEST_LIFECYCLE" -ForegroundColor Yellow
Write-Host ""

# Navigate to project directory
if (Test-Path $projectPath) {
    Set-Location $projectPath
    Write-Host "✅ Changed to project directory" -ForegroundColor Green
}
else {
    Write-Host "❌ Project path not found: $projectPath" -ForegroundColor Red
    exit 1
}

# Build verification
Write-Host "🔨 Building BusBus solution..." -ForegroundColor Blue
dotnet build BusBus.sln 2>&1 | Tee-Object -FilePath $buildLog | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful" -ForegroundColor Green
}
else {
    Write-Host "❌ Build failed - check $buildLog" -ForegroundColor Red
    Write-Host "Build output:" -ForegroundColor Yellow
    Get-Content $buildLog | Select-Object -Last 10
    exit 1
}

# Clean up old log file
if (Test-Path $logFile) {
    Remove-Item $logFile -Force
    Write-Host "🧹 Cleaned previous log file" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🚀 Starting debug session with logging capture..." -ForegroundColor Blue
Write-Host "   (Will run for $TimeoutSeconds seconds or until manually stopped)" -ForegroundColor Gray

# Start the application with timeout
$job = Start-Job -ScriptBlock {
    param($path, $logFile)
    Set-Location $path
    $env:DOTNET_ENVIRONMENT = "Development"
    $env:BUSBUS_TEST_LIFECYCLE = "true"
    dotnet run 2>&1 | Tee-Object -FilePath $logFile
} -ArgumentList $projectPath, $logFile

# Wait for specified timeout or job completion
if (Wait-Job $job -Timeout $TimeoutSeconds) {
    Write-Host "✅ Application completed within timeout" -ForegroundColor Green
}
else {
    Write-Host "⏰ Timeout reached ($TimeoutSeconds seconds) - stopping application" -ForegroundColor Yellow
    Stop-Job $job
}

Remove-Job $job -Force

Write-Host ""
Write-Host "📊 Verification Results:" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan

# Check if log file was created and contains expected content
if (Test-Path $logFile) {
    $logContent = Get-Content $logFile -Raw
    Write-Host "✅ Log file created: $logFile" -ForegroundColor Green

    # Check for key log markers
    $debugTestFound = $logContent -match "\[DEBUG-TEST\]"
    $lifecycleFound = $logContent -match "\[LIFECYCLE\]"
    $exceptionFound = $logContent -match "System\.ObjectDisposedException"
    $timestampFound = $logContent -match "\d{2}:\d{2}:\d{2}\.\d{3}"

    Write-Host ""
    Write-Host "🔍 Log Content Analysis:" -ForegroundColor Blue
    Write-Host "  [DEBUG-TEST] markers: $(if($debugTestFound){'✅ Found'}else{'❌ Missing'})" -ForegroundColor $(if ($debugTestFound) { 'Green' }else { 'Red' })
    Write-Host "  [LIFECYCLE] markers: $(if($lifecycleFound){'✅ Found'}else{'❌ Missing'})" -ForegroundColor $(if ($lifecycleFound) { 'Green' }else { 'Red' })
    Write-Host "  Timestamp format: $(if($timestampFound){'✅ Correct'}else{'❌ Missing'})" -ForegroundColor $(if ($timestampFound) { 'Green' }else { 'Red' })
    Write-Host "  ObjectDisposedException: $(if($exceptionFound){'⚠️ Found (needs debugging)'}else{'✅ Not found'})" -ForegroundColor $(if ($exceptionFound) { 'Yellow' }else { 'Green' })

    if ($Detailed) {
        Write-Host ""
        Write-Host "📄 Recent log entries:" -ForegroundColor Blue
        Get-Content $logFile | Select-Object -Last 10 | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Gray
        }
    }
}
else {
    Write-Host "❌ Log file not created - application may not have started" -ForegroundColor Red
}

Write-Host ""
Write-Host "📍 Output Location Guide:" -ForegroundColor Cyan
Write-Host "==========================" -ForegroundColor Cyan
Write-Host "🎯 Primary: Debug Console" -ForegroundColor Blue
Write-Host "   Access: View > Debug Console (Ctrl+Shift+D)" -ForegroundColor Gray
Write-Host "   Method: Press F5 in VS Code Insiders" -ForegroundColor Gray
Write-Host "   Look for: Lifecycle logs, exceptions, test markers" -ForegroundColor Gray

Write-Host ""
Write-Host "🔄 Secondary: Terminal" -ForegroundColor Blue
Write-Host "   Access: View > Terminal (Ctrl+`)" -ForegroundColor Gray
Write-Host "   Expected: Timestamped console logs (HH:mm:ss.fff format)" -ForegroundColor Gray

Write-Host ""
Write-Host "📋 Tertiary: Output Panel" -ForegroundColor Blue
Write-Host "   Access: View > Output (Ctrl+Shift+U)" -ForegroundColor Gray
Write-Host "   Dropdown: Select '.NET Core' or 'Log (Main)'" -ForegroundColor Gray
Write-Host "   Search: 'DEBUG-TEST' or 'System.ObjectDisposedException'" -ForegroundColor Gray

Write-Host ""
Write-Host "📄 File Logging" -ForegroundColor Blue
Write-Host "   Terminal Log: $logFile (this verification)" -ForegroundColor Gray
Write-Host "   App Log: busbus.log (if configured in appsettings.json)" -ForegroundColor Gray

Write-Host ""
Write-Host "🛠️ Next Steps:" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan
if ($debugTestFound -and $lifecycleFound) {
    Write-Host "✅ Logging is working correctly!" -ForegroundColor Green
    Write-Host "   • Debug VS Code Insiders with F5" -ForegroundColor Gray
    Write-Host "   • Monitor Debug Console (Ctrl+Shift+D)" -ForegroundColor Gray
    if ($exceptionFound) {
        Write-Host "   • Review System.ObjectDisposedException in logs" -ForegroundColor Yellow
        Write-Host "   • Use debugging scenario in LOGGING_MAINTENANCE_GUIDE.md" -ForegroundColor Yellow
    }
}
else {
    Write-Host "⚠️ Logging needs attention:" -ForegroundColor Yellow
    if (-not $debugTestFound) {
        Write-Host "   • Check Program.cs for AddDebug() and AddSimpleConsole()" -ForegroundColor Red
        Write-Host "   • Verify LogLevel.Debug minimum level" -ForegroundColor Red
    }
    if (-not $lifecycleFound) {
        Write-Host "   • Check TestLifecycleLogging.cs is included" -ForegroundColor Red
        Write-Host "   • Verify $env:BUSBUS_TEST_LIFECYCLE environment variable" -ForegroundColor Red
    }
    Write-Host "   • Run setup-debug-logging.ps1 if available" -ForegroundColor Gray
    Write-Host "   • See LOGGING_MAINTENANCE_GUIDE.md for troubleshooting" -ForegroundColor Gray
}

Write-Host ""
Write-Host "📚 For detailed debugging scenarios, see:" -ForegroundColor Blue
Write-Host "   LOGGING_MAINTENANCE_GUIDE.md" -ForegroundColor Gray

# Return appropriate exit code
if ($debugTestFound -and $lifecycleFound) {
    exit 0
}
else {
    exit 1
}
