#pragma warning disable CS0067 // Event is never used
#nullable enable
// <auto-added>
// using BusBus.UI; // Removed duplicate
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Services;
// using BusBus.UI; // Removed duplicate
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusBus.UI.Core;
using BusBus.Data;

#pragma warning disable CA1848 // Use LoggerMessage delegates for logging performance
#pragma warning disable CA2254 // LoggerMessage delegates for logging performance
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Suppressed because fields are initialized in SetupLayout.
namespace BusBus.UI
{
    using BusBus.UI.Core;
    /// <summary>
    /// Main application hub that manages navigation, state, and view lifecycle.
    /// This class serves as the primary container/shell for the entire application.
    /// It contains the header, side panel, content panel, and status bar.
    /// Different views (like DashboardView) are loaded into the content panel.
    /// Enhanced with SQL Server Express monitoring and performance tracking.
    /// </summary>
    using BusBus.UI.Templates;
    public partial class Dashboard : HighQualityFormTemplate, IApplicationHub
    {
        #region Fields
        // Disposal guard to prevent double disposal
        private bool _disposed = false;
        private bool _isShuttingDown = false; // Added missing field declaration

        // Use 'new' to hide inherited members from base Form/HighQualityFormTemplate
        private new readonly IServiceProvider _serviceProvider;
        private readonly IRouteService _routeService;
        private new readonly ILogger<Dashboard> _logger;
        // Remove _mainLayout from Dashboard; use inherited from HighQualityFormTemplate
        private readonly Dictionary<string, IView> _viewCache = new();
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
        private EventHandler? _performanceMonitorTimerTickHandler; // Added for robust unsubscription

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

        // Use the base class's _mainLayout; remove this field to avoid CS0108 warning
        // private TableLayoutPanel _mainLayout;
        private Panel _sidePanel;
        private Panel _contentPanel; private Panel _headerPanel;
        // Note: _footerPanel removed as it's not currently used - status bar serves as footer
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripProgressBar _progressBar; private IView? _currentView;
        private CancellationTokenSource _cancellationTokenSource = new();
        private readonly List<Task> _backgroundTasks = new(); // Track background tasks
        #endregion

        #region Constructor
        public Dashboard(IServiceProvider serviceProvider, IRouteService routeService, ILogger<Dashboard> logger)
            : base(serviceProvider)
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
            // Store the handler and subscribe
            _performanceMonitorTimerTickHandler = async (s, e) => await MonitorDatabasePerformanceAsync();
            _performanceMonitorTimer.Tick += _performanceMonitorTimerTickHandler;
            _performanceMonitorTimer.Start();

            _logger.LogDebug("[DEBUG] Dashboard constructor called. serviceProvider: {ServiceProvider}, routeService: {RouteService}, logger: {Logger}", serviceProvider, routeService, logger);

            // Log Dashboard creation for lifecycle tracking
            _logger.LogInformation("[LIFECYCLE] Dashboard created - PID: {ProcessId}, Thread: {ThreadId}",
                Environment.ProcessId, Environment.CurrentManagedThreadId);

            // Use the template's layout initialization
            InitializeForm();
            RegisterViews();
            SubscribeToEvents();

            _logger.LogInformation("Dashboard initialized successfully with performance monitoring enabled");
            // pragma disables above
        }
        #endregion

