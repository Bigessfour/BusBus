-- Fix database schema mismatch for BusBus application
-- Adding missing columns that exist in Entity Framework models but not in database

USE [BusBusDB]
GO

PRINT 'Adding missing columns to Routes table...'

-- Add PMDriverId column to Routes table (for PM Driver support)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Routes' AND COLUMN_NAME = 'PMDriverId')
BEGIN
    ALTER TABLE [Routes]
    ADD [PMDriverId] uniqueidentifier NULL
    PRINT 'Added PMDriverId column to Routes table'
END
ELSE
    PRINT 'PMDriverId column already exists in Routes table'

-- Add foreign key constraint for PMDriverId
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_Routes_Drivers_PMDriverId')
BEGIN
    ALTER TABLE [Routes]
    ADD CONSTRAINT [FK_Routes_Drivers_PMDriverId]
    FOREIGN KEY ([PMDriverId]) REFERENCES [Drivers] ([Id]) ON DELETE SET NULL
    PRINT 'Added foreign key constraint for PMDriverId'
END

PRINT 'Adding missing columns to Drivers table...'

-- Add LicenseType column to Drivers table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = 'LicenseType')
BEGIN
    ALTER TABLE [Drivers]
    ADD [LicenseType] nvarchar(50) NOT NULL DEFAULT 'CDL'
    PRINT 'Added LicenseType column to Drivers table'
END
ELSE
    PRINT 'LicenseType column already exists in Drivers table'

-- Add PerformanceMetricsJson column to Drivers table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Drivers' AND COLUMN_NAME = 'PerformanceMetricsJson')
BEGIN
    ALTER TABLE [Drivers]
    ADD [PerformanceMetricsJson] nvarchar(max) NOT NULL DEFAULT '{}'
    PRINT 'Added PerformanceMetricsJson column to Drivers table'
END
ELSE
    PRINT 'PerformanceMetricsJson column already exists in Drivers table'

PRINT 'Adding missing columns to Vehicles table...'

-- Add Make column to Vehicles table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'Make')
BEGIN
    ALTER TABLE [Vehicles]
    ADD [Make] nvarchar(100) NOT NULL DEFAULT ''
    PRINT 'Added Make column to Vehicles table'
END
ELSE
    PRINT 'Make column already exists in Vehicles table'

-- Add VINNumber column to Vehicles table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'VINNumber')
BEGIN
    ALTER TABLE [Vehicles]
    ADD [VINNumber] nvarchar(50) NOT NULL DEFAULT ''
    PRINT 'Added VINNumber column to Vehicles table'
END
ELSE
    PRINT 'VINNumber column already exists in Vehicles table'

-- Add ModelYear column to Vehicles table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'ModelYear')
BEGIN
    ALTER TABLE [Vehicles]
    ADD [ModelYear] int NOT NULL DEFAULT 2020
    PRINT 'Added ModelYear column to Vehicles table'
END
ELSE
    PRINT 'ModelYear column already exists in Vehicles table'

-- Add LastInspectionDate column to Vehicles table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'LastInspectionDate')
BEGIN
    ALTER TABLE [Vehicles]
    ADD [LastInspectionDate] datetime2 NULL
    PRINT 'Added LastInspectionDate column to Vehicles table'
END
ELSE
    PRINT 'LastInspectionDate column already exists in Vehicles table'

PRINT 'Updating existing data with sensible defaults...'

-- Update existing vehicles with Make extracted from MakeModel if possible
UPDATE [Vehicles]
SET [Make] = CASE
    WHEN [MakeModel] LIKE 'Blue Bird%' THEN 'Blue Bird'
    WHEN [MakeModel] LIKE 'Thomas%' THEN 'Thomas'
    WHEN [MakeModel] LIKE 'IC Bus%' THEN 'IC Bus'
    WHEN [MakeModel] LIKE 'Freightliner%' THEN 'Freightliner'
    ELSE 'Unknown'
END
WHERE [Make] = '' OR [Make] IS NULL

-- Update ModelYear based on Year column if available
UPDATE [Vehicles]
SET [ModelYear] = COALESCE([Year], 2020)
WHERE [ModelYear] = 2020 AND [Year] IS NOT NULL

PRINT 'Database schema update completed successfully!'
PRINT 'All missing columns have been added with appropriate defaults and constraints.'

-- Show updated schema summary
PRINT ''
PRINT 'Updated table structures:'
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Routes', 'Drivers', 'Vehicles')
AND COLUMN_NAME IN ('PMDriverId', 'LicenseType', 'PerformanceMetricsJson', 'Make', 'VINNumber', 'ModelYear', 'LastInspectionDate')
ORDER BY TABLE_NAME, COLUMN_NAME
