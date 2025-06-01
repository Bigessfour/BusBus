# Alternative Service Installation for GitHub Actions Runner
# Run this as Administrator

Write-Host "Installing GitHub Actions Runner as Windows Service (Alternative Method)" -ForegroundColor Green

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Please run PowerShell as Administrator and try again."
    exit 1
}

$runnerPath = "C:\actions-runner"
$serviceName = "GitHubActionsRunner"
$serviceDisplayName = "GitHub Actions Self-Hosted Runner"
$runnerExe = Join-Path $runnerPath "bin\Runner.Listener.exe"

Write-Host "Checking runner executable..." -ForegroundColor Yellow
if (-not (Test-Path $runnerExe)) {
    Write-Error "Runner executable not found at: $runnerExe"
    Write-Host "Make sure the runner is properly installed at C:\actions-runner"
    exit 1
}

# Stop existing service if it exists
Write-Host "Checking for existing service..." -ForegroundColor Yellow
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    Write-Host "Removing existing service..." -ForegroundColor Yellow
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

# Method 1: Try using New-Service cmdlet
Write-Host "Attempting to create service using New-Service..." -ForegroundColor Yellow
try {
    New-Service -Name $serviceName -BinaryPathName "`"$runnerExe`" --startuptype Automatic" -DisplayName $serviceDisplayName -StartupType Automatic -ErrorAction Stop
    Write-Host "Service created successfully using New-Service!" -ForegroundColor Green

    # Start the service
    Start-Service -Name $serviceName
    $service = Get-Service -Name $serviceName
    Write-Host "Service Status: $($service.Status)" -ForegroundColor Green

}
catch {
    Write-Host "New-Service failed, trying sc.exe method..." -ForegroundColor Yellow

    # Method 2: Use sc.exe command
    $scCommand = "sc.exe create `"$serviceName`" binPath= `"`"$runnerExe`" --startuptype Automatic`" DisplayName= `"$serviceDisplayName`" start= auto"
    Write-Host "Running: $scCommand" -ForegroundColor Gray

    Invoke-Expression $scCommand

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service created successfully using sc.exe!" -ForegroundColor Green

        # Start the service
        Start-Service -Name $serviceName
        $service = Get-Service -Name $serviceName
        Write-Host "Service Status: $($service.Status)" -ForegroundColor Green

    }
    else {
        Write-Host "Both methods failed. Let's try the manual approach..." -ForegroundColor Red
        Write-Host ""
        Write-Host "MANUAL STEPS:" -ForegroundColor Yellow
        Write-Host "1. Open Command Prompt as Administrator"
        Write-Host "2. Run: sc create GitHubActionsRunner binPath= `"C:\actions-runner\bin\Runner.Listener.exe --startuptype Automatic`" start= auto"
        Write-Host "3. Run: net start GitHubActionsRunner"
        Write-Host ""
        Write-Host "OR simply run the runner manually:" -ForegroundColor Yellow
        Write-Host "1. Open PowerShell"
        Write-Host "2. cd C:\actions-runner"
        Write-Host "3. .\run.cmd"
        exit 1
    }
}

Write-Host @"

✅ GitHub Actions Runner Service Installation Complete!

Service Details:
- Name: $serviceName
- Display Name: $serviceDisplayName
- Executable: $runnerExe
- Status: Running

To manage the service:
- Check status: Get-Service -Name '$serviceName'
- Stop: Stop-Service -Name '$serviceName'
- Start: Start-Service -Name '$serviceName'
- Remove: sc.exe delete '$serviceName'

The runner will now automatically start when Windows boots and run your GitHub Actions workflows!

Next steps:
1. Test by pushing code to your repository
2. Check workflow runs at: https://github.com/Bigessfour/BusBus/actions
3. Your workflows will now run on this computer instead of GitHub's servers

"@ -ForegroundColor Green
