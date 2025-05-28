using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusBus.UI.Diagnostics;

namespace BusBus
{
    public static class DashboardDiagnosticRunner
    {
        public static async Task<DiagnosticReport> RunDashboardDiagnosticsAsync(IServiceProvider serviceProvider)
        {
            // Get a logger
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<DashboardLoadingDiagnostics>() ??
                         Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<DashboardLoadingDiagnostics>();
            
            // Create the diagnostics tool
            var diagnostics = new DashboardLoadingDiagnostics(serviceProvider, logger);
            
            // Run diagnostics with a timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var report = await diagnostics.RunDiagnosticsAsync(cts.Token);
            
            // Save diagnostic report to file
            SaveDiagnosticReport(report);
            
            // Show results in a simple dialog
            ShowDiagnosticResults(report);
            
            return report;
        }
        
        private static void SaveDiagnosticReport(DiagnosticReport report)
        {
            try
            {
                // Create diagnostics directory if it doesn't exist
                var diagDir = Path.Combine(Directory.GetCurrentDirectory(), "diagnostics");
                if (!Directory.Exists(diagDir))
                {
                    Directory.CreateDirectory(diagDir);
                }
                
                // Generate filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = Path.Combine(diagDir, $"dashboard_diag_{timestamp}.log");
                
                // Write report to file
                File.WriteAllText(filename, report.ToString());
                
                Console.WriteLine($"[DashboardDiagnosticRunner] Report saved to {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardDiagnosticRunner] Error saving report: {ex.Message}");
            }
        }
        
        private static void ShowDiagnosticResults(DiagnosticReport report)
        {
            var message = report.ToString();
            
            var icon = report.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
            var title = $"Dashboard Diagnostics Results ({(report.Success ? "Success" : "Issues Found")})";
            
            // Create a more focused summary message for the dialog
            var summaryMessage = 
                $"Dashboard Loading Diagnostics Summary\r\n" +
                $"=============================\r\n" +
                $"Total Time: {report.TotalElapsedMs}ms\r\n" +
                $"UI Components: {report.ComponentsCreated}\r\n" +
                $"Tests Run: {report.TestResults.Count}\r\n" +
                $"Overall Status: {(report.Success ? "SUCCESS" : "ISSUES DETECTED")}\r\n\r\n" +
                $"See detailed log file in diagnostics folder.";
                
            MessageBox.Show(summaryMessage, title, MessageBoxButtons.OK, icon);
        }
    }
}
