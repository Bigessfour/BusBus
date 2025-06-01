# GitHub Self-Hosted Runner Troubleshooting Script
# This script helps you get the correct URL and token for your runner setup

Write-Host "GitHub Self-Hosted Runner Setup Troubleshooting" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan

Write-Host "`n🔍 Step 1: Verify Repository Information" -ForegroundColor Yellow
Write-Host "Please provide your GitHub repository details:"
Write-Host ""

# Get repository information
$owner = Read-Host "Enter your GitHub username/organization"
$repo = Read-Host "Enter your repository name (e.g., 'BusBus')"

$repoUrl = "https://github.com/$owner/$repo"
Write-Host "`n✅ Repository URL should be: $repoUrl" -ForegroundColor Green

Write-Host "`n🔑 Step 2: Get Fresh Registration Token" -ForegroundColor Yellow
Write-Host "Follow these steps to get a new token:"
Write-Host ""
Write-Host "1. Go to: $repoUrl/settings/actions/runners" -ForegroundColor White
Write-Host "2. Click 'New self-hosted runner'" -ForegroundColor White
Write-Host "3. Select 'Windows' and 'x64'" -ForegroundColor White
Write-Host "4. Copy the token from the 'Configure' section" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  Note: The token expires in 1 hour!" -ForegroundColor Red

Write-Host "`n📋 Step 3: Manual Setup Commands" -ForegroundColor Yellow
Write-Host "If the automated script doesn't work, use these manual commands:"
Write-Host ""
Write-Host "# Navigate to actions-runner directory" -ForegroundColor Gray
Write-Host "cd C:\actions-runner" -ForegroundColor White
Write-Host ""
Write-Host "# Configure with your specific details" -ForegroundColor Gray
Write-Host ".\config.cmd --url $repoUrl --token YOUR_FRESH_TOKEN_HERE" -ForegroundColor White
Write-Host ""
Write-Host "# Install as service" -ForegroundColor Gray
Write-Host ".\svc.cmd install" -ForegroundColor White
Write-Host ".\svc.cmd start" -ForegroundColor White

Write-Host "`n🛠️  Step 4: Alternative Setup Method" -ForegroundColor Yellow
Write-Host "You can also run the config interactively:"
Write-Host ""
Write-Host ".\config.cmd" -ForegroundColor White
Write-Host ""
Write-Host "This will prompt you for:"
Write-Host "- Server URL: $repoUrl"
Write-Host "- Token: [paste the fresh token]"
Write-Host "- Runner name: [press Enter for default]"
Write-Host "- Work folder: [press Enter for default]"
Write-Host "- Labels: [press Enter for default]"

Write-Host "`n🔍 Step 5: Verify Permissions" -ForegroundColor Yellow
Write-Host "Make sure you have:"
Write-Host "- Repository admin access OR organization owner access" -ForegroundColor White
Write-Host "- Actions enabled for the repository" -ForegroundColor White
Write-Host "- Self-hosted runners allowed (if organization)" -ForegroundColor White

Write-Host "`n📱 Need Help?" -ForegroundColor Cyan
Write-Host "- Check repository settings: $repoUrl/settings/actions"
Write-Host "- View runner documentation: https://docs.github.com/en/actions/hosting-your-own-runners"
Write-Host "- Check if repository is private (recommended for self-hosted runners)"

Write-Host "`n🎯 Quick Test" -ForegroundColor Yellow
$testToken = Read-Host "Enter your fresh token to test the URL format (optional)"
if ($testToken) {
    Write-Host "`nTesting configuration..." -ForegroundColor Yellow
    Write-Host "Command that will be run:" -ForegroundColor Gray
    Write-Host ".\config.cmd --url $repoUrl --token $testToken --name $env:COMPUTERNAME --replace" -ForegroundColor White
    Write-Host ""
    $confirm = Read-Host "Run this command now? (y/n)"
    if ($confirm -eq 'y' -or $confirm -eq 'Y') {
        if (Test-Path ".\config.cmd") {
            .\config.cmd --url $repoUrl --token $testToken --name $env:COMPUTERNAME --replace
        }
        else {
            Write-Host "❌ config.cmd not found. Make sure you're in the C:\actions-runner directory" -ForegroundColor Red
        }
    }
}
