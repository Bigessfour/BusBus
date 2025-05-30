#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System;
using System.IO;
using System.Linq;
using BusBus.Analytics;

namespace BusBus.Analytics
{
    public class AnalyzerRunner
    {
        // Remove the Main method to avoid multiple entry points
        public static void RunAnalysis(string[] args)
        {
            // Remove unused variable to fix CS0219
            // var analyzer = new ProjectAnalyzer();
            var projectPath = @"c:\Users\steve.mckitrick\Desktop\BusBus";

            // Analyze all C# files in the project
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"));

            Console.WriteLine("Analyzing BusBus Project...\n");

            foreach (var file in csFiles)
            {
                try
                {
                    // The AnalyzeMethod returns void, so we need to check the actual signature
                    ProjectAnalyzer.AnalyzeMethod(file, "SomeMethod", 0, 0, out bool hasIssue, out string issue);
                    Console.WriteLine($"File: {Path.GetFileName(file)}");
                    if (hasIssue)
                    {
                        Console.WriteLine($"  Issue: {issue}");
                    }
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error analyzing {file}: {ex.Message}");
                }
            }
        }
    }
}
