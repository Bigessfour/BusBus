using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace BusBus.Utils
{
    /// <summary>
    /// Provides thread-safe access to UI controls
    /// </summary>
    public static partial class ThreadSafeUI // Made partial
    {
        private static ILogger? _logger;

        // LoggerMessage delegates
        private static partial class Log
        {
            [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Control was disposed before invoke could complete: {ControlType}")]
            public static partial void ControlDisposedBeforeInvoke(ILogger logger, string controlType, Exception? ex);

            [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Invalid operation in thread-safe invoke: {Message}")]
            public static partial void InvalidOperationInInvoke(ILogger logger, string message, Exception? ex);

            [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error in thread-safe invoke: {Message}")]
            public static partial void ErrorInInvoke(ILogger logger, string message, Exception? ex);

            [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Control was disposed before BeginInvoke could complete: {ControlType}")]
            public static partial void ControlDisposedBeforeBeginInvoke(ILogger logger, string controlType, Exception? ex);

            [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Invalid operation in thread-safe BeginInvoke: {Message}")]
            public static partial void InvalidOperationInBeginInvoke(ILogger logger, string message, Exception? ex);

            [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error in thread-safe BeginInvoke: {Message}")]
            public static partial void ErrorInBeginInvoke(ILogger logger, string message, Exception? ex);

            [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Control was disposed before update could complete: {ControlType}")]
            public static partial void ControlDisposedBeforeUpdate(ILogger logger, string controlType, Exception? ex);

            [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Invalid operation in thread-safe update: {Message}")]
            public static partial void InvalidOperationInUpdate(ILogger logger, string message, Exception? ex);

            [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Error in thread-safe update: {Message}")]
            public static partial void ErrorInUpdate(ILogger logger, string message, Exception? ex);

            [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Control was disposed before text update could complete: {ControlType}")]
            public static partial void ControlDisposedBeforeTextUpdate(ILogger logger, string controlType, Exception? ex);

            [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Invalid operation in thread-safe text update: {Message}")]
            public static partial void InvalidOperationInTextUpdate(ILogger logger, string message, Exception? ex);

            [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Error in thread-safe text update: {Message}")]
            public static partial void ErrorInTextUpdate(ILogger logger, string message, Exception? ex);
        }

        /// <summary>
        /// Initialize the ThreadSafeUI helper
        /// </summary>
        public static void Initialize(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Invoke an action on the UI thread if needed
        /// </summary>
        public static void Invoke(Control control, Action action, string operation = "UI Operation")
        {
            if (control == null || action == null) return;

            try
            {
                if (control.InvokeRequired)
                {
                    // Record cross-thread operation for diagnostics
                    ThreadSafetyMonitor.RecordCrossThreadOperation(
                        operation,
                        control,
                        new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown");

                    control.Invoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (_logger != null) Log.ControlDisposedBeforeInvoke(_logger, control.GetType().Name, ex);
            }
            catch (InvalidOperationException ex)
            {
                if (_logger != null) Log.InvalidOperationInInvoke(_logger, ex.Message, ex);
            }
            catch (Exception ex)
            {
                if (_logger != null) Log.ErrorInInvoke(_logger, ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Invoke a function on the UI thread if needed and return the result
        /// </summary>
        public static T Invoke<T>(Control control, Func<T> func, string operation = "UI Operation")
        {
            if (control == null || func == null)
                throw new ArgumentNullException(control == null ? nameof(control) : nameof(func));

            try
            {
                if (control.InvokeRequired)
                {
                    // Record cross-thread operation for diagnostics
                    ThreadSafetyMonitor.RecordCrossThreadOperation(
                        operation,
                        control,
                        new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown");

                    return (T)control.Invoke(func);
                }
                else
                {
                    return func();
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (_logger != null) Log.ControlDisposedBeforeInvoke(_logger, control.GetType().Name, ex);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                if (_logger != null) Log.InvalidOperationInInvoke(_logger, ex.Message, ex);
                throw;
            }
            catch (Exception ex)
            {
                if (_logger != null) Log.ErrorInInvoke(_logger, ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Begin asynchronous invoke on the UI thread
        /// </summary>
        public static void BeginInvoke(Control control, Action action, string operation = "UI Operation")
        {
            if (control == null || action == null) return;

            try
            {
                if (control.InvokeRequired)
                {
                    // Record cross-thread operation for diagnostics
                    ThreadSafetyMonitor.RecordCrossThreadOperation(
                        operation,
                        control,
                        new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown");

                    control.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (_logger != null) Log.ControlDisposedBeforeBeginInvoke(_logger, control.GetType().Name, ex);
            }
            catch (InvalidOperationException ex)
            {
                if (_logger != null) Log.InvalidOperationInBeginInvoke(_logger, ex.Message, ex);
            }
            catch (Exception ex)
            {
                if (_logger != null) Log.ErrorInBeginInvoke(_logger, ex.Message, ex);
            }
        }

        /// <summary>
        /// Update a control property thread-safely
        /// </summary>
        public static void Update<T>(Control control, Action<T> setter, T value) where T : Control
        {
            if (control == null || setter == null) return;

            try
            {
                if (control.InvokeRequired)
                {
                    // Record cross-thread operation for diagnostics
                    ThreadSafetyMonitor.RecordCrossThreadOperation(
                        $"Update {typeof(T).Name}",
                        control,
                        new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown");

                    control.Invoke(() => setter((T)control));
                }
                else
                {
                    setter((T)control);
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (_logger != null) Log.ControlDisposedBeforeUpdate(_logger, control.GetType().Name, ex);
            }
            catch (InvalidOperationException ex)
            {
                if (_logger != null) Log.InvalidOperationInUpdate(_logger, ex.Message, ex);
            }
            catch (Exception ex)
            {
                if (_logger != null) Log.ErrorInUpdate(_logger, ex.Message, ex);
            }
        }

        /// <summary>
        /// Update a control text property thread-safely
        /// </summary>
        public static void UpdateText(Control control, string text)
        {
            if (control == null) return;

            try
            {
                if (control.InvokeRequired)
                {
                    // Record cross-thread operation for diagnostics
                    ThreadSafetyMonitor.RecordCrossThreadOperation(
                        "Update Text",
                        control,
                        new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown");

                    control.Invoke(() => control.Text = text);
                }
                else
                {
                    control.Text = text;
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (_logger != null) Log.ControlDisposedBeforeTextUpdate(_logger, control.GetType().Name, ex);
            }
            catch (InvalidOperationException ex)
            {
                if (_logger != null) Log.InvalidOperationInTextUpdate(_logger, ex.Message, ex);
            }
            catch (Exception ex)
            {
                if (_logger != null) Log.ErrorInTextUpdate(_logger, ex.Message, ex);
            }
        }
    }
}
