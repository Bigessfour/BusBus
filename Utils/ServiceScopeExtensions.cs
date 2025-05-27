#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusBus.Utils
{
    /// <summary>
    /// Thread-safe service scope extension methods for dependency injection
    /// with enhanced debugging and logging capabilities
    /// </summary>
    public static class ServiceScopeExtensions
    {
        private static readonly object _lockObject = new object();
        private static int _activeScopesCount = 0;
        private static ILogger? _logger;

        // LoggerMessage delegates
        private static readonly Action<ILogger, int, string, int, bool, string, Exception?> _logScopeStartDebug =
            LoggerMessage.Define<int, string, int, bool, string>(LogLevel.Debug, new EventId(1, nameof(LogScopeStart)), "[SCOPE-START] #{ScopeId} | Service: {ServiceName} | Thread: {ThreadId} | UI Thread: {IsUiThread} | Caller: {Caller}");

        private static readonly Action<ILogger, int, string, int, string, Exception?> _logScopeError =
            LoggerMessage.Define<int, string, int, string>(LogLevel.Error, new EventId(2, nameof(LogScopeEnd)), "[SCOPE-ERROR] #{ScopeId} | Service: {ServiceName} | Thread: {ThreadId} | Error: {ErrorType}");

        private static readonly Action<ILogger, int, string, int, Exception?> _logScopeEndDebug =
            LoggerMessage.Define<int, string, int>(LogLevel.Debug, new EventId(3, nameof(LogScopeEnd)), "[SCOPE-END] #{ScopeId} | Service: {ServiceName} | Thread: {ThreadId}");


        /// <summary>
        /// Set logger for service scope tracking
        /// </summary>
        public static void SetLogger(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
        }

        /// <summary>
        /// Execute an action with a scoped service safely with enhanced debugging
        /// </summary>
        public static void WithScopedService<TService>(
            this IServiceProvider serviceProvider,
            Action<TService> action,
            string? callerInfo = null)
            where TService : notnull
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(action);

            var threadId = Environment.CurrentManagedThreadId;
            var scopeId = GetNextScopeId();
            var serviceName = typeof(TService).Name;

            LogScopeStart(threadId, scopeId, serviceName, callerInfo);

            try
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TService>();
                action(service);
                LogScopeEnd(threadId, scopeId, serviceName, null);
            }
            catch (Exception ex)
            {
                LogScopeEnd(threadId, scopeId, serviceName, ex);
                throw;
            }
        }

        /// <summary>
        /// Execute a function with a scoped service safely with enhanced debugging
        /// </summary>
        public static TResult WithScopedService<TService, TResult>(
            this IServiceProvider serviceProvider,
            Func<TService, TResult> func,
            string? callerInfo = null)
            where TService : notnull
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(func);

            var threadId = Environment.CurrentManagedThreadId;
            var scopeId = GetNextScopeId();
            var serviceName = typeof(TService).Name;

            LogScopeStart(threadId, scopeId, serviceName, callerInfo);

            try
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TService>();
                var result = func(service);
                LogScopeEnd(threadId, scopeId, serviceName, null);
                return result;
            }
            catch (Exception ex)
            {
                LogScopeEnd(threadId, scopeId, serviceName, ex);
                throw;
            }
        }

        /// <summary>
        /// Execute an async action with a scoped service safely with enhanced debugging
        /// </summary>
        public static async Task WithScopedServiceAsync<TService>(
            this IServiceProvider serviceProvider,
            Func<TService, Task> asyncAction,
            string? callerInfo = null)
            where TService : notnull
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(asyncAction);

            var threadId = Environment.CurrentManagedThreadId;
            var scopeId = GetNextScopeId();
            var serviceName = typeof(TService).Name;

            LogScopeStart(threadId, scopeId, serviceName, callerInfo);

            try
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TService>();
                await asyncAction(service);
                LogScopeEnd(threadId, scopeId, serviceName, null);
            }
            catch (Exception ex)
            {
                LogScopeEnd(threadId, scopeId, serviceName, ex);
                throw;
            }
        }

        /// <summary>
        /// Execute an async function with a scoped service safely with enhanced debugging
        /// </summary>
        public static async Task<TResult> WithScopedServiceAsync<TService, TResult>(
            this IServiceProvider serviceProvider,
            Func<TService, Task<TResult>> asyncFunc,
            string? callerInfo = null)
            where TService : notnull
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(asyncFunc);

            var threadId = Environment.CurrentManagedThreadId;
            var scopeId = GetNextScopeId();
            var serviceName = typeof(TService).Name;

            LogScopeStart(threadId, scopeId, serviceName, callerInfo);

            try
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TService>();
                var result = await asyncFunc(service);
                LogScopeEnd(threadId, scopeId, serviceName, null);
                return result;
            }
            catch (Exception ex)
            {
                LogScopeEnd(threadId, scopeId, serviceName, ex);
                throw;
            }
        }

        /// <summary>
        /// Get next scope ID for tracking
        /// </summary>
        private static int GetNextScopeId()
        {
            return Interlocked.Increment(ref _activeScopesCount);
        }

        /// <summary>
        /// Log scope starting
        /// </summary>
        private static void LogScopeStart(int threadId, int scopeId, string serviceName, string? callerInfo)
        {
            if (_logger == null) return;

            var isUiThread = DebugUtils.IsOnUIThread();
            var caller = string.IsNullOrEmpty(callerInfo) ? "unknown" : callerInfo;

            _logScopeStartDebug(_logger, scopeId, serviceName, threadId, isUiThread, caller, null);
        }

        /// <summary>
        /// Log scope ending
        /// </summary>
        private static void LogScopeEnd(int threadId, int scopeId, string serviceName, Exception? exception)
        {
            if (_logger == null) return;

            if (exception != null)
            {
                _logScopeError(_logger, scopeId, serviceName, threadId, exception.GetType().Name, exception);
                return;
            }

            _logScopeEndDebug(_logger, scopeId, serviceName, threadId, null);
        }
    }
}
