#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.Monitoring
{
    public static class PerformanceMonitor
    {
        private static PerformanceCounter cpuCounter;
        private static PerformanceCounter ramCounter;
        private static DateTime applicationStartTime;

        static PerformanceMonitor()
        {
            applicationStartTime = DateTime.Now;
            InitializeCounters();
        }

        private static void InitializeCounters()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Performance counter initialization failed: {ex.Message}");
            }
        }

        public static async Task<PerformanceMetrics> GetCurrentMetricsAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new PerformanceMetrics
                {
                    Timestamp = DateTime.Now,
                    ApplicationUptime = DateTime.Now - applicationStartTime,
                    MemoryUsage = GC.GetTotalMemory(false) / 1024 / 1024, // MB
                    ThreadCount = Process.GetCurrentProcess().Threads.Count
                };

                try
                {
                    metrics.CpuUsage = cpuCounter?.NextValue() ?? 0;
                    metrics.AvailableMemory = ramCounter?.NextValue() ?? 0;
                }
                catch
                {
                    metrics.CpuUsage = 0;
                    metrics.AvailableMemory = 0;
                }

                return metrics;
            });
        }

        public static void LogDatabaseOperation(string operation, TimeSpan duration)
        {
            System.Diagnostics.Debug.WriteLine($"DB Operation: {operation} took {duration.TotalMilliseconds}ms");
        }
    }

    public class PerformanceMetrics
    {
        public DateTime Timestamp { get; set; }
        public TimeSpan ApplicationUptime { get; set; }
        public float CpuUsage { get; set; }
        public float AvailableMemory { get; set; }
        public long MemoryUsage { get; set; }
        public int ThreadCount { get; set; }
    }
}
