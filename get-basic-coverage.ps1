Write-Host "=== Basic Test Coverage Report ===" -ForegroundColor Cyan

# Run only the basic tests (exclude problematic AutoFixture tests)
Write-Host "Running basic tests with coverage..." -ForegroundColor Green

dotnet test BusBus.Tests/BusBus.Tests.csproj `
    --filter "FullyQualifiedName~BasicTests|FullyQualifiedName~ModelTests|FullyQualifiedName~ServiceTests" `
    --collect:"XPlat Code Coverage" `
    --results-directory TestResults `
    --logger "console;verbosity=normal"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Some tests failed, but continuing with coverage report..." -ForegroundColor Yellow
}

# Find coverage files
$coverageFiles = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse

if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files found!" -ForegroundColor Red
    exit 1
}

# Install/update report generator
dotnet tool install --global dotnet-reportgenerator-globaltool --ignore-failed-sources 2>$null

# Generate report
$coverageFile = $coverageFiles[0].FullName
Write-Host "Generating coverage report from: $coverageFile" -ForegroundColor Blue

reportgenerator `
    "-reports:$coverageFile" `
    "-targetdir:TestResults/BasicCoverageReport" `
    "-reporttypes:Html;TextSummary" `
    "-sourcedirs:BusBus/" `
    "-title:BusBus Basic Test Coverage"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Coverage report generated!" -ForegroundColor Green
    Write-Host "Location: TestResults/BasicCoverageReport/index.html" -ForegroundColor Cyan
    
    if (Test-Path "TestResults/BasicCoverageReport/Summary.txt") {
        Write-Host "`n=== COVERAGE SUMMARY ===" -ForegroundColor Yellow
        Get-Content "TestResults/BasicCoverageReport/Summary.txt"
    }
    
    # Open report
    $reportPath = Join-Path (Get-Location) "TestResults/BasicCoverageReport/index.html"
    Start-Process $reportPath
} else {
    Write-Host "Failed to generate report" -ForegroundColor Red
}
