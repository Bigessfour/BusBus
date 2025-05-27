-- Create BusBusDB database
USE master;
GO

-- Drop database if it exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'BusBusDB')
BEGIN
    ALTER DATABASE BusBusDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BusBusDB;
    PRINT 'Existing BusBusDB database dropped.';
END

-- Create new database
CREATE DATABASE BusBusDB;
GO

PRINT 'BusBusDB database created successfully.';

-- Switch to the new database
USE BusBusDB;
GO

PRINT 'Ready for EF Core migrations.';
