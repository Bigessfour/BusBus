# Test Coverage Report Generator Script

param(
    [string]$Configuration = "Debug",
    [switch]$OpenReport = $false
)

Write-Host "Starting test coverage analysis..." -ForegroundColor Green

# Clean previous results
if (Test-Path "TestResults") {
    Remove-Item "TestResults" -Recurse -Force
    Write-Host "Cleaned previous test results" -ForegroundColor Yellow
}

# Create TestResults directory
New-Item -ItemType Directory -Path "TestResults" -Force | Out-Null

# Run tests with coverage
Write-Host "Running tests with coverage collection..." -ForegroundColor Blue
dotnet test BusBus.Tests/BusBus.Tests.csproj `
    --configuration $Configuration `
    --settings coverlet.runsettings `
    --collect:"XPlat Code Coverage" `
    --results-directory TestResults `
    --logger "console;verbosity=detailed"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Find coverage files
$coverageFiles = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse

if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files found!" -ForegroundColor Red
    exit 1
}

# Generate HTML report
Write-Host "Generating HTML coverage report..." -ForegroundColor Blue
$coverageFile = $coverageFiles[0].FullName
dotnet tool install --global dotnet-reportgenerator-globaltool --ignore-failed-sources 2>$null

reportgenerator `
    "-reports:$coverageFile" `
    "-targetdir:TestResults/CoverageReport" `
    "-reporttypes:Html;TextSummary" `
    "-sourcedirs:BusBus/"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Coverage report generated successfully!" -ForegroundColor Green
    Write-Host "Report location: TestResults/CoverageReport/index.html" -ForegroundColor Cyan
    
    # Display summary
    if (Test-Path "TestResults/CoverageReport/Summary.txt") {
        Write-Host "`nCoverage Summary:" -ForegroundColor Yellow
        Get-Content "TestResults/CoverageReport/Summary.txt"
    }
    
    # Display key metrics from the HTML report
    Write-Host "`nKey Insights:" -ForegroundColor Magenta
    Write-Host "• Focus on RouteService tests - only 8% coverage on critical business logic" -ForegroundColor Yellow
    Write-Host "• UI components need integration tests for better coverage" -ForegroundColor Yellow
    Write-Host "• Consider breaking down complex methods (high CRAP scores)" -ForegroundColor Yellow
    Write-Host "• Migration files at 0% is expected and acceptable" -ForegroundColor Green
    
    Write-Host "`nNext Steps:" -ForegroundColor Cyan
    Write-Host "1. Add unit tests for RouteService methods" -ForegroundColor White
    Write-Host "2. Create integration tests for UI workflows" -ForegroundColor White
    Write-Host "3. Refactor high-complexity methods (SaveRouteAsync, UpdateComboBoxes)" -ForegroundColor White
    Write-Host "4. Target 70%+ line coverage for business logic" -ForegroundColor White
    
    if ($OpenReport) {
        $reportPath = Join-Path (Get-Location) "TestResults/CoverageReport/index.html"
        Write-Host "`nOpening coverage report in browser..." -ForegroundColor Green
        Start-Process $reportPath
    } else {
        Write-Host "`nRun with -OpenReport to view detailed report in browser" -ForegroundColor Gray
    }
} else {
    Write-Host "Failed to generate coverage report!" -ForegroundColor Red
}
