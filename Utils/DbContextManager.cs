// <auto-added>
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using BusBus.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusBus.Utils
{
    /// <summary>
    /// Custom exception for DbContextManager errors.
    /// </summary>
    public class DbContextManagerException : Exception
    {
        public DbContextManagerException() : base() { }
        public DbContextManagerException(string message) : base(message) { }
        public DbContextManagerException(string message, Exception? innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Manages database contexts to ensure proper scope and disposal
    /// </summary>
    public partial class DbContextManager : IDisposable // Added partial modifier
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DbContextManager> _logger; // Changed to ILogger<DbContextManager>
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private IServiceScope? _currentScope;
        private AppDbContext? _dbContext;
        private bool _disposed;

        /// <summary>
        /// Gets the current DbContext, creating a new one if needed
        /// </summary>
        public AppDbContext Context => GetOrCreateContext();

        /// <summary>
        /// Create a new DbContextManager
        /// </summary>
        public DbContextManager(IServiceProvider serviceProvider, ILogger<DbContextManager> logger) // Changed to ILogger<DbContextManager>
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets an existing context or creates a new one if needed
        /// </summary>
        private AppDbContext GetOrCreateContext()
        {
            // Check if we need to create a new context
            if (_dbContext == null || _currentScope == null)
            {
                // Get a scoped DbContext from DI
                _currentScope = _serviceProvider.CreateScope();
                _dbContext = _currentScope.ServiceProvider.GetRequiredService<AppDbContext>();

                Log.CreatedNewDbContext(_logger, null);
            }

            return _dbContext;
        }
          /// <summary>
        /// Execute a database operation with automatic context management
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<AppDbContext, Task<T>> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _semaphore.WaitAsync();

            try
            {
                // Get or create the context
                var context = GetOrCreateContext();

                // Execute the operation
                return await operation(context);
            }
            catch (Exception ex)
            {
                Log.ErrorExecutingDbOperation(_logger, ex);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
          /// <summary>
        /// Execute a database operation with automatic context management
        /// </summary>
        public async Task ExecuteAsync(Func<AppDbContext, Task> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _semaphore.WaitAsync();

            try
            {
                // Get or create the context
                var context = GetOrCreateContext();

                // Execute the operation
                await operation(context);
            }
            catch (Exception ex)
            {
                Log.ErrorExecutingDbOperation(_logger, ex);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Execute a database operation with retry for transient failures
        /// </summary>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<AppDbContext, Task<T>> operation, int maxRetries = 3)        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            int attempt = 0;
            Exception? lastException = null;

            while (attempt < maxRetries)
            {
                try
                {
                    return await ExecuteAsync(operation);
                }
                catch (Exception ex) when (IsTransientException(ex))
                {
                    attempt++;
                    lastException = ex;

                    Log.TransientErrorExecutingDbOperation(_logger, attempt, maxRetries, ex);

                    if (attempt < maxRetries)
                    {
                        // Reset the context before retrying
                        await ResetContextAsync();

                        // Wait before retrying (with increasing delay)
                        await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
                    }
                }
            }

            throw new DbContextManagerException("Maximum retry attempts exceeded for database operation", lastException);
        }

        /// <summary>
        /// Reset the current context by disposing and clearing it
        /// </summary>
        public async Task ResetContextAsync()
        {
            await _semaphore.WaitAsync();

            try
            {
                if (_dbContext != null)
                {
                    await _dbContext.DisposeAsync();
                    _dbContext = null;
                }

                if (_currentScope != null)
                {
                    _currentScope.Dispose();
                    _currentScope = null;
                }

                Log.ResetDbContext(_logger, null);
            }
            catch (Exception ex)
            {
                Log.ErrorResettingDbContext(_logger, ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Check if an exception is a transient database exception
        /// </summary>
        private static bool IsTransientException(Exception ex) // Made static
        {
            // Check for common transient error patterns
            return ex is Microsoft.Data.SqlClient.SqlException sqlEx &&
                  (sqlEx.Number == 1205 || // Deadlock victim
                   sqlEx.Number == 1204 || // Lock resources
                   sqlEx.Number == 49920 || // Cannot process request
                   sqlEx.Number == 10054 || // Connection forcibly closed
                   sqlEx.Number == 10060 || // Connection timeout
                   sqlEx.Number == 40197 || // Error processing request
                   sqlEx.Number == 40501 || // Service busy
                   sqlEx.Number == 40613 || // Database unavailable
                   sqlEx.Number == 233) || // Transport-level error
                   ex is TimeoutException ||
                   ex is System.Data.Common.DbException;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    _semaphore.Dispose();
                    _dbContext?.Dispose();
                    _currentScope?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // Nested class for LoggerMessage.Define
        private static partial class Log
        {
            [LoggerMessage(
                EventId = 0,
                Level = LogLevel.Debug,
                Message = "[DB-MANAGER] Created new DbContext instance")]
            public static partial void CreatedNewDbContext(ILogger logger, Exception? ex);

            [LoggerMessage(
                EventId = 1,
                Level = LogLevel.Error,
                Message = "[DB-MANAGER] Error executing database operation")]
            public static partial void ErrorExecutingDbOperation(ILogger logger, Exception? ex);

            [LoggerMessage(
                EventId = 2,
                Level = LogLevel.Warning,
                Message = "[DB-MANAGER] Transient error executing database operation (attempt {Attempt}/{MaxRetries})")]
            public static partial void TransientErrorExecutingDbOperation(ILogger logger, int attempt, int maxRetries, Exception? ex);

            [LoggerMessage(
                EventId = 3,
                Level = LogLevel.Debug,
                Message = "[DB-MANAGER] Reset DbContext")]
            public static partial void ResetDbContext(ILogger logger, Exception? ex);

            [LoggerMessage(
                EventId = 4,
                Level = LogLevel.Error,
                Message = "[DB-MANAGER] Error resetting DbContext")]
            public static partial void ErrorResettingDbContext(ILogger logger, Exception? ex);
        }
    }
}
