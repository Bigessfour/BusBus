using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.DataAccess;
using BusBus.Models;
using BusBus.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusBus.UI
{
    /// <summary>
    /// Diagnostic tool that simulates various problematic scenarios
    /// to help diagnose and fix them
    /// </summary>
    public partial class DiagnosticsTester : Form // Made partial
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiagnosticsTester> _logger;
        private readonly List<Task> _tasks = new List<Task>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // Renamed static class to avoid collision with instance method Log
        private static partial class LoggerMessages
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{Message}")]
            public static partial void LogGenericMessage(ILogger logger, string message);

            [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Cross-thread update failed as expected")]
            public static partial void CrossThreadUpdateFailed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error in background task {TaskId}")]
            public static partial void ErrorInBackgroundTask(ILogger logger, int taskId, Exception? ex);

            [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error in thread-safe UI update task")]
            public static partial void ErrorInThreadSafeUIUpdateTask(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Database connection test failed")]
            public static partial void DbConnectionTestFailed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Database context disposal simulation failed")]
            public static partial void DbContextDisposalSimFailed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "DbContextManager test failed")]
            public static partial void DbContextManagerTestFailed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Parallel database operations test failed")]
            public static partial void ParallelDbOperationsTestFailed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Create undisposed resources test failed")]
            public static partial void CreateUndisposedResourcesTestFailed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Create and track resources test failed")]
            public static partial void CreateAndTrackResourcesTestFailed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "View active resources test failed")]
            public static partial void ViewActiveResourcesTestFailed(ILogger logger, Exception? ex);

            [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Managed resource test failed")]
            public static partial void ManagedResourceTestFailed(ILogger logger, Exception? ex);
        }

        public DiagnosticsTester(IServiceProvider serviceProvider, ILogger<DiagnosticsTester> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "BusBus Diagnostics Tester";
            this.Width = 800;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create tab control
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Create tabs
            var threadingTab = new TabPage("Threading Tests");
            var databaseTab = new TabPage("Database Tests");
            var resourceTab = new TabPage("Resource Tests");

            // Add tabs
            tabControl.TabPages.Add(threadingTab);
            tabControl.TabPages.Add(databaseTab);
            tabControl.TabPages.Add(resourceTab);

            // Threading tests
            var threadingPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            threadingPanel.Controls.Add(CreateButton("Simulate UI Thread Blocking", SimulateUIThreadBlocking));
            threadingPanel.Controls.Add(CreateButton("Simulate Cross-Thread UI Update", SimulateCrossThreadUIUpdate));
            threadingPanel.Controls.Add(CreateButton("Run Multiple Background Tasks", RunMultipleBackgroundTasks));
            threadingPanel.Controls.Add(CreateButton("Run Thread-Safe UI Update", RunThreadSafeUIUpdate));

            // Database tests
            var databasePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            databasePanel.Controls.Add(CreateButton("Test Database Connection", TestDatabaseConnection));
            databasePanel.Controls.Add(CreateButton("Simulate Database Context Disposal Issue", SimulateDatabaseContextDisposalIssue));
            databasePanel.Controls.Add(CreateButton("Use Database Context Manager", UseDatabaseContextManager));
            databasePanel.Controls.Add(CreateButton("Run Parallel Database Operations", RunParallelDatabaseOperations));

            // Resource tests
            var resourcePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            resourcePanel.Controls.Add(CreateButton("Create Undisposed Resources", CreateUndisposedResources));
            resourcePanel.Controls.Add(CreateButton("Create and Track Resources", CreateAndTrackResources));
            resourcePanel.Controls.Add(CreateButton("View Active Resources", ViewActiveResources));
            resourcePanel.Controls.Add(CreateButton("Run Managed Resource Test", RunManagedResourceTest));

            // Output panel
            var outputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 200
            };

            var outputLabel = new Label
            {
                Text = "Output:",
                Dock = DockStyle.Top,
                Height = 20
            };

            var outputBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Tag = "OutputBox"
            };

            // Add controls
            outputPanel.Controls.Add(outputBox);
            outputPanel.Controls.Add(outputLabel);

            threadingTab.Controls.Add(threadingPanel);
            databaseTab.Controls.Add(databasePanel);
            resourceTab.Controls.Add(resourcePanel);

            this.Controls.Add(tabControl);
            this.Controls.Add(outputPanel);

            // Set up event handlers
            this.FormClosing += (s, e) =>
            {
                _cts.Cancel();
                _cts.Dispose();
            };
        }

        // Made static to resolve CA1822
        private static Button CreateButton(string text, EventHandler clickHandler)
        {
            var button = new Button
            {
                Text = text,
                Width = 300,
                Height = 40,
                Margin = new Padding(5)
            };

            if (clickHandler != null)
            {
                button.Click += clickHandler;
            }

            return button;
        }

        private void Log(string message)
        {
            var outputBox = this.Controls.Find("OutputBox", true)[0] as TextBox;
            if (outputBox != null)
            {
                // Use ThreadSafeUI to update the text box
                ThreadSafeUI.Invoke(outputBox, () =>
                {
                    outputBox.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                    outputBox.SelectionStart = outputBox.Text.Length;
                    outputBox.ScrollToCaret();
                });
            }

            // _logger.LogInformation(message);
            LoggerMessages.LogGenericMessage(_logger, message);
        }

        #region Threading Tests

        private void SimulateUIThreadBlocking(object? sender, EventArgs e)
        {
            Log("Simulating UI thread blocking...");

            // This will block the UI thread
            Thread.Sleep(5000);

            Log("UI thread blocking completed");
        }

        private void SimulateCrossThreadUIUpdate(object? sender, EventArgs e)
        {
            Log("Simulating cross-thread UI update...");

            var task = Task.Run(() =>
            {
                // Simulate work
                Thread.Sleep(1000);

                try
                {
                    // This will cause a cross-thread exception if CheckForIllegalCrossThreadCalls is true
                    var outputBox = this.Controls.Find("OutputBox", true)[0] as TextBox;
                    if (outputBox != null)
                    {
                        outputBox.AppendText($"This is an unsafe cross-thread update! {Environment.NewLine}");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // Log.CrossThreadUpdateFailed(_logger, ex);
                    LoggerMessages.CrossThreadUpdateFailed(_logger, ex);

                    // Now use the proper way
                    this.Invoke(() =>
                    {
                        Log("Cross-thread update caught: " + ex.Message);
                    });
                }
            });

            _tasks.Add(task);
        }

        private void RunMultipleBackgroundTasks(object? sender, EventArgs e)
        {
            Log("Running multiple background tasks...");

            for (int i = 0; i < 5; i++)
            {
                int taskId = i;
                var task = Task.Run(async () =>
                {
                    ThreadSafetyMonitor.RegisterThread($"Background Task {taskId}");

                    try
                    {
                        Log($"Background task {taskId} started");

                        // Simulate work
                        await Task.Delay(2000 + (taskId * 500), _cts.Token);

                        // Use ThreadSafeUI for safe UI updates
                        ThreadSafeUI.BeginInvoke(this, () =>
                        {
                            Log($"Background task {taskId} completed");
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log.ErrorInBackgroundTask(_logger, taskId, ex);
                        LoggerMessages.ErrorInBackgroundTask(_logger, taskId, ex);
                    }
                });

                _tasks.Add(task);
            }
        }

        private void RunThreadSafeUIUpdate(object? sender, EventArgs e)
        {
            Log("Running thread-safe UI update...");

            var task = Task.Run(async () =>
            {
                ThreadSafetyMonitor.RegisterThread("Safe UI Update Task");

                try
                {
                    // Simulate work
                    await Task.Delay(1000, _cts.Token);

                    // Use ThreadSafeUI for safe UI updates
                    ThreadSafeUI.Invoke(this, () =>
                    {
                        Log("This is a safe cross-thread update using ThreadSafeUI.Invoke");
                    });

                    await Task.Delay(1000, _cts.Token);

                    // Try another approach with BeginInvoke (non-blocking)
                    ThreadSafeUI.BeginInvoke(this, () =>
                    {
                        Log("This is a safe cross-thread update using ThreadSafeUI.BeginInvoke");
                    });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Log.ErrorInThreadSafeUIUpdateTask(_logger, ex);
                    LoggerMessages.ErrorInThreadSafeUIUpdateTask(_logger, ex);
                }
            });

            _tasks.Add(task);
        }

        #endregion

        #region Database Tests

        private async void TestDatabaseConnection(object? sender, EventArgs e)
        {
            Log("Testing database connection...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Test connection
                bool canConnect = await dbContext.Database.CanConnectAsync();
                Log($"Connection test result: {(canConnect ? "SUCCESS" : "FAILED")}");

                if (canConnect)
                {
                    // Get some basic stats
                    int routesCount = await dbContext.Routes.CountAsync();
                    int driversCount = await dbContext.Drivers.CountAsync();
                    int vehiclesCount = await dbContext.Vehicles.CountAsync();

                    Log($"Routes: {routesCount}, Drivers: {driversCount}, Vehicles: {vehiclesCount}");
                }
            }
            catch (Exception ex)
            {
                Log($"Database connection error: {ex.Message}");
                // Log.DbConnectionTestFailed(_logger, ex);
                LoggerMessages.DbConnectionTestFailed(_logger, ex);
            }
        }

        private void SimulateDatabaseContextDisposalIssue(object? sender, EventArgs e)
        {









            Log("Simulating safe database context usage (no disposal issue)...");

            try
            {
                // Start a background task that uses its own context
                var task = Task.Run(async () =>
                {
                    try
                    {
                        // Simulate delay to allow form to continue
                        await Task.Delay(2000, _cts.Token);

                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        ThreadSafeUI.BeginInvoke(this, () =>
                        {
                            Log("Background task using its own DbContext...");
                        });

                        var drivers = await dbContext.Drivers.ToListAsync();

                        ThreadSafeUI.BeginInvoke(this, () =>
                        {
                            Log($"Found {drivers.Count} drivers (safe context usage)");
                        });
                    }
                    catch (Exception ex)
                    {
                        ThreadSafeUI.BeginInvoke(this, () =>
                        {
                            Log($"Error in background task: {ex.Message}");
                        });
                    }
                });

                _tasks.Add(task);

                Log("Background task started with its own context. No disposal issue will occur.");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                LoggerMessages.DbContextDisposalSimFailed(_logger, ex);
            }
        }

        private async void UseDatabaseContextManager(object? sender, EventArgs e)
        {
            Log("Using DbContextManager for safe context management...");

            try
            {
                // Create a DbContextManager
                // var dbManager = new DbContextManager(_serviceProvider, _logger);
                using var dbManager = new DbContextManager(_serviceProvider, _serviceProvider.GetRequiredService<ILogger<DbContextManager>>());

                // Use the manager to execute operations
                var routesCount = await dbManager.ExecuteAsync(async db =>
                {
                    return await db.Routes.CountAsync();
                });

                Log($"Routes count: {routesCount}");

                // Run a background task that also uses the manager
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(1000, _cts.Token);

                        // Safe execution with retry
                        var driverCount = await dbManager.ExecuteWithRetryAsync(async db =>
                        {
                            return await db.Drivers.CountAsync();
                        });

                        ThreadSafeUI.BeginInvoke(this, () =>
                        {
                            Log($"Background task found {driverCount} drivers safely");
                        });
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        ThreadSafeUI.BeginInvoke(this, () =>
                        {
                            Log($"Error in background task: {ex.Message}");
                        });
                    }
                });

                _tasks.Add(task);

                // Even if we dispose the manager, the background task will still work
                // because it uses the same manager instance with proper locking
                await Task.Delay(2000);
                Log("Disposing DbContextManager...");
                // dbManager.Dispose(); // Removed as 'using' will handle disposal

                Log("DbContextManager disposed safely");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                // Log.DbContextManagerTestFailed(_logger, ex);
                LoggerMessages.DbContextManagerTestFailed(_logger, ex);
            }
        }

        private async void RunParallelDatabaseOperations(object? sender, EventArgs e)
        {
            Log("Running parallel database operations...");

            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task>();

            try
            {
                // Run multiple operations in parallel
                for (int i = 0; i < 5; i++)
                {
                    int opId = i;
                    var task = Task.Run(async () =>
                    {
                        // Create a new scope for each operation
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        try
                        {
                            ThreadSafeUI.BeginInvoke(this, () =>
                            {
                                Log($"Database operation {opId} starting...");
                            });

                            // Simulate some work
                            await Task.Delay(500, _cts.Token);

                            // Execute a database query
                            var entities = await dbContext.Routes.Take(5).ToListAsync();

                            ThreadSafeUI.BeginInvoke(this, () =>
                            {
                                Log($"Database operation {opId} completed with {entities.Count} routes");
                            });
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            ThreadSafeUI.BeginInvoke(this, () =>
                            {
                                Log($"Error in database operation {opId}: {ex.Message}");
                            });
                        }
                    });

                    tasks.Add(task);
                }

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);

                stopwatch.Stop();
                Log($"All parallel database operations completed in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                // Log.ParallelDbOperationsTestFailed(_logger, ex);
                LoggerMessages.ParallelDbOperationsTestFailed(_logger, ex);
            }
        }

        #endregion

        #region Resource Tests

        private void CreateUndisposedResources(object? sender, EventArgs e)
        {
            Log("Creating undisposed resources...");

            try
            {
#pragma warning disable CA2000 // Intentionally not disposed for testing resource leak detection
                // Create some resources without disposing them
                for (int i = 0; i < 5; i++)
                {
                    var font = new Font("Arial", 12 + i);
                    Log($"Created Font {i} (Arial, {12 + i}pt) - not disposed or tracked");
                }
#pragma warning restore CA2000
                Log("Created 5 undisposed Font resources");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                // Log.CreateUndisposedResourcesTestFailed(_logger, ex);
                LoggerMessages.CreateUndisposedResourcesTestFailed(_logger, ex);
            }
        }

        private void CreateAndTrackResources(object? sender, EventArgs e)
        {
            Log("Creating and tracking resources...");

            try
            {
#pragma warning disable CA2000 // Intentionally not disposed here to test ResourceTracker functionality
                // Create and track resources
                for (int i = 0; i < 5; i++)
                {
                    var font = new Font("Verdana", 10 + i);

                    // Track the resource
                    Guid resourceId = ResourceTracker.TrackResource(
                        font,
                        "Font",
                        $"Verdana {10 + i}pt for diagnostic test");

                    Log($"Created and tracked Font {i} (Verdana, {10 + i}pt) with ID: {resourceId}");
                }
#pragma warning restore CA2000
                Log("Created 5 tracked Font resources");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                // Log.CreateAndTrackResourcesTestFailed(_logger, ex);
                LoggerMessages.CreateAndTrackResourcesTestFailed(_logger, ex);
            }
        }

        private void ViewActiveResources(object? sender, EventArgs e)
        {
            Log("Viewing active resources...");

            try
            {
                var resources = ResourceTracker.GetTrackedResources();
                Log($"Found {resources.Length} tracked resources:");

                foreach (var resource in resources)
                {
                    Log($"- {resource.ResourceType}: {resource.Description} (ID: {resource.Id})");
                }

                // Show leak detection
                // ResourceTracker.LogLeakedResources();
                ResourceTracker.CheckForUndisposedResources();
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                // Log.ViewActiveResourcesTestFailed(_logger, ex);
                LoggerMessages.ViewActiveResourcesTestFailed(_logger, ex);
            }
        }

        private async void RunManagedResourceTest(object? sender, EventArgs e)
        {
            Log("Running managed resource test...");

            try
            {
                // Use ResourceTracker.CreateAutoTracker
                using (ResourceTracker.CreateAutoTracker(
                    new Font("Segoe UI", 14),
                    "Font",
                    "Auto-tracked font that will be auto-disposed"))
                {
                    Log("Created auto-tracked font resource");

                    // Simulate some work
                    await Task.Delay(2000);

                    Log("Work completed, font will be automatically disposed");
                }

                Log("Auto-tracked font has been disposed");

                // View the current resources
                ViewActiveResources(sender, e);
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                // Log.ManagedResourceTestFailed(_logger, ex);
                LoggerMessages.ManagedResourceTestFailed(_logger, ex);
            }
        }

        #endregion
    }
}
