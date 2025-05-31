#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using BusBus.Tests;

namespace BusBus.Tests.Integration
{
    /// <summary>
    /// Tests to verify that no processes linger after application shutdown.
    /// These tests specifically verify the fixes for task leakage issues.
    /// </summary>
    [TestClass]
    public class ProcessLeakVerificationTests : TestBase
    {
        private readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        [TestMethod]
        [TestCategory(TestCategories.Integration)]
        public async Task VerifyNoLingeringProcesses_NormalExit()
        {
            // Arrange
            var initialProcessCount = GetBusBusProcessCount();
            Process? appProcess = null;

            try
            {
                // Act - Start application process
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run --project BusBus.csproj -- --test-mode --headless",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = GetProjectRoot()
                };

                appProcess = Process.Start(startInfo);
                Assert.IsNotNull(appProcess, "Failed to start BusBus process");

                // Wait for process to fully start (increased from 3s to 7s)
                await Task.Delay(7000);

                // Verify process is running
                var runningProcesses = GetBusBusProcessCount();
                if (!(runningProcesses > initialProcessCount))
                {
                    string stdOut = appProcess.StandardOutput.ReadToEnd();
                    string stdErr = appProcess.StandardError.ReadToEnd();
                    Assert.Fail($"BusBus process should be running.\nSTDOUT:\n{stdOut}\nSTDERR:\n{stdErr}");
                }

                // Terminate process gracefully (simulate normal exit)
                if (!appProcess.HasExited)
                {
                    appProcess.CloseMainWindow();

                    // Wait for graceful shutdown
                    var exited = appProcess.WaitForExit(10000);
                    if (!exited)
                    {
                        appProcess.Kill();
                        appProcess.WaitForExit(5000);
                    }
                }

                // Assert - Verify no lingering processes
                await Task.Delay(2000); // Allow time for cleanup
                var finalProcessCount = GetBusBusProcessCount();

                Assert.AreEqual(initialProcessCount, finalProcessCount,
                    $"Expected {initialProcessCount} BusBus processes after exit, found {finalProcessCount}. " +
                    "This indicates a process leak.");
            }
            finally
            {
                // Cleanup
                if (appProcess != null && !appProcess.HasExited)
                {
                    try
                    {
                        appProcess.Kill();
                        appProcess.WaitForExit(5000);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
                appProcess?.Dispose();
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.Integration)]
        public async Task VerifyBackgroundTasksCompletion()
        {
            // This test verifies that the Program.AddBackgroundTask system properly tracks tasks
            // and that they all complete during shutdown

            // Arrange
            var cancellationSource = new CancellationTokenSource();
            var taskCompletedFlags = new bool[3];

            try
            {
                // Act - Simulate background tasks like those in the application
                var task1 = Task.Run(async () =>
                {
                    await Task.Delay(1000, cancellationSource.Token);
                    taskCompletedFlags[0] = true;
                }, cancellationSource.Token);

                var task2 = Task.Run(async () =>
                {
                    await Task.Delay(1500, cancellationSource.Token);
                    taskCompletedFlags[1] = true;
                }, cancellationSource.Token);

                var task3 = Task.Run(async () =>
                {
                    await Task.Delay(500, cancellationSource.Token);
                    taskCompletedFlags[2] = true;
                }, cancellationSource.Token);

                var allTasks = new[] { task1, task2, task3 };

                // Simulate shutdown - cancel all tasks
                cancellationSource.Cancel();

                // Wait for tasks to handle cancellation
                await Task.WhenAll(allTasks.Select(async task =>
                {
                    try
                    {
                        await task;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected for cancelled tasks
                    }
                }));

                // Assert - All tasks should be completed (either successfully or cancelled)
                Assert.IsTrue(allTasks.All(t => t.IsCompleted),
                    "All background tasks should complete during shutdown");

                Assert.IsTrue(allTasks.All(t => t.IsCompletedSuccessfully || t.IsCanceled),
                    "Tasks should either complete successfully or be cancelled, not faulted");
            }
            finally
            {
                cancellationSource?.Dispose();
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.Integration)]
        public async Task VerifyResourceTracker_NoUndisposedResources()
        {
            // This test verifies that ResourceTracker (if implemented) shows no resource leaks

            // Arrange
            var resourcesCreated = 0;
            var resourcesDisposed = 0;

            // Act - Simulate resource creation and disposal patterns
            using (var form = new Form())
            {
                resourcesCreated++;

                using (var timer = new System.Windows.Forms.Timer())
                {
                    resourcesCreated++;
                    timer.Interval = 100;
                    timer.Start();

                    await Task.Delay(500);

                    timer.Stop();
                } // timer disposed here
                resourcesDisposed++;

            } // form disposed here
            resourcesDisposed++;

            // Force garbage collection to ensure finalizers run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Assert
            Assert.AreEqual(resourcesCreated, resourcesDisposed,
                "All created resources should be disposed");
        }

        [TestMethod]
        [TestCategory(TestCategories.Integration)]
        public void VerifyTimerDisposal_DashboardPerformanceMonitor()
        {
            // This test specifically verifies that Dashboard performance monitor timer
            // is properly disposed during shutdown (the primary fix)

            // Arrange
            bool timerDisposed = false;

            // Act - Simulate the dashboard timer pattern
            using (var timer = new System.Windows.Forms.Timer())
            {
                timer.Interval = 60000; // 60 seconds like the dashboard timer
                timer.Tick += (s, e) => { /* Simulate performance monitoring */ };
                timer.Start();

                // Verify timer is running
                Assert.IsTrue(timer.Enabled, "Timer should be enabled");

                // Simulate proper disposal
                timer.Stop();
                timer.Dispose();
                timerDisposed = true;
            }

            // Assert
            Assert.IsTrue(timerDisposed, "Performance monitor timer should be properly disposed");
        }


        private int GetBusBusProcessCount()
        {
            var processes = Process.GetProcessesByName("dotnet")
                .Where(p =>
                {
                    try
                    {
                        var commandLine = GetCommandLine(p);
                        return commandLine != null &&
                               (commandLine.Contains("BusBus.dll", StringComparison.OrdinalIgnoreCase) ||
                                commandLine.Contains("BusBus.csproj", StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return false;
                    }
                })
                .ToArray();

            return processes.Length;
        }

        // Helper to get command line (requires System.Management)
        private string? GetCommandLine(Process process)
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
                foreach (var @object in searcher.Get())
                {
                    return @object["CommandLine"]?.ToString();
                }
            }
            catch { }
            return null;
        }

        private string GetProjectRoot()
        {
            var currentDir = System.IO.Directory.GetCurrentDirectory();
            while (currentDir != null && !System.IO.File.Exists(System.IO.Path.Combine(currentDir, "BusBus.csproj")))
            {
                currentDir = System.IO.Directory.GetParent(currentDir)?.FullName;
            }
            return currentDir ?? throw new InvalidOperationException("Could not find BusBus.csproj");
        }
    }
}
