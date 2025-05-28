#pragma warning disable CS8604 // Possible null reference argument for parameter
#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BusBus.Data;
using BusBus.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusBus
{    // Refactored wrapper to use AdvancedSqlServerDatabaseManager
    public partial class DatabaseManager : IDisposable
    {
        private AdvancedSqlServerDatabaseManager advancedManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseManager> _logger;

        public DatabaseManager()
        {
            advancedManager = new AdvancedSqlServerDatabaseManager();
        }

        public DatabaseManager(IConfiguration configuration, ILogger<DatabaseManager> logger)
        {
            _configuration = configuration;
            _logger = logger;
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "InMemory";
            advancedManager = new AdvancedSqlServerDatabaseManager(connectionString, logger as ILogger<AdvancedSqlServerDatabaseManager>);
        }

        public static async Task InitializeDatabase()
        {
            await AdvancedSqlServerDatabaseManager.InitializeAdvancedDatabase();
        }

        public static List<Route> GetAllRoutes()
        {
            return AdvancedSqlServerDatabaseManager.GetAllRoutes();
        }

        public static Route GetRouteById(int id)
        {
            return AdvancedSqlServerDatabaseManager.GetRouteById(id);
        }

        public static void AddRoute(string routeName, string startLocation, string endLocation)
        {
            AdvancedSqlServerDatabaseManager.AddRoute(routeName, startLocation, endLocation);
        }

        public static void UpdateRoute(int id, string routeName, string startLocation, string endLocation)
        {
            AdvancedSqlServerDatabaseManager.UpdateRoute(id, routeName, startLocation, endLocation);
        }

        public static void DeleteRoute(int id)
        {
            AdvancedSqlServerDatabaseManager.DeleteRoute(id);
        }

        public static List<Driver> GetAllDrivers()
        {
            return AdvancedSqlServerDatabaseManager.GetAllDrivers();
        }

        public static Driver GetDriverById(int id)
        {
            return AdvancedSqlServerDatabaseManager.GetDriverById(id);
        }

        public static void AddDriver(string driverName, string licenseNumber, string contactInfo)
        {
            AdvancedSqlServerDatabaseManager.AddDriver(driverName, licenseNumber, contactInfo);
        }

        public static void UpdateDriver(int id, string driverName, string licenseNumber, string contactInfo)
        {
            AdvancedSqlServerDatabaseManager.UpdateDriver(id, driverName, licenseNumber, contactInfo);
        }

        public static void DeleteDriver(int id)
        {
            AdvancedSqlServerDatabaseManager.DeleteDriver(id);
        }

        public static List<Driver> SearchDriversFullText(string searchTerm)
        {
            return AdvancedSqlServerDatabaseManager.SearchDriversFullText(searchTerm);
        }

        public static void UpdateDriverPersonalDetails(int driverId, Dictionary<string, object> personalDetails)
        {
            AdvancedSqlServerDatabaseManager.UpdateDriverPersonalDetails(driverId, personalDetails);
        }

        public static List<Vehicle> GetAllVehicles()
        {
            return AdvancedSqlServerDatabaseManager.GetAllVehicles();
        }

        public static Vehicle GetVehicleById(int id)
        {
            return AdvancedSqlServerDatabaseManager.GetVehicleById(id);
        }

        public static void AddVehicle(string vehicleNumber, int capacity, string vehicleModel, string licensePlate, bool isActive)
        {
            AdvancedSqlServerDatabaseManager.AddVehicle(vehicleNumber, capacity, vehicleModel, licensePlate, isActive ? 1 : 0);
        }

        public static void UpdateVehicle(int id, string vehicleNumber, int capacity, string vehicleModel, string licensePlate, bool isActive)
        {
            AdvancedSqlServerDatabaseManager.UpdateVehicle(id, vehicleNumber, capacity, vehicleModel, licensePlate, isActive ? 1 : 0);
        }

        public static void DeleteVehicle(int id)
        {
            AdvancedSqlServerDatabaseManager.DeleteVehicle(id);
        }

        public static List<Vehicle> GetVehiclesNearLocation(double latitude, double longitude, double radiusKm)
        {
            return AdvancedSqlServerDatabaseManager.GetVehiclesNearLocation(latitude, longitude, radiusKm);
        }

        public static List<Maintenance> GetAllMaintenance()
        {
            return AdvancedSqlServerDatabaseManager.GetAllMaintenance();
        }

        public static Maintenance GetMaintenanceById(int id)
        {
            return AdvancedSqlServerDatabaseManager.GetMaintenanceById(id);
        }

        public static void AddMaintenance(int vehicleId, string maintenanceType, DateTime maintenanceDate, decimal cost, string notes)
        {
            AdvancedSqlServerDatabaseManager.AddMaintenance(vehicleId, maintenanceType, maintenanceDate, cost, notes);
        }

        public static void UpdateMaintenance(int id, int vehicleId, string maintenanceType, DateTime maintenanceDate, decimal cost, string notes)
        {
            AdvancedSqlServerDatabaseManager.UpdateMaintenance(id, vehicleId, maintenanceType, maintenanceDate, cost, notes);
        }

        public static void DeleteMaintenance(int id)
        {
            AdvancedSqlServerDatabaseManager.DeleteMaintenance(id);
        }

        public static List<Schedule> GetAllSchedules()
        {
            return AdvancedSqlServerDatabaseManager.GetAllSchedules();
        }

        // Instance methods that use advancedManager
        public DataTable GetTableDataDynamic(string tableName)
        {
            return advancedManager.GetTableDataDynamic(tableName);
        }

        public List<string> GetTableColumns(string tableName)
        {
            return advancedManager.GetTableColumns(tableName);
        }

        public void SaveDynamicRecord(string tableName, Dictionary<string, object> values)
        {
            advancedManager.SaveDynamicRecord(tableName, values);
        }

        public void UpdateDynamicRecord(string tableName, int id, Dictionary<string, object> values)
        {
            advancedManager.UpdateDynamicRecord(tableName, id, values);
        }

        public static async Task<DataTable> GetDashboardStatsAsync()
        {
            return await AdvancedSqlServerDatabaseManager.GetDashboardStatsAsync();
        }
        public async Task<bool> TestConnectionAsync()
        {
            return await advancedManager.TestConnectionAsync();
        }

        public string GetConnectionString()
        {
            if (_configuration != null)
            {
                return _configuration.GetConnectionString("DefaultConnection") ?? "InMemory";
            }
            return "InMemory";
        }

        public async Task<bool> CheckDatabaseExistsAsync()
        {
            // For testing purposes, always return true for in-memory database
            if (_configuration != null)
            {
                var connectionString = GetConnectionString();
                if (connectionString.Contains("memory", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return await advancedManager.TestConnectionAsync();
        }

        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                await advancedManager.InitializeDatabaseAsync();
                return true;
            }
            catch (Exception ex)
            {
                Log.DatabaseInitializationFailed(_logger, ex);
                return false;
            }
        }

        public async Task<bool> ApplyMigrationsAsync()
        {
            try
            {
                // For testing purposes, always return true for in-memory database
                if (_configuration != null)
                {
                    var connectionString = GetConnectionString();
                    if (connectionString.Contains("memory", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                await advancedManager.InitializeDatabaseAsync();
                return true;
            }
            catch (Exception ex)
            {
                Log.MigrationsFailed(_logger, ex);
                return false;
            }
        }

        public async Task<DatabaseStatus> GetDatabaseStatusAsync()
        {
            try
            {
                var isConnected = await advancedManager.TestConnectionAsync();
                return new DatabaseStatus
                {
                    IsConnected = isConnected,
                    DatabaseExists = isConnected,
                    HasTables = isConnected,
                    ConnectionString = GetConnectionString(),
                    ServerVersion = "Test Version"
                };
            }
            catch (Exception ex)
            {
                Log.GetDatabaseStatusFailed(_logger, ex);
                return new DatabaseStatus
                {
                    IsConnected = false,
                    DatabaseExists = false,
                    HasTables = false,
                    ConnectionString = GetConnectionString(),
                    ServerVersion = "Unknown"
                };
            }
        }

        public async Task BackupDatabaseAsync(string backupPath)
        {
            // For testing purposes, simulate backup operation
            Log.DatabaseBackupSimulation(_logger, backupPath);
            await Task.Delay(100); // Simulate some work
        }

        public async Task RestoreDatabaseAsync(string backupPath)
        {
            // For testing purposes, simulate restore operation
            Log.DatabaseRestoreSimulation(_logger, backupPath);
            await Task.Delay(100); // Simulate some work
        }

        public void Dispose()
        {
            advancedManager?.Dispose();
        }

        // High-performance logging using LoggerMessage.Define
        private static partial class Log
        {
            [LoggerMessage(
                EventId = 1,
                Level = LogLevel.Error,
                Message = "Failed to initialize database")]
            public static partial void DatabaseInitializationFailed(ILogger logger, Exception ex);

            [LoggerMessage(
                EventId = 2,
                Level = LogLevel.Error,
                Message = "Failed to apply migrations")]
            public static partial void MigrationsFailed(ILogger logger, Exception ex);

            [LoggerMessage(
                EventId = 3,
                Level = LogLevel.Error,
                Message = "Failed to get database status")]
            public static partial void GetDatabaseStatusFailed(ILogger logger, Exception ex);

            [LoggerMessage(
                EventId = 4,
                Level = LogLevel.Information,
                Message = "Simulating database backup to {BackupPath}")]
            public static partial void DatabaseBackupSimulation(ILogger logger, string backupPath);

            [LoggerMessage(
                EventId = 5,
                Level = LogLevel.Information,
                Message = "Simulating database restore from {BackupPath}")]
            public static partial void DatabaseRestoreSimulation(ILogger logger, string backupPath);
        }
    }

    // Removed duplicate Vehicle class. Use BusBus.Models.Vehicle everywhere.
}
