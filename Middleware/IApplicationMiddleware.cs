#nullable enable
using System;
using System.Threading.Tasks;

namespace BusBus.Middleware
{
    /// <summary>
    /// Modern middleware pipeline for Windows Forms applications
    /// Enables request/response processing similar to ASP.NET Core
    /// </summary>
    public interface IApplicationMiddleware
    {
        Task InvokeAsync(ApplicationContext context, Func<Task> next);
    }

    /// <summary>
    /// Application context for middleware pipeline
    /// </summary>
    public class ApplicationContext
    {
        public string RequestType { get; set; } = string.Empty;
        public object? Data { get; set; }
        public Exception? Exception { get; set; }
        public bool IsHandled { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
