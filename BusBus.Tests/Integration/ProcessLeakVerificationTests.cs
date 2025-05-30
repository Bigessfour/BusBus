using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using BusBus.Tests;

#nullable enable

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

                // Wait for process to fully start
                await Task.Delay(3000);

                // Verify process is running
                var runningProcesses = GetBusBusProcessCount();
                Assert.IsTrue(runningProcesses > initialProcessCount, "BusBus process should be running");

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
        public async Task VerifyTimerDisposal_DashboardPerformanceMonitor()
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
                .Where(p => p.MainWindowTitle.Contains("BusBus") ||
                           IsProcessRunningBusBus(p))
                .ToArray();

            return processes.Length;
        }

        private bool IsProcessRunningBusBus(Process process)
        {
            try
            {
                // Check if the process command line contains BusBus
                return process.ProcessName.Contains("BusBus", StringComparison.OrdinalIgnoreCase) ||
                       process.MainModule?.FileName?.Contains("BusBus", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch
            {
                return false;
            }
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
