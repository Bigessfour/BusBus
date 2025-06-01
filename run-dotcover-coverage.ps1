# PowerShell script to run JetBrains dotCover coverage and generate HTML report

$dotCoverPath = "C:\Program Files\JetBrains\dotCover\dotCover.exe"
$solution = "BusBus.sln"
$coverageDir = "CoverageReport"
$coverageSnapshot = "$coverageDir\dotCover.dcvr"
$coverageHtml = "$coverageDir\dotCover.html"

if (-not (Test-Path $dotCoverPath)) {
    Write-Host "dotCover.exe not found at $dotCoverPath. Please install JetBrains dotCover and update the path if needed."
    exit 1
}

if (-not (Test-Path $coverageDir)) {
    New-Item -ItemType Directory -Path $coverageDir | Out-Null
}

# Run coverage
& $dotCoverPath cover /TargetExecutable="dotnet" /TargetArguments="test $solution --no-build --configuration Release" /Output="$coverageSnapshot"

# Generate HTML report
& $dotCoverPath report /Source="$coverageSnapshot" /Output="$coverageHtml" /ReportType=HTML

Write-Host "dotCover HTML coverage report generated at $coverageHtml"

# Open the report in Chrome if available
if (Test-Path $coverageHtml) {
    Start-Process "chrome.exe" $coverageHtml
}
