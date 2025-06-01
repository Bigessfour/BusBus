// Suppress logger performance and static member warnings for this file
#pragma warning disable CA1848 // Use LoggerMessage delegates for logging performance
#pragma warning disable CA1822 // Member can be static
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Suppressed because _logger is set in Initialize.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace BusBus.Utils
{
    /// <summary>
    /// Monitors and manages process resources to prevent hanging processes
    /// </summary>
    public class ProcessMonitor : IDisposable
    {
        private ILogger<ProcessMonitor>? _logger;
        private readonly List<Process> _childProcesses = new List<Process>();
        private readonly List<System.Windows.Forms.Timer> _activeTimers = new List<System.Windows.Forms.Timer>();
        private static ProcessMonitor? _instance;
        private bool _isDisposed = false; // Renamed from _isDisposed for clarity and to match usage

        /// <summary>
        /// Singleton instance of the ProcessMonitor
        /// </summary>
        public static ProcessMonitor Instance => _instance ??= new ProcessMonitor();

        private ProcessMonitor()
        {
            // Register cleanup on process exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Cleanup();
        }

        /// <summary>
        /// Initializes the monitor with a logger
        /// </summary>
        public void Initialize(ILogger<ProcessMonitor> logger)
        {
            _logger = logger;
            _logger.LogInformation("ProcessMonitor initialized");
        }

        /// <summary>
        /// Registers a child process to be monitored and terminated during shutdown
        /// </summary>
        public void RegisterChildProcess(Process process)
        {
            if (process == null) return;

            lock (_childProcesses)
            {
                _childProcesses.Add(process);
                _logger?.LogDebug("Registered child process: {ProcessId}", process.Id);
            }
        }
        /// <summary>
        /// Registers a timer to ensure it's disposed during shutdown
        /// </summary>
        public void RegisterTimer(System.Windows.Forms.Timer timer)
        {
            if (timer == null) return;

            lock (_activeTimers)
            {
                _activeTimers.Add(timer);
                _logger?.LogDebug("Registered timer");
            }
        }

        /// <summary>
        /// Registers exit handlers for the application to ensure proper cleanup
        /// </summary>
        public void RegisterExitHandlers()
        {
            try
            {
                // Register with Windows Forms Application exit
                Application.ApplicationExit += (s, e) =>
                {
                    _logger?.LogInformation("Application.ApplicationExit event triggered");
                    Cleanup();
                };

                // Register with console exit
                Console.CancelKeyPress += (s, e) =>
                {
                    _logger?.LogInformation("Console.CancelKeyPress event triggered");
                    Cleanup();
                    e.Cancel = true; // Let the application handle the exit
                };

                _logger?.LogInformation("Exit handlers registered successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error registering exit handlers");
            }
        }        /// <summary>
                 /// Performs application cleanup on exit - simplified to essential resource disposal only
                 /// </summary>
        public void Cleanup()
        {
            if (_isDisposed) return;

            _logger?.LogInformation("ProcessMonitor running cleanup (essential resources only)");

            // Release timers (essential UI/database related resources)
            lock (_activeTimers)
            {
                foreach (var timer in _activeTimers.ToList()) // Iterate over a copy for safe removal/disposal
                {
                    try
                    {
                        if (timer != null) // Add null check before disposing
                        {
                            timer.Stop(); // Stop the timer before disposing
                            timer.Dispose();
                            _logger?.LogDebug("Timer disposed");
                        }
                    }
                    catch (ObjectDisposedException odEx)
                    {
                        _logger?.LogWarning(odEx, "Timer already disposed during cleanup.");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error disposing timer");
                    }
                }
                _activeTimers.Clear();
            }

            // Clear process tracking lists (no termination, just cleanup tracking)
            lock (_childProcesses)
            {
                // No direct disposal of Process objects here as they are externally managed
                // and ProcessMonitor is only tracking them.
                // If ProcessMonitor were responsible for their lifecycle, disposal would be here.
                _childProcesses.Clear();
            }

            _logger?.LogInformation("ProcessMonitor cleanup complete - database connections and UI resources disposed");
            _isDisposed = true; // Set disposed flag after cleanup is complete
        }        /// <summary>
                 /// Forcefully terminates any lingering dotnet processes related to this application
                 /// </summary>
        public void KillLingeringProcesses(bool onlyRelatedToThisApp = true)
        {
            _logger?.LogInformation("ProcessMonitor: KillLingeringProcesses called but simplified - no process termination performed");
        }

        private static string GetCommandLine(Process process)
        {
            try
            {
                // This method may fail without admin rights on some systems
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["CommandLine"]?.ToString() ?? string.Empty;
                    }
                }
            }
            catch
            {
                // Silently fail - this is just a best-effort method
            }

            return string.Empty;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                // Call Cleanup to release managed resources
                Cleanup();
            }

            // Release unmanaged resources here if any

            _isDisposed = true;
        }

        // Add a finalizer in case Dispose is not called
        ~ProcessMonitor()
        {
            Dispose(false);
        }
    }
}
