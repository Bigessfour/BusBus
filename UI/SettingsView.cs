#pragma warning disable CS8612 // Nullability of reference types in type of event doesn't match implicitly implemented member
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Data;
using BusBus.Services;
using BusBus.UI.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusBus.UI
{
    public class SettingsView : IView
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SettingsView>? _logger;
        private readonly AdvancedSqlServerDatabaseManager _databaseManager;
        private Panel _panel;
        private Button _updateDatabaseButton;
        private Button _testConnectionButton;
        private TextBox _statusTextBox;
        private Label _connectionStatusLabel; private Label _migrationStatusLabel;
        private ProgressBar _progressBar;

        // LoggerMessage delegates for performance
        private static readonly Action<ILogger, Exception?> s_logDatabaseConnectionError =
            LoggerMessage.Define(LogLevel.Error, new EventId(1, "DatabaseConnectionError"),
                "Failed to test database connection");

        private static readonly Action<ILogger, Exception?> s_logSchemaUpdateError =
            LoggerMessage.Define(LogLevel.Error, new EventId(2, "SchemaUpdateError"),
                "Failed to update database schema");

        private static readonly Action<ILogger, Exception?> s_logSchemaFileNotFound =
            LoggerMessage.Define(LogLevel.Warning, new EventId(3, "SchemaFileNotFound"),
                "Schema file not found at expected location"); private static readonly Action<ILogger, Exception?> s_logSchemaUpdateCancelled =
            LoggerMessage.Define(LogLevel.Warning, new EventId(4, "SchemaUpdateCancelled"),
                "Database schema update was cancelled");

        private static readonly Action<ILogger, string, Exception?> s_logSqlStatementWarning =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(5, "SqlStatementWarning"),
                "SQL statement execution warning: {Statement}");

        private static readonly Action<ILogger, Exception?> s_logMigrationStatusCheckFailed =
            LoggerMessage.Define(LogLevel.Warning, new EventId(6, "MigrationStatusCheckFailed"),
                "Failed to check migration status");        // Static readonly arrays to avoid CA1861 warning
        private static readonly string[] NewLineArray = { Environment.NewLine };
        private static readonly string[] SemicolonArray = { ";" };
        private static readonly string[] GoStatementArray = { "GO", "\r\nGO\r\n", "\nGO\n" };
        private static readonly string[] SpaceArray = { " " };

        public string ViewName => "settings";
        public string Title => "Settings";
        public Control Control => _panel;

#pragma warning disable CS0414, CS0067 // Events are assigned but never used, required by interface
        public event EventHandler<NavigationEventArgs> NavigationRequested = null!;
        public event EventHandler<StatusEventArgs> StatusUpdated = null!;
