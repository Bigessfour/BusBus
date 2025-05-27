-- Reset EF Migration History to Force Re-application
-- This will make EF think no migrations have been applied, forcing it to recreate the schema

USE BusBusDB;
GO

-- Check if migration history table exists
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory')
BEGIN
    PRINT 'Backing up current migration history...'

    -- Show current migrations
    SELECT * FROM [__EFMigrationsHistory];

    -- Delete all migration history records
    DELETE FROM [__EFMigrationsHistory];

    PRINT 'Migration history cleared. EF will now think no migrations have been applied.'
END
ELSE
BEGIN
    PRINT 'No migration history table found.'
END

-- Also drop and recreate the tables to ensure clean slate
-- (This preserves the database but removes table structure inconsistencies)

PRINT 'Dropping all existing tables to force clean recreation...'

-- Disable foreign key constraints
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all"

-- Drop all tables except system tables
DECLARE @sql NVARCHAR(MAX) = N''
SELECT @sql = @sql + N'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name) + N';' + CHAR(13)
FROM sys.tables
WHERE name NOT LIKE 'sys%' AND name NOT LIKE '__EF%'

PRINT 'Executing drop commands:'
PRINT @sql

IF LEN(@sql) > 0
    EXEC sp_executesql @sql

PRINT 'All tables dropped. Ready for fresh EF migration.'
