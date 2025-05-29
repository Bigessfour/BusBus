# Run only the SimpleDashboardUI tests that are known to work
# This script provides a simple way to test the UI without hanging

Write-Host "=== Running Working Dashboard UI Tests ===" -ForegroundColor Green
Write-Host "This script runs only the dashboard UI tests that are known to complete successfully." -ForegroundColor Yellow
Write-Host ""

# Build the project
Write-Host "Building project..." -ForegroundColor Cyan
dotnet build --configuration Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Cannot run tests." -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Run the tests that are known to work
Write-Host "Running working Dashboard UI tests..." -ForegroundColor Cyan
dotnet test --filter "TestCategory=SimpleDashboardUI" --no-build

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=== Dashboard UI Tests Completed Successfully! ===" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "=== Dashboard UI Tests Failed ===" -ForegroundColor Red
}

exit $LASTEXITCODE
