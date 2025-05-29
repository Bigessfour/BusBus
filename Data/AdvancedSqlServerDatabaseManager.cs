#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Microsoft.Data.SqlClient;
using BusBus.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Threading;
using Polly;
using Polly.Extensions.Http;

namespace BusBus.Data
{
    /// <summary>
    /// Advanced SQL Server database manager with optimizations for SQL Server Express
    /// including connection pooling, retry logic, and pagination
    /// </summary>
    public partial class AdvancedSqlServerDatabaseManager : IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<AdvancedSqlServerDatabaseManager>? _logger;
        private readonly SemaphoreSlim _connectionSemaphore;
        private readonly IAsyncPolicy _retryPolicy;
        private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);
        private static readonly int MaxConcurrentConnections = 10;

        // Performance tracking
        private readonly Dictionary<string, long> _queryPerformanceMetrics = new();
        private readonly object _metricsLock = new object();

        // Logger message definitions for performance
        private static readonly Action<ILogger, string, long, Exception?> _logQueryPerformance =
            LoggerMessage.Define<string, long>(
                LogLevel.Debug,
                new EventId(1, "QueryPerformance"),
                "Query '{QueryName}' completed in {ElapsedMs}ms");

        private static readonly Action<ILogger, string, int, Exception?> _logRetryAttempt =
            LoggerMessage.Define<string, int>(
                LogLevel.Warning,
                new EventId(2, "RetryAttempt"),
                "Retrying operation '{OperationName}' - Attempt {AttemptNumber}");

        private static readonly Action<ILogger, string, Exception?> _logConnectionPooling =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(3, "ConnectionPooling"),
                "Connection pool status: {Status}");

        public AdvancedSqlServerDatabaseManager()
        {
            _connectionString = @"Server=.\SQLEXPRESS;Database=BusBusDb;Trusted_Connection=true;TrustServerCertificate=true;Connection Timeout=30;Command Timeout=30;";
            _logger = null; // Will use console logging if logger is not available
            _connectionSemaphore = new SemaphoreSlim(MaxConcurrentConnections, MaxConcurrentConnections);
            _retryPolicy = CreateRetryPolicy();
        }

        public AdvancedSqlServerDatabaseManager(string connectionString)
        {
            _connectionString = EnhanceConnectionString(connectionString);
            _connectionSemaphore = new SemaphoreSlim(MaxConcurrentConnections, MaxConcurrentConnections);
            _retryPolicy = CreateRetryPolicy();
        }

        public AdvancedSqlServerDatabaseManager(IConfiguration configuration, ILogger<AdvancedSqlServerDatabaseManager> logger)
        {
            var baseConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? @"Server=.\SQLEXPRESS;Database=BusBusDb;Trusted_Connection=true;TrustServerCertificate=true;";
            _connectionString = EnhanceConnectionString(baseConnectionString);
            _logger = logger;
            _connectionSemaphore = new SemaphoreSlim(MaxConcurrentConnections, MaxConcurrentConnections);
            _retryPolicy = CreateRetryPolicy();
        }

        public AdvancedSqlServerDatabaseManager(string connectionString, ILogger<AdvancedSqlServerDatabaseManager> logger)
        {
            _connectionString = EnhanceConnectionString(connectionString);
            _logger = logger;
            _connectionSemaphore = new SemaphoreSlim(MaxConcurrentConnections, MaxConcurrentConnections);
            _retryPolicy = CreateRetryPolicy();
        }

        /// <summary>
        /// Enhances connection string with SQL Server Express optimizations
        /// </summary>
        private static string EnhanceConnectionString(string baseConnectionString)
        {
            var builder = new SqlConnectionStringBuilder(baseConnectionString)            {
                ConnectTimeout = 30,
                CommandTimeout = 30,
                Pooling = true,
                MinPoolSize = 5,
                MaxPoolSize = 100,
                LoadBalanceTimeout = 5,
                ApplicationName = "BusBus-Application"
            };
            return builder.ConnectionString;
        }        /// <summary>
        /// Creates a retry policy for database operations
        /// </summary>
