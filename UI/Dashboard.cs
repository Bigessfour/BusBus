#pragma warning disable CS0067 // Event is never used
#nullable enable
// <auto-added>
using BusBus.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusBus.Data;
using System.Diagnostics;

#pragma warning disable CA1848 // Use LoggerMessage delegates for logging performance
#pragma warning disable CA2254 // LoggerMessage delegates for logging performance
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Suppressed because fields are initialized in SetupLayout.
namespace BusBus.UI
{
    // Remove using BusBus.UI.Dashboard; // This might be causing namespace conflicts

    /// <summary>
    /// Main application hub that manages navigation, state, and view lifecycle.
    /// This class serves as the primary container/shell for the entire application.
    /// It contains the header, side panel, content panel, and status bar.
    /// Different views (like DashboardView) are loaded into the content panel.
    /// Enhanced with SQL Server Express monitoring and performance tracking.
    /// </summary>
    public partial class Dashboard : Form, IApplicationHub
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly IRouteService _routeService;
        private readonly ILogger<Dashboard> _logger;
        private readonly Dictionary<string, BusBus.UI.IView> _viewCache = new();
        private readonly Stack<string> _navigationHistory = new();
        private readonly DashboardState _state = new();
        private readonly AdvancedSqlServerDatabaseManager? _databaseManager;

        // Added missing service fields
        private readonly IDriverService _driverService;
        private readonly IVehicleService _vehicleService;

        // Performance monitoring
        private readonly Dictionary<string, long> _performanceMetrics = new();
        private readonly System.Windows.Forms.Timer _performanceMonitorTimer;
        private readonly object _metricsLock = new object();

        // Logger message definitions for performance
        private static readonly Action<ILogger, string, long, Exception?> _logNavigationPerformance =
            LoggerMessage.Define<string, long>(
                LogLevel.Debug,
                new EventId(1, "NavigationPerformance"),
                "Navigation to '{ViewName}' completed in {ElapsedMs}ms");

        private static readonly Action<ILogger, string, int, Exception?> _logDatabasePerformance =
            LoggerMessage.Define<string, int>(
                LogLevel.Information,
                new EventId(2, "DatabasePerformance"),
                "Database health check: {Status} - Active connections: {ConnectionCount}");

        private TableLayoutPanel _mainLayout;
        private Panel _sidePanel;
        private Panel _contentPanel; private Panel _headerPanel;
        // Note: _footerPanel removed as it's not currently used - status bar serves as footer
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripProgressBar _progressBar;
        private BusBus.UI.IView? _currentView;
        private CancellationTokenSource _cancellationTokenSource = new();
        private readonly List<Task> _backgroundTasks = new(); // Track background tasks

        // IApplicationHub.CurrentView explicit implementation
        BusBus.UI.IView? IApplicationHub.CurrentView => _currentView;

        public event EventHandler<BusBus.UI.NavigationEventArgs>? NavigationChanged;

        // Explicit interface implementation for ShowNotification
        void IApplicationHub.ShowNotification(string title, string message, BusBus.UI.NotificationType type)
        {
            // Basic implementation, can be expanded with a custom notification UI
            MessageBox.Show(message, title, MessageBoxButtons.OK,
                type == BusBus.UI.NotificationType.Error ? MessageBoxIcon.Error :
                type == BusBus.UI.NotificationType.Warning ? MessageBoxIcon.Warning :
                MessageBoxIcon.Information);
            _logger.LogInformation($"Notification shown: [{type}] {title} - {message}");
        }

        // Added to implement the missing interface member for Dashboard.NotificationType
        void IApplicationHub.ShowNotification(string title, string message, Dashboard.NotificationType dashboardType)
        {
            BusBus.UI.NotificationType mappedUiType;
            string dashboardTypeString = dashboardType.ToString();

            // Attempt to parse Dashboard.NotificationType string representation to BusBus.UI.NotificationType
            if (Enum.TryParse<BusBus.UI.NotificationType>(dashboardTypeString, true, out var parsedUiType))
            {
                mappedUiType = parsedUiType;
            }
            else
            {
                // Log a warning and default if mapping by name fails.
                _logger.LogWarning($"Unable to map Dashboard.NotificationType '{dashboardTypeString}' to BusBus.UI.NotificationType. Defaulting to Info.");
                mappedUiType = BusBus.UI.NotificationType.Info; // Default to a safe value
            }

            // Call the other explicit implementation with the mapped type
            ((IApplicationHub)this).ShowNotification(title, message, mappedUiType);
        }