        #region Layout Setup
        private void SetupLayout()
        {
            _logger.LogDebug("Starting SetupLayout");
            SuspendLayout();

            // Set form properties
            Text = "BusBus - Transport Management System";
            this.WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1024, 768);
            ((Form)this).StartPosition = FormStartPosition.CenterScreen;            // Create main table layout
                                                                                    // Use the base class's _mainLayout instead of declaring a new one
            var mainLayout = GetType().BaseType?.GetField("_mainLayout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(this) as TableLayoutPanel;
            if (mainLayout == null)
            {
                throw new InvalidOperationException("Base class _mainLayout not found. Ensure HighQualityFormTemplate defines _mainLayout.");
            }

            // Configure layout proportions
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65)); // Header - slightly increased
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // Status - slightly increased

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250)); // Sidebar
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Main content

            // Create panels
            CreateHeaderPanel();
            CreateSidePanel();
            CreateContentPanel();
            CreateStatusBar();

            // Add panels to layout
            mainLayout.Controls.Add(_headerPanel, 0, 0);
            mainLayout.SetColumnSpan(_headerPanel, 2);

            mainLayout.Controls.Add(_sidePanel, 0, 1);
            mainLayout.Controls.Add(_contentPanel, 1, 1);

            mainLayout.Controls.Add(_statusStrip, 0, 2);
            mainLayout.SetColumnSpan(_statusStrip, 2);

            Controls.Add(mainLayout);            // Apply theme
            BusBus.UI.Core.ThemeManager.ApplyTheme(this, BusBus.UI.Core.ThemeManager.CurrentTheme);

            // Apply high-quality text rendering to the entire form
            TextRenderingManager.RegisterForHighQualityTextRendering(this);

            ResumeLayout(false);
            PerformLayout();
            this.Visible = true; // Ensure form is visible
            _headerPanel.Visible = true;
            _sidePanel.Visible = true;
            _contentPanel.Visible = true;
            _statusStrip.Visible = true;
            _logger.LogDebug("SetupLayout completed and set all panels visible");
        }
        private void CreateHeaderPanel()
        {
            // Use the template's CreateHeaderSection for consistency
            _headerPanel = CreateHeaderSection("BusBus Transport Management", $"Welcome, {Environment.UserName}");
            // Add theme toggle button if needed
            var themeToggle = new Button
            {
                Text = "🌙",
                Font = new Font("Segoe UI", 12.5F),
                Size = new Size(45, 45),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = "ThemeToggle",
                Padding = new Padding(0, 2, 0, 0)
            };
            themeToggle.FlatAppearance.BorderSize = 0;
            themeToggle.Click += (s, e) => ToggleTheme();
            _headerPanel.Controls.Add(themeToggle);
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
                button.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonHoverBackground;
            }

            return button;
        }
        private void CreateContentPanel()
        {
            try
            {
                _logger?.LogDebug("Creating content panel");
                _contentPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Tag = "ContentPanel",
                    Padding = new Padding(30), // Increased padding for better text spacing
                    AutoScroll = true // Ensure content can be scrolled if it's too large
                };

                // Register the panel for high-quality text rendering
                TextRenderingManager.RegisterForHighQualityTextRendering(_contentPanel);
                _logger?.LogDebug("Content panel created successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create content panel");
                throw;
            }
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
                _logger.LogInformation("Navigating to view: {ViewName}", viewName);

                if (_contentPanel == null || _sidePanel == null || _headerPanel == null)
                {
                    _logger.LogWarning("One or more panels (content, side, header) are null. Reinitializing layout.");
                    SetupLayout(); // Ensure panels are created if they were missed
                }

                if (_contentPanel == null)
                {
                    throw new InvalidOperationException("Content panel is not initialized.");
                }

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
                }                // Robust null check for _contentPanel with recovery attempt
                if (_contentPanel == null)
                {
                    _logger?.LogWarning("Content panel is null, attempting to recreate");
                    try
                    {
                        CreateContentPanel();
                        if (_contentPanel != null)
                        {
                            // Re-add to layout if needed
                            var mainLayout = Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                            if (mainLayout != null && !mainLayout.Controls.Contains(_contentPanel))
                            {
                                mainLayout.Controls.Add(_contentPanel, 1, 1);
                            }
                            _logger?.LogInformation("Content panel successfully recreated");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to recreate content panel");
                    }
                    // Final check after recreation attempt
                    if (_contentPanel == null)
                    {
                        var errMsg = $"Critical error: Content panel is not initialized when navigating to '{viewName}'.";
                        _logger?.LogError(errMsg);
                        ShowStatus(errMsg, StatusType.Error);
                        HideProgress();
                        return;
                    }
                }

                // Optionally use parameter for view activation if needed (not currently used)                // Load view with proper overlap prevention
                try
                {
                    // Ensure we're on the UI thread
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            if (_contentPanel != null)
                                _contentPanel.Controls.Clear();
                        }));
                    }
                    else
                    {
                        _contentPanel.Controls.Clear();
                    }
                }
                catch (Exception controlEx)
                {
                    _logger?.LogError(controlEx, $"Error clearing content panel for {viewName}");
                    throw;
                }

                // Activate view first - this creates the Control for views that need it
                try
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    await view.ActivateAsync(_cancellationTokenSource.Token);
                    watch.Stop();
                    _currentView = view;

                    // Log navigation performance
                    if (_logger != null)
                        _logNavigationPerformance(_logger, viewName, watch.ElapsedMilliseconds, null);
                }
                catch (Exception activationEx)
                {
                    _logger?.LogError(activationEx, $"Error activating view: {viewName}");
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
                            _logger?.LogDebug($"Successfully added view control for: {viewName}");
                        }
                        else
                        {
                            _logger?.LogWarning($"View control for {viewName} is disposed, recreating view");
                            _viewCache.Remove(viewName);
                            // Recursive call to recreate the view
                            await NavigateToAsync(viewName, parameter);
                            return;
                        }
                    }
                    else
                    {
                        _logger?.LogError($"View control is still null after activation for: {viewName}");
                        ShowStatus($"Failed to load {viewName} - no control available", StatusType.Error);
                        return;
                    }
                }
                catch (Exception controlEx)
                {
                    _logger?.LogError(controlEx, $"Error managing view controls for {viewName}");
                    throw;
                }

                // Update UI with proper exception handling
                UpdateNavigationButtons(viewName);
                UpdateTitle(view.Title);

                HideProgress();
                ShowStatus($"{view.Title} loaded", StatusType.Success);
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
                    {
                        view = viewName switch
                        {
                            "dashboard" => new DashboardOverviewView(_serviceProvider), // Home/overview panel
                            "routes" => new RouteListPanel(_routeService, _driverService, _vehicleService), // Full CRUD routes functionality - now enabled
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

                    _logger.LogDebug("Creating new view instance for {ViewName}", viewName);
                    _logger.LogDebug("Created view instance: {ViewType}, Control: {ControlType}", view?.GetType().Name, view?.Control?.GetType().Name);
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
            // Enhanced null check to prevent crashes during initialization
            if (_sidePanel?.Controls == null || _sidePanel.Controls.Count == 0)
            {
                _logger?.LogDebug("UpdateNavigationButtons called before _sidePanel is fully initialized, skipping update");
                return;
            }

            // Additional null check for activeView parameter
            if (string.IsNullOrEmpty(activeView))
            {
                _logger?.LogDebug("UpdateNavigationButtons called with null or empty activeView, skipping update");
                return;
            }

            try
            {
                // Add null check for ThemeManager.CurrentTheme
                var currentTheme = BusBus.UI.Core.ThemeManager.CurrentTheme;
                if (currentTheme == null)
                {
                    _logger?.LogWarning("ThemeManager.CurrentTheme is null, cannot update navigation button colors");
                    return;
                }

                foreach (Control control in _sidePanel.Controls)
                {
                    if (control is Button button && button.Tag is string viewName)
                    {
                        button.BackColor = viewName.Equals(activeView, StringComparison.OrdinalIgnoreCase)
                            ? currentTheme.ButtonHoverBackground
                            : currentTheme.SidePanelBackground;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error updating navigation buttons for view: {ActiveView}", activeView);
            }
        }

        private void UpdateTitle(string viewTitle)
        {
            Text = $"BusBus - {viewTitle}";
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


            if (_statusLabel != null)
            {
                _statusLabel.Text = message;
                _statusLabel.ForeColor = type switch
                {
                    StatusType.Success => Color.Green,
                    StatusType.Warning => Color.Orange,
                    StatusType.Error => Color.Red,
                    _ => SystemColors.ControlText
                };
            }

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

            if (_statusLabel != null)
                _statusLabel.Text = message;
            if (_progressBar != null)
                _progressBar.Visible = true;
        }

        public void HideProgress()
        {
            if (InvokeRequired)
            {
                Invoke(() => HideProgress());
                return;
            }

            if (_progressBar != null)
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
            if (_currentView != null)
            {
                _logger?.LogDebug("RefreshCurrentView called for {ViewName}", _currentView.ViewName);
                // Re-activate the current view to refresh its data
                var refreshTask = Task.Run(async () =>
                {
                    try
                    {
                        if (_cancellationTokenSource.IsCancellationRequested) return;
                        await _currentView.DeactivateAsync();
                        if (_cancellationTokenSource.IsCancellationRequested) return;
                        await _currentView.ActivateAsync(_cancellationTokenSource.Token);
                        _logger?.LogDebug("View {ViewName} refreshed successfully.", _currentView.ViewName);
                    }
                    catch (OperationCanceledException oce)
                    {
                        _logger?.LogWarning(oce, "RefreshCurrentView: Operation canceled for view {ViewName}", _currentView.ViewName);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "RefreshCurrentView: Error refreshing view {ViewName}", _currentView.ViewName);
                        // Optionally, notify the user or take other recovery actions.
                    }
                }, _cancellationTokenSource.Token);

                // Task 4: Track the refresh task
                lock (_backgroundTasks) // Ensure thread-safe access
                {
                    _backgroundTasks.Add(refreshTask);
                    _logger?.LogDebug("Added RefreshCurrentView task to _backgroundTasks. Count: {Count}", _backgroundTasks.Count);
                }

                refreshTask.ContinueWith(t =>
                {
                    lock (_backgroundTasks) // Ensure thread-safe access for removal
                    {
                        _backgroundTasks.Remove(t);
                        _logger?.LogDebug("Removed RefreshCurrentView task from _backgroundTasks. Count: {Count}", _backgroundTasks.Count);
                    }
                    if (t.IsFaulted)
                    {
                        _logger?.LogError(t.Exception?.GetBaseException(), "Error occurred in RefreshCurrentView background task for view {ViewName}", _currentView?.ViewName);
                    }
                    else if (t.IsCanceled)
                    {
                        _logger?.LogWarning("RefreshCurrentView background task was canceled for view {ViewName}", _currentView?.ViewName);
                    }
                }, TaskScheduler.Default); // Use TaskScheduler.Default to ensure it runs off the UI thread and reliably for cleanup
            }
            else
            {
                _logger?.LogDebug("RefreshCurrentView called but _currentView is null.");
            }
        }
        #endregion

        #region Theme Management
        private void ToggleTheme()
        {
            var newTheme = BusBus.UI.Core.ThemeManager.CurrentTheme.Name == "Dark" ? "Light" : "Dark";
            BusBus.UI.Core.ThemeManager.SwitchTheme(newTheme);
            _state.CurrentTheme = newTheme;
            SaveState();
        }

        private void SubscribeToEvents()
        {
            BusBus.UI.Core.ThemeManager.ThemeChanged += OnThemeChanged;
            // Handle parent form closing if this is used as a main form control
            if (this.ParentForm != null)
            {
                this.ParentForm.FormClosing += OnFormClosing;
            }
            else
            {
                // Subscribe when parent changes
                this.ParentChanged += (s, e) =>
                {
                    if (this.ParentForm != null)
                    {
                        this.ParentForm.FormClosing += OnFormClosing;
                    }
                };
            }
            Load += OnFormLoad;
        }
        private void OnThemeChanged(object? sender, EventArgs e)
        {
            // Update theme toggle button only if _headerPanel is initialized
            if (_headerPanel != null)
            {
                var themeButton = _headerPanel.Controls.OfType<Button>()
                    .FirstOrDefault(b => b.Tag?.ToString() == "ThemeToggle");

                if (themeButton != null)
                {
                    themeButton.Text = BusBus.UI.Core.ThemeManager.CurrentTheme.Name == "Dark" ? "☀️" : "🌙";
                }
            }
        }
        #endregion

        #region State Management
        private async void OnFormLoad(object? sender, EventArgs e)
        {
            LoadState();
            this.Show(); // Force form to display
            _logger.LogDebug("Form shown in OnFormLoad");
            await NavigateToAsync(_state.LastView ?? "dashboard");
        }
        // Make OnFormClosing async to allow awaiting PerformShutdownAsync
        private async void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            _logger?.LogInformation("[LIFECYCLE] Dashboard OnFormClosing triggered. Reason: {CloseReason}, PID: {ProcessId}", e.CloseReason, Environment.ProcessId);

            if (_disposed)
            {
                _logger?.LogDebug("Dashboard already disposed, OnFormClosing exiting early.");
                return;
            }

            if (_isShuttingDown)
            {
                _logger?.LogDebug("Shutdown already in progress (_isShuttingDown is true), OnFormClosing exiting to prevent re-entrancy.");
                // Optionally, if the form MUST wait for the ongoing shutdown, this could be a loop with a timeout,
                // or e.Cancel could be set. For now, exiting.
                return;
            }

            if (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.ApplicationExitCall)
            {
                _logger?.LogInformation("UserClosing or ApplicationExitCall detected. Initiating PerformShutdownAsync.");
                // Prevent the form from closing immediately if we want to wait for async shutdown.
                // However, PerformShutdownAsync is void. If it were Task, we could await and then decide on e.Cancel.
                // For now, we assume PerformShutdownAsync handles what it can and the application will exit.
                // To truly wait, PerformShutdownAsync would need to be a Task and OnFormClosing would need to manage e.Cancel.
                // This current structure will initiate shutdown and the form will likely close while PerformShutdownAsync runs.
                // If PerformShutdownAsync must complete before the form closes, a more complex e.Cancel management is needed.

                // Set _isShuttingDown here to prevent re-entry from other sources if PerformShutdownAsync is lengthy.
                _isShuttingDown = true;
                await PerformShutdownAsync(); // This will call Dispose(true) in its finally block.
                                              // After this, _isShuttingDown will be reset in PerformShutdownAsync's finally.
            }
            else
            {
                _logger?.LogDebug("OnFormClosing: CloseReason ({CloseReason}) is not UserClosing or ApplicationExitCall, normal closure assumed without full async shutdown orchestration here.", e.CloseReason);
            }

            // Original Console.WriteLine and direct CTS disposal are removed as PerformShutdownAsync and Dispose now handle cleanup.
        }

        private async Task PerformShutdownAsync()
        {
            if (_disposed) // If already disposed, skip.
            {
                _logger?.LogDebug("PerformShutdownAsync called but Dashboard is already disposed. Skipping.");
                return;
            }
            if (_isShuttingDown && System.Threading.Thread.CurrentThread.ManagedThreadId != GetHashCode()) // Basic re-entrancy guard, crude check
            {
                _logger?.LogDebug("Shutdown already in progress (PerformShutdownAsync re-entry guard), skipping.");
                return;
            }

            // Ensure _isShuttingDown is set if not already (e.g. if called directly, not from OnFormClosing)
            if (!_isShuttingDown) _isShuttingDown = true;


            try
            {
                _logger?.LogInformation("Starting async shutdown process (PerformShutdownAsync)");
                SaveState(); // Ensure state is saved

                // Cancel all background operations
                try
                {
                    if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested && !IsCtsDisposed(_cancellationTokenSource))
                    {
                        _cancellationTokenSource.Cancel();
                        _logger?.LogDebug("Cancellation requested during shutdown via PerformShutdownAsync");
                    }
                }
                catch (ObjectDisposedException)
                {
                    _logger?.LogDebug("CancellationTokenSource already disposed during PerformShutdownAsync cancellation attempt.");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error canceling token source during PerformShutdownAsync");
                }

                // Task 5: Wait for background tasks to complete with a timeout
                Task[] tasksToWaitFor;
                lock (_backgroundTasks) // Ensure thread-safe access for copying the list
                {
                    tasksToWaitFor = _backgroundTasks.ToArray(); // Create a snapshot
                }

                if (tasksToWaitFor.Length > 0)
                {
                    _logger?.LogInformation("PerformShutdownAsync: Waiting for {TaskCount} background tasks to complete with a timeout...", tasksToWaitFor.Length);
                    try
                    {
                        var allTasksCompletionTask = Task.WhenAll(tasksToWaitFor);
                        var timeoutDelayTask = Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource?.Token ?? CancellationToken.None); // Use CTS for delay, fallback if CTS is null/disposed

                        var completedTask = await Task.WhenAny(allTasksCompletionTask, timeoutDelayTask);

                        if (completedTask == timeoutDelayTask)
                        {
                            _logger?.LogWarning("PerformShutdownAsync: Timeout reached while waiting for background tasks. {RemainingCount} tasks might still be running.", tasksToWaitFor.Count(t => !t.IsCompleted));
                        }
                        else // allTasksCompletionTask completed
                        {
                            if (allTasksCompletionTask.IsFaulted)
                            {
                                _logger?.LogError(allTasksCompletionTask.Exception?.GetBaseException(), "PerformShutdownAsync: One or more background tasks failed during shutdown.");
                            }
                            else if (allTasksCompletionTask.IsCanceled)
                            {
                                _logger?.LogWarning("PerformShutdownAsync: One or more background tasks were canceled during shutdown.");
                            }
                            else
                            {
                                _logger?.LogInformation("PerformShutdownAsync: All background tasks completed successfully or were cancelled gracefully.");
                            }
                        }
                    }
                    catch (OperationCanceledException oce) // Catch cancellation of the Task.Delay or other operations if using the main CTS
                    {
                        _logger?.LogWarning(oce, "PerformShutdownAsync: Waiting for background tasks was canceled.");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "PerformShutdownAsync: Error occurred while waiting for background tasks.");
                    }
                }
                else
                {
                    _logger?.LogInformation("PerformShutdownAsync: No background tasks to wait for.");
                }

                // Deactivate current view with timeout protection
                if (_currentView != null)
                {
                    try
                    {
                        // Check if control is valid before deactivating
                        if (_currentView.Control != null && !_currentView.Control.IsDisposed && _currentView.Control.IsHandleCreated)
                        {
                            var deactivateTask = _currentView.DeactivateAsync();
                            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(500), Program.AppCancellationToken); // Increased timeout, use app token

                            await Task.WhenAny(deactivateTask, timeoutTask);

                            if (!deactivateTask.IsCompleted)
                            {
                                _logger?.LogWarning("View deactivation timed out during PerformShutdownAsync");
                            }
                        }
                        else
                        {
                            _logger?.LogWarning("Current view control is null, disposed, or handle not created; skipping deactivation.");
                        }
                    }
                    catch (ObjectDisposedException odEx)
                    {
                        _logger?.LogWarning(odEx, "ObjectDisposedException while deactivating current view during shutdown. View or its components might have been disposed prematurely.");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error deactivating view during PerformShutdownAsync");
                    }
                    finally
                    {
                        _currentView = null; // Clear current view reference
                    }
                }


                // Dispose all cached views quickly and safely
                var disposeTasks = new List<Task>();
                // Use ToList() to create a copy for safe iteration if _viewCache could be modified elsewhere (though less likely during shutdown)
                foreach (var kvp in _viewCache.ToList()) // Ensure ToList() is used if not already
                {
                    var view = kvp.Value;

                    // Task 3: Unsubscribe event handlers before disposing the view
                    if (view != null) // Corrected syntax: added parentheses
                    {
                        view.NavigationRequested -= OnViewNavigationRequested;
                        view.StatusUpdated -= OnViewStatusUpdated;
                        _logger?.LogDebug("Unsubscribed event handlers for cached view {ViewName} during PerformShutdownAsync", kvp.Key);
                    }

                    if (view is IDisposable disposableView)
                    {
                        try
                        {
                            // Check if control is valid before disposing
                            // Corrected syntax error: added parentheses
                            if (view.Control != null && !view.Control.IsDisposed)
                            {
                                var disposeTask = Task.Run(() =>
                                {
                                    try
                                    {
                                        disposableView.Dispose();
                                        _logger?.LogDebug("Disposed cached view {ViewName} during PerformShutdownAsync", kvp.Key);
                                    }
                                    catch (ObjectDisposedException odExInner)
                                    {
                                        _logger?.LogWarning(odExInner, "ObjectDisposedException during async disposal of cached view {ViewName}.", kvp.Key);
                                    }
                                    catch (Exception exInner)
                                    {
                                        _logger?.LogError(exInner, "Error during async disposal of cached view {ViewName}", kvp.Key);
                                    }
                                    // Pass the application's CancellationToken to ensure tasks are cancelled if the app shuts down abruptly
                                }, Program.AppCancellationToken); // Use global app cancellation token
                                disposeTasks.Add(disposeTask);
                                // BusBus.Program.AddBackgroundTask(disposeTask); // Consider if AddBackgroundTask is robust for shutdown scenarios
                            }
                            else
                            {
                                _logger?.LogDebug("Skipping disposal of cached view {ViewName} as its control is null or disposed.", kvp.Key);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error scheduling view disposal for {ViewName} during PerformShutdownAsync", kvp.Key);
                        }
                    }
                }
                // Wait for disposals with a reasonable timeout
                if (disposeTasks.Count > 0)
                {
                    _logger?.LogInformation("Waiting for {DisposeTaskCount} view disposal tasks to complete during PerformShutdownAsync", disposeTasks.Count);
                    try
                    {
                        // Use a timeout relevant for shutdown; Program.AppCancellationToken will also help cut this short if app is force-closing
                        var allDisposalsTask = Task.WhenAll(disposeTasks);
                        var timeoutDelayTask = Task.Delay(TimeSpan.FromSeconds(2), Program.AppCancellationToken); // 2-second timeout for all view disposals

                        var completedTask = await Task.WhenAny(allDisposalsTask, timeoutDelayTask);

                        if (completedTask == timeoutDelayTask)
                        {
                            _logger?.LogWarning("Timed out waiting for some view disposal tasks during PerformShutdownAsync.");
                            int incompleteCount = disposeTasks.Count(t => !t.IsCompleted);
                            if (incompleteCount > 0)
                            {
                                _logger?.LogWarning("{IncompleteCount} disposal tasks were still running at timeout.", incompleteCount);
                            }
                        }
                        else if (allDisposalsTask.IsFaulted)
                        {
                            _logger?.LogError(allDisposalsTask.Exception?.GetBaseException(), "Exception occurred in one or more view disposal tasks.");
                        }
                        else
                        {
                            _logger?.LogInformation("All scheduled view disposal tasks completed or timed out.");
                        }
                    }
                    catch (OperationCanceledException oce) when (oce.CancellationToken == Program.AppCancellationToken)
                    {
                        _logger?.LogWarning("View disposal task waiting was canceled by application shutdown.");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error waiting for view disposal tasks during PerformShutdownAsync");
                    }
                }

                // Clear caches
                _viewCache.Clear();
                _navigationHistory.Clear();
                _logger?.LogDebug("View cache and navigation history cleared during PerformShutdownAsync.");

                // Signal application to clean up all background tasks
                try
                {
                    _logger?.LogInformation("Calling global application shutdown from PerformShutdownAsync");
                    Program.ShutdownApplication();
                    _logger?.LogInformation("Global application shutdown call completed from PerformShutdownAsync");
                }
                catch (Exception shutdownEx)
                {
                    _logger?.LogError(shutdownEx, "Error invoking application shutdown from PerformShutdownAsync");
                }

                _logger?.LogInformation("Async shutdown process (PerformShutdownAsync) completed its main steps.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unhandled exception during PerformShutdownAsync main logic.");
            }
            finally
            {
                _logger?.LogDebug("PerformShutdownAsync finally block: Resetting _isShuttingDown and calling Dispose(true).");
                _isShuttingDown = false; // Reset flag before Dispose, as Dispose transitions to a final state.
                this.Dispose(true); // Call the main dispose logic
                // _isShuttingDown = false; // Moved before Dispose(true)
                _logger?.LogDebug("PerformShutdownAsync finally block: Completed.");
            }
        }

        // ... existing LoadState/SaveState ...
        private void LoadState()
        {
            // Load state from settings/file
            // For now, just use defaults
            _state.CurrentTheme = "Dark"; // Assuming _state is DashboardState and accessible
            _state.LastView = "dashboard";
            _logger?.LogDebug("Dashboard state loaded. Theme: {Theme}, LastView: {LastView}", _state.CurrentTheme, _state.LastView);
        }

        private void SaveState()
        {
            if (_state != null) // Add null check for _state
            {
                _state.LastView = _currentView?.ViewName; // Assuming _state is DashboardState and accessible
                // Save state to settings/file (actual persistence logic would go here)
                _logger?.LogDebug("Dashboard state saved. LastView: {LastView}", _state.LastView);
            }
            else
            {
                _logger?.LogWarning("SaveState called but _state is null. Cannot save state.");
            }
        }
        #endregion

        #region IApplicationHub Implementation

        public IServiceProvider ServiceProvider => _serviceProvider;

        // Ensure CurrentView property is correctly implemented for IApplicationHub
        public IView? CurrentView => _currentView;
        // public string ViewName => throw new NotImplementedException(); // This was likely a placeholder

        // Ensure Title property is correctly implemented (if part of IApplicationHub or used internally)
        // public string Title => throw new NotImplementedException(); // This was likely a placeholder

        // Ensure NavigationChanged event is correctly implemented for IApplicationHub
        public event EventHandler<NavigationEventArgs>? NavigationChanged;
        // Method to raise the event if needed
        protected virtual void OnNavigationChanged(NavigationEventArgs e)
        {
            NavigationChanged?.Invoke(this, e);
        }

        // Ensure ShowNotification is correctly implemented for IApplicationHub
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
            // Consider if MessageBox is always desired or should be conditional
            if (type == NotificationType.Error || type == NotificationType.Warning)
            {
                // Ensure MessageBox is called on the UI thread if ShowNotification can be called from background threads
                if (InvokeRequired)
                {
                    Invoke(new Action(() => MessageBox.Show(this, message, title, MessageBoxButtons.OK, type == NotificationType.Error ? MessageBoxIcon.Error : MessageBoxIcon.Warning)));
                }
                else
                {
                    MessageBox.Show(this, message, title, MessageBoxButtons.OK, type == NotificationType.Error ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
                }
            }
        }
        #endregion

        #region Disposal
        // Add IDisposable pattern implementation
        protected override void Dispose(bool disposing)
        {
            // Add a trace log for entry, helps diagnose if it's called multiple times unexpectedly.
            _logger?.LogTrace("[LIFECYCLE] Dashboard.Dispose(disposing: {IsDisposing}) called. Current _disposed state: {DisposedState}", disposing, _disposed);

            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _logger?.LogDebug("[LIFECYCLE] Dashboard.Dispose(true) - Disposing managed resources.");

                // Task 2: Stop and dispose the performance monitor timer
                if (_performanceMonitorTimer != null)
                {
                    _performanceMonitorTimer.Stop();
                    if (_performanceMonitorTimerTickHandler != null)
                    {
                        _performanceMonitorTimer.Tick -= _performanceMonitorTimerTickHandler;
                        _logger?.LogDebug("Unsubscribed from _performanceMonitorTimer.Tick.");
                    }
                    _performanceMonitorTimer.Dispose();
                    // _performanceMonitorTimer = null; // Field is readonly, cannot set to null here.
                    _logger?.LogDebug("Performance monitor timer stopped and disposed.");
                }

                // Task 6: Dispose CancellationTokenSource
                if (_cancellationTokenSource != null && !IsCtsDisposed(_cancellationTokenSource))
                {
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            _cancellationTokenSource.Cancel();
                            _logger?.LogDebug("Cancellation requested via CancellationTokenSource in Dashboard.Dispose.");
                        }
                        catch (ObjectDisposedException)
                        {
                            _logger?.LogWarning("Attempted to cancel an already disposed CancellationTokenSource in Dashboard.Dispose.");
                        }
                    }
                    try
                    {
                        _cancellationTokenSource.Dispose();
                        _logger?.LogDebug("CancellationTokenSource disposed in Dashboard.Dispose.");
                    }
                    catch (ObjectDisposedException)
                    {
                        _logger?.LogWarning("Attempted to dispose an already disposed CancellationTokenSource in Dashboard.Dispose.");
                    }
                    // _cancellationTokenSource = null; // Field is readonly, cannot set to null here.
                }


                // Task 7: Unsubscribe Dashboard-level event handlers
                try
                {
                    BusBus.UI.Core.ThemeManager.ThemeChanged -= OnThemeChanged;
                    _logger?.LogDebug("Unsubscribed from ThemeManager.ThemeChanged.");

                    this.Load -= OnFormLoad;
                    _logger?.LogDebug("Unsubscribed from this.Load.");

                    if (this.ParentForm != null)
                    {
                        // This unsubscription might be tricky if ParentChanged handler re-subscribed it multiple times
                        // or if ParentForm changed. This handles the current ParentForm.
                        this.ParentForm.FormClosing -= OnFormClosing;
                        _logger?.LogDebug("Attempted to unsubscribe from this.ParentForm.FormClosing.");
                    }
                    // Note: The ParentChanged lambda that subscribes to ParentForm.FormClosing is not unsubscribed here.
                    // This could be a minor leak if Dashboard instances are created/destroyed repeatedly
                    // AND their ParentForm changes during their lifetime. For a long-lived, single Dashboard instance,
                    // this is generally not a significant issue.
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Exception during unsubscription of Dashboard-level event handlers.");
                }

                _logger?.LogDebug("Dashboard-level event handlers unsubscription attempted.");
            }

            // Dispose unmanaged resources here if any (none identified for Dashboard itself)
            _logger?.LogDebug("Setting _disposed = true and calling base.Dispose({IsDisposing}).", disposing);
            _disposed = true; // Set disposed flag before calling base, to prevent re-entry from base if it calls back.
            base.Dispose(disposing); // IMPORTANT: Call base class dispose
            _logger?.LogTrace("[LIFECYCLE] Dashboard.Dispose(disposing: {IsDisposing}) finished.", disposing);
        }

        // Helper method to check if CancellationTokenSource is disposed
        private bool IsCtsDisposed(CancellationTokenSource? cts)
        {
            if (cts == null)
            {
                _logger?.LogTrace("IsCtsDisposed: CTS is null, considering it disposed.");
                return true;
            }
            try
            {
                // Accessing Token property throws ObjectDisposedException if disposed.
                _ = cts.Token;
                return false;
            }
            catch (ObjectDisposedException)
            {
                _logger?.LogTrace("IsCtsDisposed: CTS is disposed (ObjectDisposedException).");
                return true;
            }
            // Should not happen if cts is not null, but as a fallback.
            catch (NullReferenceException)
            {
                _logger?.LogWarning("IsCtsDisposed: CTS is not null but threw NullReferenceException, considering it disposed.");
                return true;
            }
        }
        #endregion
    }
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Suppressed because fields are initialized in SetupLayout.

