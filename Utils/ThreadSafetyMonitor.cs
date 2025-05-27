#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace BusBus.Utils
{
    /// <summary>
    /// Monitors thread usage in the application to detect potential cross-thread issues
    /// </summary>
    public static partial class ThreadSafetyMonitor // Added partial modifier
    {
        private static readonly ConcurrentDictionary<int, ThreadInfo> _activeThreads = new ConcurrentDictionary<int, ThreadInfo>();
        private static readonly ConcurrentBag<CrossThreadOperation> _crossThreadOperations = new ConcurrentBag<CrossThreadOperation>();
        private static readonly object _lockObject = new object();
        private static ILogger? _logger; // Changed to ILogger
        private static bool _isEnabled;
        private static int _uiThreadId = -1;

        /// <summary>
        /// Initialize the thread safety monitor
        /// </summary>
        public static void Initialize(ILogger logger) // Changed to ILogger
        {
            _logger = logger;
            _isEnabled = true;

            // Remember UI thread ID
            _uiThreadId = Environment.CurrentManagedThreadId; // CA1840

            // Register this thread
            RegisterThread("UI Thread", true);

            _logger?.LogInitialized(_uiThreadId, null);
        }

        /// <summary>
        /// Register a thread with the monitor
        /// </summary>
        public static void RegisterThread(string purpose, bool isUiThread = false)
        {
            if (!_isEnabled || _logger == null) return;

            var threadId = Environment.CurrentManagedThreadId; // CA1840
            var threadInfo = new ThreadInfo
            {
                ThreadId = threadId,
                IsUiThread = isUiThread,
                Purpose = purpose,
                StartTime = DateTime.Now
            };

            _activeThreads.TryAdd(threadId, threadInfo);

            _logger.LogThreadRegistered(threadId, purpose, isUiThread, null);
        }

        /// <summary>
        /// Record a UI operation being performed from a background thread
        /// </summary>
        public static void RecordCrossThreadOperation(string operation, object control, string callerMember)
        {
            if (!_isEnabled || _logger == null) return;

            var threadId = Environment.CurrentManagedThreadId; // CA1840
            var isOnUiThread = threadId == _uiThreadId;

            // Only record if this is not the UI thread
            if (!isOnUiThread && control is Control)
            {
                var controlType = control.GetType().Name;
                var controlName = (control as Control)?.Name ?? "Unknown";

                var record = new CrossThreadOperation
                {
                    ThreadId = threadId,
                    Operation = operation,
                    ControlType = controlType,
                    ControlName = controlName,
                    CallerMember = callerMember,
                    Timestamp = DateTime.Now
                };

                _crossThreadOperations.Add(record);

                _logger.LogCrossThreadOperationDetected(operation, controlType, controlName, threadId, callerMember, null);
            }
        }

        /// <summary>
        /// Check if the current thread is the UI thread
        /// </summary>
        public static bool IsOnUiThread()
        {
            return Environment.CurrentManagedThreadId == _uiThreadId; // CA1840
        }

        /// <summary>
        /// Get a report of all active threads
        /// </summary>
        public static ReadOnlyCollection<ThreadInfo> GetActiveThreads() // CA1002
        {
            return _activeThreads.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Get a report of all cross-thread operations
        /// </summary>
        public static ReadOnlyCollection<CrossThreadOperation> GetCrossThreadOperations() // CA1002
        {
            return _crossThreadOperations.ToList().AsReadOnly();
        }

        /// <summary>
        /// Throw exception if we're on a background thread trying to access UI controls
        /// </summary>
        public static void EnforceUIThreadAccess(object control, string operation, string callerMember)
        {
            if (!_isEnabled) return;

            if (!IsOnUiThread() && control is Control)
            {
                RecordCrossThreadOperation(operation, control, callerMember);

                throw new InvalidOperationException(
                    $"Cross-thread operation not valid: {operation} on {control.GetType().Name} from {callerMember}. " +
                    "Use Control.Invoke or BeginInvoke to access UI controls from background threads.");
            }
        }
    }

    /// <summary>
    /// Information about a thread
    /// </summary>
    public class ThreadInfo
    {
        public int ThreadId { get; set; }
        public bool IsUiThread { get; set; }
        public string Purpose { get; set; } = "";
        public DateTime StartTime { get; set; }
    }

    /// <summary>
    /// Information about a cross-thread operation
    /// </summary>
    public class CrossThreadOperation
    {
        public int ThreadId { get; set; }
        public string Operation { get; set; } = "";
        public string ControlType { get; set; } = "";
        public string ControlName { get; set; } = "";
        public string CallerMember { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    // Nested class for LoggerMessage.Define
    static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Information,
            Message = "[THREAD-MONITOR] Initialized with UI thread ID: {ThreadId}")]
        public static partial void LogInitialized(this ILogger logger, int threadId, Exception? ex);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Debug,
            Message = "[THREAD-MONITOR] Thread registered: {ThreadId} | Purpose: {Purpose} | IsUI: {IsUI}")]
        public static partial void LogThreadRegistered(this ILogger logger, int threadId, string purpose, bool isUI, Exception? ex);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Warning,
            Message = "[THREAD-MONITOR] Cross-thread operation detected: {Operation} | Control: {ControlType} ({ControlName}) | Thread: {ThreadId} | Caller: {Caller}")]
        public static partial void LogCrossThreadOperationDetected(this ILogger logger, string operation, string controlType, string controlName, int threadId, string caller, Exception? ex);
    }
}
