#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace BusBus.Utils
{
    /// <summary>
    /// Configures and manages application logging
    /// </summary>
    public static class LoggingManager
    {
        private static readonly object _lockObject = new object();
        private static readonly Dictionary<string, ILogger> _loggers = new Dictionary<string, ILogger>();
        private static ILoggerFactory? _loggerFactory;
        private static LogLevel _minimumLogLevel = LogLevel.Information;
        private static bool _enableFileLogging = true;
        private static bool _initialized = false;
        private static string _logDirectory = string.Empty;
        private static readonly Dictionary<string, List<LogEntry>> _inMemoryLogs = new Dictionary<string, List<LogEntry>>();
        private static readonly List<LogEntry> _recentLogs = new List<LogEntry>();
        private const int MaxRecentLogEntries = 1000;

        // LoggerMessage delegates
        private static readonly Action<ILogger, string, Exception?> _logVerboseLoggingStatus =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, nameof(SetVerboseLogging)), "Verbose logging {Status}");

        private static readonly Action<ILogger, string, Exception?> _logFileLoggingStatus =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, nameof(SetFileLogging)), "File logging {Status}");

        private static readonly Action<ILogger, string, Exception?> _logAppStarting =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(3, nameof(LogApplicationStartup)), "Application Starting: {AppName}");

        private static readonly Action<ILogger, OperatingSystem, Exception?> _logOSVersion =
            LoggerMessage.Define<OperatingSystem>(LogLevel.Information, new EventId(4, nameof(LogApplicationStartup)), "OS Version: {OS}");

        private static readonly Action<ILogger, Version, Exception?> _logDotNetVersion =
            LoggerMessage.Define<Version>(LogLevel.Information, new EventId(5, nameof(LogApplicationStartup)), ".NET Version: {Runtime}");

        private static readonly Action<ILogger, string, Exception?> _logDirectoryInfo =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(6, nameof(LogApplicationStartup)), "Directory: {Dir}");

        private static readonly Action<ILogger, string, Exception?> _logEnvironmentInfo =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(7, nameof(LogApplicationStartup)), "Environment: {Env}");

        private static readonly Action<ILogger, int, Exception?> _logProcessorCount =
            LoggerMessage.Define<int>(LogLevel.Information, new EventId(8, nameof(LogApplicationStartup)), "Processors: {CPUs}");

        private static readonly Action<ILogger, string, string, int, bool, Exception?> _logUIDebug =
            LoggerMessage.Define<string, string, int, bool>(LogLevel.Debug, new EventId(9, nameof(LogUIOperation)), "[UI] {Component} | {Operation} | Thread: {ThreadId} | IsUIThread: {IsUIThread}");

        private static readonly Action<ILogger, string, string, int, Exception?> _logUIWarning =
            LoggerMessage.Define<string, string, int>(LogLevel.Warning, new EventId(10, nameof(LogUIOperation)), "[UI-WARN] Potential cross-thread operation in {Component}.{Operation} on thread {ThreadId}");

        private static readonly Action<ILogger, string, Exception?> _logSavedToFile =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(11, nameof(SaveLogsToFile)), "Logs saved to {FilePath}");

        private static readonly Action<ILogger, Exception?> _logSeparatorLine =
            LoggerMessage.Define(LogLevel.Information, new EventId(12, "LogSeparator"), "====================================================");


        /// <summary>
        /// Initialize logging for application
        /// </summary>
        public static void Initialize(ILoggerFactory loggerFactory)
        {
            if (_initialized) return;

            ArgumentNullException.ThrowIfNull(loggerFactory);
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            // Create logs directory if it doesn't exist
            try
            {
                _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // Create today's log file
                CreateDailyLogFile();

                // Log application startup
                var logger = GetLogger("BusBus.Startup");
                LogApplicationStartup(logger);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing logging: {ex.Message}");
            }

            _initialized = true;
        }

        /// <summary>
        /// Enable verbose logging (Debug level)
        /// </summary>
        public static void SetVerboseLogging(bool enabled)
        {
            _minimumLogLevel = enabled ? LogLevel.Debug : LogLevel.Information;

            // Log the change
            var logger = GetLogger("BusBus.LoggingManager");
            _logVerboseLoggingStatus(logger, enabled ? "enabled" : "disabled", null);
        }

        /// <summary>
        /// Enable or disable file logging
        /// </summary>
        public static void SetFileLogging(bool enabled)
        {
            _enableFileLogging = enabled;

            // Log the change
            var logger = GetLogger("BusBus.LoggingManager");
            _logFileLoggingStatus(logger, enabled ? "enabled" : "disabled", null);
        }

        /// <summary>
        /// Create a new log file for today
        /// </summary>
        private static void CreateDailyLogFile()
        {
            if (!_enableFileLogging) return;

            try
            {
                var today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var logFilePath = Path.Combine(_logDirectory, $"busbus-{today}.log");

                // Create the file if it doesn't exist
                if (!File.Exists(logFilePath))
                {
                    using var file = File.Create(logFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating daily log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Log application startup information
        /// </summary>
        private static void LogApplicationStartup(ILogger logger)
        {
            _logSeparatorLine(logger, null);
            _logAppStarting(logger, AppDomain.CurrentDomain.FriendlyName, null);
            _logOSVersion(logger, Environment.OSVersion, null);
            _logDotNetVersion(logger, Environment.Version, null);
            _logDirectoryInfo(logger, AppDomain.CurrentDomain.BaseDirectory, null);
            _logEnvironmentInfo(logger, Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production", null);
            _logProcessorCount(logger, Environment.ProcessorCount, null);
            _logSeparatorLine(logger, null);
        }
          /// <summary>
        /// Log a UI operation for debugging thread issues
        /// </summary>
        public static void LogUIOperation(ILogger logger, string component, string operation)
        {
            if (logger == null) return;
            ArgumentNullException.ThrowIfNull(operation);

            var threadId = Environment.CurrentManagedThreadId;
            var isOnUIThread = ThreadSafetyMonitor.IsOnUiThread();

            _logUIDebug(logger, component, operation, threadId, isOnUIThread, null);

            // Also log a warning if it's potentially a cross-thread operation
            if (!isOnUIThread && (operation.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
                              operation.Contains("Set", StringComparison.OrdinalIgnoreCase) ||
                              operation.Contains("Add", StringComparison.OrdinalIgnoreCase)))
            {
                _logUIWarning(logger, component, operation, threadId, null);
            }
        }
          /// <summary>
        /// Get the most recent log entries
        /// </summary>
        public static ReadOnlyCollection<LogEntry> GetRecentLogs(int maxEntries = 100)
        {
            lock (_lockObject)
            {
                var entries = _recentLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(Math.Min(maxEntries, _recentLogs.Count))
                    .ToList();
                return new ReadOnlyCollection<LogEntry>(entries);
            }
        }

        /// <summary>
        /// Save all in-memory logs to a file
        /// </summary>
        public static void SaveLogsToFile(string? customFilePath = null)
        {
            if (!_enableFileLogging) return;

            try
            {
                var filePath = customFilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
                    filePath = Path.Combine(_logDirectory, $"busbus-dump-{timestamp}.log");
                }

                using var writer = new StreamWriter(filePath, false);

                // Write header
                writer.WriteLine($"BusBus Log Dump - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
                writer.WriteLine("====================================================");

                // Write all in-memory logs
                lock (_lockObject)
                {
                    foreach (var category in _inMemoryLogs.Keys.OrderBy(k => k))
                    {
                        writer.WriteLine($"\nCategory: {category}");
                        writer.WriteLine("----------------------------------------------------");

                        foreach (var entry in _inMemoryLogs[category].OrderBy(e => e.Timestamp))
                        {
                            writer.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Message}");
                        }
                    }
                }

                // Write footer
                writer.WriteLine("\n====================================================");
                writer.WriteLine($"End of log dump - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");

                // Log that we saved the logs
                var logger = GetLogger("BusBus.LoggingManager");
                _logSavedToFile(logger, filePath, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving logs to file: {ex.Message}");
            }
        }
          /// <summary>
        /// Filter logs by level and category
        /// </summary>
        public static ReadOnlyCollection<LogEntry> FilterLogs(
            LogLevel minLevel = LogLevel.Debug,
            string? categoryFilter = null,
            string? textFilter = null,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            lock (_lockObject)
            {
                var query = _recentLogs.AsEnumerable();

                // Apply filters
                if (minLevel != LogLevel.Debug)
                {
                    query = query.Where(log => log.Level >= minLevel);
                }

                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    query = query.Where(log => log.Category.Contains(categoryFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(textFilter))
                {
                    query = query.Where(log => log.Message.Contains(textFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (startTime.HasValue)
                {
                    query = query.Where(log => log.Timestamp >= startTime.Value);
                }

                if (endTime.HasValue)
                {
                    query = query.Where(log => log.Timestamp <= endTime.Value);
                }

                var filteredEntries = query
                    .OrderByDescending(log => log.Timestamp)
                    .ToList();

                return new ReadOnlyCollection<LogEntry>(filteredEntries);
            }
        }

        /// <summary>
        /// Add a log entry to in-memory storage
        /// </summary>
        internal static void AddLogEntry(string category, LogLevel level, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Category = category,
                Level = level,
                Message = message,
                ThreadId = Environment.CurrentManagedThreadId
            };

            lock (_lockObject)
            {
                // Add to category-specific collection
                if (!_inMemoryLogs.TryGetValue(category, out var categoryLogs))
                {
                    categoryLogs = new List<LogEntry>();
                    _inMemoryLogs[category] = categoryLogs;
                }

                categoryLogs.Add(entry);

                // Add to recent logs with size limit
                _recentLogs.Add(entry);
                if (_recentLogs.Count > MaxRecentLogEntries)
                {
                    _recentLogs.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Get or create a logger for a specific category
        /// </summary>
        public static ILogger GetLogger(string category)
        {
            if (!_initialized)
            {
                Console.WriteLine("Warning: Logging system not initialized, returning console logger");
                return new ConsoleLogger(category);
            }

            lock (_lockObject)
            {
                if (_loggers.TryGetValue(category, out var existingLogger))
                {
                    return existingLogger;
                }

                var logger = _loggerFactory!.CreateLogger(category);
                _loggers[category] = logger;
                return logger;
            }
        }
          /// <summary>
        /// Get or create a logger for a specific type
        /// </summary>
        public static ILogger GetLogger(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return GetLogger(type.FullName ?? type.Name);
        }
          /// <summary>
        /// Simple console logger for when the system isn't initialized
        /// </summary>
        private class ConsoleLogger : ILogger
        {
            private readonly string _categoryName;

            public ConsoleLogger(string categoryName)
            {
                _categoryName = categoryName;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}");

                if (exception != null)
                {
                    Console.WriteLine($"Exception: {exception}");
                }
            }

            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new NullScope();

                private NullScope() { }

                public void Dispose() { }
            }
        }
    }

    /// <summary>
    /// Log entry structure
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; } = string.Empty;
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ThreadId { get; set; }
    }
}
