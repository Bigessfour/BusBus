
#pragma warning disable CA1848 // Use LoggerMessage delegates for logging performance
// Enable nullable reference types for this file
#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BusBus.DataAccess;
using BusBus.Services;
using BusBus.UI;
using BusBus.Utils;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using BusBus.Analytics;
using BusBus.UI.Diagnostics;

namespace BusBus
{
    internal static partial class Program // Added partial modifier
    {
        // Guard to prevent duplicate shutdowns
        private static bool _shutdownInProgress = false;
        public static bool ShutdownInProgress => _shutdownInProgress;
        private static readonly int[] SqlRetryErrorNumbers = new[] { 1205, 10054 };
        private static readonly List<Task> _backgroundTasks = new List<Task>();
        private static readonly CancellationTokenSource _appCancellationTokenSource = new CancellationTokenSource();
        private static IServiceProvider? _serviceProvider; public static void AddBackgroundTask(Task task)
        {
            if (task == null)
                return;

            Console.WriteLine($"[Program] Adding background task: {task.Id}");

            // Debug information for task monitoring
            if (Environment.GetEnvironmentVariable("BUSBUS_MONITOR_BACKGROUND_TASKS") == "true")
            {
                // Get stack trace to help identify where task was created
                var stackTrace = new System.Diagnostics.StackTrace(true);
                var caller = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType?.Name;
                var method = stackTrace.GetFrame(1)?.GetMethod()?.Name;
                Console.WriteLine($"TASK_LEAK: Task {task.Id} created by {caller}.{method}");
            }

            // Don't add already completed tasks
            if (task.IsCompleted)
            {
                Console.WriteLine($"[Program] Task {task.Id} already completed, not adding to tracker");
                return;
            }

            // Add continuation to track task completion and auto-remove from the list
            task.ContinueWith(t =>
            {
                Console.WriteLine($"TASK_COMPLETED: Task {t.Id} {(t.IsFaulted ? "faulted" : t.IsCanceled ? "canceled" : "completed")}");

                // Auto-remove the task from tracking list once completed
                lock (_backgroundTasks)
                {
                    _backgroundTasks.Remove(t);
                    Console.WriteLine($"[Program] Removed completed task {t.Id}. Remaining tasks: {_backgroundTasks.Count}");
                }
            });

            lock (_backgroundTasks)
            {
                _backgroundTasks.Add(task);
            }

            // Clean the list of any completed tasks periodically
            if (_backgroundTasks.Count % 10 == 0) // Every 10 tasks added
            {
                CleanupCompletedTasks();
            }

            Console.WriteLine($"[Program] Added background task. Total tasks: {_backgroundTasks.Count}");
        }        /// <summary>
                 /// The main entry point for the application.
                 /// </summary>        [STAThread]
        static async Task Main(string[] args)
        {
            Console.WriteLine("[Program] Application starting...");            // Add a console handler for Ctrl+C to ensure clean shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("[Program] Console cancel key pressed - initiating emergency shutdown");
                e.Cancel = true; // Cancel the immediate termination

                // Use emergency shutdown for immediate termination - TRACK THE TASK
                var emergencyTask = Task.Run(async () =>
                {
                    await Task.Delay(100, AppCancellationToken); // Brief delay with cancellation support
                    if (!AppCancellationToken.IsCancellationRequested)
                    {
                        EmergencyShutdown();
                    }
                }, AppCancellationToken);

                AddBackgroundTask(emergencyTask);
            };// .NET 8 Windows Forms modern initialization
            ApplicationConfiguration.Initialize();

            // Additional modern configuration
            ModernApplicationBootstrap.Configure();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();            // Initialize logging and debug utilities
            var logger = _serviceProvider.GetRequiredService<ILogger<object>>();
            Utils.LoggingManager.Initialize(_serviceProvider.GetRequiredService<ILoggerFactory>());
            Utils.ServiceScopeExtensions.SetLogger(logger);            // Initialize thread safety monitoring
            Utils.ThreadSafetyMonitor.Initialize(logger);
            Utils.ThreadSafeUI.Initialize(logger);
            Utils.ResourceTracker.Initialize(logger);

