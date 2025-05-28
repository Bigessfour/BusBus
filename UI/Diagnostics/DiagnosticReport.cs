#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusBus.UI.Diagnostics
{
    /// <summary>
    /// Represents a diagnostic test result
    /// </summary>
    public class DiagnosticResult
    {
        /// <summary>
        /// Name of the test
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the test succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Time taken in milliseconds
        /// </summary>
        public long ElapsedMs { get; set; }

        /// <summary>
        /// Additional message or error information
        /// </summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// Comprehensive diagnostic report containing all test results and timing information
    /// </summary>
    public class DiagnosticReport
    {
        /// <summary>
        /// When the diagnostic process started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Total time taken by all tests
        /// </summary>
        public long TotalElapsedMs { get; set; }

        /// <summary>
        /// Collection of individual test results
        /// </summary>
        public List<DiagnosticResult> TestResults { get; set; } = new List<DiagnosticResult>();

        /// <summary>
        /// Number of UI components created during testing
        /// </summary>
        public int ComponentsCreated { get; set; }

        /// <summary>
        /// Detailed log of the diagnostic process
        /// </summary>
        public string DiagnosticLog { get; set; } = string.Empty;

        /// <summary>
        /// Additional metrics collected during diagnostics
        /// </summary>
        public Dictionary<string, long> TimingMetrics { get; set; } = new Dictionary<string, long>();

        /// <summary>
        /// Whether all tests passed
        /// </summary>
        public bool Success => TestResults.All(r => r.Success);

        /// <summary>
        /// Convert the report to a string format for display or logging
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Dashboard Diagnostics Report");
            sb.AppendLine("==========================");
            sb.AppendLine($"Date: {StartTime}");
            sb.AppendLine($"Overall Result: {(Success ? "SUCCESS" : "FAILED")}");
            sb.AppendLine($"Total time: {TotalElapsedMs}ms");
            sb.AppendLine($"UI components created: {ComponentsCreated}");
            sb.AppendLine();

            sb.AppendLine("Test Results:");
            foreach (var result in TestResults)
            {
                sb.AppendLine($"  {result.TestName}: {(result.Success ? "PASS" : "FAIL")} ({result.ElapsedMs}ms)");
                if (!string.IsNullOrEmpty(result.Message))
                {
                    sb.AppendLine($"    Message: {result.Message}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Timing Metrics:");
            foreach (var metric in TimingMetrics.OrderBy(m => m.Value))
            {
                sb.AppendLine($"  {metric.Key}: {metric.Value}ms");
            }

            if (!string.IsNullOrEmpty(DiagnosticLog))
            {
                sb.AppendLine();
                sb.AppendLine("Diagnostic Log:");
                sb.AppendLine(DiagnosticLog);
            }

            return sb.ToString();
        }
    }
}
