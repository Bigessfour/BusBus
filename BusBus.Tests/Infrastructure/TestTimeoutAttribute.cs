#pragma warning disable NUnit1033 // The Write methods are wrappers on TestContext.Out
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BusBus.Tests.Infrastructure
{
    /// <summary>
    /// Attribute to enforce test timeouts and prevent hanging tests.
    /// Uses NUnit's CancelAfterAttribute for safe async timeouts, with additional logging.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class TestTimeoutAttribute : NUnit.Framework.CancelAfterAttribute, ITestAction
    {
        /// <summary>
        /// Creates a test timeout attribute
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 30 seconds)</param>
        public TestTimeoutAttribute(int timeoutMs = 30000) : base(timeoutMs)
        {
            // Log when timeout is applied (constructor runs at discovery, not execution)
        }

        public void BeforeTest(NUnit.Framework.Interfaces.ITest test)
        {
            ArgumentNullException.ThrowIfNull(test);
            TestTimeoutHelper.RecordTestStart(test.FullName);
            TestContext.WriteLine($"[TestTimeout] Starting test: {test.FullName}");
        }

        public void AfterTest(NUnit.Framework.Interfaces.ITest test)
        {
            ArgumentNullException.ThrowIfNull(test);
            TestTimeoutHelper.RecordTestEnd(test.FullName);
            TestContext.WriteLine($"[TestTimeout] Finished test: {test.FullName}");
        }

        public ActionTargets Targets => ActionTargets.Test;
    }

    /// <summary>
    /// Utility class for test timeouts and monitoring
    /// </summary>
    public static class TestTimeoutHelper
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, DateTime> _testStartTimes = new Dictionary<string, DateTime>();

        /// <summary>
        /// Records when a test starts (for debugging hanging tests)
        /// </summary>
        public static void RecordTestStart(string testName)
        {
            lock (_lock)
            {
                _testStartTimes[testName] = DateTime.Now;
                TestContext.WriteLine($"[TestTimeout] Test '{testName}' started at {DateTime.Now:HH:mm:ss.fff}");
            }
        }

        /// <summary>
        /// Records when a test completes
        /// </summary>
        public static void RecordTestEnd(string testName)
        {
            lock (_lock)
            {
                if (_testStartTimes.TryGetValue(testName, out DateTime startTime))
                {
                    var duration = DateTime.Now - startTime;
                    TestContext.WriteLine($"[TestTimeout] Test '{testName}' completed in {duration.TotalMilliseconds:F0}ms");
                    _testStartTimes.Remove(testName);
                }
            }
        }

        /// <summary>
        /// Gets currently running tests (for debugging)
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
}