            // Initialize process monitoring
            try
            {
                var processMonitor = Utils.ProcessMonitor.Instance;
                processMonitor.Initialize(_serviceProvider.GetRequiredService<ILogger<Utils.ProcessMonitor>>());
                processMonitor.RegisterExitHandlers();
                logger.LogInformation("ProcessMonitor initialized");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize ProcessMonitor");
            }

            // Parse command line arguments for debug options
            bool debugDatabase = Array.Exists(args, arg => arg == "--debug-db");
            bool debugThreads = Array.Exists(args, arg => arg == "--debug-threads");
            bool debugResources = Array.Exists(args, arg => arg == "--debug-resources");
            bool verboseLogging = Array.Exists(args, arg => arg == "--verbose");
            bool debugLayout = Array.Exists(args, arg => arg == "--debug-layout");

            // Parse dashboard diagnostics flag
            var cmdLineArgs = Environment.GetCommandLineArgs();
            bool runDashboardDiagnostics = cmdLineArgs.Any(arg => arg.Equals("--diagnose-dashboard", StringComparison.OrdinalIgnoreCase));

            if (verboseLogging)
            {
                Utils.LoggingManager.SetVerboseLogging(true);
                Log.VerboseLoggingEnabled(logger, null);
            }

            Log.ApplicationStartupInitialized(logger, null);

            // Check command line arguments
            var cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1)
            {                // Handle debug commands
                if (cmdArgs[1] == "--test-db-connection" || debugDatabase)
                {
                    await TestDatabaseConnectionAsync();
                    return;
                }
                else if (debugThreads)
                {
                    LaunchThreadMonitoringConsole();
                    return;
                }
                else if (debugResources)
                {
                    LaunchResourceMonitoringConsole();
                    return;
                }
            }

