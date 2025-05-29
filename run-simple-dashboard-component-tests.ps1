# Run Simple Dashboard Components Tests
# This script runs the simplified component-specific tests for the Dashboard UI

Write-Host "=== Running Simple Dashboard Component Tests ===" -ForegroundColor Green
Write-Host "This script runs tests for Dashboard services, database handling, buttons, content view area, dynamic grid view, and statistics panel." -ForegroundColor Yellow
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

# Run the specific dashboard component tests
Write-Host "Running Simple Dashboard Component tests..." -ForegroundColor Cyan
dotnet test --filter "TestCategory=SimpleDashboardComponents" --no-build

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=== Simple Dashboard Component Tests Completed Successfully! ===" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "=== Simple Dashboard Component Tests Failed ===" -ForegroundColor Red
}

exit $LASTEXITCODE
