# GitHub Actions Runner - Auto-Start Service Installer
# This makes the runner start automatically when Windows boots
# Run this as Administrator

Write-Host "Setting up GitHub Actions Runner to auto-start..." -ForegroundColor Green

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host @"
❌ You need to run this as Administrator!

Steps:
1. Right-click PowerShell
2. Select 'Run as Administrator'
3. Run this script again

"@ -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

$serviceName = "GitHubActionsRunner"
$runnerPath = "C:\actions-runner"
$exePath = "$runnerPath\bin\Runner.Listener.exe"

# Verify runner exists
if (-not (Test-Path $exePath)) {
    Write-Error "❌ Runner not found at $exePath"
    exit 1
}

Write-Host "✅ Runner found at $exePath" -ForegroundColor Green
Write-Host "Service '$ServiceName' already exists. Stopping and removing..." -ForegroundColor Yellow
Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
sc.exe delete $ServiceName
Start-Sleep -Seconds 2
}

# Create the service
$runnerExe = Join-Path $RunnerPath "bin\Runner.Listener.exe"
if (-not (Test-Path $runnerExe)) {
    Write-Error "Runner executable not found at: $runnerExe"
    exit 1
}

Write-Host "Creating Windows service..." -ForegroundColor Yellow
$command = "sc.exe create `"$ServiceName`" binPath= `"$runnerExe --startuptype Automatic`" DisplayName= `"$ServiceDisplayName`" start= auto"
Invoke-Expression $command

if ($LASTEXITCODE -eq 0) {
    Write-Host "Service created successfully!" -ForegroundColor Green

    # Start the service
    Write-Host "Starting the service..." -ForegroundColor Yellow
    Start-Service -Name $ServiceName

    # Check service status
    $service = Get-Service -Name $ServiceName
    Write-Host "Service Status: $($service.Status)" -ForegroundColor Green

    Write-Host @"

✅ GitHub Actions Runner installed as Windows Service!

Service Details:
- Name: $ServiceName
- Display Name: $ServiceDisplayName
- Status: $($service.Status)
- Startup Type: Automatic

To manage the service:
- Start: Start-Service -Name '$ServiceName'
- Stop: Stop-Service -Name '$ServiceName'
- Status: Get-Service -Name '$ServiceName'
- Remove: sc.exe delete '$ServiceName'

The runner will now automatically start when Windows boots.
"@ -ForegroundColor Green

} else {
    Write-Error "Failed to create service. Error code: $LASTEXITCODE"
}
