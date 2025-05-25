using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace BusBus.Utils
{
    /// <summary>
    /// Debugging utilities to help with development and troubleshooting
    /// </summary>
    public static partial class DebugUtils
    {
        private static readonly object _lockObject = new object();

        // Generated LoggerMessage delegates
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "[CONTEXT] {MemberName} | Thread: {ThreadId} | IsUI: {IsMainThread} | {FileName}:{LineNumber}")]
        private static partial void LogContextMessage(ILogger logger, string memberName, int threadId, bool isMainThread, string fileName, int lineNumber);

        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "[EXCEPTION] {MemberName} | Thread: {ThreadId} | {FileName}:{LineNumber} | {ExceptionType}: {Message}")]
        private static partial void LogExceptionContextMessage(ILogger logger, Exception ex, string memberName, int threadId, string fileName, int lineNumber, string exceptionType, string message);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "[INNER-{Depth}] {ExceptionType}: {Message}")]
        private static partial void LogInnerExceptionMessage(ILogger logger, int depth, string exceptionType, string message);

        [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "[THREAD-SAFETY] {Operation} | Member: {MemberName} | Thread: {ThreadId} | IsUIThread: {IsUIThread}")]
        private static partial void LogThreadSafetyMessage(ILogger logger, string operation, string memberName, int threadId, bool isUIThread);

        [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "[PERF-START] {Operation} | {MemberName}")]
        private static partial void LogPerfStartMessage(ILogger logger, string operation, string memberName);

        [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "[PERF-END] {Operation} | {MemberName} | Duration: {Duration}ms")]
        private static partial void LogPerfEndMessage(ILogger logger, string operation, string memberName, long duration);

        /// <summary>
        /// Log detailed thread and context information
        /// </summary>
        public static void LogContext(ILogger logger,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            ArgumentNullException.ThrowIfNull(logger);

            lock (_lockObject)
            {
                var threadId = Environment.CurrentManagedThreadId;
                var isMainThread = Thread.CurrentThread.IsBackground == false;
                var fileName = System.IO.Path.GetFileName(sourceFilePath);
                LogContextMessage(logger, memberName, threadId, isMainThread, fileName, sourceLineNumber);
            }
        }

        /// <summary>
        /// Log exception with full context
        /// </summary>
        public static void LogExceptionContext(ILogger logger, Exception ex,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(ex);

            lock (_lockObject)
            {
                var threadId = Environment.CurrentManagedThreadId;
                var fileName = System.IO.Path.GetFileName(sourceFilePath);
                LogExceptionContextMessage(logger, ex, memberName, threadId, fileName, sourceLineNumber, ex.GetType().Name, ex.Message);

                // Log inner exceptions
                var innerEx = ex.InnerException;
                int depth = 1;
                while (innerEx != null && depth <= 5)
                {
                    LogInnerExceptionMessage(logger, depth, innerEx.GetType().Name, innerEx.Message);
                    innerEx = innerEx.InnerException;
                    depth++;
                }
            }
        }

        /// <summary>
        /// Check if we\'re on the UI thread (WinForms)
        /// </summary>
        public static bool IsOnUIThread()
        {
            try
            {
                // This check is specific to Windows Forms applications.
                // For WPF, use Dispatcher.CheckAccess().
                // For other UI frameworks, use their respective mechanisms.
                // Consider making this method more generic or providing framework-specific versions.
                return System.Windows.Forms.Application.MessageLoop;
            }
            catch (InvalidOperationException)
            {
                // This can happen if Application.MessageLoop is accessed from a non-UI thread
                // before the message loop has started, or if no message loop exists (e.g., console app).
                return false;
            }
            catch
            {
                // Fallback for any other unexpected issues
                return false;
            }
        }

        /// <summary>
        /// Log thread safety information
        /// </summary>
        public static void LogThreadSafety(ILogger logger, string operation,
            [CallerMemberName] string memberName = "")
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(operation);

            var isOnUI = IsOnUIThread();
            var threadId = Environment.CurrentManagedThreadId;
            LogThreadSafetyMessage(logger, operation, memberName, threadId, isOnUI);
        }

        /// <summary>
        /// Conditional debug break - only breaks in debug builds
        /// </summary>
        [Conditional("DEBUG")]
        public static void DebugBreakIfAttached()
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        /// <summary>
        /// Performance timing helper
        /// </summary>
        public static IDisposable TimeOperation(ILogger logger, string operationName,
            [CallerMemberName] string memberName = "")
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(operationName);
            return new PerformanceTimer(logger, operationName, memberName);
        }

        private partial class PerformanceTimer : IDisposable
        {
            private readonly ILogger _logger;
            private readonly string _operationName;
            private readonly string _memberName;
            private readonly Stopwatch _stopwatch;

            public PerformanceTimer(ILogger logger, string operationName, string memberName)
            {
                _logger = logger;
                _operationName = operationName;
                _memberName = memberName;
                _stopwatch = Stopwatch.StartNew();
                LogPerfStartMessage(_logger, _operationName, _memberName);
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                LogPerfEndMessage(_logger, _operationName, _memberName, _stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
