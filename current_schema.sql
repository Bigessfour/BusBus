CREATE TABLE [CustomFields] (
    [Name] nvarchar(450) NOT NULL,
    [Label] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Required] bit NOT NULL,
    [DefaultValue] nvarchar(max) NULL,
    CONSTRAINT [PK_CustomFields] PRIMARY KEY ([Name])
);
GO


CREATE TABLE [Drivers] (
    [Id] uniqueidentifier NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [LicenseNumber] nvarchar(max) NOT NULL,
    [DriverID] int NOT NULL,
    [DriverName] nvarchar(max) NOT NULL,
    [ContactInfo] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [HireDate] datetime2 NOT NULL,
    [LastPerformanceReview] datetime2 NULL,
    [SalaryGrade] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [RowVersion] varbinary(max) NOT NULL,
    [EmergencyContactJson] nvarchar(max) NOT NULL,
    [PersonalDetails] nvarchar(max) NOT NULL,
    [PerformanceScore] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_Drivers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Vehicles] (
    [Id] uniqueidentifier NOT NULL,
    [VehicleId] int NOT NULL,
    [VehicleGuid] uniqueidentifier NOT NULL,
    [Number] nvarchar(max) NOT NULL,
    [BusNumber] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NULL,
    [Capacity] int NOT NULL,
    [Model] nvarchar(max) NULL,
    [LicensePlate] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [MakeModel] nvarchar(max) NOT NULL,
    [Year] int NULL,
    [Mileage] decimal(18,2) NOT NULL,
    [FuelType] nvarchar(max) NOT NULL,
    [IsMaintenanceRequired] bit NOT NULL,
    [LastMaintenanceDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [RowVersion] varbinary(max) NULL,
    [NextMaintenanceDate] datetime2 NULL,
    [VehicleCode] nvarchar(max) NULL,
    [MaintenanceDue] bit NOT NULL,
    [Latitude] float NULL,
    [Longitude] float NULL,
    [LastLocationUpdate] datetime2 NULL,
    [MaintenanceHistoryJson] nvarchar(max) NOT NULL,
    [SpecificationsJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Routes] (
    [Id] uniqueidentifier NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [RouteDate] datetime2 NOT NULL,
    [AMStartingMileage] int NOT NULL,
    [AMEndingMileage] int NOT NULL,
    [AMRiders] int NOT NULL,
    [PMStartMileage] int NOT NULL,
    [PMEndingMileage] int NOT NULL,
    [PMRiders] int NOT NULL,
    [DriverId] uniqueidentifier NULL,
    [VehicleId] uniqueidentifier NULL,
    [StartLocation] nvarchar(max) NOT NULL,
    [EndLocation] nvarchar(max) NOT NULL,
    [ScheduledTime] datetime2 NOT NULL,
    [RouteID] int NOT NULL,
    [RouteName] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [RouteCode] nvarchar(max) NOT NULL,
    [Distance] int NOT NULL,
    [StopsJson] nvarchar(max) NOT NULL,
    [ScheduleJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Routes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Routes_Drivers_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Drivers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Routes_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE SET NULL
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ContactInfo', N'CreatedBy', N'CreatedDate', N'DriverID', N'DriverName', N'Email', N'EmergencyContactJson', N'FirstName', N'HireDate', N'LastName', N'LastPerformanceReview', N'LicenseNumber', N'ModifiedDate', N'Name', N'PerformanceScore', N'PersonalDetails', N'PhoneNumber', N'RowVersion', N'SalaryGrade', N'Status') AND [object_id] = OBJECT_ID(N'[Drivers]'))
    SET IDENTITY_INSERT [Drivers] ON;
INSERT INTO [Drivers] ([Id], [ContactInfo], [CreatedBy], [CreatedDate], [DriverID], [DriverName], [Email], [EmergencyContactJson], [FirstName], [HireDate], [LastName], [LastPerformanceReview], [LicenseNumber], [ModifiedDate], [Name], [PerformanceScore], [PersonalDetails], [PhoneNumber], [RowVersion], [SalaryGrade], [Status])
VALUES ('11111111-1111-1111-1111-111111111111', N'', N'', '0001-01-01T00:00:00.0000000', 0, N'', NULL, N'', N'John', '0001-01-01T00:00:00.0000000', N'Smith', NULL, N'DL123456', '0001-01-01T00:00:00.0000000', N'John Smith', 5.0, N'', NULL, 0x, 0, N'Active'),
('22222222-2222-2222-2222-222222222222', N'', N'', '0001-01-01T00:00:00.0000000', 0, N'', NULL, N'', N'Mary', '0001-01-01T00:00:00.0000000', N'Johnson', NULL, N'DL234567', '0001-01-01T00:00:00.0000000', N'Mary Johnson', 5.0, N'', NULL, 0x, 0, N'Active'),
('33333333-3333-3333-3333-333333333333', N'', N'', '0001-01-01T00:00:00.0000000', 0, N'', NULL, N'', N'Robert', '0001-01-01T00:00:00.0000000', N'Brown', NULL, N'DL345678', '0001-01-01T00:00:00.0000000', N'Robert Brown', 5.0, N'', NULL, 0x, 0, N'Active'),
('44444444-4444-4444-4444-444444444444', N'', N'', '0001-01-01T00:00:00.0000000', 0, N'', NULL, N'', N'Lisa', '0001-01-01T00:00:00.0000000', N'Davis', NULL, N'DL456789', '0001-01-01T00:00:00.0000000', N'Lisa Davis', 5.0, N'', NULL, 0x, 0, N'Active'),
('55555555-5555-5555-5555-555555555555', N'', N'', '0001-01-01T00:00:00.0000000', 0, N'', NULL, N'', N'Michael', '0001-01-01T00:00:00.0000000', N'Wilson', NULL, N'DL567890', '0001-01-01T00:00:00.0000000', N'Michael Wilson', 5.0, N'', NULL, 0x, 0, N'Active');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ContactInfo', N'CreatedBy', N'CreatedDate', N'DriverID', N'DriverName', N'Email', N'EmergencyContactJson', N'FirstName', N'HireDate', N'LastName', N'LastPerformanceReview', N'LicenseNumber', N'ModifiedDate', N'Name', N'PerformanceScore', N'PersonalDetails', N'PhoneNumber', N'RowVersion', N'SalaryGrade', N'Status') AND [object_id] = OBJECT_ID(N'[Drivers]'))
    SET IDENTITY_INSERT [Drivers] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BusNumber', N'Capacity', N'CreatedDate', N'FuelType', N'IsActive', N'IsMaintenanceRequired', N'LastLocationUpdate', N'LastMaintenanceDate', N'Latitude', N'LicensePlate', N'Longitude', N'MaintenanceDue', N'MaintenanceHistoryJson', N'MakeModel', N'Mileage', N'Model', N'ModifiedDate', N'Name', N'NextMaintenanceDate', N'Number', N'RowVersion', N'SpecificationsJson', N'Status', N'VehicleCode', N'VehicleGuid', N'VehicleId', N'Year') AND [object_id] = OBJECT_ID(N'[Vehicles]'))
    SET IDENTITY_INSERT [Vehicles] ON;
INSERT INTO [Vehicles] ([Id], [BusNumber], [Capacity], [CreatedDate], [FuelType], [IsActive], [IsMaintenanceRequired], [LastLocationUpdate], [LastMaintenanceDate], [Latitude], [LicensePlate], [Longitude], [MaintenanceDue], [MaintenanceHistoryJson], [MakeModel], [Mileage], [Model], [ModifiedDate], [Name], [NextMaintenanceDate], [Number], [RowVersion], [SpecificationsJson], [Status], [VehicleCode], [VehicleGuid], [VehicleId], [Year])
VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', N'101', 72, '2025-05-26T12:20:54.7500189-06:00', N'', CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, NULL, N'BUS-101', NULL, CAST(0 AS bit), N'', N'', 0.0, N'Blue Bird All American FE', '2025-05-26T12:20:54.7500254-06:00', NULL, NULL, N'101', NULL, N'', N'Available', NULL, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 0, NULL),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', N'102', 66, '2025-05-26T12:20:54.7500263-06:00', N'', CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, NULL, N'BUS-102', NULL, CAST(0 AS bit), N'', N'', 0.0, N'Thomas C2 Jouley', '2025-05-26T12:20:54.7500272-06:00', NULL, NULL, N'102', NULL, N'', N'Available', NULL, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 0, NULL),
('cccccccc-cccc-cccc-cccc-cccccccccccc', N'103', 78, '2025-05-26T12:20:54.7500295-06:00', N'', CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, NULL, N'BUS-103', NULL, CAST(0 AS bit), N'', N'', 0.0, N'IC Bus CE Series', '2025-05-26T12:20:54.7500297-06:00', NULL, NULL, N'103', NULL, N'', N'Available', NULL, 'cccccccc-cccc-cccc-cccc-cccccccccccc', 0, NULL),
('dddddddd-dddd-dddd-dddd-dddddddddddd', N'104', 72, '2025-05-26T12:20:54.7500300-06:00', N'', CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, NULL, N'BUS-104', NULL, CAST(0 AS bit), N'', N'', 0.0, N'Blue Bird Vision', '2025-05-26T12:20:54.7500302-06:00', NULL, NULL, N'104', NULL, N'', N'Available', NULL, 'dddddddd-dddd-dddd-dddd-dddddddddddd', 0, NULL),
('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', N'105', 90, '2025-05-26T12:20:54.7500304-06:00', N'', CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, NULL, N'BUS-105', NULL, CAST(0 AS bit), N'', N'', 0.0, N'Thomas HDX', '2025-05-26T12:20:54.7500305-06:00', NULL, NULL, N'105', NULL, N'', N'Available', NULL, 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 0, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BusNumber', N'Capacity', N'CreatedDate', N'FuelType', N'IsActive', N'IsMaintenanceRequired', N'LastLocationUpdate', N'LastMaintenanceDate', N'Latitude', N'LicensePlate', N'Longitude', N'MaintenanceDue', N'MaintenanceHistoryJson', N'MakeModel', N'Mileage', N'Model', N'ModifiedDate', N'Name', N'NextMaintenanceDate', N'Number', N'RowVersion', N'SpecificationsJson', N'Status', N'VehicleCode', N'VehicleGuid', N'VehicleId', N'Year') AND [object_id] = OBJECT_ID(N'[Vehicles]'))
    SET IDENTITY_INSERT [Vehicles] OFF;
GO


CREATE INDEX [IX_Routes_DriverId] ON [Routes] ([DriverId]);
GO


CREATE INDEX [IX_Routes_VehicleId] ON [Routes] ([VehicleId]);
GO


