#pragma warning disable CS0067 // Event is never used
#nullable enable
// <auto-added>
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Services;
using BusBus.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusBus.Data;
using System.Diagnostics;

#pragma warning disable CA1848 // Use LoggerMessage delegates for logging performance
#pragma warning disable CA2254 // LoggerMessage delegates for logging performance
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Suppressed because fields are initialized in SetupLayout.
namespace BusBus.UI
{    /// <summary>
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
        private readonly Dictionary<string, IView> _viewCache = new();
        private readonly Stack<string> _navigationHistory = new();
        private readonly DashboardState _state = new();
        private readonly AdvancedSqlServerDatabaseManager? _databaseManager;

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
        private ToolStripProgressBar _progressBar;        private IView? _currentView;
        private CancellationTokenSource _cancellationTokenSource = new();
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
            };            var navItems = new[]
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
        }        private Button CreateNavigationButton(NavigationItem item)
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
                var view = GetOrCreateView(viewName);
                if (view == null)
                {
                    _logger.LogWarning($"View not found: {viewName}");
                    ShowStatus($"View '{viewName}' not found", StatusType.Warning);
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
                            LogControlHierarchy();
                        }));
                    }
                    else
                    {
                        _contentPanel.Controls.Clear();
                        LogControlHierarchy();
                    }

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
                        _logger.LogWarning($"View control is null for: {viewName}");
                    }
                }
                catch (Exception controlEx)
                {
                    _logger.LogError(controlEx, $"Error managing view controls for {viewName}");
                    throw;
                }                // Activate view with exception handling for Win32 and InvalidOperation exceptions
                try
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    await view.ActivateAsync(_cancellationTokenSource.Token);
                    watch.Stop();
                    _currentView = view;

                    // Log navigation performance
                    _logNavigationPerformance(_logger, viewName, watch.ElapsedMilliseconds, null);

                    // Update UI with proper exception handling
                    UpdateNavigationButtons(viewName);
                    UpdateTitle(view.Title);

                    HideProgress();
                    ShowStatus($"{view.Title} loaded", StatusType.Success);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    _logger.LogError(ex, $"Win32 error during view activation for {viewName}");
                    HideProgress();
                    ShowStatus($"System error loading {viewName}", StatusType.Error);
                    throw;
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, $"Invalid operation during view activation for {viewName}");
                    HideProgress();
                    ShowStatus($"Operation error loading {viewName}", StatusType.Error);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error navigating to {viewName}");
                HideProgress();
                ShowStatus($"Error loading {viewName}: {ex.Message}", StatusType.Error);
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

        private IView? GetOrCreateView(string viewName)
        {
            lock (_viewLock)
            {
                // Log start of view creation for diagnostics
                _logger.LogDebug($"GetOrCreateView called for: {viewName}");
                var startTime = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    // Check if view is already cached and not disposed
                    if (_viewCache.TryGetValue(viewName, out var cachedView))
                    {
                        // Verify the view is still valid
                        if (cachedView.Control == null || cachedView.Control.IsDisposed)
                        {
                            _logger.LogDebug($"Cached view {viewName} is disposed, removing from cache");
                            _viewCache.Remove(viewName);
                        }
                        else
                        {
                            _logger.LogDebug($"Retrieved cached view: {viewName} in {startTime.ElapsedMilliseconds}ms");
                            return cachedView;
                        }
                    }                    // Create view based on name
                    IView? view = null;
                    try
                    {                        view = viewName.ToLower() switch
                        {
                            "routes" => new RouteListViewPlaceholder(_routeService),
                            "drivers" => new DriverListView(_serviceProvider),
                            "vehicles" => new VehicleListView(_serviceProvider),
                            "reports" => new ReportsView(_serviceProvider),
                            "settings" => new SettingsView(_serviceProvider),
                            _ => null
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error creating view instance for: {viewName}");
                        return null;
                    }

                    if (view != null)
                    {
                        // Verify the Control property is accessible
                        try
                        {
                            var control = view.Control;
                            if (control == null)
                            {
                                _logger.LogError($"View control is null for: {viewName}");
                                return null;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error accessing view control for: {viewName}");
                            return null;
                        }

                        // Cache the view and subscribe to events
                        _viewCache[viewName] = view;
                        view.NavigationRequested += OnViewNavigationRequested;
                        view.StatusUpdated += OnViewStatusUpdated;

                        _logger.LogDebug($"Created new view: {viewName} in {startTime.ElapsedMilliseconds}ms");
                        _logger.LogDebug($"View cache count: {_viewCache.Count}");
                    }
                    else
                    {
                        _logger.LogError($"Failed to create view: {viewName}");
                    }

                    return view;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating view: {viewName}");
                    throw; // Rethrow to be handled by NavigateToAsync
                }
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

        private void OnViewNavigationRequested(object? sender, NavigationEventArgs e)
        {
            _ = NavigateToAsync(e.ViewName);
        }

        private void OnViewStatusUpdated(object? sender, StatusEventArgs e)
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
                        ShowStatus("Database connection issues detected", StatusType.Warning);
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
        public void ShowStatus(string message, StatusType type = StatusType.Info)
        {
            if (InvokeRequired)
            {
                Invoke(() => ShowStatus(message, type));
                return;
            }

            _statusLabel.Text = message;
            _statusLabel.ForeColor = type switch
            {
                StatusType.Success => Color.Green,
                StatusType.Warning => Color.Orange,
                StatusType.Error => Color.Red,
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
            SaveState();
        }

        private void SubscribeToEvents()
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            this.FormClosing += OnFormClosing;
            this.Load += OnFormLoad;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            // Update theme toggle button
            var themeButton = _headerPanel.Controls.OfType<Button>()
                .FirstOrDefault(b => b.Tag?.ToString() == "ThemeToggle");

            if (themeButton != null)
            {
                themeButton.Text = ThemeManager.CurrentTheme.Name == "Dark" ? "☀️" : "🌙";
            }
        }
        #endregion

        #region State Management
        private async void OnFormLoad(object? sender, EventArgs e)
        {
            LoadState();
            await NavigateToAsync(_state.LastView ?? "routes");
        }
        private async void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                _logger?.LogInformation("Dashboard form closing initiated");
                SaveState();                // Set a variable to indicate we're closing - for future use if needed
                                            // bool isDisposing = true;

                // Cleanup - handle potential ObjectDisposedException
                try
                {
                    if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Cancel();
                        _logger?.LogDebug("Cancellation requested during form closing");
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error canceling token source during form closing");
                }

                // Deactivate current view with timeout protection
                if (_currentView != null)
                {
                    try
                    {
                        // Create a task with timeout to prevent hanging
                        var deactivateTask = _currentView.DeactivateAsync();
                        var timeoutTask = Task.Delay(500); // 500ms timeout - reduced from 1s to speed up closing

                        // Wait for either task to complete
                        await Task.WhenAny(deactivateTask, timeoutTask);

                        if (!deactivateTask.IsCompleted)
                        {
                            _logger?.LogWarning("View deactivation timed out after 500ms");
                        }
                        else
                        {
                            _logger?.LogDebug("View deactivated successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error deactivating view during form closing");
                    }
                }

                // Clear current view reference
                _currentView = null;

                // Dispose all cached views quickly with timeouts
                var disposeTasks = new List<Task>();
                foreach (var view in _viewCache.Values)
                {
                    if (view is IDisposable disposableView)
                    {
                        try
                        {
                            // Run disposals in parallel with individual timeouts
                            var disposeTask = Task.Run(() =>
                            {
                                try
                                {
                                    disposableView.Dispose();
                                    return true;
                                }
                                catch (Exception)
                                {
                                    return false;
                                }
                            });
                            disposeTasks.Add(disposeTask);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error scheduling view disposal");
                        }
                    }
                }

                // Wait for disposals with timeout
                if (disposeTasks.Count > 0)
                {
                    var timeoutTask = Task.Delay(300); // 300ms maximum for all disposals
                    await Task.WhenAny(Task.WhenAll(disposeTasks), timeoutTask);
                    _logger?.LogDebug("View disposal tasks completed or timed out");
                }

                // Clear the cache to release references
                _viewCache.Clear();
                _navigationHistory.Clear();

                _logger?.LogInformation("Dashboard form closing completed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unhandled exception during form closing");
            }
        }

        private void LoadState()
        {
            // Load state from settings/file
            // For now, just use defaults
            _state.CurrentTheme = "Dark";
            _state.LastView = "dashboard";
        }

        private void SaveState()
        {
            _state.LastView = _currentView?.ViewName;
            // Save state to settings/file
        }
        #endregion

        #region IApplicationHub Implementation

        public IServiceProvider ServiceProvider => _serviceProvider;

        public IView? CurrentView => _currentView;
        public event EventHandler<NavigationEventArgs>? NavigationChanged;

        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            // For now, show as a status message and optionally a MessageBox for certain types
            var statusType = type switch
            {
                NotificationType.Success => StatusType.Success,
                NotificationType.Warning => StatusType.Warning,
                NotificationType.Error => StatusType.Error,
                _ => StatusType.Info
            };
            ShowStatus($"{title}: {message}", statusType);
            if (type == NotificationType.Error || type == NotificationType.Warning)
            {
                MessageBox.Show(this, message, title, MessageBoxButtons.OK, type == NotificationType.Error ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
            }
        }

        public void ShowBusyIndicator(string message)
        {
            ShowProgress(message);
        }

        public void HideBusyIndicator()
        {
            HideProgress();
        }

        public async Task<bool> ShowConfirmationAsync(string message, string title)
        {
            if (InvokeRequired)
            {
                return await Task.FromResult((bool)Invoke(new Func<bool>(() =>
                {
                    var result = MessageBox.Show(this, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    return result == DialogResult.Yes;
                })));
            }
            else
            {
                var result = MessageBox.Show(this, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return await Task.FromResult(result == DialogResult.Yes);
            }
        }        // ShowBusyIndicator and HideBusyIndicator implementations exist above

        public void UpdateProgress(int percentage, string message = "")
        {
            Utils.ThreadSafeUI.Invoke(this, () =>
            {
                _progressBar.Style = ProgressBarStyle.Blocks;
                _progressBar.Value = Math.Max(0, Math.Min(100, percentage));
                if (!string.IsNullOrEmpty(message))
                    _statusLabel.Text = message;
            });
        }

        public void SaveState(object state)
        {
            if (state is DashboardState dashboardState)
            {
                // Save current navigation state
                dashboardState.CurrentViewName = _currentView?.GetType().Name ?? string.Empty;
                dashboardState.NavigationHistory.Clear();
                dashboardState.NavigationHistory.AddRange(_navigationHistory);
                _logger.LogInformation("Dashboard state saved with {Count} history items", dashboardState.NavigationHistory.Count);
            }
        }

        public void RestoreState(object state)
        {
            if (state is DashboardState dashboardState)
            {
                // Restore navigation history
                _navigationHistory.Clear();
                foreach (var item in dashboardState.NavigationHistory)
                {
                    _navigationHistory.Push(item);
                }                // Navigate to the previous view if available
                if (!string.IsNullOrEmpty(dashboardState.CurrentViewName))
                {
                    _ = Task.Run(async () => await NavigateToAsync(dashboardState.CurrentViewName));
                }

                _logger.LogInformation("Dashboard state restored with {Count} history items", dashboardState.NavigationHistory.Count);
            }
        }

        // Override Dispose to clean up resources
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // Enhanced disposal state checks
                    if (!IsHandleCreated || IsDisposed || Disposing)
                    {
                        _logger?.LogDebug("Skipping disposal - invalid state (HandleCreated: {HandleCreated}, IsDisposed: {IsDisposed}, Disposing: {Disposing})",
                            IsHandleCreated, IsDisposed, Disposing);
                        return;
                    }

                    _logger?.LogInformation("Dashboard disposing resources");

                    // First try to cancel all operations
                    var cts = _cancellationTokenSource;
                    if (cts != null)
                    {
                        try
                        {
                            if (!cts.IsCancellationRequested)
                            {
                                cts.Cancel();
                                _logger?.LogDebug("Cancellation requested during dispose");
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Already disposed, ignore
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error canceling token during dispose");
                        }

                        try
                        {
                            cts.Dispose();
                            _logger?.LogDebug("CancellationTokenSource disposed");
                        }
                        catch (ObjectDisposedException)
                        {
                            // Already disposed, ignore
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error disposing CancellationTokenSource");
                        }
                    }

                    // Clear references
                    _cancellationTokenSource = null!;
                    _currentView = null;

                    // Dispose cached views with enhanced error handling
                    foreach (var view in _viewCache.Values.ToList())
                    {
                        try
                        {
                            if (view is IDisposable disposableView && view.Control != null && !view.Control.IsDisposed)
                            {
                                disposableView.Dispose();
                                _logger?.LogDebug("Disposed view: {ViewType}", view.GetType().Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error disposing view {ViewType}", view.GetType().Name);
                        }
                    }

                    // Clear collections to release references
                    _viewCache.Clear();
                    _navigationHistory.Clear();

                    // Force immediate GC collection to help release resources
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    _logger?.LogInformation("Dashboard resources disposed");
                }
                catch (Exception ex)
                {
                    // Log any unexpected exceptions but don't crash
                    System.Diagnostics.Debug.WriteLine($"Error disposing Dashboard: {ex.Message}");
                    _logger?.LogError(ex, "Unhandled exception during Dashboard disposal");
                }
            }
            try
            {
                // Only call base.Dispose if we're not in the middle of creating a handle
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    base.Dispose(disposing);
                }
                else if (!this.IsDisposed)
                {
                    // If handle is not created, dispose more carefully
                    this.Invoke(new Action(() =>
                    {
                        try
                        {
                            base.Dispose(disposing);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error in delayed base Dispose call");
                        }
                    }));
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("CreateHandle") || ex.Message.Contains("Invoke"))
            {
                _logger?.LogWarning("Skipping base disposal due to handle creation conflict: {Message}", ex.Message);
                _logger?.LogDebug("Skipping disposal - invalid state (HandleCreated: {HandleCreated}, IsDisposed: {IsDisposed}, Disposing: {Disposing})",
                    this.IsHandleCreated, this.IsDisposed, disposing);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in base Dispose call");
            }
        }

        // Helper method to check if CancellationTokenSource is disposed
        private static bool IsCtsDisposed(CancellationTokenSource cts)
        {
            try
            {
                // If we can access the Token property without exception, it's not disposed
                var _ = cts.Token;
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        /// <summary>
        /// Logs the current control hierarchy for debugging UI issues
        /// </summary>
        private void LogControlHierarchy()
        {
            if (_contentPanel == null) return;

            _logger.LogDebug($"Content panel control count: {_contentPanel.Controls.Count}");
            foreach (Control control in _contentPanel.Controls)
            {
                _logger.LogDebug($"Control: {control.GetType().Name}, Name: {control.Name}, Visible: {control.Visible}, IsDisposed: {control.IsDisposed}");
            }
        }
        #endregion
    }

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
}