#pragma warning restore CS0414, CS0067

        public SettingsView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetService<ILogger<SettingsView>>();

            // Get or create database manager
            _databaseManager = _serviceProvider.GetService<AdvancedSqlServerDatabaseManager>()
                ?? new AdvancedSqlServerDatabaseManager();

            InitializeComponents();
            SetupLayout();

            // Apply theme when theme changes
            BusBus.UI.Core.ThemeManager.ThemeChanged += (s, e) => ApplyTheme();
            ApplyTheme();

            // Initialize status on load
            _ = RefreshStatusAsync();
        }

        private void InitializeComponents()
        {
            _panel = new Panel
            {
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground,
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            var titleLabel = new Label
            {
                Text = "Database Settings",
                Font = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineFont,
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText,
                AutoSize = true,
                Location = new Point(20, 20),
                BackColor = Color.Transparent
            }; _connectionStatusLabel = new Label
            {
                Text = "Connection Status: Checking...",
                Font = BusBus.UI.Core.ThemeManager.CurrentTheme.CardFont,
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardText,
                AutoSize = true,
                Location = new Point(20, 60),
                BackColor = Color.Transparent
            }; _migrationStatusLabel = new Label
            {
                Text = "Migration Status: Checking...",
                Font = BusBus.UI.Core.ThemeManager.CurrentTheme.CardFont,
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardText,
                AutoSize = true,
                Location = new Point(20, 85),
                BackColor = Color.Transparent
            }; _testConnectionButton = new Button
            {
                Text = "Test Database Connection",
                Size = new Size(200, 35),
                Location = new Point(20, 120),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            _testConnectionButton.Click += TestConnectionButton_Click; _updateDatabaseButton = new Button
            {
                Text = "Update Database Schema",
                Size = new Size(200, 35),
                Location = new Point(240, 120),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            _updateDatabaseButton.Click += UpdateDatabaseButton_Click;

            _progressBar = new ProgressBar
            {
                Size = new Size(420, 20),
                Location = new Point(20, 170),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            _statusTextBox = new TextBox
            {
                Size = new Size(420, 200),
                Location = new Point(20, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Consolas", 9),
                Text = "Ready to update database schema...\r\n\r\nThis tool will:\r\n" +
                       "• Add missing columns to the database\r\n" +
                       "• Update existing data with proper defaults\r\n" +
                       "• Maintain data integrity during updates\r\n\r\n" +
                       "Click 'Update Database Schema' to apply the latest schema fixes."
            };

            // Add all controls to panel
            _panel.Controls.AddRange(new Control[] {
                titleLabel,
                _connectionStatusLabel,
                _migrationStatusLabel,
                _testConnectionButton,
                _updateDatabaseButton,
                _progressBar,
                _statusTextBox
            });
        }
        private static void SetupLayout()
        {
            // Additional layout setup if needed
        }

        private async void TestConnectionButton_Click(object? sender, EventArgs e)
        {
            await TestDatabaseConnectionAsync();
        }

        private async void UpdateDatabaseButton_Click(object? sender, EventArgs e)
        {
            await UpdateDatabaseSchemaAsync();
        }

        private async Task TestDatabaseConnectionAsync()
        {
            try
            {
                _testConnectionButton.Enabled = false;
                _progressBar.Visible = true;
                _progressBar.Style = ProgressBarStyle.Marquee;

                AppendStatus("Testing database connection...");

                var isConnected = await _databaseManager.TestConnectionAsync();

                if (isConnected)
                {
                    _connectionStatusLabel.Text = "Connection Status: ✓ Connected";
                    _connectionStatusLabel.ForeColor = Color.Green;
                    AppendStatus("✓ Database connection test successful!");
                }
                else
                {
                    _connectionStatusLabel.Text = "Connection Status: ✗ Failed";
                    _connectionStatusLabel.ForeColor = Color.Red;
                    AppendStatus("✗ Database connection test failed!");
                }
            }
            catch (Exception ex)
            {
                _connectionStatusLabel.Text = "Connection Status: ✗ Error";
                _connectionStatusLabel.ForeColor = Color.Red;
                AppendStatus($"✗ Connection test error: {ex.Message}");
                if (_logger != null)
                    s_logDatabaseConnectionError(_logger, ex);
            }
            finally
            {
                _testConnectionButton.Enabled = true;
                _progressBar.Visible = false;
            }
        }

        private async Task UpdateDatabaseSchemaAsync()
        {
            try
            {
                _updateDatabaseButton.Enabled = false;
                _progressBar.Visible = true;
                _progressBar.Style = ProgressBarStyle.Continuous;
                _progressBar.Value = 0;

                AppendStatus("\r\n" + new string('=', 50));
                AppendStatus("Starting database schema update...");

                // Read the SQL script
                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fix-database-schema.sql");
                if (!File.Exists(scriptPath))
                {
                    AppendStatus($"✗ SQL script not found at: {scriptPath}");
                    MessageBox.Show("Database update script not found. Please ensure fix-database-schema.sql is in the application directory.",
                        "Script Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _progressBar.Value = 20;
                var sqlScript = await File.ReadAllTextAsync(scriptPath);
                AppendStatus("✓ SQL script loaded successfully");

                _progressBar.Value = 40;
                AppendStatus("Executing schema updates...");

                // Execute the schema update
                await ExecuteSqlScriptAsync(sqlScript);

                _progressBar.Value = 80;
                AppendStatus("✓ Schema update completed successfully!");

                // Test connection again to verify
                await TestDatabaseConnectionAsync();

                _progressBar.Value = 100;
                AppendStatus("✓ Database schema update completed successfully!");
                AppendStatus(new string('=', 50));

                MessageBox.Show("Database schema has been updated successfully!\n\nThe application now has all required columns and proper data defaults.",
                    "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Refresh migration status
                await RefreshMigrationStatusAsync();
            }
            catch (Exception ex)
            {
                AppendStatus($"✗ Schema update failed: {ex.Message}");
                if (_logger != null)
                    s_logSchemaUpdateError(_logger, ex);

                MessageBox.Show($"Database schema update failed:\n\n{ex.Message}\n\nPlease check the status log for details.",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _updateDatabaseButton.Enabled = true;
                _progressBar.Visible = false;
            }
        }

        private async Task ExecuteSqlScriptAsync(string sqlScript)
        {            // Split script into individual statements
            var statements = sqlScript.Split(GoStatementArray,
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var statement in statements)
            {
                var trimmedStatement = statement.Trim();
                if (string.IsNullOrEmpty(trimmedStatement) || trimmedStatement.StartsWith("--"))
                    continue;

                try
                {
                    await _databaseManager.ExecuteNonQueryAsync(trimmedStatement);

                    // Extract table name for progress reporting                    if (trimmedStatement.Contains("ALTER TABLE"))
                    {
                        var parts = trimmedStatement.Split(SpaceArray, StringSplitOptions.RemoveEmptyEntries);
                        var tableIndex = Array.IndexOf(parts, "TABLE");
                        if (tableIndex >= 0 && tableIndex + 1 < parts.Length)
                        {
                            var tableName = parts[tableIndex + 1];
                            AppendStatus($"  ✓ Updated table: {tableName}");
                        }
                    }
                }
                catch (Exception ex)
                {                    // Log the error but continue with other statements
                    AppendStatus($"  ! Warning: {ex.Message}");
                    if (_logger != null)
                        s_logSqlStatementWarning(_logger, trimmedStatement, ex);
                }
            }
        }

        private async Task RefreshStatusAsync()
        {
            await TestDatabaseConnectionAsync();
            await RefreshMigrationStatusAsync();
        }

        private async Task RefreshMigrationStatusAsync()
        {
            try
            {
                var migrationStatus = await AdvancedSqlServerDatabaseManager.CheckMigrationStatusAsync();

                if (migrationStatus.PendingMigrations.Count == 0)
                {
                    _migrationStatusLabel.Text = "Migration Status: ✓ Up to date";
                    _migrationStatusLabel.ForeColor = Color.Green;
                }
                else
                {
                    _migrationStatusLabel.Text = $"Migration Status: {migrationStatus.PendingMigrations.Count} pending";
                    _migrationStatusLabel.ForeColor = Color.Orange;
                }
            }
            catch (Exception ex)
            {
                _migrationStatusLabel.Text = "Migration Status: Unknown";
                _migrationStatusLabel.ForeColor = Color.Gray;
                if (_logger != null)
                    s_logMigrationStatusCheckFailed(_logger, ex);
            }
        }

        private void AppendStatus(string message)
        {
            if (_statusTextBox.InvokeRequired)
            {
                _statusTextBox.Invoke(() => AppendStatus(message));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _statusTextBox.AppendText($"[{timestamp}] {message}\r\n");
            _statusTextBox.SelectionStart = _statusTextBox.Text.Length;
            _statusTextBox.ScrollToCaret();
        }

        private void ApplyTheme()
        {
            _panel.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground;

            foreach (Control control in _panel.Controls)
            {
                if (control is Label label)
                {
                    label.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText;
                    if (label == _connectionStatusLabel || label == _migrationStatusLabel)
                    {
                        // Keep status colors as they are
                        continue;
                    }
                    label.Font = control.Name?.Contains("title") == true ?
                        BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineFont : BusBus.UI.Core.ThemeManager.CurrentTheme.CardFont;
                }
                else if (control is Button button)
                {
                    if (button == _updateDatabaseButton)
                    {
                        button.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground;
                    }
                    else if (button == _testConnectionButton)
                    {
                        button.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground;
                    }
                    button.ForeColor = Color.White;
                }
            }
        }

        public Task ActivateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeactivateAsync() => Task.CompletedTask;
        public void Dispose() => _panel?.Dispose();
    }
}
