# Quick Setup for BusBus Repository
# Run this in PowerShell as Administrator in C:\actions-runner

Write-Host "Setting up self-hosted runner for Bigessfour/BusBus repository..." -ForegroundColor Green

# Verify we're in the right directory
if (-not (Test-Path ".\config.cmd")) {
    Write-Error "Please run this script from the C:\actions-runner directory where you extracted the runner files."
    exit 1
}

Write-Host @"
Please follow these steps:

1. Go to: https://github.com/Bigessfour/BusBus/settings/actions/runners
2. Click 'New self-hosted runner'
3. Select Windows x64
4. Copy the token from the configuration section
5. Paste the token below when prompted

"@ -ForegroundColor Yellow

$token = Read-Host "Enter the registration token from GitHub"

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Error "Token cannot be empty. Please get a fresh token from GitHub."
    exit 1
}

Write-Host "Configuring runner with repository: https://github.com/Bigessfour/BusBus" -ForegroundColor Green

try {
    # Configure the runner
    .\config.cmd --url https://github.com/Bigessfour/BusBus --token $token --name "$env:COMPUTERNAME-BusBus" --work "_work" --replace

    Write-Host "Runner configured successfully!" -ForegroundColor Green

    # Install as service
    Write-Host "Installing as Windows service..." -ForegroundColor Yellow
    .\svc.cmd install
    .\svc.cmd start

    Write-Host @"

✅ Success! Your self-hosted runner is now:
- Connected to: https://github.com/Bigessfour/BusBus
- Running as Windows service
- Ready to execute workflows

Next steps:
1. Push code to trigger the workflow
2. Check runner status at: https://github.com/Bigessfour/BusBus/settings/actions/runners
3. View workflow runs at: https://github.com/Bigessfour/BusBus/actions

"@ -ForegroundColor Green

} catch {
    Write-Error "Failed to configure runner: $_"
    Write-Host "Common issues:" -ForegroundColor Yellow
    Write-Host "- Token expired (get a fresh one)" -ForegroundColor White
    Write-Host "- No admin permissions on repository" -ForegroundColor White
    Write-Host "- Network connectivity issues" -ForegroundColor White
}
