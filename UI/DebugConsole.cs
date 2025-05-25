using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BusBus.DataAccess;
using BusBus.Utils;
using System.Text.RegularExpressions;

namespace BusBus.UI
{
    /// <summary>
    /// Debug console for development and diagnostics
    /// </summary>
    public partial class DebugConsole : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private TextBox _outputTextBox = null!;
        private Button _testDbButton = null!;
        private Button _showActiveThreadsButton = null!;
        private Button _clearOutputButton = null!;
        private Button _testUiThreadButton = null!;
        private Button _showDbContextInfoButton = null!;
        private ComboBox _loggingLevelComboBox = null!;
        private Label _statusLabel = null!;

        public DebugConsole(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = LoggingManager.GetLogger(typeof(DebugConsole));

            InitializeComponent();
            InitializeDebugConsole();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "BusBus Debug Console";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(640, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Information;

            // Output text box
            _outputTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 10F)
            };

            // Control panel
            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = SystemColors.Control
            };

            // Status bar
            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 25,
                BackColor = Color.Navy,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Text = "Ready"
            };

            // Buttons
            _testDbButton = new Button
            {
                Text = "Test Database",
                Width = 120,
                Location = new Point(10, 10),
                Height = 30
            };
            _testDbButton.Click += TestDbButton_Click;

            _showActiveThreadsButton = new Button
            {
                Text = "Show Threads",
                Width = 120,
                Location = new Point(140, 10),
                Height = 30
            };
            _showActiveThreadsButton.Click += ShowActiveThreadsButton_Click;

            _clearOutputButton = new Button
            {
                Text = "Clear Output",
                Width = 120,
                Location = new Point(270, 10),
                Height = 30
            };
            _clearOutputButton.Click += ClearOutputButton_Click;

            _testUiThreadButton = new Button
            {
                Text = "Test UI Thread",
                Width = 120,
                Location = new Point(400, 10),
                Height = 30
            };
            _testUiThreadButton.Click += TestUiThreadButton_Click;

            _showDbContextInfoButton = new Button
            {
                Text = "DbContext Info",
                Width = 120,
                Location = new Point(530, 10),
                Height = 30
            };
            _showDbContextInfoButton.Click += ShowDbContextInfoButton_Click;

            // Logging level
            var loggingLevelLabel = new Label
            {
                Text = "Logging Level:",
                AutoSize = true,
                Location = new Point(10, 45)
            };

            _loggingLevelComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Location = new Point(100, 42)
            };
            _loggingLevelComboBox.Items.AddRange(new object[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" });
            _loggingLevelComboBox.SelectedIndex = 1; // Debug
            _loggingLevelComboBox.SelectedIndexChanged += LoggingLevelComboBox_SelectedIndexChanged;

            // Add controls
            controlPanel.Controls.Add(_testDbButton);
            controlPanel.Controls.Add(_showActiveThreadsButton);
            controlPanel.Controls.Add(_clearOutputButton);
            controlPanel.Controls.Add(_testUiThreadButton);
            controlPanel.Controls.Add(_showDbContextInfoButton);
            controlPanel.Controls.Add(loggingLevelLabel);
            controlPanel.Controls.Add(_loggingLevelComboBox);

            this.Controls.Add(_outputTextBox);
            this.Controls.Add(controlPanel);
            this.Controls.Add(_statusLabel);

            this.ResumeLayout(false);
        }

        private void InitializeDebugConsole()
        {
            WriteOutput("=== BusBus Debug Console ===");
            WriteOutput($"Started: {DateTime.Now}");
            WriteOutput($"Environment: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}");
            WriteOutput($"OS: {Environment.OSVersion}");
            WriteOutput($".NET Version: {Environment.Version}");
            WriteOutput($"UI Thread ID: {Environment.CurrentManagedThreadId}"); // CA1840
            WriteOutput("=========================");
            WriteOutput("Click on the buttons above to run diagnostics");
        }

        private async void TestDbButton_Click(object? sender, EventArgs e) // CS8622
        {
            try
            {
                SetStatus("Testing database connection...");
                _testDbButton.Enabled = false;

                WriteOutput("[Database Test] Starting database connection test...");

                // Get connection string
                string? connectionString = null;
                await _serviceProvider.WithScopedServiceAsync<AppDbContext>(async dbContext =>
                {
                    connectionString = dbContext.Database.GetConnectionString();

                    WriteOutput($"[Database Test] Provider: {dbContext.Database.ProviderName}");

                    var canConnect = await dbContext.Database.CanConnectAsync();
                    WriteOutput($"[Database Test] Can Connect: {canConnect}");

                    if (canConnect)
                    {
                        try
                        {
                            // Test a simple query
                            var result = await dbContext.Database.ExecuteSqlRawAsync("SELECT @@VERSION");
                            WriteOutput($"[Database Test] Query execution successful");

                            // Check migrations
                            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

                            WriteOutput($"[Database Test] Applied migrations: {appliedMigrations.Count()}");
                            WriteOutput($"[Database Test] Pending migrations: {pendingMigrations.Count()}");
                        }
                        catch (Exception ex)
                        {
                            WriteOutput($"[Database Test] Error executing test query: {ex.Message}");
                        }
                    }
                });

                if (!string.IsNullOrEmpty(connectionString))
                {
                    // Use the connection diagnostics utility for detailed testing
                    var success = await DatabaseDiagnostics.TestConnectionAsync(
                        connectionString,
                        _logger,
                        logDetails: true);

                    WriteOutput($"[Database Test] Connection test result: {(success ? "SUCCESS" : "FAILED")}");
                }
                else
                {
                    WriteOutput("[Database Test] Could not retrieve connection string");
                }

                SetStatus(string.Empty);
            }
            catch (Exception ex)
            {
                WriteOutput($"[ERROR] Database test failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    WriteOutput($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }
                SetStatus("Database test failed");
            }
            finally
            {
                _testDbButton.Enabled = true;
            }
        }

        private void ShowActiveThreadsButton_Click(object? sender, EventArgs e) // CS8622
        {
            try
            {
                WriteOutput("[Threads] Current thread information:");
                WriteOutput($"[Threads] Current thread ID: {Environment.CurrentManagedThreadId}"); // CA1840
                WriteOutput($"[Threads] Is UI thread: {System.Threading.Thread.CurrentThread.IsBackground == false}");
                WriteOutput($"[Threads] Thread pool threads: {System.Threading.ThreadPool.ThreadCount}");

                int workerThreads, completionPortThreads;
                System.Threading.ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
                WriteOutput($"[Threads] Available worker threads: {workerThreads}");
                WriteOutput($"[Threads] Available IO completion threads: {completionPortThreads}");
            }
            catch (Exception ex)
            {
                WriteOutput($"[ERROR] Thread info failed: {ex.Message}");
            }
        }

        private void ClearOutputButton_Click(object? sender, EventArgs e) // CS8622
        {
            _outputTextBox.Clear();
        }

        private async void TestUiThreadButton_Click(object? sender, EventArgs e) // CS8622
        {
            WriteOutput("[UI Thread Test] Testing UI thread operations");
            WriteOutput($"[UI Thread Test] Current thread ID: {Environment.CurrentManagedThreadId}"); // CA1840
            WriteOutput($"[UI Thread Test] Is UI thread: {DebugUtils.IsOnUIThread()}");

            WriteOutput("[UI Thread Test] Starting async operation...");

            _testUiThreadButton.Enabled = false;

            try
            {
                // Simulate async work
                await Task.Run(() =>
                {
                    // This is running on a non-UI thread
                    Task.Delay(1000).Wait(); // Simulate work

                    // Write output from background thread
                    this.Invoke(() =>
                    {
                        WriteOutput($"[UI Thread Test] Background thread ID: {Environment.CurrentManagedThreadId}"); // CA1840
                        WriteOutput($"[UI Thread Test] Is UI thread: {DebugUtils.IsOnUIThread()}");
                    });

                    // Incorrect way - would throw cross-thread exception
                    /*
                    if (!DebugUtils.IsOnUIThread())
                    {
                        // This would cause a cross-thread operation exception if Control.CheckForIllegalCrossThreadCalls = true
                        // WriteOutput("[UI Thread Test] This would cause a cross-thread exception in real code");
                    }
                    */

                    // Correct way - use Invoke
                    this.Invoke(() =>
                    {
                        WriteOutput("[UI Thread Test] Properly accessing UI from background thread via Invoke");
                    });
                });

                WriteOutput("[UI Thread Test] Async operation completed");
                WriteOutput($"[UI Thread Test] Back on UI thread ID: {Environment.CurrentManagedThreadId}"); // CA1840
            }
            catch (Exception ex)
            {
                WriteOutput($"[ERROR] UI thread test failed: {ex.Message}");
            }
            finally
            {
                _testUiThreadButton.Enabled = true;
            }
        }

        private async void ShowDbContextInfoButton_Click(object? sender, EventArgs e) // CS8622
        {
            try
            {
                _showDbContextInfoButton.Enabled = false;

                WriteOutput("[DbContext Info] Retrieving database context information...");

                await _serviceProvider.WithScopedServiceAsync<AppDbContext>(async dbContext =>
                {
                    WriteOutput($"[DbContext Info] Database: {dbContext.Database.GetDbConnection().Database}");
                    WriteOutput($"[DbContext Info] Connection string: {MaskConnectionString(dbContext.Database.GetConnectionString())}");
                    WriteOutput($"[DbContext Info] Provider: {dbContext.Database.ProviderName}");
                    WriteOutput($"[DbContext Info] Auto-detect changes enabled: {dbContext.ChangeTracker.AutoDetectChangesEnabled}");
                    WriteOutput($"[DbContext Info] Query tracking behavior: {dbContext.ChangeTracker.QueryTrackingBehavior}");

                    // Check entity counts
                    try
                    {
                        var routesCount = await dbContext.Routes.CountAsync();
                        var driversCount = await dbContext.Drivers.CountAsync();
                        var vehiclesCount = await dbContext.Vehicles.CountAsync();

                        WriteOutput($"[DbContext Info] Routes count: {routesCount}");
                        WriteOutput($"[DbContext Info] Drivers count: {driversCount}");
                        WriteOutput($"[DbContext Info] Vehicles count: {vehiclesCount}");
                    }
                    catch (Exception ex)
                    {
                        WriteOutput($"[DbContext Info] Error retrieving entity counts: {ex.Message}");
                    }

                    // Show active tracked entities
                    var trackedEntities = dbContext.ChangeTracker.Entries().Count();
                    WriteOutput($"[DbContext Info] Currently tracked entities: {trackedEntities}");
                });
            }
            catch (Exception ex)
            {
                WriteOutput($"[ERROR] DbContext info failed: {ex.Message}");
            }
            finally
            {
                _showDbContextInfoButton.Enabled = true;
            }
        }

        private void LoggingLevelComboBox_SelectedIndexChanged(object? sender, EventArgs e) // CS8622
        {
            var selectedLevel = _loggingLevelComboBox.SelectedItem?.ToString(); // CS8602
            WriteOutput($"[Logging] Setting minimum logging level to: {selectedLevel}");

            // Ideally this would update the logging level in the application
            // but for now we'll just simulate it
        }

        private void WriteOutput(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => WriteOutput(message));
                return;
            }

            _outputTextBox.AppendText(message + Environment.NewLine);
            _outputTextBox.SelectionStart = _outputTextBox.TextLength;
            _outputTextBox.ScrollToCaret();
        }

        private void SetStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => SetStatus(message));
                return;
            }

            _statusLabel.Text = string.IsNullOrEmpty(message) ? "Ready" : message;
        }

        [GeneratedRegex(@"(Password|PWD)=([^;]*)", RegexOptions.IgnoreCase, "en-US")] // SYSLIB1045
        private static partial Regex ConnectionStringMaskRegex();

        private static string MaskConnectionString(string? connectionString) // CA1822
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            // Simple masking of password
            return ConnectionStringMaskRegex().Replace(connectionString, "$1=******");
        }
    }
}
