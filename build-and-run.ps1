# build-and-run.ps1
# This script builds and runs the BusBus application, showing build errors and warnings

Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "         BUILDING AND RUNNING BUSBUS APPLICATION         " -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if the solution exists
if (-not (Test-Path "BusBus.sln")) {
    Write-Host "Error: BusBus.sln not found in the current directory!" -ForegroundColor Red
    exit 1
}

# Step 2: Build the solution with detailed output
Write-Host "Building the solution..." -ForegroundColor Yellow
$buildOutput = dotnet build BusBus.sln --verbosity minimal

# Step 3: Check for build errors and warnings
$errorCount = ($buildOutput | Select-String -Pattern "error" | Measure-Object).Count
$warningCount = ($buildOutput | Select-String -Pattern "warning" | Measure-Object).Count

# Display build status
Write-Host ""
if ($errorCount -gt 0) {
    Write-Host "BUILD FAILED with $errorCount error(s) and $warningCount warning(s)" -ForegroundColor Red
    Write-Host "Errors and warnings:" -ForegroundColor Red
    $buildOutput | Select-String -Pattern "error|warning" | ForEach-Object {
        Write-Host "  $_" -ForegroundColor Red
    }
    exit 1
}
else {
    Write-Host "BUILD SUCCESSFUL" -ForegroundColor Green
    if ($warningCount -gt 0) {
        Write-Host "With $warningCount warning(s):" -ForegroundColor Yellow
        $buildOutput | Select-String -Pattern "warning" | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "No warnings!" -ForegroundColor Green
    }
}

# Step 4: Provide visual debugging instructions
Write-Host ""
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "                  VISUAL DEBUG GUIDE                     " -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "As the application starts, observe the following components:" -ForegroundColor White
Write-Host ""
Write-Host "1. MAIN SHELL (Dashboard.cs)" -ForegroundColor Green
Write-Host "   ✓ Top header with app title (\"BusBus - Transport Management System\")" -ForegroundColor White
Write-Host "   ✓ Left side panel with navigation buttons (Dashboard, Routes, etc.)" -ForegroundColor White
Write-Host "   ✓ Status bar at the bottom" -ForegroundColor White
Write-Host ""
Write-Host "2. CONTENT VIEW (DashboardView.cs)" -ForegroundColor Green
Write-Host "   ✓ Loaded inside the main content area" -ForegroundColor White
Write-Host "   ✓ Has its own sections (header, today's routes, quick stats, etc.)" -ForegroundColor White
Write-Host ""
Write-Host "NAVIGATION TEST:" -ForegroundColor Magenta
Write-Host "  - Click different navigation buttons on the left" -ForegroundColor White
Write-Host "  - Observe that the content panel changes, but the outer shell stays the same" -ForegroundColor White
Write-Host ""
Write-Host "Press Enter to run the application..." -ForegroundColor Yellow
$null = Read-Host

# Step 5: Run the application
Write-Host "Starting BusBus application..." -ForegroundColor Cyan
dotnet run --project BusBus.csproj

Write-Host ""
Write-Host "Application closed. What did you observe?" -ForegroundColor Yellow
