# pre-start-cleanup.ps1
# Run this before starting BusBus to ensure a clean environment

Write-Host "=== Pre-Start Environment Cleanup ===" -ForegroundColor Cyan
Write-Host "Ensuring clean environment before starting BusBus..." -ForegroundColor Yellow
Write-Host ""

# Function to check and clean processes
function Clear-Environment {
    # Step 1: Check for existing processes
    Write-Host "Step 1: Checking for existing BusBus processes..." -ForegroundColor White

    $existingProcesses = @()
    $processNames = @("BusBus", "dotnet", "testhost", "vstest.console")

    foreach ($name in $processNames) {
        $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
        if ($procs) {
            $existingProcesses += $procs
        }
    }

    if ($existingProcesses.Count -gt 0) {
        Write-Host "⚠ Found $($existingProcesses.Count) existing processes" -ForegroundColor Yellow
        $existingProcesses | Format-Table Id, ProcessName, StartTime, WorkingSet -AutoSize

        $cleanup = Read-Host "Clean up these processes before starting? (Y/N)"
        if ($cleanup -eq "Y" -or $cleanup -eq "y") {
            Write-Host "Running emergency shutdown..." -ForegroundColor Red
            & ".\emergency-shutdown.ps1" -Force -Silent
            Start-Sleep -Seconds 2
        }
    } else {
        Write-Host "✓ No existing processes found" -ForegroundColor Green
    }

    # Step 2: Check database connections
    Write-Host ""
    Write-Host "Step 2: Checking database connectivity..." -ForegroundColor White

    try {
        # Quick database connection test
        $connectionString = "Server=localhost\SQLEXPRESS;Database=BusBusDB;Integrated Security=true;Connection Timeout=5"
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        $connection.Close()
        Write-Host "✓ Database connection successful" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠ Database connection failed: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "  This may cause hanging processes. Consider checking SQL Server." -ForegroundColor Yellow
    }

    # Step 3: Clear temporary files
    Write-Host ""
    Write-Host "Step 3: Cleaning temporary files..." -ForegroundColor White

    try {
        # Clear temp BusBus files
        $tempPaths = @(
            (Join-Path $env:TEMP "BusBus*"),
            (Join-Path $env:LOCALAPPDATA "Temp\BusBus*"),
            ".\bin\Debug\net8.0-windows\*.tmp",
            ".\obj\**\*.tmp"
        )

        $cleanedFiles = 0
        foreach ($path in $tempPaths) {
            $files = Get-ChildItem $path -ErrorAction SilentlyContinue
            if ($files) {
                Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
                $cleanedFiles += $files.Count
            }
        }

        if ($cleanedFiles -gt 0) {
            Write-Host "✓ Cleaned $cleanedFiles temporary files" -ForegroundColor Green
        } else {
            Write-Host "✓ No temporary files to clean" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "⚠ Error cleaning temporary files: $($_.Exception.Message)" -ForegroundColor Yellow
    }

    # Step 4: Memory cleanup
    Write-Host ""
    Write-Host "Step 4: Memory cleanup..." -ForegroundColor White

    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    [System.GC]::Collect()

    Write-Host "✓ Memory cleanup completed" -ForegroundColor Green

    # Step 5: Check available memory
    $availableMemory = [math]::Round((Get-WmiObject -Class Win32_OperatingSystem).FreePhysicalMemory / 1024 / 1024, 2)
    Write-Host "✓ Available memory: ${availableMemory} GB" -ForegroundColor Green

    if ($availableMemory -lt 1) {
        Write-Host "⚠ Low memory detected. Consider closing other applications." -ForegroundColor Yellow
    }
}

# Run the cleanup
Clear-Environment

Write-Host ""
Write-Host "=== Environment Check Complete ===" -ForegroundColor Cyan
Write-Host "Environment is ready for BusBus startup." -ForegroundColor Green
Write-Host ""
Write-Host "To start BusBus safely:" -ForegroundColor White
Write-Host "  1. Use: dotnet run" -ForegroundColor Gray
Write-Host "  2. Or use: .\run-app.ps1" -ForegroundColor Gray
Write-Host "  3. If issues occur, use: .\emergency-shutdown.ps1" -ForegroundColor Gray
Write-Host ""

# Optionally start the application
$startApp = Read-Host "Start BusBus now? (Y/N)"
if ($startApp -eq "Y" -or $startApp -eq "y") {
    Write-Host "Starting BusBus..." -ForegroundColor Green
    dotnet run
}
