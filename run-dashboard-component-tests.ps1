# Run Dashboard Components Tests
# This script runs the component-specific tests for the Dashboard UI

Write-Host "=== Running Dashboard Component Tests ===" -ForegroundColor Green
Write-Host "This script runs the tests for Dashboard services, database handling, buttons, content view area, dynamic grid view, CRUD operations, and statistics panel." -ForegroundColor Yellow
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
Write-Host "Running Dashboard Component tests..." -ForegroundColor Cyan
dotnet test --filter "TestCategory=DashboardComponents" --no-build

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=== Dashboard Component Tests Completed Successfully! ===" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "=== Dashboard Component Tests Failed ===" -ForegroundColor Red
}

exit $LASTEXITCODE
