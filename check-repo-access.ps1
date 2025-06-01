# Check GitHub Repository Access and Setup Runner
# This script helps verify access and provides manual setup instructions

Write-Host "GitHub Repository Access Checker" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

Write-Host "`n🔍 Repository Information:" -ForegroundColor Yellow
Write-Host "Repository: Bigessfour/BusBus"
Write-Host "URL: https://github.com/Bigessfour/BusBus"

Write-Host "`n📋 Prerequisites Check:" -ForegroundColor Yellow
Write-Host "✅ 1. Are you signed in to GitHub?"
Write-Host "✅ 2. Do you have admin access to the Bigessfour/BusBus repository?"
Write-Host "✅ 3. Is the repository private or public?"

Write-Host "`n🔑 Manual Token Retrieval:" -ForegroundColor Yellow
Write-Host "Since the automated page access failed, follow these steps:"
Write-Host ""
Write-Host "1. Open your browser and sign in to GitHub"
Write-Host "2. Go to: https://github.com/Bigessfour/BusBus"
Write-Host "3. Click the 'Settings' tab (need admin access)"
Write-Host "4. In left sidebar: Actions → Runners"
Write-Host "5. Click 'New self-hosted runner'"
Write-Host "6. Select 'Windows' and 'x64'"
Write-Host "7. Copy the token from the configuration section"

Write-Host "`n⚡ Quick Setup Commands:" -ForegroundColor Yellow
Write-Host "Once you have the token, run these commands in C:\actions-runner:"
Write-Host ""
Write-Host "# Download (if not done already)" -ForegroundColor Gray
Write-Host 'mkdir C:\actions-runner ; cd C:\actions-runner' -ForegroundColor White
Write-Host 'Invoke-WebRequest -Uri "https://github.com/actions/runner/releases/download/v2.311.0/actions-runner-win-x64-2.311.0.zip" -OutFile "actions-runner-win-x64-2.311.0.zip"' -ForegroundColor White
Write-Host 'Add-Type -AssemblyName System.IO.Compression.FileSystem ; [System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-x64-2.311.0.zip", "$PWD")' -ForegroundColor White
Write-Host ""
Write-Host "# Configure (replace YOUR_TOKEN)" -ForegroundColor Gray
Write-Host '.\config.cmd --url https://github.com/Bigessfour/BusBus --token YOUR_TOKEN --name "BusBus-Runner"' -ForegroundColor White
Write-Host ""
Write-Host "# Install as service" -ForegroundColor Gray
Write-Host '.\svc.cmd install' -ForegroundColor White
Write-Host '.\svc.cmd start' -ForegroundColor White

Write-Host "`n🛠️ Troubleshooting:" -ForegroundColor Yellow
Write-Host "If you can't access the settings page:"
Write-Host "• Contact the repository owner (Bigessfour) to:"
Write-Host "  - Add you as a collaborator with admin access"
Write-Host "  - Set up the runner for you"
Write-Host "  - Provide the registration token"

Write-Host "`n📞 Need Help?" -ForegroundColor Cyan
Write-Host "• Repository owner: Bigessfour"
Write-Host "• Repository URL: https://github.com/Bigessfour/BusBus"
Write-Host "• GitHub Docs: https://docs.github.com/en/actions/hosting-your-own-runners"

$choice = Read-Host "`nDo you have admin access to the repository? (y/n)"
if ($choice -eq 'y' -or $choice -eq 'Y') {
    $token = Read-Host "`nPaste your registration token here"
    if ($token) {
        Write-Host "`nAttempting to configure runner..." -ForegroundColor Green
        Set-Location "C:\actions-runner"
        .\config.cmd --url https://github.com/Bigessfour/BusBus --token $token --name "BusBus-Runner" --work "_work"
    }
} else {
    Write-Host "`n❌ You need admin access to set up self-hosted runners." -ForegroundColor Red
    Write-Host "Contact the repository owner (Bigessfour) for access."
}
