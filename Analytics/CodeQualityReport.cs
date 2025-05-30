#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BusBus.Analytics
{
    public class CodeQualityReport
    {
        public CodeQualityReport()
        {
        }

        /// <summary>
        /// Generates a code quality report for the specified project path and writes it to the output path.
        /// This method does not access instance data and is marked static for performance (CA1822).
        /// </summary>
        public static void GenerateReport(string projectPath, string outputPath)
        {
            var results = new List<dynamic>();
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"));

            foreach (var file in csFiles)
            {
                // Analyze each method in the file
                // You'll need to implement method parsing
                var fileResults = AnalyzeFile(file);
                results.AddRange(fileResults);
            }

            // Generate HTML report
            var html = GenerateHtmlReport(results);
            File.WriteAllText(outputPath, html);

            // Also create a summary
            var summary = GenerateSummary(results);
            Console.WriteLine(summary);
        }

        private static string GenerateHtmlReport(List<dynamic> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><title>Code Quality Report</title></head><body>");
            sb.AppendLine("<h1>BusBus Code Quality Report</h1>");
            sb.AppendLine("<table border='1'>");
            sb.AppendLine("<tr><th>File</th><th>Method</th><th>Complexity</th><th>Lines</th><th>Issues</th></tr>");

            foreach (var result in results.OrderByDescending(r => r.CyclomaticComplexity))
            {
                var rowClass = result.CyclomaticComplexity > 10 ? "style='background-color: #ffcccc'" : "";
                sb.AppendLine($"<tr {rowClass}>");
                sb.AppendLine($"<td>{Path.GetFileName(result.FilePath)}</td>");
                sb.AppendLine($"<td>{result.MethodName}</td>");
                sb.AppendLine($"<td>{result.CyclomaticComplexity}</td>");
                sb.AppendLine($"<td>{result.LineCount}</td>");
                sb.AppendLine($"<td>{result.Issue ?? "None"}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table></body></html>");
            return sb.ToString();
        }

        public static string GenerateSummary(List<dynamic> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            var totalMethods = results.Count;
            var complexMethods = results.Count(r => r.CyclomaticComplexity > 10);
            var longMethods = results.Count(r => r.LineCount > 50);
            var methodsWithIssues = results.Count(r => r.HasIssue == "True");

            return $@"
Code Quality Summary
===================
Total Methods Analyzed: {totalMethods}
Complex Methods (CC > 10): {complexMethods}
Long Methods (> 50 lines): {longMethods}
Methods with Issues: {methodsWithIssues}

Top 5 Most Complex Methods:
{string.Join("\n", results.OrderByDescending(r => r.CyclomaticComplexity).Take(5)
    .Select(r => $"  - {r.MethodName} in {Path.GetFileName(r.FilePath)} (CC: {r.CyclomaticComplexity})"))}
";
        }

        private static List<dynamic> AnalyzeFile(string filePath)

        {
            // This is a placeholder - you'd need to implement actual method parsing
            // For now, return empty list to avoid errors
            return new List<dynamic>();
        }
    }
}

