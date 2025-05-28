using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

#nullable enable

namespace BusBus.UI.Diagnostics
{
    /// <summary>
    /// Performs comprehensive diagnostics on dashboard loading performance
    /// </summary>
    public class DashboardLoadingDiagnostics
    {
        // Define LoggerMessage delegates for better performance
        private static class LogMessages
        {
            private static readonly Action<ILogger, Exception?> _diagnosticError =
                LoggerMessage.Define(LogLevel.Error,
                    new EventId(1, nameof(DiagnosticError)),
                    "Diagnostic error");

            private static readonly Action<ILogger, string, Exception?> _testError =
                LoggerMessage.Define<string>(LogLevel.Error,
                    new EventId(2, nameof(TestError)),
                    "Test error: {TestName}");

            private static readonly Action<ILogger, string, long, Exception?> _testCompleted =
                LoggerMessage.Define<string, long>(LogLevel.Debug,
                    new EventId(3, nameof(TestCompleted)),
                    "Test completed: {TestName} in {ElapsedMs}ms");

            private static readonly Action<ILogger, string, Exception?> _testStarted =
                LoggerMessage.Define<string>(LogLevel.Information,
                    new EventId(4, nameof(TestStarted)),
                    "Starting test: {TestName}");

            private static readonly Action<ILogger, Exception?> _diagnosticsStarted =
                LoggerMessage.Define(LogLevel.Information,
                    new EventId(5, nameof(DiagnosticsStarted)),
                    "Starting dashboard loading diagnostics");

            private static readonly Action<ILogger, long, Exception?> _diagnosticsCompleted =
                LoggerMessage.Define<long>(LogLevel.Information,
                    new EventId(6, nameof(DiagnosticsCompleted)),
                    "Dashboard diagnostics completed in {TotalElapsedMs}ms");

            public static void DiagnosticError(ILogger logger, Exception ex) =>
                _diagnosticError(logger, ex);

            public static void TestError(ILogger logger, string testName, Exception ex) =>
                _testError(logger, testName, ex);

            public static void TestCompleted(ILogger logger, string testName, long elapsedMs) =>
                _testCompleted(logger, testName, elapsedMs, null);

            public static void TestStarted(ILogger logger, string testName) =>
                _testStarted(logger, testName, null);

            public static void DiagnosticsStarted(ILogger logger) =>
                _diagnosticsStarted(logger, null);

            public static void DiagnosticsCompleted(ILogger logger, long totalElapsedMs) =>
                _diagnosticsCompleted(logger, totalElapsedMs, null);

            public static void DiagnosticsFailed(ILogger logger, Exception ex) =>
                _diagnosticError(logger, ex);
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DashboardLoadingDiagnostics> _logger;
        private readonly Stopwatch _stopwatch;
        private readonly Dictionary<string, long> _timingMetrics;

        public DashboardLoadingDiagnostics(IServiceProvider serviceProvider, ILogger<DashboardLoadingDiagnostics> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stopwatch = new Stopwatch();
            _timingMetrics = new Dictionary<string, long>();
        }

        /// <summary>
        /// Runs comprehensive dashboard loading diagnostics
        /// </summary>
        public async Task<DiagnosticReport> RunDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            var report = new DiagnosticReport
            {
                StartTime = DateTime.Now
            };

            _stopwatch.Start();
            LogMessages.DiagnosticsStarted(_logger);

            try
            {
                // Test 1: Service Resolution
                await TestServiceResolution(report, cancellationToken);

                // Test 2: Database Connectivity
                await TestDatabaseConnectivity(report, cancellationToken);

                // Test 3: UI Component Creation
                await TestUIComponentCreation(report, cancellationToken);

                // Test 4: Data Loading Performance
                await TestDataLoadingPerformance(report, cancellationToken);

                _stopwatch.Stop();
                report.TotalElapsedMs = _stopwatch.ElapsedMilliseconds;
                report.TimingMetrics = new Dictionary<string, long>(_timingMetrics);

                LogMessages.DiagnosticsCompleted(_logger, report.TotalElapsedMs);

                return report;
            }
            catch (Exception ex)
            {
                _stopwatch.Stop();
                report.TotalElapsedMs = _stopwatch.ElapsedMilliseconds;

                report.TestResults.Add(new DiagnosticResult
                {
                    TestName = "Overall Diagnostics",
                    Success = false,
                    ElapsedMs = _stopwatch.ElapsedMilliseconds,
                    Message = $"Diagnostics failed: {ex.Message}"
                });

                LogMessages.DiagnosticsFailed(_logger, ex);
                return report;
            }
        }

