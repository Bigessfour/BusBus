using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusBus.UI;
using BusBus.Services;

namespace BusBus
{
    /// <summary>
    /// Test class to validate instance lifecycle logging functionality
    /// </summary>
    public static class TestLifecycleLogging
    {
        /// <summary>
        /// Test method to validate .NET instance tracking
        /// </summary>
        public static void ValidateInstanceTracking()
        {
            Console.WriteLine("=== Testing .NET Instance Lifecycle Logging ===");

            try
            {
                // Create a simple service collection for testing
                var services = new ServiceCollection();
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILogger<object>>();

                logger.LogInformation("[LIFECYCLE-TEST] Starting instance tracking validation");

                // Test 1: Log current .NET instances
                LogInstanceCount(logger, "Test Validation");

                // Test 2: Simulate some work
                logger.LogInformation("[LIFECYCLE-TEST] Simulating application work...");
                System.Threading.Thread.Sleep(100);

                // Test 3: Log instances after work
                LogInstanceCount(logger, "After Work Simulation");

                logger.LogInformation("[LIFECYCLE-TEST] Instance tracking validation completed successfully");
                Console.WriteLine("✅ Lifecycle logging validation passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lifecycle logging validation failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Log the number of active .NET instances for lifecycle tracking
        /// </summary>
        private static void LogInstanceCount(ILogger logger, string context = "")
        {
            try
            {
                var dotnetProcesses = Process.GetProcessesByName("dotnet");
                var currentProcess = Process.GetCurrentProcess();
                var busbusProcesses = Process.GetProcessesByName("BusBus");

                logger.LogInformation("[LIFECYCLE] {Context} - Active .NET instances: {DotNetCount}, BusBus instances: {BusBusCount}, Current PID: {CurrentPID}",
                    context, dotnetProcesses.Length, busbusProcesses.Length, currentProcess.Id);

                // Log memory usage for this process
                var memoryMB = currentProcess.WorkingSet64 / 1024 / 1024;
                logger.LogInformation("[LIFECYCLE] {Context} - Memory usage: {MemoryMB}MB, Threads: {ThreadCount}",
                    context, memoryMB, currentProcess.Threads.Count);

                // Dispose processes to avoid resource leaks
                foreach (var process in dotnetProcesses) process.Dispose();
                foreach (var process in busbusProcesses) process.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[LIFECYCLE] Failed to log instance count for context: {Context}", context);
            }
        }
    }
}
