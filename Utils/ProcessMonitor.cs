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
        private bool _isDisposed;

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
        }

        /// <summary>
        /// Performs application cleanup on exit
        /// </summary>
        public void Cleanup()
        {
            if (_isDisposed) return;

            _logger?.LogInformation("ProcessMonitor running cleanup");

            // Release timers
            lock (_activeTimers)
            {
                foreach (var timer in _activeTimers)
                {
                    try
                    {
                        timer.Dispose();
                        _logger?.LogDebug("Timer disposed");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error disposing timer");
                    }
                }
                _activeTimers.Clear();
            }            // Terminate child processes
            lock (_childProcesses)
            {
                foreach (var process in _childProcesses.ToList())
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true); // Kill entire process tree
                            _logger?.LogInformation("Terminated child process: {ProcessId}", process.Id);
                        }
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        _logger?.LogError(ex, "Win32 error terminating process {ProcessId}: {ErrorCode}", process.Id, ex.ErrorCode);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error terminating process {ProcessId}", process.Id);
                    }
                }
                _childProcesses.Clear();
            }

            // Final forceful cleanup of all dotnet processes (except our own)
            try
            {
                var currentProcessId = Environment.ProcessId;
                var dotnetProcesses = Process.GetProcessesByName("dotnet")
                    .Where(p => p.Id != currentProcessId)
                    .ToList(); foreach (var process in dotnetProcesses)
                {
                    try
                    {
                        // Check if process has exited before accessing properties
                        if (process != null && !process.HasExited)
                        {
                            // Don't kill the current process
                            if (process.Id == Environment.ProcessId)
                            {
                                continue;
                            }

                            // Only kill related processes (check if they were created by this app)
                            var startTime = process.StartTime;
                            if ((DateTime.Now - startTime).TotalMinutes < 10) // Only recent processes
                            {
                                process.Kill(true);
                                _logger?.LogInformation("Forcefully terminated related dotnet process: {ProcessId}", process.Id);
                            }
                        }
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        _logger?.LogError(ex, "Win32 error accessing/terminating process {ProcessId}: {ErrorCode}", process?.Id ?? -1, ex.ErrorCode);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger?.LogError(ex, "Error accessing process {ProcessId} - process may have exited", process?.Id ?? -1);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error forcefully terminating process {ProcessId}", process?.Id ?? -1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in final process cleanup");
            }

            _logger?.LogInformation("ProcessMonitor cleanup complete");
        }

        /// <summary>
        /// Forcefully terminates any lingering dotnet processes related to this application
        /// </summary>
        public void KillLingeringProcesses(bool onlyRelatedToThisApp = true)
        {
            try
            {
                _logger?.LogInformation("[ProcessMonitor] Searching for lingering dotnet processes...");

                var currentProcessId = Environment.ProcessId;
                var currentProcess = Process.GetCurrentProcess();
                var startupTime = currentProcess.StartTime;

                // Find dotnet processes that might be related
                var dotnetProcesses = Process.GetProcessesByName("dotnet")
                    .Where(p => p.Id != currentProcessId)
                    .ToList();

                int killedCount = 0;
                var attemptedProcessIds = new HashSet<int>();

                foreach (var process in dotnetProcesses)
                {
                    if (attemptedProcessIds.Contains(process.Id))
                        continue;
                    attemptedProcessIds.Add(process.Id);

                    try
                    {
                        bool shouldKill = false;
                        string reason = "";

                        if (onlyRelatedToThisApp)
                        {
                            // Try to determine if this process is related to our application
                            try
                            {
                                // Check if started around the same time as our application
                                if (Math.Abs((process.StartTime - startupTime).TotalMinutes) < 5)
                                {
                                    shouldKill = true;
                                    reason += $"StartTime within 5 min; ";
                                }

                                // Try to get command line (requires admin on some systems)
                                var cmdLine = GetCommandLine(process);
                                if (!string.IsNullOrEmpty(cmdLine) &&
                                    (cmdLine.Contains("BusBus") || cmdLine.Contains("busbus")))
                                {
                                    shouldKill = true;
                                    reason += $"CmdLine match: {cmdLine}; ";
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "[ProcessMonitor] Could not determine relation for process {ProcessId}", process.Id);
                                shouldKill = false;
                            }
                        }
                        else
                        {
                            // Kill all dotnet processes except current one
                            shouldKill = true;
                            reason += "Not filtering by app; ";
                        }

                        if (shouldKill && process.Id != Environment.ProcessId)
                        {
                            _logger?.LogWarning("[ProcessMonitor] Killing lingering process: ID={ProcessId}, Started={StartTime}, Reason={Reason}",
                                process.Id, process.StartTime, reason);

                            if (!process.HasExited)
                            {
                                process.Kill(true); // Kill entire process tree
                                killedCount++;
                            }
                            else
                            {
                                _logger?.LogInformation("[ProcessMonitor] Process {ProcessId} already exited before kill attempt.", process.Id);
                            }
                        }
                        else
                        {
                            _logger?.LogInformation("[ProcessMonitor] Skipping process {ProcessId} (not related or already handled).", process.Id);
                        }
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        _logger?.LogError(ex, "[ProcessMonitor] Win32 error killing process {ProcessId}", process.Id);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger?.LogError(ex, "[ProcessMonitor] Process {ProcessId} may have already exited.", process.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "[ProcessMonitor] Error killing process {ProcessId}", process.Id);
                    }
                }

                _logger?.LogInformation("[ProcessMonitor] Killed {Count} lingering processes", killedCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[ProcessMonitor] Error in KillLingeringProcesses");
            }
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
            if (_isDisposed) return;

            Cleanup();
            _isDisposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
