# Setup Self-Hosted GitHub Actions Runner
# Run this script as Administrator

param(
    [Parameter(Mandatory = $true)]
    [string]$GitHubUrl,

    [Parameter(Mandatory = $true)]
    [string]$Token,

    [string]$RunnerName = $env:COMPUTERNAME,
    [string]$WorkFolder = "C:\actions-runner"
)

Write-Host "Setting up GitHub Actions Self-Hosted Runner..." -ForegroundColor Green

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Please run PowerShell as Administrator and try again."
    exit 1
}

# Create the actions-runner directory
Write-Host "Creating runner directory: $WorkFolder" -ForegroundColor Yellow
if (-not (Test-Path $WorkFolder)) {
    New-Item -ItemType Directory -Path $WorkFolder -Force
}
Set-Location $WorkFolder

# Download the latest runner package
Write-Host "Downloading GitHub Actions Runner..." -ForegroundColor Yellow
$runnerVersion = "2.311.0"  # Update this to the latest version
$downloadUrl = "https://github.com/actions/runner/releases/download/v$runnerVersion/actions-runner-win-x64-$runnerVersion.zip"
$zipFile = "actions-runner-win-x64-$runnerVersion.zip"

try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $zipFile
    Write-Host "Downloaded runner package successfully." -ForegroundColor Green
}
catch {
    Write-Error "Failed to download runner package: $_"
    exit 1
}

# Extract the installer
Write-Host "Extracting runner package..." -ForegroundColor Yellow
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\$zipFile", "$PWD")
    Write-Host "Extracted runner package successfully." -ForegroundColor Green
}
catch {
    Write-Error "Failed to extract runner package: $_"
    exit 1
}

# Configure the runner
Write-Host "Configuring the runner..." -ForegroundColor Yellow
try {
    .\config.cmd --url $GitHubUrl --token $Token --name $RunnerName --work "_work" --replace
    Write-Host "Runner configured successfully." -ForegroundColor Green
}
catch {
    Write-Error "Failed to configure runner: $_"
    exit 1
}

# Install and start the service
Write-Host "Installing runner as Windows service..." -ForegroundColor Yellow
try {
    .\svc.cmd install
    .\svc.cmd start
    Write-Host "Runner service installed and started successfully." -ForegroundColor Green
}
catch {
    Write-Error "Failed to install/start runner service: $_"
    exit 1
}

Write-Host @"

✅ Self-hosted runner setup complete!

The runner is now:
- Installed at: $WorkFolder
- Running as a Windows service
- Connected to: $GitHubUrl
- Runner name: $RunnerName

To check the runner status:
- Service status: Get-Service -Name "actions.runner.*"
- Runner logs: Get-Content "$WorkFolder\_diag\*.log" -Tail 20

To manage the service:
- Stop: .\svc.cmd stop
- Start: .\svc.cmd start
- Uninstall: .\svc.cmd uninstall

Your workflows can now use 'runs-on: self-hosted' to run on this machine.
"@ -ForegroundColor Green
