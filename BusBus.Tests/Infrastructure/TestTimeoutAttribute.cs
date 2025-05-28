using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.Infrastructure
{
    /// <summary>
    /// MSTest-compatible timeout helper for .NET 8 compliance
    /// Use [Timeout(30000)] attribute directly on test methods for timeouts
    /// </summary>
    public static class TestTimeoutHelper
    {
        private static readonly Dictionary<string, DateTime> _testStartTimes = new Dictionary<string, DateTime>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Records the start time of a test
        /// </summary>
        public static void RecordTestStart(string testName)
        {
            if (string.IsNullOrEmpty(testName)) return;

            lock (_lock)
            {
                _testStartTimes[testName] = DateTime.Now;
                Console.WriteLine($"[TestTimeout] Test '{testName}' started at {DateTime.Now:HH:mm:ss.fff}");
            }
        }

        /// <summary>
        /// Records the end time of a test and logs duration
        /// </summary>
        public static void RecordTestEnd(string testName)
        {
            if (string.IsNullOrEmpty(testName)) return;

            lock (_lock)
            {
                if (_testStartTimes.TryGetValue(testName, out var startTime))
                {
                    var duration = DateTime.Now - startTime;
                    Console.WriteLine($"[TestTimeout] Test '{testName}' completed in {duration.TotalMilliseconds:F0}ms");
                    _testStartTimes.Remove(testName);
                }
            }
        }

        /// <summary>
        /// Gets all currently running tests (for debugging)
        /// </summary>
        public static Dictionary<string, TimeSpan> GetRunningTests()
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                return _testStartTimes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => now - kvp.Value
                );
            }
        }
    }

    /// <summary>
    /// Legacy attribute for compatibility - use [Timeout(milliseconds)] instead
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestTimeoutAttribute : Attribute
    {
        public int TimeoutMs { get; }

        public TestTimeoutAttribute(int timeoutMs = 30000)
        {
            TimeoutMs = timeoutMs;
        }
    }
}
