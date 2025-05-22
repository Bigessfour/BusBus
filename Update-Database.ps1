# update-schema.ps1
# Streamlines schema updates for the BusBus application with enhanced automation and user interaction

# Parameters for flexibility
param(
    [switch]$Incremental, # If true, applies migrations incrementally without dropping the database
    [string]$DatabaseName = "BusBusDB", # Allows specifying the database name
    [switch]$SkipBackup # Skips database backup if true
)

# Step 1: Initialize Logging and Configuration
function Log-Message {
    param($Message, $Color = "White")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "$timestamp - $Message"
    Write-Host $logMessage -ForegroundColor $Color
    $logMessage | Out-File -Append "schema-update.log"
}

Log-Message "Starting schema update process..."

# Load configuration from config.json
$configFile = "config.json"
if (-not (Test-Path $configFile)) {
    Log-Message "Config file not found. Creating default config.json..." -Color "Yellow"
    $defaultConfig = @{
        ConnectionString = "Server=.\SQLEXPRESS;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True"
        ProjectDir       = "C:\Users\steve.mckitrick\Desktop\BusBus"
        DropDatabase     = $true
    }
    $defaultConfig | ConvertTo-Json -Depth 3 | Set-Content $configFile
}

$config = Get-Content -Path $configFile | ConvertFrom-Json
$connectionString = $config.ConnectionString
$projectDir = $config.ProjectDir
$dropDatabase = $config.DropDatabase -and (-not $Incremental)

# Step 2: Check Prerequisites
Log-Message "Checking prerequisites..."
$dotnetVersion = dotnet --version
if (-not $dotnetVersion) {
    Log-Message "Error: .NET SDK not found. Please install the .NET SDK." -Color "Red"
    exit 1
}

$efVersion = dotnet ef --version
if (-not $efVersion) {
    Log-Message "Error: EF Core tools not found. Installing globally..." -Color "Yellow"
    dotnet tool install --global dotnet-ef
    if (-not $?) {
        Log-Message "Failed to install EF Core tools. Please install manually." -Color "Red"
        exit 1
    }
}

if (-not (Test-Path $projectDir)) {
    Log-Message "Error: Project directory $projectDir not found." -Color "Red"
    exit 1
}
Set-Location $projectDir

# Step 3: Backup Database (if not skipped)
if ($dropDatabase -and -not $SkipBackup) {
    Write-Progress -Activity "Backing up database" -Status "Running..." -PercentComplete 10
    Log-Message "Backing up database $DatabaseName..."
    $backupFile = "BusBusDB_Backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').bak"
    try {
        sqlcmd -S ".\SQLEXPRESS" -Q "BACKUP DATABASE $DatabaseName TO DISK='$backupFile'"
        if ($LASTEXITCODE -ne 0) {
            Log-Message "Warning: Could not back up database. Proceeding without backup..." -Color "Yellow"
        }
        else {
            Log-Message "Database backed up to $backupFile." -Color "Green"
        }
    }
    catch {
        Log-Message "Error backing up database: $_" -Color "Red"
        exit 1
    }
}

# Step 4: Drop and Recreate Database (if not incremental)
if ($dropDatabase) {
    Write-Progress -Activity "Dropping database" -Status "Running..." -PercentComplete 30
    Log-Message "Dropping database $DatabaseName..."
    try {
        $dropQuery = "IF EXISTS (SELECT * FROM sys.databases WHERE name = '$DatabaseName') DROP DATABASE $DatabaseName"
        sqlcmd -S ".\SQLEXPRESS" -Q $dropQuery
        if ($LASTEXITCODE -ne 0) {
            Log-Message "Warning: Could not drop database. It may not exist or there may be a connection issue." -Color "Yellow"
        }
        else {
            Log-Message "Database dropped successfully." -Color "Green"
        }
    }
    catch {
        Log-Message "Error dropping database: $_" -Color "Red"
        exit 1
    }
}

# Step 5: Generate and Apply Migrations
Write-Progress -Activity "Generating migrations" -Status "Running..." -PercentComplete 50
Log-Message "Generating and applying migrations..."
if ($dropDatabase -and (Test-Path "Migrations")) {
    if (Test-Path "Migrations") {
        Remove-Item -Recurse -Force "Migrations"
        Log-Message "Removed existing migrations folder."
    }
}

dotnet ef migrations add InitialCreate --context AppDbContext
if ($LASTEXITCODE -ne 0) {
    Log-Message "Error generating migration. Check model changes and EF Core configuration." -Color "Red"
    exit 1
}

Write-Progress -Activity "Applying migrations" -Status "Running..." -PercentComplete 70
dotnet ef database update --context AppDbContext
if ($LASTEXITCODE -ne 0) {
    Log-Message "Error applying migration. Check connection string and database access." -Color "Red"
    exit 1
}
Log-Message "Migrations applied successfully." -Color "Green"

# Step 6: Seed Data
Write-Progress -Activity "Seeding database" -Status "Running..." -PercentComplete 90
Log-Message "Seeding database with sample data..."
Start-Process -FilePath "dotnet" -ArgumentList "run" -NoNewWindow -PassThru | Wait-Process
if ($LASTEXITCODE -ne 0) {
    Log-Message "Error seeding database. Check Program.cs and RouteService.cs for issues in SeedSampleDataAsync." -Color "Red"
    exit 1
}
Log-Message "Database seeded successfully." -Color "Green"

# Step 7: Validate Schema and Data
Write-Progress -Activity "Validating schema" -Status "Running..." -PercentComplete 95
Log-Message "Validating database schema and data..."
$validationQuery = "SELECT TOP 1 FirstName, LastName FROM Drivers; SELECT TOP 1 BusNumber FROM Vehicles;"
try {
    sqlcmd -S ".\SQLEXPRESS" -d $DatabaseName -Q $validationQuery
    if ($LASTEXITCODE -eq 0) {
        Log-Message "Schema validation successful. Columns FirstName, LastName, and BusNumber are present." -Color "Green"
    }
    else {
        Log-Message "Validation failed. Columns may not exist or database is inaccessible." -Color "Red"
        exit 1
    }
}
catch {
    Log-Message "Error validating schema: $_" -Color "Red"
    exit 1
}

# Step 8: Finalize
Write-Progress -Activity "Schema update complete" -Status "Done" -PercentComplete 100
Log-Message "Database schema update and seeding completed successfully!" -Color "Green"