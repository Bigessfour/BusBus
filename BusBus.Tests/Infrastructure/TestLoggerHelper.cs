using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Use the LoggerMessage delegates for improved performance
#pragma warning disable CA1840 // Use 'Environment.CurrentManagedThreadId' instead of 'Thread.CurrentThread.ManagedThreadId'

namespace BusBus.Tests.Infrastructure
{
    /// <summary>
    /// Helper class for enhanced test logging and timeout management
    /// </summary>
    public class TestLoggerHelper
    {
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;
        private readonly string _testName;
        private readonly Dictionary<string, object> _metrics;

        public TestLoggerHelper(ILogger logger, string testName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _testName = testName ?? throw new ArgumentNullException(nameof(testName));
            _stopwatch = new Stopwatch();
            _metrics = new Dictionary<string, object>();
        }

        /// <summary>
        /// Starts test execution logging
        /// </summary>
        public void StartTest()
        {
            _stopwatch.Start();
            _logger.LogInformation("🚀 Starting test: {TestName} at {Timestamp}",
                _testName, DateTime.UtcNow);

            LogSystemInfo();
        }

        /// <summary>
        /// Completes test execution logging
        /// </summary>
        public void CompleteTest(bool success = true)
        {
            _stopwatch.Stop();
            var duration = _stopwatch.Elapsed;

            var status = success ? "✅ PASSED" : "❌ FAILED";
            _logger.LogInformation("{Status} test: {TestName} completed in {Duration}ms",
                status, _testName, duration.TotalMilliseconds);

            LogMetrics();
        }

        /// <summary>
        /// Logs a test step with timing
        /// </summary>
        public void LogStep(string stepName, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var stepStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("📝 Step: {StepName} starting...", stepName);

            try
            {
                action();
                stepStopwatch.Stop();
                _logger.LogInformation("✅ Step: {StepName} completed in {Duration}ms",
                    stepName, stepStopwatch.ElapsedMilliseconds);

                _metrics[$"step_{stepName}_duration_ms"] = stepStopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                stepStopwatch.Stop();
                _logger.LogError(ex, "❌ Step: {StepName} failed after {Duration}ms",
                    stepName, stepStopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Logs an async test step with timing
        /// </summary>
        public async Task LogStepAsync(string stepName, Func<Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var stepStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("📝 Async Step: {StepName} starting...", stepName);

            try
            {
                await action();
                stepStopwatch.Stop();
                _logger.LogInformation("✅ Async Step: {StepName} completed in {Duration}ms",
                    stepName, stepStopwatch.ElapsedMilliseconds);

                _metrics[$"async_step_{stepName}_duration_ms"] = stepStopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                stepStopwatch.Stop();
                _logger.LogError(ex, "❌ Async Step: {StepName} failed after {Duration}ms",
                    stepName, stepStopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Executes an action with timeout and cancellation support
        /// </summary>
        public async Task<T> WithTimeoutAsync<T>(Func<CancellationToken, Task<T>> action,
            TimeSpan timeout, string operationName = "Operation")
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using var cts = new CancellationTokenSource(timeout);
            var timeoutTask = Task.Delay(timeout, cts.Token);

            _logger.LogInformation("⏱️ Starting {OperationName} with {TimeoutSeconds}s timeout",
                operationName, timeout.TotalSeconds);

            try
            {
                var operationTask = action(cts.Token);
                var completedTask = await Task.WhenAny(operationTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogError("⏰ {OperationName} timed out after {TimeoutSeconds}s",
                        operationName, timeout.TotalSeconds);
                    throw new TimeoutException($"{operationName} timed out after {timeout.TotalSeconds} seconds");
                }

                cts.Cancel(); // Cancel the timeout task
                return await operationTask;
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                _logger.LogError("⏰ {OperationName} was cancelled due to timeout", operationName);
                throw new TimeoutException($"{operationName} was cancelled due to timeout");
            }
        }

        /// <summary>
        /// Executes an action with timeout and cancellation support (void return)
        /// </summary>
        public async Task WithTimeoutAsync(Func<CancellationToken, Task> action,
            TimeSpan timeout, string operationName = "Operation")
        {
            await WithTimeoutAsync(async ct =>
            {
                await action(ct);
                return true;
            }, timeout, operationName);
        }

        /// <summary>
        /// Adds a custom metric for the test
        /// </summary>
        public void AddMetric(string name, object value)
        {
            _metrics[name] = value;
        }

        /// <summary>
        /// Logs system information relevant to testing
        /// </summary>
        private void LogSystemInfo()
        {
            var processId = Environment.ProcessId;
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024; // MB

            _logger.LogInformation("🖥️ System Info - PID: {ProcessId}, ThreadId: {ThreadId}, Memory: {MemoryMB}MB",
                processId, threadId, memoryUsage);
        }

        /// <summary>
        /// Logs collected metrics
        /// </summary>
        private void LogMetrics()
        {
            if (_metrics.Count > 0)
            {
                _logger.LogInformation("📊 Test Metrics for {TestName}:", _testName);
                foreach (var metric in _metrics)
                {
                    _logger.LogInformation("   {MetricName}: {MetricValue}", metric.Key, metric.Value);
                }
            }

            // Log final timing
            _logger.LogInformation("⏱️ Total test duration: {TotalDuration}ms",
                _stopwatch.ElapsedMilliseconds);
        }
    }
}
