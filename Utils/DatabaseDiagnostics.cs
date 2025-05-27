// <auto-added>
#nullable enable
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BusBus.Utils
{
    /// <summary>
    /// Utility for diagnosing database connection issues
    /// </summary>
    public static partial class DatabaseDiagnostics // Added partial modifier
    {
        /// <summary>
        /// Tests database connection and logs detailed diagnostics
        /// </summary>
        public static async Task<bool> TestConnectionAsync(
            string connectionString,
            ILogger logger,
            bool logDetails = true)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                Log.ConnectionStringNullOrEmpty(logger, null);
                return false;
            }

            if (logDetails)
            {
                // Log connection string with sensitive parts masked
                var maskedConnStr = MaskConnectionString(connectionString);
                Log.TestingConnection(logger, maskedConnStr, null);
            }

            var stopwatch = Stopwatch.StartNew();
            var success = false;

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                stopwatch.Stop();
                success = true;

                Log.ConnectionSuccessful(logger, stopwatch.ElapsedMilliseconds, connection.ServerVersion, connection.State.ToString(), null);

                // Test a simple query if connected
                if (connection.State == ConnectionState.Open)
                {
                    await TestQueryAsync(connection, logger);
                }
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                Log.SqlExceptionConnecting(logger, stopwatch.ElapsedMilliseconds, sqlEx.Number, sqlEx.Class, sqlEx);

                // Log more detailed information for specific error codes
                LogSqlErrorDetails(sqlEx, logger);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.ExceptionConnecting(logger, stopwatch.ElapsedMilliseconds, ex);
            }

            return success;
        }

        /// <summary>
        /// Tests Entity Framework connection
        /// </summary>
        public static async Task<bool> TestEfConnectionAsync(DbContext dbContext, ILogger logger)
        {
            if (dbContext == null)
            {
                Log.DbContextNull(logger, null);
                return false;
            }

            var stopwatch = Stopwatch.StartNew();
            var success = false;

            try
            {
                Log.TestingEfCoreConnection(logger, null);

                // Test if database exists
                var dbExists = await dbContext.Database.CanConnectAsync();

                if (dbExists)
                {
                    // Try to execute a simple command
                    await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");

                    stopwatch.Stop();
                    success = true;

                    Log.EfCoreConnectionSuccessful(logger, stopwatch.ElapsedMilliseconds, dbContext.Database.ProviderName ?? "Unknown", null);
                }
                else
                {
                    stopwatch.Stop();
                    Log.CannotConnectEfCore(logger, stopwatch.ElapsedMilliseconds, null);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.ExceptionEfCoreConnection(logger, stopwatch.ElapsedMilliseconds, ex);
            }

            return success;
        }

        /// <summary>
        /// Executes a simple test query to verify connection health
        /// </summary>
        private static async Task TestQueryAsync(SqlConnection connection, ILogger logger)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT @@VERSION";

                var result = await command.ExecuteScalarAsync();
                stopwatch.Stop();

                if (result != null)
                {
                    Log.TestQuerySuccessful(logger, stopwatch.ElapsedMilliseconds, null);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.ErrorExecutingTestQuery(logger, stopwatch.ElapsedMilliseconds, ex);
            }
        }

        /// <summary>
        /// Provides more detailed diagnostic information for specific SQL error codes
        /// </summary>
        private static void LogSqlErrorDetails(SqlException sqlEx, ILogger logger)
        {
            switch (sqlEx.Number)
            {
                case 4060: // Cannot open database requested
                    Log.DatabaseAccessDenied(logger, sqlEx);
                    break;

                case 18456: // Login failed
                    Log.LoginFailed(logger, sqlEx);
                    break;

                case 40: // Network connection issue
                    Log.NetworkConnectionIssue(logger, sqlEx);
                    break;

                case 53: // Server not found
                    Log.ServerNotFound(logger, sqlEx);
                    break;

                case 1205: // Deadlock victim
                    Log.DeadlockVictim(logger, sqlEx);
                    break;

                case 2:
                case 10054: // Connection forcibly closed
                    Log.ConnectionForciblyClosed(logger, sqlEx);
                    break;

                case 121: // Lease connection error
                    Log.LeaseConnectionError(logger, sqlEx);
                    break;

                default:
                    Log.UnknownSqlError(logger, sqlEx.Number, sqlEx.Message, sqlEx);
                    break;
            }
        }

        /// <summary>
        /// Masks sensitive information in a connection string
        /// </summary>
        [GeneratedRegex("(Password|Pwd|User ID|Uid)=[^;]+", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex ConnectionStringMaskRegex(); // SYSLIB1045

        private static string MaskConnectionString(string connectionString)
        {
            return ConnectionStringMaskRegex().Replace(connectionString, "$1=********");
        }

        // Nested class for LoggerMessage.Define
        private static partial class Log
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "[DB-DIAG] Connection string is null or empty")]
            public static partial void ConnectionStringNullOrEmpty(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "[DB-DIAG] Testing connection: {ConnectionString}")]
            public static partial void TestingConnection(ILogger logger, string connectionString, Exception? ex);

            [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "[DB-DIAG] Connection successful in {Duration}ms | Server Version: {ServerVersion} | State: {State}")]
            public static partial void ConnectionSuccessful(ILogger logger, long duration, string serverVersion, string state, Exception? ex);

            [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "[DB-DIAG] SQL Exception connecting to database after {Duration}ms | Error: {ErrorCode} | Severity: {Severity}")]
            public static partial void SqlExceptionConnecting(ILogger logger, long duration, int errorCode, byte severity, Exception? ex);

            [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "[DB-DIAG] Exception connecting to database after {Duration}ms")]
            public static partial void ExceptionConnecting(ILogger logger, long duration, Exception? ex);

            [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "[DB-DIAG] DbContext is null")]
            public static partial void DbContextNull(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "[DB-DIAG] Testing EF Core database connection...")]
            public static partial void TestingEfCoreConnection(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "[DB-DIAG] EF Core connection successful in {Duration}ms | Provider: {Provider}")]
            public static partial void EfCoreConnectionSuccessful(ILogger logger, long duration, string provider, Exception? ex);

            [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "[DB-DIAG] Cannot connect to database using EF Core after {Duration}ms")]
            public static partial void CannotConnectEfCore(ILogger logger, long duration, Exception? ex);

            [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "[DB-DIAG] Exception in EF Core database connection after {Duration}ms")]
            public static partial void ExceptionEfCoreConnection(ILogger logger, long duration, Exception? ex);

            [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "[DB-DIAG] Test query successful in {Duration}ms | SQL Server version detected")]
            public static partial void TestQuerySuccessful(ILogger logger, long duration, Exception? ex);

            [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "[DB-DIAG] Error executing test query after {Duration}ms")]
            public static partial void ErrorExecutingTestQuery(ILogger logger, long duration, Exception? ex);

            [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "[DB-DIAG] Database does not exist or access denied. Check database name.")]
            public static partial void DatabaseAccessDenied(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "[DB-DIAG] Login failed. Check username and password.")]
            public static partial void LoginFailed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 14, Level = LogLevel.Error, Message = "[DB-DIAG] Network connection issue. Check server name and firewall settings.")]
            public static partial void NetworkConnectionIssue(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 15, Level = LogLevel.Error, Message = "[DB-DIAG] Server not found. Check server name and ensure SQL Server is running.")]
            public static partial void ServerNotFound(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 16, Level = LogLevel.Error, Message = "[DB-DIAG] Transaction was deadlocked and chosen as victim.")]
            public static partial void DeadlockVictim(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 17, Level = LogLevel.Error, Message = "[DB-DIAG] Connection was forcibly closed. SQL Server may be restarting or under heavy load.")]
            public static partial void ConnectionForciblyClosed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 18, Level = LogLevel.Error, Message = "[DB-DIAG] SQL Server service has been paused or is shutting down.")]
            public static partial void LeaseConnectionError(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 19, Level = LogLevel.Error, Message = "[DB-DIAG] Unknown SQL Error Code: {ErrorCode} - {ErrorMessage}")]
            public static partial void UnknownSqlError(ILogger logger, int errorCode, string errorMessage, Exception? ex);
        }
    }
}
