-- Database schema update script to add missing columns
-- Run this script against your BusBusDB database to fix the schema mismatch

-- Add missing columns to Drivers table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = 'LicenseType')
BEGIN
    ALTER TABLE Drivers ADD LicenseType NVARCHAR(50) NOT NULL DEFAULT 'CDL';
    PRINT 'Added LicenseType column to Drivers table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE Drivers ADD IsActive BIT NOT NULL DEFAULT 1;
    PRINT 'Added IsActive column to Drivers table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = 'PerformanceMetricsJson')
BEGIN
    ALTER TABLE Drivers ADD PerformanceMetricsJson NVARCHAR(MAX) NOT NULL DEFAULT '{}';
    PRINT 'Added PerformanceMetricsJson column to Drivers table';
END

-- Add missing columns to Vehicles table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'Make')
BEGIN
    ALTER TABLE Vehicles ADD Make NVARCHAR(100) NOT NULL DEFAULT 'Unknown';
    PRINT 'Added Make column to Vehicles table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'ModelYear')
BEGIN
    ALTER TABLE Vehicles ADD ModelYear INT NOT NULL DEFAULT 2020;
    PRINT 'Added ModelYear column to Vehicles table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'VINNumber')
BEGIN
    ALTER TABLE Vehicles ADD VINNumber NVARCHAR(17) NOT NULL DEFAULT 'UNKNOWN';
    PRINT 'Added VINNumber column to Vehicles table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'LastInspectionDate')
BEGIN
    ALTER TABLE Vehicles ADD LastInspectionDate DATETIME2 NULL;
    PRINT 'Added LastInspectionDate column to Vehicles table';
END

-- Update existing data with better defaults
UPDATE Drivers SET LicenseType = 'CDL' WHERE LicenseType = 'Unknown' OR LicenseType IS NULL;
UPDATE Drivers SET IsActive = 1 WHERE IsActive IS NULL;
UPDATE Drivers SET PerformanceMetricsJson = '{}' WHERE PerformanceMetricsJson = '' OR PerformanceMetricsJson IS NULL;

UPDATE Vehicles SET Make = 'Ford' WHERE Make = 'Unknown' OR Make IS NULL;
UPDATE Vehicles SET ModelYear = 2020 WHERE ModelYear = 0 OR ModelYear IS NULL;
UPDATE Vehicles SET VINNumber = CONCAT('VIN', RIGHT('000' + CAST(VehicleId AS VARCHAR(3)), 3), REPLICATE('X', 13)) WHERE VINNumber = 'UNKNOWN' OR VINNumber IS NULL;

-- Add missing PMDriverId column to Routes table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Routes' AND COLUMN_NAME = 'PMDriverId')
BEGIN
    ALTER TABLE Routes ADD PMDriverId UNIQUEIDENTIFIER NULL;
    PRINT 'Added PMDriverId column to Routes table';
END

-- Add foreign key constraint for PMDriverId if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Routes_Drivers_PMDriverId')
BEGIN
    ALTER TABLE Routes ADD CONSTRAINT FK_Routes_Drivers_PMDriverId
        FOREIGN KEY (PMDriverId) REFERENCES Drivers(Id);
    PRINT 'Added foreign key constraint for PMDriverId';
END

PRINT 'Database schema update completed successfully';
