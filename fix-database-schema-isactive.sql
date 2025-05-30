-- Database Schema Fix - Add Missing IsActive Columns
-- This script adds the missing IsActive columns that are causing the application errors

USE BusBusDB;
GO

PRINT 'Starting database schema fix...';

-- Check if columns already exist before adding them
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = 'IsActive')
BEGIN
    PRINT 'Adding IsActive column to Drivers table...';
    ALTER TABLE Drivers ADD IsActive BIT NOT NULL DEFAULT 1;
    PRINT 'IsActive column added to Drivers table.';
END
ELSE
BEGIN
    PRINT 'IsActive column already exists in Drivers table.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'IsActive')
BEGIN
    PRINT 'Adding IsActive column to Vehicles table...';
    ALTER TABLE Vehicles ADD IsActive BIT NOT NULL DEFAULT 1;
    PRINT 'IsActive column added to Vehicles table.';
END
ELSE
BEGIN
    PRINT 'IsActive column already exists in Vehicles table.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Routes' AND COLUMN_NAME = 'IsActive')
BEGIN
    PRINT 'Adding IsActive column to Routes table...';
    ALTER TABLE Routes ADD IsActive BIT NOT NULL DEFAULT 1;
    PRINT 'IsActive column added to Routes table.';
END
ELSE
BEGIN
    PRINT 'IsActive column already exists in Routes table.';
END

-- Also check for other potentially missing columns that might be causing issues
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = 'LicenseType')
BEGIN
    PRINT 'Adding LicenseType column to Drivers table...';
    ALTER TABLE Drivers ADD LicenseType NVARCHAR(50) NULL;
    PRINT 'LicenseType column added to Drivers table.';
END
ELSE
BEGIN
    PRINT 'LicenseType column already exists in Drivers table.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = 'PerformanceMetricsJson')
BEGIN
    PRINT 'Adding PerformanceMetricsJson column to Drivers table...';
    ALTER TABLE Drivers ADD PerformanceMetricsJson NVARCHAR(MAX) NULL;
    PRINT 'PerformanceMetricsJson column added to Drivers table.';
END
ELSE
BEGIN
    PRINT 'PerformanceMetricsJson column already exists in Drivers table.';
END

-- Update any existing records to have IsActive = 1 (active by default)
UPDATE Drivers SET IsActive = 1 WHERE IsActive IS NULL;
UPDATE Vehicles SET IsActive = 1 WHERE IsActive IS NULL;
UPDATE Routes SET IsActive = 1 WHERE IsActive IS NULL;

PRINT 'Database schema fix completed successfully!';
PRINT 'All existing records have been marked as active (IsActive = 1).';

-- Verify the changes
PRINT '';
PRINT 'Verification - Checking column existence:';

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = 'IsActive')
    PRINT '✓ Drivers.IsActive column exists';
ELSE
    PRINT '✗ Drivers.IsActive column missing';

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'IsActive')
    PRINT '✓ Vehicles.IsActive column exists';
ELSE
    PRINT '✗ Vehicles.IsActive column missing';

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Routes' AND COLUMN_NAME = 'IsActive')
    PRINT '✓ Routes.IsActive column exists';
ELSE
    PRINT '✗ Routes.IsActive column missing';

-- Show record counts
PRINT '';
PRINT 'Current record counts:';
SELECT 'Drivers' as TableName, COUNT(*) as TotalRecords, SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) as ActiveRecords FROM Drivers
UNION ALL
SELECT 'Vehicles', COUNT(*), SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) FROM Vehicles
UNION ALL
SELECT 'Routes', COUNT(*), SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) FROM Routes;

PRINT 'Schema fix script completed.';
