# PowerShell script to automate GitHub push

# Parameters
param (
    [Parameter(Mandatory=$true)]
    [string]$CommitMessage,
    [string]$Branch = "main"
)

try {
    # Ensure we're in a Git repository
    if (-not (Test-Path -Path ".git")) {
        Write-Error "Error: Current directory is not a Git repository."
        exit 1
    }

    # Stage all changes
    Write-Host "Staging all changes..."
    git add .

    # Commit changes with the provided message
    Write-Host "Committing changes with message: $CommitMessage"
    git commit -m $CommitMessage

    # Push to the specified branch
    Write-Host "Pushing to branch: $Branch"
    git push origin $Branch

    Write-Host "Successfully pushed changes to GitHub."
}
catch {
    Write-Error "An error occurred: $_"
    exit 1
}