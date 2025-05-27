# PowerShell script to reset and rebuild database schema
# WARNING: This will drop and recreate the database, preserving data where possible

Write-Host "Database Schema Reset and Rebuild Script" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Step 1: Back up existing data
Write-Host "Step 1: Backing up existing data..." -ForegroundColor Yellow
$backupScript = @"
DECLARE @sql NVARCHAR(MAX) = ''

-- Check if Routes table exists and has data
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Routes')
BEGIN
    SELECT @sql = @sql + 'SELECT * INTO Routes_Backup FROM Routes;' + CHAR(13)
END

-- Check if Drivers table exists and has data
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Drivers')
BEGIN
    SELECT @sql = @sql + 'SELECT * INTO Drivers_Backup FROM Drivers;' + CHAR(13)
END

-- Check if Vehicles table exists and has data
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Vehicles')
BEGIN
    SELECT @sql = @sql + 'SELECT * INTO Vehicles_Backup FROM Vehicles;' + CHAR(13)
END

-- Check if Schedules table exists and has data
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Schedules')
BEGIN
    SELECT @sql = @sql + 'SELECT * INTO Schedules_Backup FROM Schedules;' + CHAR(13)
END

-- Check if Maintenance table exists and has data
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Maintenance')
BEGIN
    SELECT @sql = @sql + 'SELECT * INTO Maintenance_Backup FROM Maintenance;' + CHAR(13)
END

-- Execute backup commands
IF LEN(@sql) > 0
BEGIN
    PRINT 'Backing up existing data...'
    EXEC sp_executesql @sql
    PRINT 'Data backup completed.'
END
ELSE
BEGIN
    PRINT 'No existing data to backup.'
END
"@

$backupScript | Out-File -FilePath "backup-data.sql" -Encoding UTF8

# Step 2: Reset migration history
Write-Host "Step 2: Resetting EF Core migration history..." -ForegroundColor Yellow
try {
    # Delete the migrations history table to force a fresh start
    $resetHistoryScript = @"
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory')
BEGIN
    DROP TABLE [__EFMigrationsHistory]
    PRINT 'Migration history table dropped.'
END
ELSE
BEGIN
    PRINT 'Migration history table does not exist.'
END
"@

    $resetHistoryScript | Out-File -FilePath "reset-history.sql" -Encoding UTF8

    Write-Host "Executing backup script..." -ForegroundColor Green
    sqlcmd -S "localhost\SQLEXPRESS01" -d "BusBusDB" -E -i "backup-data.sql"

    Write-Host "Resetting migration history..." -ForegroundColor Green
    sqlcmd -S "localhost\SQLEXPRESS01" -d "BusBusDB" -E -i "reset-history.sql"
}
catch {
    Write-Host "Error during SQL execution: $_" -ForegroundColor Red
}

# Step 3: Drop all tables except backups
Write-Host "Step 3: Dropping existing tables..." -ForegroundColor Yellow
$dropTablesScript = @"
-- Drop foreign key constraints first
DECLARE @sql NVARCHAR(MAX) = ''
SELECT @sql = @sql + 'ALTER TABLE [' + SCHEMA_NAME(schema_id) + '].[' + OBJECT_NAME(parent_object_id) + '] DROP CONSTRAINT [' + name + '];' + CHAR(13)
FROM sys.foreign_keys

-- Drop tables (except backup tables)
SELECT @sql = @sql + 'DROP TABLE [' + SCHEMA_NAME(schema_id) + '].[' + name + '];' + CHAR(13)
FROM sys.tables
WHERE name NOT LIKE '%_Backup'

-- Execute drop commands
IF LEN(@sql) > 0
BEGIN
    PRINT 'Dropping existing tables and constraints...'
    EXEC sp_executesql @sql
    PRINT 'Tables dropped successfully.'
END
"@

$dropTablesScript | Out-File -FilePath "drop-tables.sql" -Encoding UTF8

try {
    Write-Host "Dropping existing tables..." -ForegroundColor Green
    sqlcmd -S "localhost\SQLEXPRESS01" -d "BusBusDB" -E -i "drop-tables.sql"
}
catch {
    Write-Host "Error dropping tables: $_" -ForegroundColor Red
}

# Step 4: Create fresh database schema
Write-Host "Step 4: Creating fresh database schema..." -ForegroundColor Yellow
try {
    Write-Host "Running EF Core database update..." -ForegroundColor Green
    dotnet ef database update --project BusBus.csproj

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database schema created successfully!" -ForegroundColor Green
    } else {
        Write-Host "EF Core update failed. Attempting to apply migrations manually..." -ForegroundColor Yellow

        # Generate migration script and apply manually
        dotnet ef migrations script -o "fresh-schema.sql" --project BusBus.csproj
        sqlcmd -S "localhost\SQLEXPRESS01" -d "BusBusDB" -E -i "fresh-schema.sql"
    }
}
catch {
    Write-Host "Error creating fresh schema: $_" -ForegroundColor Red
}

# Step 5: Restore data where possible
Write-Host "Step 5: Restoring data from backups..." -ForegroundColor Yellow
$restoreScript = @"
-- Restore Routes data if backup exists
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Routes_Backup')
AND EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Routes')
BEGIN
    PRINT 'Restoring Routes data...'
    INSERT INTO Routes (RouteID, RouteName, StartLocation, EndLocation, Distance, Duration, IsActive, CreatedDate, ModifiedDate)
    SELECT
        RouteID,
        COALESCE(RouteName, 'Unknown Route'),
        COALESCE(StartLocation, 'Unknown Start'),
        COALESCE(EndLocation, 'Unknown End'),
        COALESCE(Distance, 0.0),
        COALESCE(Duration, '00:00:00'),
        COALESCE(IsActive, 1),
        COALESCE(CreatedDate, GETDATE()),
        COALESCE(ModifiedDate, GETDATE())
    FROM Routes_Backup
    PRINT 'Routes data restored.'
