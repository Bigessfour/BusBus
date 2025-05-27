# PowerShell script to delete and recreate the BusBusDB database
Write-Host "Deleting and Recreating BusBusDB Database" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Step 1: Delete the database using EF Core
Write-Host "Step 1: Deleting existing database..." -ForegroundColor Yellow
try {
    dotnet ef database drop --force --project BusBus.csproj
    Write-Host "Database deleted successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error deleting database (may not exist): $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 2: Recreate the database with all migrations
Write-Host "Step 2: Creating fresh database with correct schema..." -ForegroundColor Yellow
try {
    dotnet ef database update --project BusBus.csproj
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database created successfully with correct schema!" -ForegroundColor Green
    } else {
        Write-Host "Error creating database. Exit code: $LASTEXITCODE" -ForegroundColor Red
    }
}
catch {
    Write-Host "Error creating database: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 3: Verify the new schema
Write-Host "Step 3: Verifying database schema..." -ForegroundColor Yellow
try {
    # Build the project first
    dotnet build BusBus.csproj
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build successful!" -ForegroundColor Green

        # Try to run the application briefly to test database connection
        Write-Host "Testing database connection..." -ForegroundColor Yellow
        $process = Start-Process -FilePath "dotnet" -ArgumentList "run --project BusBus.csproj -- --test-connection" -PassThru -WindowStyle Hidden
        Start-Sleep -Seconds 10
        if (!$process.HasExited) {
            $process.Kill()
        }
        Write-Host "Database connection test completed." -ForegroundColor Green
    } else {
        Write-Host "Build failed. Exit code: $LASTEXITCODE" -ForegroundColor Red
    }
}
catch {
    Write-Host "Error during verification: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Database recreation completed!" -ForegroundColor Green
Write-Host "You can now run the application with the correct schema." -ForegroundColor Yellow