#pragma warning disable CA1859 // Use concrete types when possible for improved performance - IAsyncPolicy provides flexibility
        private IAsyncPolicy CreateRetryPolicy()
#pragma warning restore CA1859
        {
            return Policy
                .Handle<SqlException>(ex => IsTransientError(ex))
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var operationName = context.GetValueOrDefault("OperationName", "Unknown");
                        if (_logger != null)
                            _logRetryAttempt(_logger, operationName?.ToString() ?? "Unknown", retryCount, null);
                    });
        }

        /// <summary>
        /// Determines if a SQL exception is transient and should be retried
        /// </summary>
        private static bool IsTransientError(SqlException ex)
        {
            // Common transient error numbers for SQL Server Express
            var transientErrors = new[]
            {
                2,      // Timeout
                20,     // Instance failure
                64,     // Connection failed
                233,    // Connection init error
                10053,  // Connection broken
                10054,  // Connection reset
                10060,  // Network unreachable
                40197,  // Service busy
                40501,  // Service busy
                40613   // Database unavailable
            };

            return transientErrors.Contains(ex.Number);
        }

        /// <summary>
        /// Executes a database operation with connection pooling and retry logic
        /// </summary>
        private async Task<T> ExecuteWithOptimizationsAsync<T>(
            string operationName,
            Func<SqlConnection, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();            await _connectionSemaphore.WaitAsync(cancellationToken);
            if (_logger != null)
                _logConnectionPooling(_logger, $"Acquired connection for {operationName}", null);

            try
            {
                var context = new Context(operationName) { ["OperationName"] = operationName };

                return await _retryPolicy.ExecuteAsync(async (ctx) =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync(cancellationToken);
                    return await operation(connection);
                }, context);
            }
            finally
            {                _connectionSemaphore.Release();
                stopwatch.Stop();

                if (_logger != null)
                {
                    _logQueryPerformance(_logger, operationName, stopwatch.ElapsedMilliseconds, null);
                }                lock (_metricsLock)
                {
                    _queryPerformanceMetrics[operationName] = stopwatch.ElapsedMilliseconds;
                }

                if (_logger != null)
                    _logConnectionPooling(_logger, $"Released connection for {operationName}", null);
            }
        }

        /// <summary>
        /// Gets performance metrics for monitoring
        /// </summary>
        public Dictionary<string, long> GetPerformanceMetrics()
        {
            lock (_metricsLock)
            {
                return new Dictionary<string, long>(_queryPerformanceMetrics);
            }
        }

        /// <summary>
        /// Pagination helper for large result sets
        /// </summary>
        public class PagedResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
            public bool HasPreviousPage => PageNumber > 1;
            public bool HasNextPage => PageNumber < TotalPages;
        }

        // --- Synchronous wrappers and legacy signatures for DatabaseManager compatibility ---
        #region DatabaseManager Compatibility
        public static List<Route> GetAllRoutes() { throw new NotImplementedException("GetAllRoutes() needs implementation."); }
        public static Route GetRouteById(int id) { throw new NotImplementedException("GetRouteById(int) needs implementation based on your Route key type."); }
        public static void AddRoute(string routeName, string startLocation, string endLocation) { throw new NotImplementedException("AddRoute(string, string, string) needs implementation."); }
        public static void UpdateRoute(int routeId, string routeName, string startLocation, string endLocation) { throw new NotImplementedException("UpdateRoute(int, ...) needs implementation."); }
        public static void DeleteRoute(int routeId) { throw new NotImplementedException("DeleteRoute(int) needs implementation."); }
        public static List<Driver> GetAllDrivers() { throw new NotImplementedException("GetAllDrivers() needs implementation."); }
        public static Driver GetDriverById(int id) { throw new NotImplementedException("GetDriverById(int) needs implementation."); }
        public static void AddDriver(string driverName, string licenseNumber, string contactInfo) { throw new NotImplementedException("AddDriver(string, ...) needs implementation."); }
        public static void UpdateDriver(int id, string driverName, string licenseNumber, string contactInfo) { throw new NotImplementedException("UpdateDriver(int, ...) needs implementation."); }
        public static void DeleteDriver(int id) { throw new NotImplementedException("DeleteDriver(int) needs implementation."); }
        public static List<Driver> SearchDriversFullText(string searchTerm) { throw new NotImplementedException("SearchDriversFullText(string) needs implementation."); }
        public static void UpdateDriverPersonalDetails(int driverId, Dictionary<string, object> personalDetails) { throw new NotImplementedException("UpdateDriverPersonalDetails(int, ...) needs implementation."); }
        public static List<Vehicle> GetAllVehicles() { throw new NotImplementedException("GetAllVehicles() needs implementation."); }
        public static Vehicle GetVehicleById(int id) { throw new NotImplementedException("GetVehicleById(int) needs implementation."); }
        public static void AddVehicle(string vehicleNumber, int capacity, string vehicleModel, string licensePlate, int isActive) { throw new NotImplementedException("AddVehicle(string, int, string, string, int) needs implementation."); }
        public static void UpdateVehicle(int id, string vehicleNumber, int capacity, string status, string makeModel, int year) { throw new NotImplementedException("UpdateVehicle(int, ...) needs implementation."); }
        public static void DeleteVehicle(int id) { throw new NotImplementedException("DeleteVehicle(int) needs implementation."); }
        public static List<Vehicle> GetVehiclesNearLocation(double latitude, double longitude, double radiusKm) { throw new NotImplementedException("GetVehiclesNearLocation(double, double, double) needs implementation."); }
        public static List<Maintenance> GetAllMaintenance() { throw new NotImplementedException("GetAllMaintenance() needs implementation."); }
        public static Maintenance GetMaintenanceById(int id) { throw new NotImplementedException("GetMaintenanceById(int) needs implementation."); }
        public static void AddMaintenance(int vehicleId, string maintenanceType, DateTime date, decimal cost, string description) { throw new NotImplementedException("AddMaintenance(int, ...) needs implementation."); }
        public static void UpdateMaintenance(int id, int vehicleId, string maintenanceType, DateTime date, decimal cost, string description) { throw new NotImplementedException("UpdateMaintenance(int, ...) needs implementation."); }
        public static void DeleteMaintenance(int maintenanceId) { throw new NotImplementedException("DeleteMaintenance(int) needs implementation."); }
        public static List<Schedule> GetAllSchedules() { throw new NotImplementedException("GetAllSchedules() needs implementation."); }
        public System.Data.DataTable GetTableDataDynamic(string tableName) { throw new NotImplementedException("GetTableDataDynamic(string) needs implementation."); }
        public List<string> GetTableColumns(string tableName) { throw new NotImplementedException("GetTableColumns(string) needs implementation."); }
        public void SaveDynamicRecord(string tableName, Dictionary<string, object> values) { throw new NotImplementedException("SaveDynamicRecord(string, ...) needs implementation."); }
        public void UpdateDynamicRecord(string tableName, int id, Dictionary<string, object> values) { throw new NotImplementedException("UpdateDynamicRecord(string, int, ...) needs implementation."); }
        public static Task<System.Data.DataTable> GetDashboardStatsAsync() { throw new NotImplementedException("GetDashboardStatsAsync() needs implementation."); }
        #endregion        // Driver Management with optimizations
        public async Task<List<Driver>> GetDriversAsync()
        {
            return await ExecuteWithOptimizationsAsync("GetDriversAsync", async connection =>
            {
                var drivers = new List<Driver>();
                const string sql = "SELECT Id, FirstName, LastName, PhoneNumber, Email, LicenseNumber FROM Drivers";
                using var command = new SqlCommand(sql, connection) { CommandTimeout = (int)CommandTimeout.TotalSeconds };
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    drivers.Add(new Driver
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber"))
                    });
                }
                return drivers;
            });
        }

        public async Task<PagedResult<Driver>> GetDriversPagedAsync(int pageNumber = 1, int pageSize = 20)
        {
            return await ExecuteWithOptimizationsAsync("GetDriversPagedAsync", async connection =>
            {
                var result = new PagedResult<Driver>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                // Get total count
                const string countSql = "SELECT COUNT(*) FROM Drivers";
                using (var countCommand = new SqlCommand(countSql, connection))
                {
                    var countResult = await countCommand.ExecuteScalarAsync();
                    result.TotalCount = countResult != null ? (int)countResult : 0;
                }

                // Get paged results
                const string sql = @"
                    SELECT Id, FirstName, LastName, PhoneNumber, Email, LicenseNumber
                    FROM Drivers
                    ORDER BY LastName, FirstName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                using var command = new SqlCommand(sql, connection) { CommandTimeout = (int)CommandTimeout.TotalSeconds };
                command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                command.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new Driver
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber"))                    });
                }

                return result;
            });
        }

        public async Task<Driver?> GetDriverByIdAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "SELECT Id, FirstName, LastName, PhoneNumber, Email, LicenseNumber FROM Drivers WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Driver
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                    LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber"))
                };
            }

            return null;
        }

        public async Task AddDriverAsync(Driver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = @"INSERT INTO Drivers (Id, FirstName, LastName, PhoneNumber, Email, LicenseNumber) VALUES (@Id, @FirstName, @LastName, @PhoneNumber, @Email, @LicenseNumber)";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", driver.Id);
            command.Parameters.AddWithValue("@FirstName", driver.FirstName);
            command.Parameters.AddWithValue("@LastName", driver.LastName);
            command.Parameters.AddWithValue("@PhoneNumber", (object?)driver.PhoneNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)driver.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@LicenseNumber", driver.LicenseNumber);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateDriverAsync(Driver driver)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"UPDATE Drivers
                               SET FirstName = @FirstName, LastName = @LastName, LicenseNumber = @LicenseNumber, PhoneNumber = @PhoneNumber, Email = @Email
                               WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", driver.Id);
            command.Parameters.AddWithValue("@FirstName", driver.FirstName);
            command.Parameters.AddWithValue("@LastName", driver.LastName);
            command.Parameters.AddWithValue("@LicenseNumber", driver.LicenseNumber);
            command.Parameters.AddWithValue("@PhoneNumber", driver.PhoneNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Email", driver.Email ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteDriverAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "DELETE FROM Drivers WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        // Vehicle Management
        public async Task<List<Vehicle>> GetVehiclesAsync()
        {
            var vehicles = new List<Vehicle>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = "SELECT Id, Number FROM Vehicles";
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                vehicles.Add(new Vehicle
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Number = reader.GetString(reader.GetOrdinal("Number"))
                });
            }
            return vehicles;
        }

        public async Task<Vehicle> GetVehicleByIdAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "SELECT Id, Number FROM Vehicles WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync(); if (await reader.ReadAsync())
            {
                return new Vehicle
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Number = reader.GetString(reader.GetOrdinal("Number"))
                };
            }

            return null;
        }

        public async Task AddVehicleAsync(Vehicle vehicle)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = @"INSERT INTO Vehicles (Id, Number) VALUES (@Id, @Number)";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", vehicle.Id);
            command.Parameters.AddWithValue("@Number", vehicle.Number);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateVehicleAsync(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"UPDATE Vehicles
                               SET Number = @Number
                               WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", vehicle.Id);
            command.Parameters.AddWithValue("@Number", vehicle.Number);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteVehicleAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "DELETE FROM Vehicles WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        // Route Management
        public async Task<List<Route>> GetRoutesAsync()
        {
            var routes = new List<Route>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = "SELECT Id, Name, RouteDate FROM Routes";
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                routes.Add(new Route
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    RouteDate = reader.GetDateTime(reader.GetOrdinal("RouteDate"))
                });
            }
            return routes;
        }

        public async Task<Route> GetRouteByIdAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "SELECT Id, Name, RouteDate FROM Routes WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Route
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    RouteDate = reader.GetDateTime(reader.GetOrdinal("RouteDate"))
                };
            }

            return null;
        }

        public async Task AddRouteAsync(Route route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = @"INSERT INTO Routes (Id, Name, RouteDate) VALUES (@Id, @Name, @RouteDate)";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", route.Id);
            command.Parameters.AddWithValue("@Name", route.Name);
            command.Parameters.AddWithValue("@RouteDate", route.RouteDate);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateRouteAsync(Route route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"UPDATE Routes
                               SET Name = @Name, RouteDate = @RouteDate
                               WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", route.Id);
            command.Parameters.AddWithValue("@Name", route.Name);
            command.Parameters.AddWithValue("@RouteDate", route.RouteDate);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteRouteAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "DELETE FROM Routes WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        // Maintenance Management
        public async Task<List<Maintenance>> GetMaintenanceRecordsAsync()
        {
            var records = new List<Maintenance>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"SELECT Id, VehicleId, MaintenanceDate, Description, Cost
                               FROM Maintenance ORDER BY MaintenanceDate DESC";
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new Maintenance
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
                    MaintenanceDate = reader.GetDateTime(reader.GetOrdinal("MaintenanceDate")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    Cost = reader.GetDecimal(reader.GetOrdinal("Cost"))
                });
            }

            return records;
        }

        public async Task<int> AddMaintenanceRecordAsync(Maintenance maintenance)
        {
            if (maintenance == null)
            {
                throw new ArgumentNullException(nameof(maintenance));
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"INSERT INTO Maintenance (VehicleId, MaintenanceDate, Description, Cost)
                               VALUES (@VehicleId, @MaintenanceDate, @Description, @Cost);
                               SELECT SCOPE_IDENTITY();";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@VehicleId", maintenance.VehicleId);
            command.Parameters.AddWithValue("@MaintenanceDate", maintenance.MaintenanceDate);
            command.Parameters.AddWithValue("@Description", maintenance.Description);
            command.Parameters.AddWithValue("@Cost", maintenance.Cost);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Schedule Management
        public async Task<List<Schedule>> GetSchedulesAsync()
        {
            var schedules = new List<Schedule>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"SELECT Id, RouteId, VehicleId, DriverId, DepartureTime, ArrivalTime, ScheduleDate
                FROM Schedules ORDER BY ScheduleDate, DepartureTime";
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                schedules.Add(new Schedule
                {
                    Id = reader.GetInt32(0),
                    RouteId = reader.GetInt32(1),
                    VehicleId = reader.GetInt32(2),
                    DriverId = reader.GetInt32(3),
                    DepartureTime = reader.GetTimeSpan(4),
                    ArrivalTime = reader.GetTimeSpan(5),
                    ScheduleDate = reader.GetDateTime(6)
                });
            }

            return schedules;
        }

        public async Task<int> AddScheduleAsync(Schedule schedule)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"INSERT INTO Schedules (RouteId, VehicleId, DriverId, DepartureTime, ArrivalTime, ScheduleDate)
                               VALUES (@RouteId, @VehicleId, @DriverId, @DepartureTime, @ArrivalTime, @ScheduleDate);
                               SELECT SCOPE_IDENTITY();";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RouteId", schedule.RouteId);
            command.Parameters.AddWithValue("@VehicleId", schedule.VehicleId);
            command.Parameters.AddWithValue("@DriverId", schedule.DriverId);
            command.Parameters.AddWithValue("@DepartureTime", schedule.DepartureTime);
            command.Parameters.AddWithValue("@ArrivalTime", schedule.ArrivalTime);
            command.Parameters.AddWithValue("@ScheduleDate", schedule.ScheduleDate);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }        // Database Management
        public async Task<bool> TestConnectionAsync()
        {
            try
            {                // Ensure _logger is initialized to prevent NullReferenceException
                if (_logger == null)
                {
                    // Can't modify readonly field, so just use local logger
                    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    var logger = loggerFactory.CreateLogger<AdvancedSqlServerDatabaseManager>();

                    try
                    {
                        using var conn = new SqlConnection(_connectionString);
                        await conn.OpenAsync();
                        return conn.State == System.Data.ConnectionState.Open;
                    }
                    catch (Exception e)
                    {
                        s_databaseConnectionTestFailed(logger, e);
                        return false;
                    }
                }

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                return connection.State == System.Data.ConnectionState.Open;
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    s_databaseConnectionTestFailed(_logger, ex);
                }
                return false;
            }
        }

        /// <summary>
        /// Executes a SQL command that doesn't return a result set
        /// </summary>
        /// <param name="sql">The SQL command to execute</param>
        /// <returns>The number of rows affected</returns>
        public async Task<int> ExecuteNonQueryAsync(string sql)
        {
            return await ExecuteWithOptimizationsAsync($"ExecuteNonQuery", async connection =>
            {
                using var command = new SqlCommand(sql, connection)
                {
                    CommandTimeout = (int)CommandTimeout.TotalSeconds
                };

                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task InitializeDatabaseAsync()
        {
            // Get or create a logger
            ILogger localLogger;
            if (_logger != null)
            {
                localLogger = _logger;
            }
            else
            {
                var factory = LoggerFactory.Create(builder => builder.AddConsole());
                localLogger = factory.CreateLogger<AdvancedSqlServerDatabaseManager>();
            }

            try
            {
                s_databaseSchemaInitializing(localLogger, null);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Create tables if they don't exist
                var createTablesScript = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Drivers' AND xtype='U')
                CREATE TABLE Drivers (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    LicenseNumber NVARCHAR(50) NOT NULL UNIQUE,
                    PhoneNumber NVARCHAR(20)
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Routes' AND xtype='U')
                CREATE TABLE Routes (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    StartLocation NVARCHAR(100) NOT NULL,
                    EndLocation NVARCHAR(100) NOT NULL,
                    Distance DECIMAL(10,2) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Vehicles' AND xtype='U')
                CREATE TABLE Vehicles (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    LicensePlate NVARCHAR(20) NOT NULL UNIQUE,
                    Model NVARCHAR(50) NOT NULL,
                    Capacity INT NOT NULL,
                    Year INT NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Maintenance' AND xtype='U')
                CREATE TABLE Maintenance (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    VehicleId INT NOT NULL,
                    MaintenanceDate DATETIME NOT NULL,
                    Description NVARCHAR(500) NOT NULL,
                    Cost DECIMAL(10,2) NOT NULL,
                    FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Schedules' AND xtype='U')
                CREATE TABLE Schedules (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    RouteId INT NOT NULL,
                    VehicleId INT NOT NULL,
                    DriverId INT NOT NULL,
                    DepartureTime TIME NOT NULL,
                    ArrivalTime TIME NOT NULL,
                    ScheduleDate DATE NOT NULL,
                    FOREIGN KEY (RouteId) REFERENCES Routes(Id),
                    FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id),
                    FOREIGN KEY (DriverId) REFERENCES Drivers(Id)                );";

                using var command = new SqlCommand(createTablesScript, connection);
                await command.ExecuteNonQueryAsync();

                s_databaseSchemaInitialized(localLogger, null);
            }
            catch (Exception ex)
            {
                // Log the exception
                s_databaseConnectionTestFailed(localLogger, ex);
            }
        }

        public static Task InitializeAdvancedDatabase()
        {
            // Add implementation for database initialization
            return Task.CompletedTask;
        }
        public static Task<MigrationStatus> CheckMigrationStatusAsync()
        {
            // For testing purposes, return a dummy migration status
            return Task.FromResult(new MigrationStatus
            {
                PendingMigrations = new List<string>(),
                AppliedMigrations = new List<string> { "20250523002503_InitialSetup", "20250524104940_AddRouteLocationsAndScheduledTime" },
                LastMigrationDate = DateTime.UtcNow.AddDays(-1)
            });
        }
        public static Task<DatabaseSizeInfo> GetDatabaseSizeInfoAsync()
        {
            // For testing purposes, return dummy size information
            return Task.FromResult(new DatabaseSizeInfo
            {
                DatabaseName = "BusBusDb",
                DataFileSize = 10.5m,
                LogFileSize = 2.1m,
                TotalSize = 12.6m
            });
        }
        public static Task<Dictionary<string, TableStatistics>> GetTableStatisticsAsync()
        {
            // For testing purposes, return dummy table statistics
            return Task.FromResult(new Dictionary<string, TableStatistics>
            {
                ["Routes"] = new TableStatistics { TableName = "Routes", RowCount = 5, DataSize = 1024 },
                ["Drivers"] = new TableStatistics { TableName = "Drivers", RowCount = 10, DataSize = 2048 },
                ["Vehicles"] = new TableStatistics { TableName = "Vehicles", RowCount = 8, DataSize = 1536 }
            });
        }

        public static ConnectionStringComponents ParseConnectionString(string connectionString)
        {
            var components = new ConnectionStringComponents();
            if (string.IsNullOrEmpty(connectionString))
                return components;

            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    var value = keyValue[1].Trim();

                    switch (key)
                    {
                        case "server":
                        case "data source":
                            components.Server = value;
                            break;
                        case "database":
                        case "initial catalog":
                            components.Database = value;
                            break;
                        case "trusted_connection":
                            components.IntegratedSecurity = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "user id":
                            components.UserId = value;
                            break;
                    }
                }
            }
            return components;
        }
        public static Task<List<DatabaseIndex>> GetDatabaseIndexesAsync()
        {
            // For testing purposes, return dummy index information
            return Task.FromResult(new List<DatabaseIndex>
            {
                new DatabaseIndex { IndexName = "PK_Routes", TableName = "Routes", IndexType = "PRIMARY KEY" },
                new DatabaseIndex { IndexName = "PK_Drivers", TableName = "Drivers", IndexType = "PRIMARY KEY" },
                new DatabaseIndex { IndexName = "PK_Vehicles", TableName = "Vehicles", IndexType = "PRIMARY KEY" }
            });
        }
        public static Task<DatabaseHealth> CheckDatabaseHealthAsync()
        {
            // For testing purposes, return dummy health status
            return Task.FromResult(new DatabaseHealth
            {
                IsHealthy = true,
                Metrics = new Dictionary<string, object>
                {
                    ["ConnectionCount"] = 5,
                    ["AvgResponseTime"] = 25.5,
                    ["TotalQueries"] = 1000
                },
                Recommendations = new List<string>
                {
                    "Database is running optimally",
                    "Consider indexing frequently queried columns"
                }
            });
        }

        // High-performance logging using LoggerMessage
        private static readonly Action<ILogger, Exception?> s_databaseConnectionTestFailed =
            LoggerMessage.Define(LogLevel.Error, new EventId(201, "DatabaseConnectionTestFailed"),
                "Database connection test failed");

        private static readonly Action<ILogger, Exception?> s_databaseSchemaInitializing =
            LoggerMessage.Define(LogLevel.Information, new EventId(202, "DatabaseSchemaInitializing"),
                "Initializing database schema...");

        private static readonly Action<ILogger, Exception?> s_databaseSchemaInitialized =
            LoggerMessage.Define(LogLevel.Information, new EventId(203, "DatabaseSchemaInitialized"),
                "Database schema initialized successfully");        public void Dispose()
        {
            _connectionSemaphore?.Dispose();            // Log performance metrics summary            if (_logger != null && _queryPerformanceMetrics.Count > 0)
            {
#pragma warning disable CA1848 // Use LoggerMessage delegates for performance - minimal impact in this context
                _logger?.LogInformation("Database Manager Performance Summary:");
#pragma warning restore CA1848
                lock (_metricsLock)
                {                    foreach (var metric in _queryPerformanceMetrics)
                    {
#pragma warning disable CA1848 // Use LoggerMessage delegates for performance - minimal impact in this context
                        _logger?.LogInformation("  {Operation}: {ElapsedMs}ms", metric.Key, metric.Value);
#pragma warning restore CA1848
                    }
                }
            }
        }
    }
}
