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

# Get the current Git branch
try {
    $currentBranch = git rev-parse --abbrev-ref HEAD
    if (-not $currentBranch -or $currentBranch -eq "HEAD") {
        # HEAD means detached state
        Write-Host "⚠️ Could not determine current Git branch or in detached HEAD state. Will try default branch for workflow." -ForegroundColor Yellow
        $currentBranch = "" # Fallback to default branch behavior of gh cli
    }
    else {
        Write-Host "ℹ️  Using current Git branch: $currentBranch for workflow operations." -ForegroundColor Cyan
    }
}
catch {
    Write-Host "⚠️ Could not determine current Git branch via 'git rev-parse'. Will try default branch for workflow. Error: $_" -ForegroundColor Yellow
    $currentBranch = "" # Fallback
}

# Define the local path to the workflow file
$localWorkflowFile = ".github/workflows/safe-tests.yml" # Relative to workspace root

# Check if the workflow file exists by querying GitHub and locally
try {
    Write-Host "Checking for 'safe-tests.yml' workflow on GitHub repository (branch: $($currentBranch | ForEach-Object {if ($_){$_} else {'default'}}))..." -ForegroundColor Gray
    $workflowFoundOnGitHub = $false
    if ($currentBranch) {
        gh workflow view safe-tests.yml --repo Bigessfour/BusBus --ref $currentBranch --yaml 2>$null 1>$null
        if ($LASTEXITCODE -eq 0) {
            $workflowFoundOnGitHub = $true
        }
    }
    else {
        # Check on default branch if no specific branch is determined or if it's 'HEAD'
        gh workflow view safe-tests.yml --repo Bigessfour/BusBus --yaml 2>$null 1>$null
        if ($LASTEXITCODE -eq 0) {
            $workflowFoundOnGitHub = $true
        }
    }

    if (-not $workflowFoundOnGitHub) {
        Write-Host "❌ Workflow 'safe-tests.yml' not found on the GitHub repository (branch: $($currentBranch | ForEach-Object {if ($_){$_} else {'default'}}))." -ForegroundColor Red

        # Check if the file exists locally
        if (Test-Path $localWorkflowFile) {
            Write-Host "ℹ️  The file \'$localWorkflowFile\' exists locally but has not been pushed to the GitHub repository." -ForegroundColor Yellow
            Write-Host "Please commit and push this file to your GitHub repository. For example:" -ForegroundColor Yellow
            Write-Host "  git add $localWorkflowFile" -ForegroundColor Cyan
            Write-Host "  git commit -m \"Add safe-tests.yml workflow\"" -ForegroundColor Cyan
            Write-Host "  git push" -ForegroundColor Cyan
            Write-Host "`nAfter pushing the file, please re-run this script." -ForegroundColor Yellow
        }
        else {
            Write-Host "❌ The file \'$localWorkflowFile\' also does not exist locally." -ForegroundColor Red
            Write-Host "Please ensure the workflow file is created and pushed to your GitHub repository." -ForegroundColor Yellow
            Write-Host "1. Create the directory if it doesn\'t exist: mkdir -p .github/workflows" -ForegroundColor Cyan
            Write-Host "2. Create \'$localWorkflowFile\' using the template from:" -ForegroundColor Cyan
            Write-Host "   https://github.com/Bigessfour/BusBus/blob/main/.github/workflows/safe-tests.yml.example" -ForegroundColor Cyan
            Write-Host "3. Commit and push the file to GitHub." -ForegroundColor Cyan
        }
        exit 1
    }
    else {
        Write-Host "✅ Workflow 'safe-tests.yml' found on GitHub repository (branch: $($currentBranch | ForEach-Object {if ($_){$_} else {'default'}}))." -ForegroundColor Green
    }
}
catch {
    $errorMessage = $_.Exception.Message
    if ($errorMessage -match "could not find workflow") {
        # This case should ideally be handled by the if (-not $workflowExistsOnGitHub) block
        Write-Host "❌ Error: \'gh workflow view\' indicated the workflow was not found or another CLI error occurred." -ForegroundColor Red
    }
    else {
        Write-Host "❌ An error occurred while checking the workflow status on GitHub: $errorMessage" -ForegroundColor Red
    }
    Write-Host "Please ensure you're authenticated with GitHub CLI (run 'gh auth login')" -ForegroundColor Yellow
    exit 1
}

# Trigger the workflow
Write-Host "Triggering workflow with parameters:" -ForegroundColor Cyan
Write-Host "  Test Filter: '$TestFilter'" -ForegroundColor Gray
Write-Host "  Timeout: $TimeoutMinutes minutes" -ForegroundColor Gray

try {
    $workflowRunCommand = "gh workflow run safe-tests.yml --repo Bigessfour/BusBus"
    if ($currentBranch) {
        $workflowRunCommand += " --ref $currentBranch"
    }

    $fields = @()
    if ($TestFilter) {
        $fields += "--field test_filter='$TestFilter'"
    }
    $fields += "--field timeout_minutes='$TimeoutMinutes'"

    $workflowRunCommand += " " + ($fields -join " ")

    Write-Host "Executing: $workflowRunCommand" -ForegroundColor DarkGray
    Invoke-Expression $workflowRunCommand

    Write-Host "✅ Workflow triggered successfully!" -ForegroundColor Green
    Write-Host "View progress at: https://github.com/Bigessfour/BusBus/actions" -ForegroundColor Cyan

    # Wait a moment then show recent runs
    Start-Sleep -Seconds 2
    Write-Host "`nRecent workflow runs (branch: $($currentBranch | ForEach-Object {if ($_){$_} else {'default'}})):" -ForegroundColor Yellow
    if ($currentBranch) {
        gh run list --workflow=safe-tests.yml --repo Bigessfour/BusBus --branch $currentBranch --limit=3
    }
    else {
        gh run list --workflow=safe-tests.yml --repo Bigessfour/BusBus --limit=3
    }
}
catch {
    Write-Error "Failed to trigger workflow: $_"
    Write-Host "You can manually trigger it at: https://github.com/Bigessfour/BusBus/actions/workflows/safe-tests.yml" -ForegroundColor Yellow
}
