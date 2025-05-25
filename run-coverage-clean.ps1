# Clean Test Coverage Report Generator

param(
    [string]$Configuration = "Debug",
    [switch]$OpenReport = $false,
    [switch]$SkipBuild = $false
)

Write-Host "=== Clean Test Coverage Analysis ===" -ForegroundColor Cyan

# Step 1: Clean and build
if (-not $SkipBuild) {
    Write-Host "Step 1: Cleaning and building solution..." -ForegroundColor Green
    dotnet clean
    dotnet build --configuration $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# Step 2: Clean previous results
Write-Host "Step 2: Cleaning previous test results..." -ForegroundColor Green
if (Test-Path "TestResults") {
    Remove-Item "TestResults" -Recurse -Force
}
New-Item -ItemType Directory -Path "TestResults" -Force | Out-Null

# Step 3: Run tests first without coverage to verify they work
Write-Host "Step 3: Verifying tests run successfully..." -ForegroundColor Green
dotnet test BusBus.Tests/BusBus.Tests.csproj --configuration $Configuration --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests are failing! Fix tests before running coverage." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "✓ All tests pass! Proceeding with coverage..." -ForegroundColor Green

# Step 4: Run tests with coverage
Write-Host "Step 4: Running tests with coverage collection..." -ForegroundColor Blue
dotnet test BusBus.Tests/BusBus.Tests.csproj `
    --configuration $Configuration `
    --collect:"XPlat Code Coverage" `
    --results-directory TestResults `
    --logger "console;verbosity=normal" `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude="[*.Tests]*,[*]*.Program,[*]*.Startup"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Coverage collection failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Step 5: Find and process coverage files
Write-Host "Step 5: Processing coverage results..." -ForegroundColor Blue
$coverageFiles = Get-ChildItem -Path "TestResults" -Filter "coverage.opencover.xml" -Recurse

if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files found! Looking for alternative formats..." -ForegroundColor Yellow
    $coverageFiles = Get-ChildItem -Path "TestResults" -Filter "*.xml" -Recurse | Where-Object { $_.Name -like "*coverage*" }
}

if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files found at all!" -ForegroundColor Red
    Write-Host "Available files in TestResults:" -ForegroundColor Yellow
    Get-ChildItem -Path "TestResults" -Recurse | Format-Table Name, FullName
    exit 1
}

Write-Host "Found coverage file: $($coverageFiles[0].FullName)" -ForegroundColor Green

# Step 6: Install report generator
Write-Host "Step 6: Installing/updating report generator..." -ForegroundColor Blue
dotnet tool install --global dotnet-reportgenerator-globaltool --ignore-failed-sources 2>$null
dotnet tool update --global dotnet-reportgenerator-globaltool --ignore-failed-sources 2>$null

# Step 7: Generate HTML report
Write-Host "Step 7: Generating HTML coverage report..." -ForegroundColor Blue
$coverageFile = $coverageFiles[0].FullName
reportgenerator `
    "-reports:$coverageFile" `
    "-targetdir:TestResults/CoverageReport" `
    "-reporttypes:Html;TextSummary;Badges" `
    "-sourcedirs:BusBus/" `
    "-title:BusBus Test Coverage"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Coverage report generated successfully!" -ForegroundColor Green
    Write-Host "Report location: TestResults/CoverageReport/index.html" -ForegroundColor Cyan
    
    # Display summary
    if (Test-Path "TestResults/CoverageReport/Summary.txt") {
        Write-Host "`n=== COVERAGE SUMMARY ===" -ForegroundColor Yellow
        Get-Content "TestResults/CoverageReport/Summary.txt"
    }
    
    # List test files that were covered
    Write-Host "`n=== TEST FILES INCLUDED ===" -ForegroundColor Yellow
    Get-ChildItem -Path "BusBus.Tests" -Filter "*.cs" | ForEach-Object {
        Write-Host "  ✓ $($_.Name)" -ForegroundColor Green
    }
    
    if ($OpenReport) {
        $reportPath = Join-Path (Get-Location) "TestResults/CoverageReport/index.html"
        Start-Process $reportPath
    }
} else {
    Write-Host "Failed to generate coverage report!" -ForegroundColor Red
}

Write-Host "`n=== COMPLETED ===" -ForegroundColor Cyan
