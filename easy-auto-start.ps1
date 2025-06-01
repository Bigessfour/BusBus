# SIMPLE GitHub Actions Runner - Auto-Start Setup
# This makes your runner start automatically when Windows boots up
# Just run this ONCE as Administrator and forget about it!

Write-Host @"
GITHUB ACTIONS RUNNER AUTO-START SETUP
=======================================
This will make your runner start automatically when Windows boots.
You'll never have to remember to start it again!

"@ -ForegroundColor Cyan

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host @"
ERROR: NEED ADMIN RIGHTS!

To run as Administrator:
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
    Write-Host "ERROR: Runner not found at $exePath" -ForegroundColor Red
    Write-Host "Make sure the runner is installed first!" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "SUCCESS: Found runner at $exePath" -ForegroundColor Green

# Stop and remove any existing service
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "UPDATING: Updating existing service..." -ForegroundColor Yellow
    try {
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        sc.exe delete $serviceName | Out-Null
        Start-Sleep -Seconds 2
        Write-Host "SUCCESS: Old service removed" -ForegroundColor Green
    }
    catch {
        Write-Host "WARNING: Could not remove old service, continuing..." -ForegroundColor Yellow
    }
}

# Create the new service
Write-Host "INSTALLING: Creating Windows service..." -ForegroundColor Green

$binPath = "`"$exePath`" --startuptype automatic run"
$createResult = sc.exe create $serviceName binpath= $binPath start= auto DisplayName= "GitHub Actions Runner (BusBus)"

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: Service created!" -ForegroundColor Green

    # Start the service
    Write-Host "STARTING: Starting service..." -ForegroundColor Green
    try {
        Start-Service -Name $serviceName -ErrorAction Stop
        Start-Sleep -Seconds 5

        # Verify it's running
        $service = Get-Service -Name $serviceName
        if ($service.Status -eq 'Running') {
            Write-Host @"

SUCCESS! SUCCESS! SUCCESS!

Your GitHub Actions Runner is now:
[X] Installed as a Windows Service
[X] Set to AUTO-START when Windows boots
[X] Running in the background right now
[X] Ready to handle your GitHub workflows

YOU CAN FORGET ABOUT IT NOW!
It will automatically start every time you restart your computer.

Service name: $serviceName
Check status anytime: Get-Service $serviceName

"@ -ForegroundColor Green
        }
        else {
            Write-Host @"
WARNING: Service created but status is: $($service.Status)
Try: Start-Service $serviceName
Or check Services.msc for details.
"@ -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host @"
WARNING: Service created but failed to start automatically.
You can start it manually with: Start-Service $serviceName
Or check Services.msc for details.
"@ -ForegroundColor Yellow
    }
}
else {
    Write-Host "ERROR: Failed to create service (Error: $LASTEXITCODE)" -ForegroundColor Red
    Write-Host "You might need to run PowerShell as Administrator or check system permissions." -ForegroundColor Yellow
}

Write-Host "`nPress Enter to exit..." -ForegroundColor Gray
Read-Host
