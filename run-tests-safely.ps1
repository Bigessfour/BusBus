# Run Tests Safely via GitHub Self-Hosted Runner
# This triggers the tests on the runner instead of locally to prevent infinite loops

param(
    [string]$TestFilter = "",
    [int]$TimeoutMinutes = 10,
    [switch]$ForceLocal = $false
)

Write-Host "
╔══════════════════════════════════════════════════════════════╗
║  BusBus - SAFE TEST EXECUTION                                 ║
║  ⚠️  DO NOT RUN TESTS DIRECTLY WITH dotnet test  ⚠️          ║
╚══════════════════════════════════════════════════════════════╝
" -ForegroundColor Cyan

# Check if running in GitHub Actions
$inGitHubActions = $env:GITHUB_ACTIONS -eq 'true'

# Only show warning if not in GitHub Actions
if (-not $inGitHubActions) {
    Write-Host "Triggering safe test execution via GitHub Actions self-hosted runner..." -ForegroundColor Green
    Write-Host "This prevents local machine lockup from infinite test loops." -ForegroundColor Yellow
}

# Check if ForceLocal was specified
if ($ForceLocal -and -not $inGitHubActions) {
    Write-Host "⚠️ WARNING: Force local execution requested. This is NOT recommended!" -ForegroundColor Red
    Write-Host "This will bypass safety measures and may cause system hangs requiring hard reboot." -ForegroundColor Red
    $confirm = Read-Host "Type 'I UNDERSTAND THE RISKS' to continue (or anything else to cancel)"

    if ($confirm -ne "I UNDERSTAND THE RISKS") {
        Write-Host "Operation cancelled. Using GitHub runner is recommended for safety." -ForegroundColor Green
        exit 0
    }

    # Run locally with maximum safety settings
    Write-Host "Running tests locally with safety measures..." -ForegroundColor Yellow

    try {
        $testCommand = "dotnet test BusBus.Tests/BusBus.Tests.csproj --settings coverlet.runsettings"
        if ($TestFilter) {
            $testCommand += " --filter `"$TestFilter`""
        }

        Write-Host "Executing: $testCommand" -ForegroundColor Gray

        # Set a timer to kill the process if it runs too long
        $job = Start-Job -ScriptBlock {
            param($cmd)
            Invoke-Expression $cmd
        } -ArgumentList $testCommand

        if (Wait-Job $job -Timeout ($TimeoutMinutes * 60)) {
            Receive-Job $job
        }
        else {
            Write-Host "❌ Tests timed out after $TimeoutMinutes minutes!" -ForegroundColor Red
            Stop-Job $job -PassThru | Remove-Job

            Write-Host "Cleaning up potential hung processes..." -ForegroundColor Yellow
            Get-Process -Name dotnet | Where-Object { $_.MainWindowTitle -like "*test*" } | Stop-Process -Force
        }

        exit 0    
    }
    catch {
        Write-Host "❌ Error executing tests: $_" -ForegroundColor Red
        exit 1
    }
}

# Check if GitHub CLI is installed
$ghCommand = Get-Command gh -ErrorAction SilentlyContinue
if (-not $ghCommand) {
    Write-Host @"
GitHub CLI is not installed. Please either:

1. Install GitHub CLI:
   winget install --id GitHub.cli

2. Or manually trigger the workflow at:
   https://github.com/Bigessfour/BusBus/actions/workflows/safe-tests.yml

3. Set up a local GitHub runner (recommended):
   https://github.com/Bigessfour/BusBus/blob/main/docs/setup-self-hosted-runner.md

"@ -ForegroundColor Red
    exit 1
}

# Check if the workflow file exists by querying GitHub
try {
    $workflowExists = gh workflow view safe-tests.yml --json name -q ".name" 2>$null
    if (-not $workflowExists) {
        Write-Host "❌ Could not find workflow 'safe-tests.yml' in the repository." -ForegroundColor Red
        Write-Host "Please ensure the workflow exists at: .github/workflows/safe-tests.yml" -ForegroundColor Yellow

        # Suggest setup of the workflow file
        Write-Host @"

It appears you need to set up the safe-tests.yml workflow file. Please follow these steps:

1. Create the directory structure:
   mkdir -p .github/workflows

2. Create a new file at .github/workflows/safe-tests.yml with the template from:
   https://github.com/Bigessfour/BusBus/blob/main/.github/workflows/safe-tests.yml.example

3. Ensure you have a self-hosted runner configured:
   https://github.com/Bigessfour/BusBus/blob/main/docs/setup-self-hosted-runner.md

"@ -ForegroundColor Cyan
        exit 1
    }
}
catch {
    Write-Host "❌ Error checking workflow existence: $_" -ForegroundColor Red
    Write-Host "Please ensure you're authenticated with GitHub CLI (run 'gh auth login')" -ForegroundColor Yellow
    exit 1
}

# Trigger the workflow
Write-Host "Triggering workflow with parameters:" -ForegroundColor Cyan
Write-Host "  Test Filter: '$TestFilter'" -ForegroundColor Gray
Write-Host "  Timeout: $TimeoutMinutes minutes" -ForegroundColor Gray

try {
    if ($TestFilter) {
        gh workflow run safe-tests.yml --field test_filter="$TestFilter" --field timeout_minutes="$TimeoutMinutes"
    }
    else {
        gh workflow run safe-tests.yml --field timeout_minutes="$TimeoutMinutes"
    }

    Write-Host "✅ Workflow triggered successfully!" -ForegroundColor Green
    Write-Host "View progress at: https://github.com/Bigessfour/BusBus/actions" -ForegroundColor Cyan

    # Wait a moment then show recent runs
    Start-Sleep -Seconds 2
    Write-Host "`nRecent workflow runs:" -ForegroundColor Yellow
    gh run list --workflow=safe-tests.yml --limit=3
}
catch {
    Write-Error "Failed to trigger workflow: $_"
    Write-Host "You can manually trigger it at: https://github.com/Bigessfour/BusBus/actions/workflows/safe-tests.yml" -ForegroundColor Yellow
}