            // Initialize theme system with dark theme for testing
            Console.WriteLine("[Program] Initializing theme system...");
            try
            {
                // Default to dark theme unless explicitly requested otherwise
                bool startWithLightTheme = cmdLineArgs.Length > 1 && cmdLineArgs[1].Equals("--light", StringComparison.OrdinalIgnoreCase);

                if (startWithLightTheme)
                {
                    Console.WriteLine("[Program] Starting with light theme...");
                    BusBus.UI.Core.ThemeManager.SwitchTheme("Light");
                }
                else
                {
                    Console.WriteLine("[Program] Starting with dark theme (default)...");
                    BusBus.UI.Core.ThemeManager.SwitchTheme("Dark");
                }
                Console.WriteLine($"[Program] Current theme: {BusBus.UI.Core.ThemeManager.CurrentTheme.Name}");
                Console.WriteLine($"[Program] Available themes: {string.Join(", ", BusBus.UI.Core.ThemeManager.AvailableThemes)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] Error initializing theme: {ex.Message}");
            }            // Setup application exit handler (nullable EventHandler)
            EventHandler? applicationExitHandler = null;
            applicationExitHandler = (sender, e) =>
            {
                Console.WriteLine("[Program] Application exit handler started");
                ShutdownApplication();

                // Force process exit after a short delay if still running - TRACK THE TASK
                var exitTask = Task.Run(async () =>
                {
                    await Task.Delay(500, AppCancellationToken); // Give time for clean shutdown with cancellation support
                    if (!AppCancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("[Program] Forcing process exit");
                        try
                        {
                            // Kill any child processes first
                            foreach (var process in System.Diagnostics.Process.GetProcessesByName("dotnet"))
                            {
                                try
                                {
                                    // Only kill child processes, not the main process
                                    if (process.Id != Environment.ProcessId &&
                                        process.MainWindowTitle.Contains("BusBus"))
                                    {
                                        if (!process.HasExited)
                                        {
                                            process.Kill();
                                            Console.WriteLine($"[Program] Killed child process: {process.Id}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"[Program] Child process {process.Id} already exited.");
                                        }
                                    }
                                }
                                catch (InvalidOperationException)
                                {
                                    // Process already exited, ignore
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[Program] Error killing process {process.Id}: {ex.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Program] Error in process cleanup: {ex.Message}");
                        }

                        // Final force exit with no further wait
                        Environment.Exit(0);
                    }
                }, AppCancellationToken);

                AddBackgroundTask(exitTask);
            };

            Application.ApplicationExit += applicationExitHandler;
            // Add additional handler for process exit
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Console.WriteLine("[Program] Process exit event triggered");
                try
                {
                    ShutdownApplication();

                    // Final attempt to clean up any lingering resources
                    // This is our last chance before the process terminates
                    foreach (var process in System.Diagnostics.Process.GetProcessesByName("dotnet"))
                    {
                        try
                        {
                            if (process.Id != Environment.ProcessId &&
                                process.MainWindowTitle.Contains("BusBus"))
                            {
                                if (!process.HasExited)
                                {
                                    process.Kill();
                                    Console.WriteLine($"[Program] Killed child process during ProcessExit: {process.Id}");
                                }
                                else
                                {
                                    Console.WriteLine($"[Program] Child process {process.Id} already exited during ProcessExit.");
                                }
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Process already exited, ignore
                        }
                        catch (Exception) { /* Ignore exceptions during final cleanup */ }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] Error in ProcessExit handler: {ex.Message}");
                }
            }; try
            {
                // Test database connection first
                Console.WriteLine("[Program] Testing database connection...");
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Test basic connectivity
                    try
                    {
                        var canConnect = await dbContext.Database.CanConnectAsync();
                        if (!canConnect)
                        {
                            throw new InvalidOperationException("Cannot connect to database");
                        }
                        Console.WriteLine("[Program] Database connection successful");
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"[Program] Database connection failed: {dbEx.Message}");
                        Console.WriteLine("[Program] Possible solutions:");
                        Console.WriteLine("  1. Run: .\\verify-sqlserver-express.ps1");
                        Console.WriteLine("  2. Run: .\\setup-database.ps1");
                        Console.WriteLine("  3. Check SQL Server Express installation");
                        Console.WriteLine("  4. Verify connection string in appsettings.json");

                        MessageBox.Show(
                            $"Database connection failed: {dbEx.Message}\n\n" +
                            "Please run verify-sqlserver-express.ps1 to check your SQL Server Express installation.",
                            "Database Connection Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        Environment.Exit(-1);
                        return;
                    }
                }

                // Seed sample data using a scoped service
                using (var scope = _serviceProvider.CreateScope())
                {
                    var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();
                    Console.WriteLine("[Program] Seeding sample data..."); try
                    {
                        await routeService.SeedSampleDataAsync(AppCancellationToken);
                        Console.WriteLine("[Program] Sample data seeded successfully");
                    }
                    catch (Exception seedEx)
                    {
                        Console.WriteLine($"[Program] Warning: Sample data seeding failed: {seedEx.Message}");
                        // Continue anyway - this is not critical for app startup
                    }
                }
                var mainForm = _serviceProvider.GetRequiredService<Dashboard>();

                // Apply layout debugging if enabled
                if (debugLayout)
                {
                    mainForm.Shown += (s, e) =>
                    {
                        Utils.LayoutDebugger.EnableDebugMode();
                        Utils.LayoutDebugger.LogControlHierarchy(mainForm); // Log the control hierarchy instead
                    };
                }

                // Enable visual debugging indicators (safe, non-intrusive)
                // This helps visualize the UI structure without changing functionality
                bool visualDebug = Array.Exists(args, arg => arg == "--visual-debug") || debugLayout;
                if (visualDebug)
                {
                    try
                    {
                        UI.VisualDebugHelper.SetEnabled(true);
                        mainForm.Shown += (s, e) =>
                        {
                            UI.VisualDebugHelper.HighlightDashboardComponents(mainForm);
                            Console.WriteLine("[Program] Visual debugging enabled - UI components highlighted");
                        };
                    }
                    catch (Exception ex)
                    {
                        // Silently continue if visual debugging fails - it's just a helper
                        Console.WriteLine($"[Program] Visual debugging helper could not be initialized: {ex.Message}");
                    }
                }

                // Run dashboard diagnostics if requested
                if (runDashboardDiagnostics)
                {
                    mainForm.Shown += async (s, e) =>
                    {
                        Console.WriteLine("[Program] Running dashboard diagnostics...");
                        try
                        {
                            var report = await DashboardDiagnosticRunner.RunDashboardDiagnosticsAsync(_serviceProvider);
                            Console.WriteLine($"[Program] Dashboard diagnostics completed: {(report.Success ? "SUCCESS" : "FAILED")} in {report.TotalElapsedMs}ms");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Program] Error running dashboard diagnostics: {ex.Message}");
                        }
                    };
                }

                // Add keyboard shortcut to toggle debug mode
                mainForm.KeyDown += (s, e) =>
                {
                    if (e.Control && e.Shift && e.KeyCode == Keys.D)
                    {
                        Utils.LayoutDebugger.ToggleDebugMode();
                        mainForm.Invalidate(true); // Force redraw
                    }
                };
                mainForm.KeyPreview = true; // Enable form to receive key events

                Console.WriteLine("[Program] Running main form...");
                Application.Run(mainForm);
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
        private static bool _isShuttingDown = false;

        public static void ShutdownApplication()
        {
            // Avoid multiple shutdown attempts
            if (_isShuttingDown)
            {
                Console.WriteLine("[Program] Shutdown already in progress, ignoring duplicate request");
                return;
            }

            _isShuttingDown = true;
            Console.WriteLine("[Program] Starting application shutdown");

            // Set up a very aggressive safety timer to force exit if shutdown takes too long
            var safetyTimer = new System.Timers.Timer(1000); // 1 second max (reduced from 2)
            safetyTimer.Elapsed += (s, e) =>
            {
                Console.WriteLine("[Program] Shutdown timeout reached - FORCE KILLING ALL DOTNET PROCESSES");

                // Kill ALL dotnet processes (including this one)
                try
                {
                    var processes = System.Diagnostics.Process.GetProcessesByName("dotnet");
                    foreach (var process in processes)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                Console.WriteLine($"[Program] Force killing dotnet process {process.Id}");
                                process.Kill(entireProcessTree: true);
                            }
                            else
                            {
                                Console.WriteLine($"[Program] Dotnet process {process.Id} already exited.");
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Process already exited, ignore
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Program] Error killing process {process.Id}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] Error during force process cleanup: {ex.Message}");
                }

                // Ultimate fallback - terminate this process immediately
                Environment.Exit(1);
            };
            safetyTimer.Start();

            try
            {
                // Signal cancellation first
                if (_appCancellationTokenSource != null && !_appCancellationTokenSource.IsCancellationRequested)
                {
                    _appCancellationTokenSource.Cancel();
                    Console.WriteLine("[Program] Cancellation token signaled");
                }

                // Wait for background tasks to complete with timeout
                Task[] tasksToWait;
                lock (_backgroundTasks)
                {
                    tasksToWait = _backgroundTasks.ToArray();
                    // Clear the list to prevent further access
                    _backgroundTasks.Clear();
                }

                Console.WriteLine($"[Program] Waiting for {tasksToWait.Length} background tasks to complete...");                // Wait for tasks with a very short timeout (100ms) to prevent hanging
                var waitTask = Task.WhenAll(tasksToWait);
                if (!waitTask.Wait(TimeSpan.FromMilliseconds(100))) // Reduced from 200ms
                {
                    Console.WriteLine("[Program] Timeout waiting for background tasks - EMERGENCY TERMINATION");

                    // Don't wait for tasks - just force terminate ALL dotnet processes
                    try
                    {
                        var processes = System.Diagnostics.Process.GetProcessesByName("dotnet");
                        foreach (var process in processes)
                        {
                            try
                            {
                                if (!process.HasExited)
                                {
                                    Console.WriteLine($"[Program] Emergency killing dotnet process {process.Id}");
                                    process.Kill(entireProcessTree: true);
                                }
                                else
                                {
                                    Console.WriteLine($"[Program] Dotnet process {process.Id} already exited (emergency).");
                                }
                            }
                            catch (InvalidOperationException)
                            {
                                // Process already exited, ignore
                            }
                            catch (Exception killEx)
                            {
                                Console.WriteLine($"[Program] Error killing process {process.Id}: {killEx.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Program] Error during emergency process cleanup: {ex.Message}");
                    }

                    // Immediate exit with error code
                    Environment.Exit(1);
                }
                else
                {
                    Console.WriteLine("[Program] All background tasks completed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] Error during task shutdown: {ex.Message}");
                // Continue with shutdown despite errors
            }
            try
            {
                // Give a brief moment for UI cleanup before disposing services
                Thread.Sleep(100);

                // Make a copy of any remaining background tasks to properly clean them up
                Task[] remainingTasks;
                lock (_backgroundTasks)
                {
                    remainingTasks = _backgroundTasks.ToArray();
                    _backgroundTasks.Clear();
                }

                if (remainingTasks.Length > 0)
                {
                    Console.WriteLine($"[Program] Found {remainingTasks.Length} unfinished background tasks during final cleanup");

                    // Cancel them if they're still running
                    foreach (var task in remainingTasks.Where(t => !t.IsCompleted && t.Status != TaskStatus.Canceled))
                    {
                        Console.WriteLine($"[Program] Task {task.Id} status: {task.Status} - forcing cancellation");
                    }

                    // Try to wait with very short timeout
                    try
                    {
                        Task.WaitAll(remainingTasks, 100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Program] Error waiting for remaining tasks: {ex.Message}");
                    }
                }

                // Use ProcessMonitor to clean up any lingering processes or timers
                try
                {
                    Utils.ProcessMonitor.Instance.Cleanup();
                    Console.WriteLine("[Program] ProcessMonitor cleanup completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] ProcessMonitor cleanup error: {ex.Message}");
                }

                // Dispose service provider
                if (_serviceProvider is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                        Console.WriteLine("[Program] Service provider disposed");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Program] Error disposing service provider: {ex.Message}");
                    }
                }

                // Dispose the cancellation token source
                try
                {
                    if (_appCancellationTokenSource != null)
                    {
                        _appCancellationTokenSource.Dispose();
                        Console.WriteLine("[Program] CancellationTokenSource disposed");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] Error disposing CancellationTokenSource: {ex.Message}");
                }

                // Force GC collection to help clean up resources
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Second GC collection pass to ensure finalizers that released resources are collected
                GC.Collect();
                GC.WaitForPendingFinalizers(); Console.WriteLine("[Program] Application shutdown complete");

                // Stop the safety timer since we completed successfully
                safetyTimer?.Stop();
                safetyTimer?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] Error during final cleanup: {ex.Message}");
                // Still try to stop the timer
                safetyTimer?.Stop();
                safetyTimer?.Dispose();
            }
        }
        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development"}.json", optional: true, reloadOnChange: true)
                .Build();

            // Add configuration to DI
            services.AddSingleton<IConfiguration>(configuration);            // Add enhanced logging for debugging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole(options =>
                {
                    options.FormatterName = "simple";
                });
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "HH:mm:ss.fff ";
                    options.UseUtcTimestamp = false;
                });
                builder.AddDebug();                // Set minimum level based on environment
                var isDevelopment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
                var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
                Console.WriteLine($"Environment: {env}");
                builder.SetMinimumLevel(isDevelopment ? LogLevel.Debug : LogLevel.Information);
            });            // Add DbContext with SQL Server and retry on transient failures
            services.AddDbContext<AppDbContext>(options =>
            {
                var dbOptions = options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(60),
                        errorNumbersToAdd: SqlRetryErrorNumbers
                    )
                )
                .EnableDetailedErrors();                // Only enable sensitive data logging in development
                var isDevelopment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
                var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
                Console.WriteLine($"Database Environment: {env}");
                if (isDevelopment)
                {
                    Console.WriteLine("Enabling sensitive data logging for development");
                    dbOptions.EnableSensitiveDataLogging();
                }
                else
                {
                    Console.WriteLine("Sensitive data logging disabled for production");
                }
            });

            // Register services with proper scoping to avoid DbContext threading issues
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IDriverService, DriverService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IStatisticsService, StatisticsService>();            // Register UI components
            services.AddTransient<Dashboard>((provider) =>
            {
                var routeService = provider.GetRequiredService<IRouteService>();
                var logger = provider.GetRequiredService<ILogger<Dashboard>>();
                return new Dashboard(provider, routeService, logger);
            });

            // Register views as transient to avoid UI conflicts
            services.AddTransient<DashboardView>();
        }        /// <summary>
                 /// Tests database connection and reports status
                 /// </summary>
        private static async Task TestDatabaseConnectionAsync()
        {
            Console.WriteLine("Testing database connection...");

            try
            {
                if (_serviceProvider == null)
                {
                    Console.WriteLine("ERROR: Service provider is null");
                    return;
                }

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

                    // Get connection string
                    var connectionString = dbContext.Database.GetConnectionString();
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        Console.WriteLine("ERROR: Connection string is null or empty");
                        return;
                    }

                    // Test connection
                    var success = await Utils.DatabaseDiagnostics.TestConnectionAsync(
                        connectionString,
                        logger,
                        logDetails: true);

                    Console.WriteLine($"Database connection test result: {(success ? "SUCCESS" : "FAILED")}");

                    // Test EF Core connection
                    bool efSuccess = await Utils.DatabaseDiagnostics.TestEfConnectionAsync(dbContext, logger);
                    Console.WriteLine($"EF Core connection test result: {(efSuccess ? "SUCCESS" : "FAILED")}");

                    // If successful, check entity counts
                    if (efSuccess)
                    {
                        try
                        {
                            var routesCount = await dbContext.Routes.CountAsync();
                            var driversCount = await dbContext.Drivers.CountAsync();
                            var vehiclesCount = await dbContext.Vehicles.CountAsync();

                            Console.WriteLine($"Routes count: {routesCount}");
                            Console.WriteLine($"Drivers count: {driversCount}");
                            Console.WriteLine($"Vehicles count: {vehiclesCount}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error counting entities: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR testing database connection: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }        /// <summary>
                 /// Shows a message that debug console functionality has been removed
                 /// </summary>
        private static void LaunchDebugConsoleWindow()
        {
            Console.WriteLine("[Program] Debug console functionality has been removed");
            MessageBox.Show(
                "Debug console functionality has been removed.\nPlease use the built-in diagnostics tools in the main application.",
                "Feature Removed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// Launch the thread monitoring console
        /// </summary>
        private static void LaunchThreadMonitoringConsole()
        {
            Console.WriteLine("[Program] Launching Thread Monitoring Console...");

            try
            {
                // Create a simple UI form to display thread information
                using var form = new Form
                {
                    Text = "Thread Safety Monitor",
                    Width = 800,
                    Height = 600,
                    StartPosition = FormStartPosition.CenterScreen
                };

                // Add a tab control for different views
                var tabControl = new TabControl
                {
                    Dock = DockStyle.Fill
                };

                // Active threads tab
                var threadsTab = new TabPage("Active Threads");
                var threadsList = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true
                };
                threadsList.Columns.Add("Thread ID", 80);
                threadsList.Columns.Add("Purpose", 200);
                threadsList.Columns.Add("UI Thread", 80);
                threadsList.Columns.Add("Start Time", 150);

                // Cross-thread operations tab
                var crossThreadTab = new TabPage("Cross-Thread Operations");
                var crossThreadList = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true
                };
                crossThreadList.Columns.Add("Thread ID", 80);
                crossThreadList.Columns.Add("Operation", 200);
                crossThreadList.Columns.Add("Control Type", 120);
                crossThreadList.Columns.Add("Control Name", 120);
                crossThreadList.Columns.Add("Caller", 150);
                crossThreadList.Columns.Add("Timestamp", 150);

                // Add to form
                threadsTab.Controls.Add(threadsList);
                crossThreadTab.Controls.Add(crossThreadList);
                tabControl.TabPages.Add(threadsTab);
                tabControl.TabPages.Add(crossThreadTab);
                form.Controls.Add(tabControl);

                // Add refresh button
                var refreshButton = new Button
                {
                    Text = "Refresh",
                    Dock = DockStyle.Bottom
                };

                refreshButton.Click += (s, e) =>
                {                    // Refresh thread information
                    threadsList.Items.Clear();
                    var threads = Utils.ThreadSafetyMonitor.GetActiveThreads();
                    foreach (var thread in threads)
                    {
                        var item = new ListViewItem(thread.ThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        item.SubItems.Add(thread.Purpose);
                        item.SubItems.Add(thread.IsUiThread.ToString());
                        item.SubItems.Add(thread.StartTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
                        threadsList.Items.Add(item);
                    }

                    // Refresh cross-thread operations
                    crossThreadList.Items.Clear();
                    var operations = Utils.ThreadSafetyMonitor.GetCrossThreadOperations();
                    foreach (var op in operations)
                    {
                        var item = new ListViewItem(op.ThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        item.SubItems.Add(op.Operation);
                        item.SubItems.Add(op.ControlType);
                        item.SubItems.Add(op.ControlName);
                        item.SubItems.Add(op.CallerMember);
                        item.SubItems.Add(op.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
                        crossThreadList.Items.Add(item);
                    }
                };

                form.Controls.Add(refreshButton);

                // Automatically refresh on load
                form.Load += (s, e) => refreshButton.PerformClick();

                // Show the form
                Application.Run(form);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error launching thread monitor: {ex.Message}");
            }
        }

        /// <summary>
        /// Launch the resource monitoring console
        /// </summary>
        private static void LaunchResourceMonitoringConsole()
        {
            Console.WriteLine("[Program] Launching Resource Monitoring Console...");

            try
            {
                // Create a simple UI form to display resource information
                using var form = new Form
                {
                    Text = "Resource Monitor",
                    Width = 900,
                    Height = 600,
                    StartPosition = FormStartPosition.CenterScreen
                };

                // Create list view for resources
                var resourcesList = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true
                };

                resourcesList.Columns.Add("ID", 100);
                resourcesList.Columns.Add("Type", 150);
                resourcesList.Columns.Add("Description", 250);
                resourcesList.Columns.Add("Created At", 150);
                resourcesList.Columns.Add("Disposed", 80);

                // Add details text box
                var detailsLabel = new Label
                {
                    Text = "Stack Trace:",
                    Dock = DockStyle.Bottom,
                    Height = 20
                };

                var detailsBox = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Bottom,
                    Height = 150,
                    ReadOnly = true
                };

                // Add buttons panel
                var buttonsPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 40
                };

                var refreshButton = new Button
                {
                    Text = "Refresh",
                    Location = new System.Drawing.Point(10, 10),
                    Width = 100
                };

                var releaseButton = new Button
                {
                    Text = "Release Selected",
                    Location = new System.Drawing.Point(120, 10),
                    Width = 150,
                    Enabled = false
                };

                var releaseAllButton = new Button
                {
                    Text = "Release All",
                    Location = new System.Drawing.Point(280, 10),
                    Width = 100
                };

                // Add event handlers
                refreshButton.Click += (s, e) =>
                {
                    resourcesList.Items.Clear();
                    var resources = Utils.ResourceTracker.GetTrackedResources();

                    foreach (var resource in resources)
                    {
                        var item = new ListViewItem(resource.Id.ToString());
                        item.SubItems.Add(resource.ResourceType);
                        item.SubItems.Add(resource.Description);
                        item.SubItems.Add(resource.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
                        item.SubItems.Add(resource.DisposedAt.HasValue ? "Yes" : "No");
                        item.Tag = resource; // Store the resource for later use
                        resourcesList.Items.Add(item);
                    }

                    detailsBox.Text = "";
                };

                resourcesList.SelectedIndexChanged += (s, e) =>
                {
                    releaseButton.Enabled = resourcesList.SelectedItems.Count > 0;

                    if (resourcesList.SelectedItems.Count > 0 && resourcesList.SelectedItems[0].Tag is Utils.TrackedResource resource)
                    {
                        detailsBox.Text = resource.StackTrace;
                    }
                    else
                    {
                        detailsBox.Text = "";
                    }
                };

                releaseButton.Click += (s, e) =>
                {
                    if (resourcesList.SelectedItems.Count > 0 && resourcesList.SelectedItems[0].Tag is Utils.TrackedResource resource)
                    {
                        Utils.ResourceTracker.ReleaseResource(resource.Id);
                        refreshButton.PerformClick();
                    }
                };

                releaseAllButton.Click += (s, e) =>
                {
                    Utils.ResourceTracker.ReleaseAllResources();
                    refreshButton.PerformClick();
                };

                // Add controls to form
                buttonsPanel.Controls.Add(refreshButton);
                buttonsPanel.Controls.Add(releaseButton);
                buttonsPanel.Controls.Add(releaseAllButton);

                form.Controls.Add(resourcesList);
                form.Controls.Add(detailsLabel);
                form.Controls.Add(detailsBox);
                form.Controls.Add(buttonsPanel);

                // Automatically refresh on load
                form.Load += (s, e) => refreshButton.PerformClick();

                // Show the form
                Application.Run(form);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error launching resource monitor: {ex.Message}");
            }
        }

        // Pro-level: Prevent access after disposal
        public static CancellationToken AppCancellationToken
        {
            get
            {
                if (_appCancellationTokenSource == null)
                    throw new ObjectDisposedException(nameof(_appCancellationTokenSource));
                return _appCancellationTokenSource.Token;
            }
        }

        // Nested class for LoggerMessage.Define
        static partial class Log
        {
            [LoggerMessage(EventId = 0,
                Level = LogLevel.Information,
                Message = "[Program] Verbose logging enabled")]
            public static partial void VerboseLoggingEnabled(ILogger logger, Exception? ex);

            [LoggerMessage(
                EventId = 1,
                Level = LogLevel.Information,
                Message = "Application startup initialized")]
            public static partial void ApplicationStartupInitialized(ILogger logger, Exception? ex);
        }

        /// <summary>
        /// Emergency shutdown that immediately terminates the process
        /// </summary>
        public static void EmergencyShutdown()
        {
            if (_shutdownInProgress)
            {
                Console.WriteLine("[Program] Shutdown already in progress, ignoring duplicate request");
                return;
            }
            _shutdownInProgress = true;
            Console.WriteLine("[Program] EMERGENCY SHUTDOWN INITIATED - IMMEDIATE TERMINATION");

            try
            {
                // Cancel all operations immediately
                _appCancellationTokenSource?.Cancel();

                // Wait for all background tasks to finish
                try
                {
                    Task.WaitAll(_backgroundTasks.ToArray(), 2000); // Wait up to 2 seconds for background tasks
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] Error waiting for background tasks: {ex.Message}");
                }

                // AGGRESSIVE: Kill ALL dotnet processes immediately
                try
                {
                    var processes = System.Diagnostics.Process.GetProcessesByName("dotnet");
                    Console.WriteLine($"[Program] Found {processes.Length} dotnet processes to terminate");

                    foreach (var process in processes)
                    {
                        try
                        {
                            Console.WriteLine($"[Program] EMERGENCY: Killing dotnet process {process.Id} ({process.ProcessName})");
                            process.Kill(entireProcessTree: true);
                            process.WaitForExit(1000); // Wait max 1 second
                        }
                        catch (Exception killEx)
                        {
                            Console.WriteLine($"[Program] Error killing process {process.Id}: {killEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] Error during process cleanup: {ex.Message}");
                }

                // Quick cleanup of critical resources (but don't wait)
                try
                {
                    if (_serviceProvider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] Service disposal error: {ex.Message}");
                }

                try
                {
                    _appCancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] Token disposal error: {ex.Message}");
                }

                Console.WriteLine("[Program] Critical resources disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] Emergency shutdown error: {ex.Message}");
            }
            // FINAL: Force terminate this process immediately
            Console.WriteLine("[Program] TERMINATING PROCESS IMMEDIATELY");
            Environment.Exit(1);
        }

        private static void CleanupCompletedTasks()
        {
            lock (_backgroundTasks)
            {
                var completedTasks = _backgroundTasks.Where(t => t.IsCompleted).ToList();
                if (completedTasks.Count > 0)
                {
                    foreach (var task in completedTasks)
                    {
                        _backgroundTasks.Remove(task);
                    }
                    Console.WriteLine($"[Program] Cleaned up {completedTasks.Count} completed tasks. Remaining: {_backgroundTasks.Count}");
                }
            }
        }
    }
}
