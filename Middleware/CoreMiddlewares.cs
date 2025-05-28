#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BusBus.Middleware
{
    /// <summary>
    /// Exception handling middleware for Windows Forms applications
    /// Provides centralized error handling across the application
    /// </summary>
    public class ExceptionHandlingMiddleware : IApplicationMiddleware
    {
        public async Task InvokeAsync(ApplicationContext context, Func<Task> next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            try
            {
                await next();
            }
            catch (Exception ex)
            {
                context.Exception = ex;
                context.IsHandled = true;

                // Log the exception
                Console.WriteLine($"[ExceptionMiddleware] {context.RequestType}: {ex.Message}");

                // Could show user-friendly error dialog here
                System.Windows.Forms.MessageBox.Show(
                    $"An error occurred: {ex.Message}",
                    "Application Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }

    /// <summary>
    /// Logging middleware for tracking application operations
    /// </summary>
    public class LoggingMiddleware : IApplicationMiddleware
    {
        public async Task InvokeAsync(ApplicationContext context, Func<Task> next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            var startTime = DateTime.UtcNow;
            Console.WriteLine($"[LoggingMiddleware] Starting: {context.RequestType}");

            await next();

            var duration = DateTime.UtcNow - startTime;
            Console.WriteLine($"[LoggingMiddleware] Completed: {context.RequestType} in {duration.TotalMilliseconds}ms");
        }
    }

    /// <summary>
    /// Authentication middleware for secured operations
    /// </summary>
    public class AuthenticationMiddleware : IApplicationMiddleware
    {
        public async Task InvokeAsync(ApplicationContext context, Func<Task> next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            // Simple authentication check - could be expanded
            var isAuthenticated = !string.IsNullOrEmpty(Environment.UserName);

            if (!isAuthenticated)
            {
                context.IsHandled = true;
                System.Windows.Forms.MessageBox.Show(
                    "Access denied. Please ensure you are logged in.",
                    "Authentication Required",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            await next();
        }
    }
}