        #endregion

        #region Constructor
        public Dashboard(IServiceProvider serviceProvider, IRouteService routeService, ILogger<Dashboard> logger)
        {
            if (serviceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] serviceProvider is null in Dashboard constructor");
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            if (routeService == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] routeService is null in Dashboard constructor!");
                System.Diagnostics.Debugger.Break();
                throw new ArgumentNullException(nameof(routeService));
            }
            if (logger == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] logger is null in Dashboard constructor");
                throw new ArgumentNullException(nameof(logger));
            }


            _serviceProvider = serviceProvider;
            _routeService = routeService;
            _logger = logger;
            // Resolve missing services
            _driverService = _serviceProvider.GetRequiredService<IDriverService>();
            _vehicleService = _serviceProvider.GetRequiredService<IVehicleService>();

            // Get database manager for performance monitoring
            _databaseManager = _serviceProvider.GetService<AdvancedSqlServerDatabaseManager>();

            // Initialize performance monitoring timer
            _performanceMonitorTimer = new System.Windows.Forms.Timer
            {
                Interval = 60000 // 1 minute intervals
            };
            _performanceMonitorTimer.Tick += async (s, e) => await MonitorDatabasePerformanceAsync();
            _performanceMonitorTimer.Start();

            _logger.LogDebug("[DEBUG] Dashboard constructor called. serviceProvider: {ServiceProvider}, routeService: {RouteService}, logger: {Logger}", serviceProvider, routeService, logger);

            // InitializeComponent(); // Removed to avoid duplicate/hidden controls
            SetupLayout();
            RegisterViews();
            SubscribeToEvents();

            _logger.LogInformation("Dashboard initialized successfully with performance monitoring enabled");
            // pragma disables above
        }
        #endregion

        #region Layout Setup
        private void SetupLayout()
        {
            this.SuspendLayout();

            // Set form properties
            this.Text = "BusBus - Transport Management System";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(1024, 768);
            this.StartPosition = FormStartPosition.CenterScreen;            // Create main table layout
            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(0) // Remove default padding
            };

            // Configure layout proportions
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65)); // Header - slightly increased
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // Status - slightly increased

            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250)); // Sidebar
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Main content

            // Create panels
            CreateHeaderPanel();
            CreateSidePanel();
            CreateContentPanel();
            CreateStatusBar();

            // Add panels to layout
            _mainLayout.Controls.Add(_headerPanel, 0, 0);
            _mainLayout.SetColumnSpan(_headerPanel, 2);

            _mainLayout.Controls.Add(_sidePanel, 0, 1);
            _mainLayout.Controls.Add(_contentPanel, 1, 1);

            _mainLayout.Controls.Add(_statusStrip, 0, 2);
            _mainLayout.SetColumnSpan(_statusStrip, 2);

            this.Controls.Add(_mainLayout);            // Apply theme
            ThemeManager.ApplyTheme(this, ThemeManager.CurrentTheme);

            // Apply high-quality text rendering to the entire form
            TextRenderingManager.RegisterForHighQualityTextRendering(this);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
        private void CreateHeaderPanel()
        {
            _headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Tag = "MainShellHeaderPanel", // Updated tag to clearly identify this is the main shell header
                Height = 65 // Slightly increased for better spacing
            };

            var titleLabel = new Label
            {
                Text = "BusBus Transport Management",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                Location = new Point(25, 15), // Increased left margin
                AutoSize = true,
                Padding = new Padding(2) // Add padding to prevent text from touching borders
            };

            var userInfoLabel = new Label
            {
                Text = $"Welcome, {Environment.UserName}",
                Font = new Font("Segoe UI", 10.5F), // Slightly increased for better readability
                ForeColor = ThemeManager.CurrentTheme.SecondaryText,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_headerPanel.Width - 205, 20),
                AutoSize = true,
                Padding = new Padding(2) // Add padding to prevent text from touching borders
            }; var themeToggle = new Button
            {
                Text = "🌙",
                Font = new Font("Segoe UI", 12.5F), // Slightly larger font
                Size = new Size(45, 45), // Slightly larger for better touch target
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_headerPanel.Width - 55, 10), // Adjusted position
                Tag = "ThemeToggle",
                Padding = new Padding(0, 2, 0, 0) // Add padding to center the icon vertically
            };

            themeToggle.FlatAppearance.BorderSize = 0; // Remove border
            themeToggle.Click += (s, e) => ToggleTheme();

            // Register controls for high-quality text rendering
            TextRenderingManager.RegisterForHighQualityTextRendering(_headerPanel);

            _headerPanel.Controls.AddRange(new Control[] { titleLabel, userInfoLabel, themeToggle });
        }
        private void CreateSidePanel()
        {
            _sidePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Tag = "SidePanel",
                AutoScroll = true,
                Padding = new Padding(0, 15, 0, 15) // Increased vertical padding for better spacing
            }; var navItems = new[]
            {
                new NavigationItem("�", "New Feature Here", "new-feature", true),
                new NavigationItem("🚌", "Routes", "routes"),
                new NavigationItem("👥", "Drivers", "drivers"),
                new NavigationItem("🚗", "Vehicles", "vehicles"),
                new NavigationItem("📊", "Reports", "reports"),
                new NavigationItem("⚙️", "Settings", "settings")
            };

            int yPos = 25; // Increased initial position
            foreach (var item in navItems)
            {
                var navButton = CreateNavigationButton(item);
                navButton.Location = new Point(15, yPos); // Increased left margin
                _sidePanel.Controls.Add(navButton);
                yPos += 55; // Increased spacing between buttons
            }

            // Register for high-quality text rendering
            TextRenderingManager.RegisterForHighQualityTextRendering(_sidePanel);
        }
        private Button CreateNavigationButton(NavigationItem item)
        {
            var button = new Button
            {
                Text = $"{item.Icon} {item.Title}",
                Size = new Size(230, 45),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10.5F),
                Tag = item.ViewName,
                Padding = new Padding(12, 0, 0, 0),
                Margin = new Padding(0, 5, 0, 5)
            };

            button.FlatAppearance.BorderSize = 0;

            // Only wire the click event for non-new-feature buttons
            if (item.ViewName != "new-feature")
            {
                button.Click += async (s, e) => await NavigateToAsync(item.ViewName);
            }
            else
            {
                // New feature button - no functionality yet
                button.Click += (s, e) =>
                {
                    // Placeholder - no action
                };
            }

            // Register for high-quality text rendering
            TextRenderingManager.RegisterForHighQualityTextRendering(button);

            if (item.IsActive)
            {
                button.BackColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
            }

            return button;
        }
        private void CreateContentPanel()
        {
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Tag = "ContentPanel",
                Padding = new Padding(30), // Increased padding for better text spacing
                AutoScroll = true // Ensure content can be scrolled if it's too large
            };

            // Register the panel for high-quality text rendering
            TextRenderingManager.RegisterForHighQualityTextRendering(_contentPanel);
        }
        private void CreateStatusBar()
        {
            _statusStrip = new StatusStrip
            {
                Tag = "StatusBar",
                SizingGrip = false // Remove the sizing grip in the corner
            };

            _statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F), // Ensure readable font size
                Padding = new Padding(5, 0, 0, 0) // Add left padding for text
            };

            _progressBar = new ToolStripProgressBar
            {
                Width = 120,
                Visible = false,
                Style = ProgressBarStyle.Marquee,
                Margin = new Padding(5, 3, 5, 3) // Add margin around progress bar
            };

            var connectionLabel = new ToolStripStatusLabel
            {
                Text = "Connected",
                ForeColor = Color.Green,
                Font = new Font("Segoe UI", 9F), // Ensure readable font size
                Padding = new Padding(0, 0, 5, 0) // Add right padding for text
            };

            _statusStrip.Items.AddRange(new ToolStripItem[]
            {
                _statusLabel,
                _progressBar,
                new ToolStripSeparator(),
                connectionLabel
            });
        }
        #endregion

        #region Navigation
        public async Task NavigateToAsync(string viewName, object? parameter = null)
        {
            if (viewName == null)
            {
                throw new ArgumentNullException(nameof(viewName));
            }

            try
            {
                _logger.LogInformation($"Navigating to view: {viewName}");
                ShowProgress($"Loading {viewName}...");

                // Save current view state
                if (_currentView != null)
                {
                    await _currentView.DeactivateAsync();
                    _navigationHistory.Push(_currentView.ViewName);
                }

                // Get or create view
                var view = GetOrCreateView(viewName); // This should return BusBus.UI.IView
                if (view == null)
                {
                    _logger.LogWarning($"View not found: {viewName}");
                    ShowStatus($"View '{viewName}' not found", BusBus.UI.StatusType.Warning);
                    return;
                }

                // Optionally use parameter for view activation if needed (not currently used)                // Load view with proper overlap prevention
                try
                {
                    // Ensure we're on the UI thread
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            _contentPanel.Controls.Clear();
                            // LogControlHierarchy();
                        }));
                    }
                    else
                    {
                        _contentPanel.Controls.Clear();
                        // LogControlHierarchy();
                    }
                }
                catch (Exception controlEx)
                {
                    _logger.LogError(controlEx, $"Error clearing content panel for {viewName}");
                    throw;
                }

                // Activate view first - this creates the Control for views that need it
                try
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    await view.ActivateAsync(_cancellationTokenSource.Token);
                    watch.Stop();
                    _currentView = view; // Assign BusBus.UI.IView to BusBus.UI.IView

                    // Log navigation performance
                    _logNavigationPerformance(_logger, viewName, watch.ElapsedMilliseconds, null);
                }
                catch (Exception activationEx)
                {
                    _logger.LogError(activationEx, $"Error activating view: {viewName}");
                    throw;
                }

                // Now add the control to the UI (after activation)
                try
                {
                    if (view.Control != null)
                    {
                        // Verify the control is not disposed before adding
                        if (!view.Control.IsDisposed)
                        {
                            view.Control.Dock = DockStyle.Fill;
                            if (InvokeRequired)
                            {
                                Invoke(new Action(() =>
                                {
                                    _contentPanel.Controls.Add(view.Control);
                                    view.Control.BringToFront();
                                }));
                            }
                            else
                            {
                                _contentPanel.Controls.Add(view.Control);
                                view.Control.BringToFront();
                            }
                            _logger.LogDebug($"Successfully added view control for: {viewName}");
                        }
                        else
                        {
                            _logger.LogWarning($"View control for {viewName} is disposed, recreating view");
                            _viewCache.Remove(viewName);
                            // Recursive call to recreate the view
                            await NavigateToAsync(viewName, parameter);
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogError($"View control is still null after activation for: {viewName}");
                        ShowStatus($"Failed to load {viewName} - no control available", BusBus.UI.StatusType.Error);
                        return;
                    }
                }
                catch (Exception controlEx)
                {
                    _logger.LogError(controlEx, $"Error managing view controls for {viewName}");
                    throw;
                }

                // Update UI with proper exception handling
                UpdateNavigationButtons(viewName);
                UpdateTitle(view.Title);

                HideProgress();
                ShowStatus($"{view.Title} loaded", BusBus.UI.StatusType.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error navigating to {viewName}");
                HideProgress();
                ShowStatus($"Error loading {viewName}: {ex.Message}", BusBus.UI.StatusType.Error);
            }
        }

        public async Task NavigateBackAsync()
        {
            if (_navigationHistory.Count > 0)
            {
                var previousView = _navigationHistory.Pop();
                await NavigateToAsync(previousView);
            }
        }
        private readonly object _viewLock = new();

        private BusBus.UI.IView? GetOrCreateView(string viewName) // Ensure return type is BusBus.UI.IView
        {
            lock (_viewLock)
            {
                if (_viewCache.TryGetValue(viewName, out var cachedView))
                {
                    if (cachedView.Control != null && !cachedView.Control.IsDisposed)
                    {
                        _logger.LogDebug($"Returning cached view: {viewName}");
                        return cachedView; // Returns BusBus.UI.IView from cache
                    }
                    else
                    {
                        _logger.LogWarning($"Cached view '{viewName}' control is null or disposed. Removing from cache.");
                        _viewCache.Remove(viewName);
                    }
                }

                _logger.LogDebug($"Creating new view: {viewName}");
                BusBus.UI.IView? view = null; // Explicitly type as BusBus.UI.IView
                try
                {
                    // Use a switch expression for cleaner view creation
                    view = viewName switch
                    {
                        "dashboard" => _serviceProvider.GetRequiredService<DashboardOverviewView>(),
                        "routes" => _serviceProvider.GetRequiredService<RouteListPanel>(),
                        "drivers" => _serviceProvider.GetRequiredService<DriverListView>(), // Corrected to DriverListView
                        "vehicles" => _serviceProvider.GetRequiredService<VehicleListView>(), // Corrected to VehicleListView
                        "reports" => _serviceProvider.GetRequiredService<ReportsView>(),
                        "settings" => _serviceProvider.GetRequiredService<SettingsView>(),
                        // "new-feature" => new NewFeatureView(), // Commented out as NewFeatureView is not defined
                        _ => null
                    };

                    if (view != null)
                    {
                        view.NavigationRequested += OnViewNavigationRequested;
                        view.StatusUpdated += OnViewStatusUpdated;
                        _viewCache[viewName] = view;
                    }
                    else
                    {
                        _logger.LogError($"Failed to create view: {viewName}. Service not registered or view name incorrect.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating view '{viewName}'");
                    view = null; // Ensure view is null on error
                }
                return view;
            }
        }

        private void UpdateNavigationButtons(string activeView)
        {
            foreach (Control control in _sidePanel.Controls)
            {
                if (control is Button button && button.Tag is string viewName)
                {
                    button.BackColor = viewName.Equals(activeView, StringComparison.OrdinalIgnoreCase)
                        ? ThemeManager.CurrentTheme.ButtonHoverBackground
                        : ThemeManager.CurrentTheme.SidePanelBackground;
                }
            }
        }

        private void UpdateTitle(string viewTitle)
        {
            this.Text = $"BusBus - {viewTitle}";
        }
        #endregion

        #region View Management
        private static void RegisterViews()
        {
            // We don't pre-register views anymore to avoid potential UI conflicts
            // Views will be created on-demand in GetOrCreateView
        }

        private void OnViewNavigationRequested(object? sender, BusBus.UI.NavigationEventArgs e) // Corrected EventArgs type
        {
            _ = NavigateToAsync(e.ViewName);
        }

        private void OnViewStatusUpdated(object? sender, BusBus.UI.StatusEventArgs e) // Ensure correct StatusEventArgs namespace
        {
            ShowStatus(e.Message, e.Type);
        }
        #endregion

        #region Performance Monitoring
        private async Task MonitorDatabasePerformanceAsync()
        {
            try
            {
                if (_databaseManager != null)
                {
                    // Get database performance metrics
                    var metrics = _databaseManager.GetPerformanceMetrics();

                    lock (_metricsLock)
                    {
                        // Update stored metrics
                        foreach (var metric in metrics)
                        {
                            _performanceMetrics[metric.Key] = metric.Value;
                        }
                    }

                    // Test database connection health
                    var isHealthy = await _databaseManager.TestConnectionAsync();
                    var connectionCount = metrics.Count;

                    _logDatabasePerformance(_logger, isHealthy ? "Healthy" : "Unhealthy", connectionCount, null);

                    // Update status if database is unhealthy
                    if (!isHealthy)
                    {
                        ShowStatus("Database connection issues detected", BusBus.UI.StatusType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database performance monitoring");
            }
        }

        public Dictionary<string, long> GetPerformanceMetrics()
        {
            lock (_metricsLock)
            {
                return new Dictionary<string, long>(_performanceMetrics);
            }
        }
        #endregion

        #region Status Management
        public void ShowStatus(string message, BusBus.UI.StatusType type = BusBus.UI.StatusType.Info) // Ensure this matches IApplicationHub
        {
            if (InvokeRequired)
            {
                Invoke(() => ShowStatus(message, type));
                return;
            }

            _statusLabel.Text = message;
            _statusLabel.ForeColor = type switch
            {
                BusBus.UI.StatusType.Success => Color.Green,
                BusBus.UI.StatusType.Warning => Color.Orange,
                BusBus.UI.StatusType.Error => Color.Red,
                _ => SystemColors.ControlText
            };

            _logger.LogInformation($"Status: {message} [{type}]");
            // pragma disables above
        }

        public void ShowProgress(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => ShowProgress(message));
                return;
            }

            _statusLabel.Text = message;
            _progressBar.Visible = true;
        }

        public void HideProgress()
        {
            if (InvokeRequired)
            {
                Invoke(() => HideProgress());
                return;
            }

            _progressBar.Visible = false;
        }

        public void UpdateStatusMessage(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = message;
            }
        }

        public void RefreshCurrentView()
        {
            // Refresh the current view implementation
            if (_contentPanel?.Controls.Count > 0)
            {
                var currentControl = _contentPanel.Controls[0];
                if (currentControl is IRefreshable refreshable)
                {
                    refreshable.Refresh();
                }
            }
        }
        #endregion

        #region Theme Management
        private void ToggleTheme()
        {
            var newTheme = ThemeManager.CurrentTheme.Name == "Dark" ? "Light" : "Dark";
            ThemeManager.SwitchTheme(newTheme);
            _state.CurrentTheme = newTheme;
            // SaveState(); // Commented out as SaveState() is not defined
        }

        private void SubscribeToEvents()
        {
            // Subscribe to theme changes
            ThemeManager.ThemeChanged += OnThemeChanged; // Corrected: Was ThemeManager_ThemeChanged
        }

        private static Color GetStatusColor(BusBus.UI.StatusType type) // Made static
        {
            return type switch
            {
                BusBus.UI.StatusType.Info => Color.Gray,
                BusBus.UI.StatusType.Success => Color.Green,
                BusBus.UI.StatusType.Warning => Color.Orange,
                BusBus.UI.StatusType.Error => Color.Red,
                _ => Color.Black
            };
        }

        #endregion

        #region Event Handlers
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // LogControlHierarchy(); // Commented out as it's not defined
            await NavigateToAsync("dashboard");
            // SaveState(); // Ensure this is removed
        }

        private void OnThemeChanged(object? sender, EventArgs e) // Keep this one
        {
            ThemeManager.ApplyTheme(this, ThemeManager.CurrentTheme);
            // Refresh child controls if necessary
            foreach (Control c in _sidePanel.Controls)
            {
                ThemeManager.ApplyTheme(c, ThemeManager.CurrentTheme);
            }
            ThemeManager.ApplyTheme(_contentPanel, ThemeManager.CurrentTheme);
            if (_currentView?.Control != null)
            {
                ThemeManager.ApplyTheme(_currentView.Control, ThemeManager.CurrentTheme);
            }
        }
        #endregion

        #region Disposal and Cleanup
        // Override Dispose to clean up resources
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from events to prevent memory leaks
                ThemeManager.ThemeChanged -= OnThemeChanged; // Corrected: Was ThemeManager_ThemeChanged
                _performanceMonitorTimer?.Dispose();
                _cancellationTokenSource?.Cancel();

                foreach (var view in _viewCache.Values.ToList())
                {
                    try
                    {
                        if (view is IDisposable disposableView && view.Control != null && !view.Control.IsDisposed)
                        {
                            disposableView.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error disposing view {ViewType}", view.GetType().Name);
                    }
                }

                _viewCache.Clear();
                _navigationHistory.Clear();

                try
                {
                    _logger?.LogInformation("Notifying Program class about dashboard disposal");
                    var programType = Type.GetType("BusBus.Program, BusBus");
                    var shutdownMethod = programType?.GetMethod("ShutdownApplication",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    if (shutdownMethod != null)
                    {
                        shutdownMethod.Invoke(null, null);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to notify Program about dashboard disposal");
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                _logger?.LogInformation("Dashboard resources disposed");
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Interfaces and Supporting Classes
        // Duplicate IApplicationHub interface removed. Use the definition from IApplicationHub.cs
        public interface IView : IDisposable
        {
            string ViewName { get; }
            string Title { get; }
            Control? Control { get; }
            event EventHandler<NavigationEventArgs>? NavigationRequested;
            event EventHandler<StatusEventArgs>? StatusUpdated;
            Task ActivateAsync(CancellationToken cancellationToken);
            Task DeactivateAsync();
        }

        public interface IStatefulView
        {
            void SaveState(object state);
            void RestoreState(object state);
        }

        public class NavigationEventArgs : EventArgs
        {
            public string ViewName { get; }
            public object? Parameter { get; }

            public NavigationEventArgs(string viewName, object? parameter = null)
            {
                ViewName = viewName;
                Parameter = parameter;
            }
        }

        public class StatusEventArgs : EventArgs
        {
            public string Message { get; }
            public StatusType Type { get; }

            public StatusEventArgs(string message, StatusType type = StatusType.Info)
            {
                Message = message;
                Type = type;
            }
        }

        public enum StatusType
        {
            Info,
            Success,
            Warning,
            Error
        }

        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
        }
        #endregion

        // IApplicationHub stubs
        public void ShowNotification(string title, string message, UI.NotificationType type = UI.NotificationType.Info)
        {
            // Basic implementation, can be expanded with a custom notification UI
            MessageBox.Show(message, title, MessageBoxButtons.OK,
                type == UI.NotificationType.Error ? MessageBoxIcon.Error :
                type == UI.NotificationType.Warning ? MessageBoxIcon.Warning :
                MessageBoxIcon.Information);
            _logger.LogInformation($"Notification shown: [{type}] {title} - {message}");
        }
    }
}

