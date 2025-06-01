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
            _performanceMonitorTimer.Tick += async (s, e) => await MonitorDatabasePerformanceAsync();
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
         // Ensure the main layout components are correctly initialized and added.
         // This includes setting up the header, side panel, and content area.
         _logger.LogDebug("SetupLayout completed");
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
                // Re-activate the current view to refresh its data
                var refreshTask = Task.Run(async () =>
                {
                    await _currentView.DeactivateAsync();
                    await _currentView.ActivateAsync(_cancellationTokenSource.Token);
                }, _cancellationTokenSource.Token);

                refreshTask.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Error refreshing current view");
                    }
                }); // Schedule scrubbed
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
            await NavigateToAsync(_state.LastView ?? "routes");
        }
        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            Console.WriteLine("[Dashboard] Form closing started");

            // Log Dashboard closing for lifecycle tracking
            _logger.LogInformation("[LIFECYCLE] Dashboard closing - Reason: {CloseReason}, PID: {ProcessId}",
                e.CloseReason, Environment.ProcessId);

            if (e.CloseReason != CloseReason.UserClosing && e.CloseReason != CloseReason.ApplicationExitCall)
                return;

            try
            {
                Console.WriteLine($"Dashboard closing: {e.CloseReason}");

                // Cancel any pending operations
                _cancellationTokenSource?.Cancel();
                Console.WriteLine("[Dashboard] Cancellation requested");

                // Wait for all background tasks to complete
                Task[] tasksToWait;
                lock (_backgroundTasks)
                {
                    tasksToWait = _backgroundTasks.Where(t => !t.IsCompleted).ToArray();
                }

                if (tasksToWait.Length > 0)
                {
                    Console.WriteLine($"[Dashboard] Waiting for {tasksToWait.Length} background tasks to complete");
                    try
                    {
                        // await Task.WhenAll(tasksToWait).ConfigureAwait(false); // CS7069: Call to async method 'Task.WhenAll' should not be awaited in a void returning method.
                        Task.WhenAll(tasksToWait).GetAwaiter().GetResult(); // Synchronously wait
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("[Dashboard] Background tasks cancelled");
                    }
                }

                // Force garbage collection to help with cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Disconnect event handlers that might prevent proper disposal
                // BusBus.UI.BusBus.UI.Core.ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged; // CS0120: An object reference is required for the non-static field, method, or property 'Dashboard.OnThemeChanged(object?, EventArgs)'
                BusBus.UI.Core.ThemeManager.ThemeChanged -= OnThemeChanged; // Corrected: Use instance method
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"[Dashboard] Object already disposed during shutdown: {ex.Message}");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                Console.WriteLine("[Dashboard] Form closing completed");
            }
        }

        // Removed unused field _isShuttingDown to resolve CS0414 warning

        private async Task PerformShutdownAsync()
        {
            if (_isShuttingDown) // Add guard against re-entrancy
            {
                _logger?.LogDebug("Shutdown already in progress, skipping PerformShutdownAsync.");
                return;
            }
            _isShuttingDown = true; // Set flag

            try
            {
                _logger?.LogInformation("Starting async shutdown process");
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
                foreach (var kvp in _viewCache.ToList())
                {
                    var view = kvp.Value;
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
                // This should be one of the last steps to ensure other components are shutting down gracefully.
                try
                {
                    _logger?.LogInformation("Calling global application shutdown from PerformShutdownAsync");
                    // Ensure this is called only once globally. Program.ShutdownApplication should have its own guards.
                    Program.ShutdownApplication(); // Removed isInitiatedByDashboard parameter
                    _logger?.LogInformation("Global application shutdown call completed from PerformShutdownAsync");
                }
                catch (Exception shutdownEx)
                {
                    _logger?.LogError(shutdownEx, "Error invoking application shutdown from PerformShutdownAsync");
                }

                _logger?.LogInformation("Async shutdown process completed");
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

        public IView? CurrentView => _currentView; public string ViewName => throw new NotImplementedException();

        public string Title => throw new NotImplementedException();

        // Remove or rename this property to avoid hiding Form.WindowState
        // public FormWindowState WindowState { get; private set; }

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
                    var navigationTask = Task.Run(async () => await NavigateToAsync(dashboardState.CurrentViewName), Program.AppCancellationToken);
                    BusBus.Program.AddBackgroundTask(navigationTask);
                }

                _logger.LogInformation("Dashboard state restored with {Count} history items", dashboardState.NavigationHistory.Count);
            }
        }        // Override Dispose to clean up resources
        protected override void Dispose(bool disposing)
        {
            // Disposal guard to prevent double disposal
            if (_disposed)
            {
                _logger?.LogDebug("Dispose called again, skipping due to guard");
                return;
            }
            // _disposed = true; // Moved down to prevent issues if an error occurs mid-disposal

            if (disposing)
            {
                // Log Dashboard disposal for lifecycle tracking
                _logger?.LogInformation("[LIFECYCLE] Dashboard disposing - PID: {ProcessId}, Thread: {ThreadId}",
                    Environment.ProcessId, Environment.CurrentManagedThreadId);

                try
                {
                    // Check if already disposing or if handle is not created, which might indicate an invalid state for disposal.
                    if (IsDisposed || Disposing || !IsHandleCreated) // Added IsDisposed and Disposing checks
                    {
                        _logger?.LogDebug("Skipping disposal - invalid state (IsDisposed: {IsDisposed}, Disposing: {Disposing}, HandleCreated: {HandleCreated})",
                            IsDisposed, Disposing, IsHandleCreated);
                        if (!_disposed) _disposed = true; // Ensure disposed is set if we exit early
                        return;
                    }
                    _logger?.LogInformation("Dashboard disposing resources");

                    // Unsubscribe from events first to prevent handlers from running during/after disposal
                    BusBus.UI.Core.ThemeManager.ThemeChanged -= OnThemeChanged;
                    _logger?.LogDebug("Unsubscribed from BusBus.UI.Core.ThemeManager.ThemeChanged");


                    if (_performanceMonitorTimer != null)
                    {
                        _performanceMonitorTimer.Stop();
                        _performanceMonitorTimer.Dispose();
                        // _performanceMonitorTimer = null; // Removed assignment to readonly field
                        _logger?.LogDebug("Performance monitor timer stopped and disposed");
                    }

                    // Cancel and dispose CancellationTokenSource safely
                    if (_cancellationTokenSource != null && !IsCtsDisposed(_cancellationTokenSource))
                    {
                        try
                        {
                            if (!_cancellationTokenSource.IsCancellationRequested)
                            {
                                _cancellationTokenSource.Cancel();
                                _logger?.LogDebug("CancellationTokenSource cancellation requested.");
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            _logger?.LogDebug("CancellationTokenSource already disposed when attempting to cancel.");
                        }
                        _cancellationTokenSource.Dispose();
                        _logger?.LogDebug("CancellationTokenSource disposed");
                    }
                    _cancellationTokenSource = null!; // Set to null after disposal

                    // Deactivate and clear current view
                    if (_currentView != null)
                    {
                        try
                        {
                            // Corrected: Check _currentView.Control directly.
                            if (_currentView.Control != null && !_currentView.Control.IsDisposed)
                            {
                                // Attempt a quick deactivation, but don't let it hang shutdown
                                var deactivateTask = _currentView.DeactivateAsync();
                                if (!deactivateTask.Wait(TimeSpan.FromMilliseconds(200))) // Short timeout
                                {
                                    _logger?.LogWarning("Current view deactivation timed out during dispose.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error deactivating current view during dispose.");
                        }
                        _currentView = null;
                    }


                    foreach (var view in _viewCache.Values.ToList()) // ToList to allow modification
                    {
                        try
                        {
                            if (view is IDisposable disposableView && view.Control != null && !view.Control.IsDisposed) // Check if view.Control is not null
                            {
                                disposableView.Dispose();
                                _logger?.LogDebug("Disposed cached view: {ViewType}", view.GetType().Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error disposing cached view {ViewType}", view.GetType().Name);
                        }
                    }

                    _viewCache.Clear();
                    _navigationHistory.Clear();

                    // Notify Program class about dashboard disposal - consider if this is still needed or handled by PerformShutdownAsync
                    // This might be redundant if PerformShutdownAsync also calls Program.ShutdownApplication
                    // try
                    // {
                    //     _logger?.LogInformation("Notifying Program class about dashboard disposal");
                    //     var programType = Type.GetType("BusBus.Program, BusBus");
                    //     var shutdownMethod = programType?.GetMethod("ShutdownApplication",
                    //         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    //     if (shutdownMethod != null)
                    //     {
                    //         shutdownMethod.Invoke(null, null);
                    //         _logger?.LogDebug("Successfully notified Program about dashboard disposal");
                    //     }
                    // }
                    // catch (Exception ex)
                    // {
                    //     _logger?.LogError(ex, "Failed to notify Program about dashboard disposal during Dashboard.Dispose");
                    // }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    _logger?.LogInformation("Dashboard resources disposed");
                }
                catch (Exception ex)
                {
                    // Use System.Diagnostics.Debug.WriteLine for critical errors during disposal if logger fails
                    System.Diagnostics.Debug.WriteLine($"CRITICAL: Error disposing Dashboard: {ex.Message}");
                    _logger?.LogError(ex, "Unhandled exception during Dashboard disposal");
                }
                finally
                {
                    _disposed = true; // Set disposed flag here to ensure it's always set
                }
            }
            base.Dispose(disposing);
        }

        private static bool IsCtsDisposed(CancellationTokenSource cts)
        {
            if (cts == null) return true; // Consider null as disposed for safety
            try
            {
                var _ = cts.Token;
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }
        protected Task OnActivateAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected Task OnDeactivateAsync()
        {
            throw new NotImplementedException();
        }

        #region Interfaces and Supporting Classes
        // Duplicate IApplicationHub interface removed. Use the definition from IApplicationHub.cs
        // public interface IView : IDisposable
        // {
        //     string ViewName { get; }
        //     string Title { get; }
        //     Control? Control { get; }
        //     event EventHandler<NavigationEventArgs>? NavigationRequested;
        //     event EventHandler<StatusEventArgs>? StatusUpdated;
        //     Task ActivateAsync(CancellationToken cancellationToken);
        //     Task DeactivateAsync();
        // }

        // public interface IStatefulView
        // {
        //     void SaveState(object state);
        //     void RestoreState(object state);
        // }

        // public class NavigationEventArgs : EventArgs
        // {
        //     public string ViewName { get; }
        //     public object? Parameter { get; }

        //     public NavigationEventArgs(string viewName, object? parameter = null)
        //     {
        //         ViewName = viewName;
        //         Parameter = parameter;
        //     }
        // }

        // public class StatusEventArgs : EventArgs
        // {
        //     public string Message { get; }
        //     public StatusType Type { get; }

        //     public StatusEventArgs(string message, StatusType type = StatusType.Info)
        //     {
        //         Message = message;
        //         Type = type;
        //     }
        // }

        // public enum StatusType
        // {
        //     Info,
        //     Success,
        //     Warning,
        //     Error
        // }

        // public enum NotificationType
        // {
        //     Info,
        //     Success,
        //     Warning,
        //     Error
        // }
        #endregion

        #endregion
    }
}