END

-- Restore Drivers data if backup exists (with column mapping)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Drivers_Backup')
AND EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Drivers')
BEGIN
    PRINT 'Restoring Drivers data...'
    INSERT INTO Drivers (DriverID, DriverName, LicenseNumber, PhoneNumber, Email, HireDate, IsActive, CreatedDate, ModifiedDate)
    SELECT
        DriverID,
        COALESCE(DriverName, 'Unknown Driver'),
        COALESCE(LicenseNumber, 'UNKNOWN'),
        COALESCE(PhoneNumber, '000-000-0000'),
        COALESCE(Email, 'unknown@example.com'),
        COALESCE(HireDate, GETDATE()),
        COALESCE(IsActive, 1),
        COALESCE(CreatedDate, GETDATE()),
        COALESCE(ModifiedDate, GETDATE())
    FROM Drivers_Backup
    PRINT 'Drivers data restored.'
END

-- Restore Vehicles data if backup exists (with column mapping)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Vehicles_Backup')
AND EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Vehicles')
BEGIN
    PRINT 'Restoring Vehicles data...'
    INSERT INTO Vehicles (VehicleID, VehicleNumber, Make, Model, Year, Capacity, IsActive, CreatedDate, ModifiedDate)
    SELECT
        VehicleID,
        COALESCE(VehicleNumber, 'UNKNOWN'),
        COALESCE(Make, 'Unknown Make'),
        COALESCE(Model, 'Unknown Model'),
        COALESCE(Year, 2020),
        COALESCE(Capacity, 50),
        COALESCE(IsActive, 1),
        COALESCE(CreatedDate, GETDATE()),
        COALESCE(ModifiedDate, GETDATE())
    FROM Vehicles_Backup
    PRINT 'Vehicles data restored.'
END

-- Clean up backup tables
DROP TABLE IF EXISTS Routes_Backup
DROP TABLE IF EXISTS Drivers_Backup
DROP TABLE IF EXISTS Vehicles_Backup
DROP TABLE IF EXISTS Schedules_Backup
DROP TABLE IF EXISTS Maintenance_Backup

PRINT 'Data restoration completed and backup tables cleaned up.'
"@

$restoreScript | Out-File -FilePath "restore-data.sql" -Encoding UTF8

try {
    Write-Host "Restoring data..." -ForegroundColor Green
    sqlcmd -S "localhost\SQLEXPRESS01" -d "BusBusDB" -E -i "restore-data.sql"
}
catch {
    Write-Host "Error restoring data: $_" -ForegroundColor Red
    Write-Host "Data backup files may still exist for manual recovery." -ForegroundColor Yellow
}

# Step 6: Verify final schema
Write-Host "Step 6: Verifying final schema..." -ForegroundColor Yellow
$verifyScript = @"
PRINT 'Database Schema Verification Report'
PRINT '=================================='

-- Check table existence and column counts
DECLARE @tables TABLE (TableName NVARCHAR(128), ColumnCount INT)

INSERT INTO @tables (TableName, ColumnCount)
SELECT
    t.TABLE_NAME,
    COUNT(c.COLUMN_NAME)
FROM INFORMATION_SCHEMA.TABLES t
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME
WHERE t.TABLE_TYPE = 'BASE TABLE'
GROUP BY t.TABLE_NAME
ORDER BY t.TABLE_NAME

SELECT
    TableName as [Table Name],
    ColumnCount as [Column Count]
FROM @tables

-- Check migration history
PRINT ''
PRINT 'Migration History:'
PRINT '=================='
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory')
BEGIN
    SELECT MigrationId, ProductVersion FROM [__EFMigrationsHistory] ORDER BY MigrationId
END
ELSE
BEGIN
    PRINT 'No migration history found.'
END
"@

$verifyScript | Out-File -FilePath "verify-schema.sql" -Encoding UTF8

try {
    Write-Host "Verifying schema..." -ForegroundColor Green
    sqlcmd -S "localhost\SQLEXPRESS01" -d "BusBusDB" -E -i "verify-schema.sql"
}
catch {
    Write-Host "Error verifying schema: $_" -ForegroundColor Red
}

# Step 7: Test application connection
Write-Host "Step 7: Testing application database connection..." -ForegroundColor Yellow
try {
    Write-Host "Building and testing application..." -ForegroundColor Green
    dotnet build BusBus.sln

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build successful! Testing database connection..." -ForegroundColor Green
        # Run a quick database connection test
        dotnet run --project BusBus.csproj -- --test-db-connection
    } else {
        Write-Host "Build failed. Please check for compilation errors." -ForegroundColor Red
    }
}
catch {
    Write-Host "Error testing application: $_" -ForegroundColor Red
}

# Cleanup temporary files
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
Remove-Item -Path "backup-data.sql", "reset-history.sql", "drop-tables.sql", "restore-data.sql", "verify-schema.sql", "fresh-schema.sql" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Database schema reset completed!" -ForegroundColor Green
Write-Host "If there were any errors, please check the output above and run individual steps manually." -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Run 'dotnet build' to ensure no compilation errors" -ForegroundColor White
Write-Host "2. Run 'dotnet test' to verify tests pass" -ForegroundColor White
Write-Host "3. Start the application to verify everything works" -ForegroundColor White
