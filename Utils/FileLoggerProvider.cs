using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace BusBus.Utils
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _filePath));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _filePath;
        private static readonly object _lock = new object();

        public FileLogger(string categoryName, string filePath)
        {
            _categoryName = categoryName;
            _filePath = filePath;
        }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None; public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || formatter == null)
                return;

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var level = logLevel.ToString().ToUpper();
            var message = formatter(state, exception);
            var category = _categoryName.Split('.').LastOrDefault() ?? _categoryName;

            var logEntry = $"{timestamp} [{level}] [{category}] {message}";

            if (exception != null)
            {
                logEntry += Environment.NewLine + exception.ToString();
            }

            logEntry += Environment.NewLine;

            lock (_lock)
            {
                try
                {
                    // Create the file if it doesn't exist and write immediately
                    File.AppendAllText(_filePath, logEntry);

                    // Also write to console for immediate visibility
                    Console.Write(logEntry);
                }
                catch (Exception ex)
                {
                    // Write to console if file write fails
                    Console.WriteLine($"LOG FILE ERROR: {ex.Message}");
                    Console.Write(logEntry);
                }
            }
        }
    }
}