        private Task TestServiceResolution(DiagnosticReport report, CancellationToken cancellationToken)
        {
            var testStopwatch = Stopwatch.StartNew();
            var testName = "Service Resolution";

            try
            {
                LogMessages.TestStarted(_logger, testName);

                // Test core services
                var dashboard = _serviceProvider.GetService<Dashboard>();
                var dbContext = _serviceProvider.GetService<DataAccess.AppDbContext>();

                testStopwatch.Stop();
                _timingMetrics["ServiceResolution"] = testStopwatch.ElapsedMilliseconds;

                report.TestResults.Add(new DiagnosticResult
                {
                    TestName = testName,
                    Success = dashboard != null && dbContext != null,
                    ElapsedMs = testStopwatch.ElapsedMilliseconds,
                    Message = $"Dashboard: {(dashboard != null ? "OK" : "FAILED")}, DbContext: {(dbContext != null ? "OK" : "FAILED")}"
                });

                LogMessages.TestCompleted(_logger, testName, testStopwatch.ElapsedMilliseconds);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                testStopwatch.Stop(); _timingMetrics["ServiceResolution"] = testStopwatch.ElapsedMilliseconds;

                report.TestResults.Add(new DiagnosticResult
                {
                    TestName = testName,
                    Success = false,
                    ElapsedMs = testStopwatch.ElapsedMilliseconds,
                    Message = $"Service resolution failed: {ex.Message}"
                });

                LogMessages.TestError(_logger, testName, ex);
                return Task.CompletedTask;
            }
        }

        private async Task TestDatabaseConnectivity(DiagnosticReport report, CancellationToken cancellationToken)
        {
            var testStopwatch = Stopwatch.StartNew();
            var testName = "Database Connectivity";

            try
            {
                LogMessages.TestStarted(_logger, testName);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataAccess.AppDbContext>();

                // Test database connection
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

                testStopwatch.Stop();
                _timingMetrics["DatabaseConnectivity"] = testStopwatch.ElapsedMilliseconds;

                report.TestResults.Add(new DiagnosticResult
                {
                    TestName = testName,
                    Success = canConnect,
                    ElapsedMs = testStopwatch.ElapsedMilliseconds,
                    Message = canConnect ? "Database connection successful" : "Database connection failed"
                });

                LogMessages.TestCompleted(_logger, testName, testStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                testStopwatch.Stop(); _timingMetrics["DatabaseConnectivity"] = testStopwatch.ElapsedMilliseconds;

                report.TestResults.Add(new DiagnosticResult
                {
                    TestName = testName,
                    Success = false,
                    ElapsedMs = testStopwatch.ElapsedMilliseconds,
                    Message = $"Database connectivity test failed: {ex.Message}"
                });

                LogMessages.TestError(_logger, testName, ex);
            }
        }

        private Task TestUIComponentCreation(DiagnosticReport report, CancellationToken cancellationToken)
        {
            var testStopwatch = Stopwatch.StartNew();
            var testName = "UI Component Creation";

            try
            {
                LogMessages.TestStarted(_logger, testName);

                // Test dashboard creation
                var dashboard = _serviceProvider.GetService<Dashboard>();

                int componentCount = 0;
                if (dashboard != null)
                {
                    // Count controls (simplified test)
                    componentCount = CountControlsRecursively(dashboard);
                    report.ComponentsCreated = componentCount;
                }

                testStopwatch.Stop();
                _timingMetrics["UIComponentCreation"] = testStopwatch.ElapsedMilliseconds;

                report.TestResults.Add(new DiagnosticResult
                {
                    TestName = testName,
                    Success = dashboard != null && componentCount > 0,
                    ElapsedMs = testStopwatch.ElapsedMilliseconds,
                    Message = $"Dashboard created with {componentCount} components"
                });

                LogMessages.TestCompleted(_logger, testName, testStopwatch.ElapsedMilliseconds);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                testStopwatch.Stop(); _timingMetrics["UIComponentCreation"] = testStopwatch.ElapsedMilliseconds;

                report.TestResults.Add(new DiagnosticResult
                {
                    TestName = testName,
                    Success = false,
                    ElapsedMs = testStopwatch.ElapsedMilliseconds,
                    Message = $"UI component creation test failed: {ex.Message}"
                });

                LogMessages.TestError(_logger, testName, ex);
                return Task.CompletedTask;
            }
        }

        private async Task TestDataLoadingPerformance(DiagnosticReport report, CancellationToken cancellationToken)
        {
            var testStopwatch = Stopwatch.StartNew();
            var testName = "Data Loading Performance";

            try
            {
                LogMessages.TestStarted(_logger, testName);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataAccess.AppDbContext>();
                // Test sample data queries
                var routeCount = await dbContext.Routes.CountAsync(cancellationToken);
                var driverCount = await dbContext.Drivers.CountAsync(cancellationToken);

                testStopwatch.Stop();
                _timingMetrics["DataLoading"] = testStopwatch.ElapsedMilliseconds;

                report.TestResults.Add(new DiagnosticResult
                {
                    TestName = testName,
                    Success = true,
                    ElapsedMs = testStopwatch.ElapsedMilliseconds,
                    Message = $"Loaded data: {routeCount} routes, {driverCount} drivers"
                });

                LogMessages.TestCompleted(_logger, testName, testStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                testStopwatch.Stop(); _timingMetrics["DataLoading"] = testStopwatch.ElapsedMilliseconds;

                report.TestResults.Add(new DiagnosticResult
                {
                    TestName = testName,
                    Success = false,
                    ElapsedMs = testStopwatch.ElapsedMilliseconds,
                    Message = $"Data loading test failed: {ex.Message}"
                });

                LogMessages.TestError(_logger, testName, ex);
            }
        }

        private static int CountControlsRecursively(System.Windows.Forms.Control control)
        {
            int count = 1; // Count the control itself

            foreach (System.Windows.Forms.Control child in control.Controls)
            {
                count += CountControlsRecursively(child);
            }

            return count;
        }
    }
}
