using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BusBus.DataAccess;
using BusBus.Services;
using BusBus.UI;
using System;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusBus
{
    internal static class Program
    {
        private static readonly int[] SqlRetryErrorNumbers = new[] { 1205, 10054 };
        private static readonly List<Task> _backgroundTasks = new List<Task>();
        private static readonly CancellationTokenSource _appCancellationTokenSource = new CancellationTokenSource();
        private static IServiceProvider? _serviceProvider;

        public static void AddBackgroundTask(Task task)
        {
            Console.WriteLine($"[Program] Adding background task: {task.Id}");
            lock (_backgroundTasks)
            {
                _backgroundTasks.Add(task);
            }
            Console.WriteLine($"[Program] Added background task. Total tasks: {_backgroundTasks.Count}");
        }        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            Console.WriteLine("[Program] Application starting...");

            // Add a console handler for Ctrl+C to ensure clean shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("[Program] Console cancel key pressed - initiating shutdown");
                e.Cancel = true; // Cancel the immediate termination
                ShutdownApplication();
                Environment.Exit(0);
            };

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Initialize theme system with dark theme for testing
            Console.WriteLine("[Program] Initializing theme system...");
            try 
            {
                var args = Environment.GetCommandLineArgs();
                
                // Default to dark theme unless explicitly requested otherwise
                bool startWithLightTheme = args.Length > 1 && args[1].Equals("--light", StringComparison.OrdinalIgnoreCase);
                
                if (startWithLightTheme)
                {
                    Console.WriteLine("[Program] Starting with light theme...");
                    ThemeManager.SwitchTheme("Light");
                }
                else
                {
                    Console.WriteLine("[Program] Starting with dark theme (default)...");
                    ThemeManager.SwitchTheme("Dark");
                }
                  Console.WriteLine($"[Program] Current theme: {ThemeManager.CurrentTheme.Name}");
                Console.WriteLine($"[Program] Available themes: {string.Join(", ", ThemeManager.AvailableThemes)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] Error initializing theme: {ex.Message}");
            }// Setup application exit handler (nullable EventHandler)
            EventHandler? applicationExitHandler = null;
            applicationExitHandler = (sender, e) =>
            {
                Console.WriteLine("[Program] Application exit handler started");
                ShutdownApplication();
            };

            Application.ApplicationExit += applicationExitHandler;            try
            {
                // Seed sample data into the RouteService
                var routeService = _serviceProvider.GetRequiredService<IRouteService>();
                Console.WriteLine("[Program] Seeding sample data...");
                await routeService.SeedSampleDataAsync();
                Console.WriteLine("[Program] Sample data seeded successfully");

                var dashboard = _serviceProvider.GetRequiredService<Dashboard>();
                Console.WriteLine("[Program] Running main form...");
                Application.Run(dashboard);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Program] Invalid operation: {ex}");
                MessageBox.Show($"An invalid operation occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            catch (System.Data.DataException ex)
            {
                Console.WriteLine($"[Program] Data error: {ex}");
                MessageBox.Show($"A data error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
            {
                Console.WriteLine($"[Program] Unhandled exception: {ex}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            finally
            {
                Console.WriteLine("[Program] Application.Run completed - ensuring shutdown");
                ShutdownApplication();
            }
        }

        private static void ShutdownApplication()
        {
            Console.WriteLine("[Program] Starting application shutdown");

            // Log task status before disposal
            foreach (var task in _backgroundTasks)
            {
                if (!task.IsCompleted)
                {
                    Console.WriteLine($"[Program] Task {task.Id} not completed: {task.Status}");
                }
            }

            _appCancellationTokenSource.Cancel();
            Console.WriteLine("[Program] Cancellation token signaled");

            // Wait for background tasks to complete with timeout
            Task[] tasksToWait;
            lock (_backgroundTasks)
            {
                tasksToWait = _backgroundTasks.ToArray();
            }

            Console.WriteLine($"[Program] Waiting for {tasksToWait.Length} background tasks to complete...");
            try
            {
                // Wait for tasks with 5 second timeout
                var waitTask = Task.WhenAll(tasksToWait);
                if (!waitTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    Console.WriteLine("[Program] Timeout waiting for background tasks - forcing shutdown");
                }
                else
                {
                    Console.WriteLine("[Program] All background tasks completed");
                }
            }
            catch (AggregateException ex)
            {
                Console.WriteLine($"[Program] Error waiting for tasks: {ex.Message}");
            }

            // Dispose service provider
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                Console.WriteLine("[Program] Service provider disposed");
            }

            // Give time for cleanup
            Thread.Sleep(500);
            Console.WriteLine("[Program] Application shutdown complete");

            // Force exit if needed
            Environment.Exit(0);
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Add DbContext with SQL Server and retry on transient failures
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(60),
                        errorNumbersToAdd: SqlRetryErrorNumbers
                    )
                )
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
            );            // Register RouteService with singleton pattern to maintain in-memory data
            services.AddSingleton<IRouteService, RouteService>();

            // Register AppDbContext as IAppDbContext
            services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            // Register main form
            services.AddScoped<Dashboard>();
        }

        public static CancellationToken AppCancellationToken => _appCancellationTokenSource.Token;
    }
}
