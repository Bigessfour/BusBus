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
    internal static partial class Program // Added partial modifier
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
        /// </summary>        [STAThread]
        static async Task Main(string[] args)
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
            _serviceProvider = services.BuildServiceProvider();            // Initialize logging and debug utilities
            var logger = _serviceProvider.GetRequiredService<ILogger<object>>();
            Utils.LoggingManager.Initialize(_serviceProvider.GetRequiredService<ILoggerFactory>());
            Utils.ServiceScopeExtensions.SetLogger(logger);

            // Initialize thread safety monitoring
            Utils.ThreadSafetyMonitor.Initialize(logger);
            Utils.ThreadSafeUI.Initialize(logger);
            Utils.ResourceTracker.Initialize(logger);

            // Parse command line arguments for debug options
            bool debugDatabase = Array.Exists(args, arg => arg == "--debug-db");
            bool debugThreads = Array.Exists(args, arg => arg == "--debug-threads");
            bool debugResources = Array.Exists(args, arg => arg == "--debug-resources");
            bool verboseLogging = Array.Exists(args, arg => arg == "--verbose");

            if (verboseLogging)
            {
                Utils.LoggingManager.SetVerboseLogging(true);
                Log.VerboseLoggingEnabled(logger, null);
            }

            Log.ApplicationStartupInitialized(logger, null);

            // Check command line arguments
            var cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1)
            {
                // Handle debug commands
                if (cmdArgs[1] == "--test-db-connection" || debugDatabase)
                {
                    await TestDatabaseConnectionAsync();
                    return;
                }
                else if (cmdArgs[1] == "--debug-console")
                {
                    LaunchDebugConsoleWindow();
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

            // Initialize theme system with dark theme for testing                Console.WriteLine("[Program] Initializing theme system...");
            try
            {
                var cmdLineArgs = Environment.GetCommandLineArgs();

                // Default to dark theme unless explicitly requested otherwise
                bool startWithLightTheme = cmdLineArgs.Length > 1 && cmdLineArgs[1].Equals("--light", StringComparison.OrdinalIgnoreCase);

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
                // Seed sample data using a scoped service
                using (var scope = _serviceProvider.CreateScope())
                {
                    var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();
                    Console.WriteLine("[Program] Seeding sample data...");
                    await routeService.SeedSampleDataAsync();
                    Console.WriteLine("[Program] Sample data seeded successfully");
                }

                var mainForm = _serviceProvider.GetRequiredService<Dashboard>();
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
        }        private static void ShutdownApplication()
        {
            Console.WriteLine("[Program] Starting application shutdown");

            // Signal cancellation first
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
                // Wait for tasks with 2 second timeout (reduced to prevent hanging)
                var waitTask = Task.WhenAll(tasksToWait);
                if (!waitTask.Wait(TimeSpan.FromSeconds(2)))
                {
                    Console.WriteLine("[Program] Timeout waiting for background tasks - continuing shutdown");
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

            // Give a brief moment for UI cleanup before disposing services
            Thread.Sleep(100);

            // Dispose service provider
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                Console.WriteLine("[Program] Service provider disposed");
            }

            Console.WriteLine("[Program] Application shutdown complete");
        }private static void ConfigureServices(IServiceCollection services)
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
                builder.AddDebug();

                // Set minimum level based on environment
                var isDevelopment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
                builder.SetMinimumLevel(isDevelopment ? LogLevel.Debug : LogLevel.Information);
            });

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

            );

            // Register services with proper scoping to avoid DbContext threading issues
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IDriverService, DriverService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IStatisticsService, StatisticsService>();

            // Register UI components
            services.AddTransient<Dashboard>();
            services.AddTransient<UI.DiagnosticsTester>();
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
        }

        /// <summary>
        /// Launch the debug console window
        /// </summary>
        private static void LaunchDebugConsoleWindow()
        {
            Console.WriteLine("[Program] Launching Debug Console...");

            try
            {                if (_serviceProvider == null)
                {
                    Console.WriteLine("ERROR: Service provider is null");
                    return;
                }
                  // Get the diagnostics tester from DI
                var diagnosticsTester = _serviceProvider.GetRequiredService<UI.DiagnosticsTester>();

                // Run the form
                Application.Run(diagnosticsTester);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error launching debug console: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
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
                    {                        var item = new ListViewItem(resource.Id.ToString());
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

        public static CancellationToken AppCancellationToken => _appCancellationTokenSource.Token;

        // Nested class for LoggerMessage.Define
        static partial class Log
        {
            [LoggerMessage(
                EventId = 0,
                Level = LogLevel.Information,
                Message = "[Program] Verbose logging enabled")]
            public static partial void VerboseLoggingEnabled(ILogger logger, Exception? ex);

            [LoggerMessage(
                EventId = 1,
                Level = LogLevel.Information,
                Message = "Application startup initialized")]
            public static partial void ApplicationStartupInitialized(ILogger logger, Exception? ex);
        }
    }
}
