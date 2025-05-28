#nullable enable
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI
{    /// <summary>
     /// Modern async-friendly application bootstrap for Windows Forms
     /// Implements .NET 8 Windows Forms best practices and patterns
     /// </summary>
    public static class ModernApplicationBootstrap
    {
        /// <summary>
        /// Configures the application with .NET 8 Windows Forms features
        /// </summary>
        public static void Configure()
        {
            // .NET 8 DPI improvements - enable advanced scaling
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            // Modern Windows Forms visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // .NET 8 Modern font configuration with Segoe UI
            try
            {
                Application.SetDefaultFont(new Font("Segoe UI", 9F, FontStyle.Regular));
            }
            catch (Exception)
            {
                // Fallback to system default if Segoe UI is not available
                Application.SetDefaultFont(SystemFonts.DefaultFont);
            }

            // Enhanced thread safety checks
            Control.CheckForIllegalCrossThreadCalls = true;

            // .NET 8 Improved exception handling
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Register for global exception handling
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.ThreadException += OnThreadException;
        }/// <summary>
         /// Async-friendly application runner (prepares for .NET 9)
         /// </summary>
        public static async Task RunAsync<TForm>(Func<Task<TForm>> formFactory) where TForm : Form
        {
            ArgumentNullException.ThrowIfNull(formFactory);

            Configure();

            try
            {
                var form = await formFactory();
                Application.Run(form);
            }
            catch (Exception ex)
            {
                // Modern error handling
                MessageBox.Show(
                    $"Application startup failed: {ex.Message}",
                    "Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// Handles unhandled exceptions from the application domain
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception ?? new Exception("Unknown error occurred");
            LogException("AppDomain", exception);

            if (e.IsTerminating)
            {
                MessageBox.Show(
                    $"A critical error occurred and the application must close.\n\nError: {exception.Message}",
                    "Critical Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles unhandled exceptions from Windows Forms threads
        /// </summary>
        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            LogException("UI Thread", e.Exception);

            var result = MessageBox.Show(
                $"An error occurred in the user interface.\n\nError: {e.Exception.Message}\n\nWould you like to continue running the application?",
                "Application Error",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// Logs exceptions to the console and debug output
        /// </summary>
        private static void LogException(string source, Exception exception)
        {
            var message = $"[{source}] Unhandled exception: {exception.Message}";
            Console.WriteLine(message);
            System.Diagnostics.Debug.WriteLine($"{message}\nStack trace: {exception.StackTrace}");
        }
    }
}
