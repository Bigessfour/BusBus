#nullable enable
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusBus.UI.Common;
using BusBus.Services;
using BusBus.Models;
using System.Data;

namespace BusBus.UI
{
    /// <summary>
    /// The DashboardView is the home/main view that appears in the Dashboard's content panel.
    /// This is loaded when the user clicks on the "Dashboard" navigation button in the side panel.
    /// This view contains the header, side panel, statistics panel, and a shared data grid view.
    /// </summary>
    public class DashboardView : BaseView
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DashboardView> _logger;
        private readonly Stopwatch _loadingStopwatch = new Stopwatch();
        private TableLayoutPanel _mainLayout = null!;
        private Panel _sidePanel = null!;
        private Panel _statsPanel = null!;
        private Panel _contentPanel = null!;
        private Panel _crudPanel = null!;

        // Dynamic DataGridView components
        private DynamicDataGridView<Route>? _routeDataGrid;
        private DynamicDataGridView<Driver>? _driverDataGrid;
        private DynamicDataGridView<Vehicle>? _vehicleDataGrid;
        private Control? _currentDataView;
        private string _currentViewType = "";

        // Navigation buttons for state management
        private readonly Dictionary<string, Button> _navigationButtons = new();
        private readonly Dictionary<string, Button> _crudButtons = new();

        // Data view configurations
        private RouteDataViewConfiguration? _routeConfig;
        private DriverDataViewConfiguration? _driverConfig;
        private VehicleDataViewConfiguration? _vehicleConfig;

        // Timing tracking for performance analysis
        private Dictionary<string, long> _timingMetrics = new Dictionary<string, long>();
        private int _uiComponentsCreated = 0;

        // Statistics tracking
        private readonly Dictionary<string, Label> _statisticsLabels = new();
        private System.Windows.Forms.Timer? _statisticsUpdateTimer;

        // Logger message definitions for performance
        private static readonly Action<ILogger, string, Exception?> _logDebug =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, "UIDebug"),
                "{Message}");

        private static readonly Action<ILogger, string, long, int, Exception?> _logPerformance =
            LoggerMessage.Define<string, long, int>(
                LogLevel.Information,
                new EventId(2, "UIPerformance"),
                "{Operation} completed in {ElapsedMs}ms with {ComponentCount} components"); private static readonly Action<ILogger, string, Exception?> _logInformation =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(3, "UIInformation"),
                "{Message}");

        private static readonly Action<ILogger, string, Exception?> _logWarning =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(4, "UIWarning"),
                "{Message}");

        private static readonly Action<ILogger, string, Exception?> _logError =
            LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(5, "UIError"),
                "{Message}");

        public override string ViewName => "dashboard";
        public override string Title => "What's Happening Today";

        public DashboardView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // Get the logger from the service provider
            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<DashboardView>() ??
                     Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<DashboardView>();

            // Start performance tracking
            _loadingStopwatch.Start();
            LogDebug("DashboardView constructor called");

            // Start tracking creation time
            _timingMetrics["Constructor_Start"] = _loadingStopwatch.ElapsedMilliseconds;

            InitializeView();

            // Record initialization completion time
            _timingMetrics["InitializeView_Complete"] = _loadingStopwatch.ElapsedMilliseconds;
            LogPerformance("Initialization", _loadingStopwatch.ElapsedMilliseconds, _uiComponentsCreated);            // Add event handlers for proper shutdown
            this.Load += OnDashboardLoad;        }

        // Helper logging methods that use the high-performance LoggerMessage pattern
        private void LogDebug(string message) => _logDebug(_logger, message, null);

        private void LogPerformance(string operation, long elapsedMs, int componentCount) =>
            _logPerformance(_logger, operation, elapsedMs, componentCount, null);

        private void LogInformation(string message) => _logInformation(_logger, message, null);

        private void LogWarning(string message) => _logWarning(_logger, message, null);

        private void LogError(string message, Exception? exception = null) => _logError(_logger, message, exception);

        // Helper for tracking UI component creation
        private void TrackComponentCreation(Control control)
        {
            _uiComponentsCreated++;
            LogDebug($"Component created: {control.GetType().Name} ({control.Name ?? control.Tag?.ToString() ?? "unnamed"})");
        }

        protected override void InitializeView()
        {
            LogDebug("InitializeView: Starting initialization");
            _timingMetrics["InitializeView_Start"] = _loadingStopwatch.ElapsedMilliseconds;

            base.InitializeView();
            LogDebug("InitializeView: Base initialization complete");

            // Apply theme to this control
            ThemeManager.ApplyThemeToControl(this);
            LogDebug("InitializeView: Theme applied");

            // Initialize data configurations
            InitializeDataConfigurations();

            // Main dashboard layout - 2 columns, 3 rows for side panel, content, CRUD, and stats
            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10),
                BackColor = ThemeManager.CurrentTheme.MainBackground,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            // Configure column distribution - side panel 250px, content takes remaining space
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250)); // Side panel
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Content area            // Configure row distribution - content area takes most space, CRUD and stats panels are fixed height
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Main content row
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // CRUD panel height
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Stats panel height (compact for small boxes)// Create UI components
            CreateSidePanel();
            CreateContentPanel();
            CreateCrudPanel();
            CreateStatsPanel();

            // Add panels to the layout
            _mainLayout.Controls.Add(_sidePanel, 0, 0);     // Side panel (left, spans rows 0-1)
            _mainLayout.SetRowSpan(_sidePanel, 2);          // Side panel spans content and CRUD rows
            _mainLayout.Controls.Add(_contentPanel, 1, 0);  // Content panel (right, top)
            _mainLayout.Controls.Add(_crudPanel, 1, 1);     // CRUD panel (right, middle)
            _mainLayout.Controls.Add(_statsPanel, 0, 2);    // Stats panel (spans both columns, bottom)
            _mainLayout.SetColumnSpan(_statsPanel, 2);      // Stats panel spans full width

            this.Controls.Add(_mainLayout);

            // Register for resize events to dynamically adjust layouts
            this.Resize += OnDashboardResize;            // Apply high-quality text rendering to all controls
            TextRenderingManager.RegisterForHighQualityTextRendering(this);            // Initialize statistics update timer
            InitializeStatisticsTimer();
        }        private void InitializeDataConfigurations()
        {
            try
            {
                // Get services from dependency injection
                var routeService = _serviceProvider.GetRequiredService<IRouteService>();
                var driverService = _serviceProvider.GetRequiredService<IDriverService>();
                var vehicleService = _serviceProvider.GetRequiredService<IVehicleService>();

                // Create data view configurations
                _routeConfig = new RouteDataViewConfiguration(routeService);
                _driverConfig = new DriverDataViewConfiguration(driverService);
                _vehicleConfig = new VehicleDataViewConfiguration(vehicleService);

                // Create data grid instances once (reuse instead of recreating)
                InitializeDataGrids();

                LogDebug("Data configurations initialized successfully");
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize data configurations", ex);
            }
        }

        private void InitializeDataGrids()
        {
            try
            {
                var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();

                // Create Route data grid
                if (_routeConfig != null)
                {
                    var routeLogger = loggerFactory?.CreateLogger<DynamicDataGridView<Route>>();
                    _routeDataGrid = new DynamicDataGridView<Route>(_routeConfig, routeLogger);
                    _routeDataGrid.StatusUpdated += OnDataViewStatusUpdated;
                    _routeDataGrid.EntitySelected += OnRouteSelected;
                    _routeDataGrid.Dock = DockStyle.Fill;
                    _routeDataGrid.Visible = false; // Hidden by default
                }

                // Create Driver data grid
                if (_driverConfig != null)
                {
                    var driverLogger = loggerFactory?.CreateLogger<DynamicDataGridView<Driver>>();
                    _driverDataGrid = new DynamicDataGridView<Driver>(_driverConfig, driverLogger);
                    _driverDataGrid.StatusUpdated += OnDataViewStatusUpdated;
                    _driverDataGrid.EntitySelected += OnDriverSelected;
                    _driverDataGrid.Dock = DockStyle.Fill;
                    _driverDataGrid.Visible = false; // Hidden by default
                }

                // Create Vehicle data grid
                if (_vehicleConfig != null)
                {
                    var vehicleLogger = loggerFactory?.CreateLogger<DynamicDataGridView<Vehicle>>();
                    _vehicleDataGrid = new DynamicDataGridView<Vehicle>(_vehicleConfig, vehicleLogger);
                    _vehicleDataGrid.StatusUpdated += OnDataViewStatusUpdated;
                    _vehicleDataGrid.EntitySelected += OnVehicleSelected;
                    _vehicleDataGrid.Dock = DockStyle.Fill;
                    _vehicleDataGrid.Visible = false; // Hidden by default
                }

                LogDebug("Data grids initialized successfully");
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize data grids", ex);
            }
        }

        private void InitializeStatisticsTimer()
        {
            _statisticsUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 30000 // Update every 30 seconds
            };
            _statisticsUpdateTimer.Tick += async (sender, e) => await UpdateStatisticsAsync();
            _statisticsUpdateTimer.Start();        }

        private async void OnDashboardLoad(object? sender, EventArgs e)
        {
            try
            {
                LogDebug("Dashboard Load event triggered - initializing default view");

                // Small delay to ensure UI is fully loaded
                await Task.Delay(100);

                // Load default view (Routes)
                await SwitchToViewAsync("Routes");
                LogDebug("Routes view initialization completed");
            }
            catch (Exception ex)
            {
                LogError("Error during dashboard load initialization", ex);
            }
        }

        private void OnDashboardResize(object? sender, EventArgs e)
        {
            // This method will be called whenever the dashboard is resized
            // Refresh layout calculations for each section
            RefreshSectionLayouts();
        }

        private void RefreshSectionLayouts()
        {
            // Force all sections to recalculate their layouts
            foreach (Control control in _mainLayout.Controls)
            {
                control.PerformLayout();

                // If this is a container, refresh its child controls as well
                foreach (Control child in control.Controls)
                {
                    child.PerformLayout();
                }
            }
        }

        private void CreateSidePanel()
        {
            _sidePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Padding = new Padding(15),
                Tag = "SidePanelContainer",
                AutoScroll = true
            };

            // Apply glassmorphism styling to side panel
            ThemeManager.CurrentTheme.StyleModernCard(_sidePanel);

            // Navigation items matching the main Dashboard navigation
            var navItems = new[]
            {
                ("🚌", "Routes"),
                ("👥", "Drivers"),
                ("🚗", "Vehicles"),
                ("📊", "Reports"),
                ("⚙️", "Settings")
            };

            int yPos = 25;
            foreach (var (icon, title) in navItems)
            {
                var navButton = CreateNavigationButton(icon, title);
                navButton.Location = new Point(15, yPos);
                _sidePanel.Controls.Add(navButton);
                _navigationButtons[title] = navButton;
                yPos += 55;
            }

            // Track component creation for performance metrics
            TrackComponentCreation(_sidePanel);
            LogDebug("Side panel created with navigation buttons for Routes, Drivers, Vehicles, Reports, and Settings");
        }

        private void CreateStatsPanel()
        {
            _statsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Padding = new Padding(15),
                Tag = "StatsPanelContainer"
            };

            // Apply glassmorphism styling to stats panel
            ThemeManager.CurrentTheme.StyleModernCard(_statsPanel);

            var statsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 3,
                BackColor = Color.Transparent
            };

            // Configure row styles
            statsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Title
            statsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Stats content

            // Configure column styles - equal distribution
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            // Title for stats section
            var statsTitle = new Label
            {
                Text = "Quick Statistics",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 5)
            };

            // Updated placeholders for miles and fuel data
            var milesDrivenMonthLabel = CreateStatLabel("Miles Driven This Month", "� 0 mi");
            var fuelUsedLabel = CreateStatLabel("Fuel Used", "⛽ 0 gal");
            var totalMilesDrivenLabel = CreateStatLabel("Miles Driven", "🚗 0 mi");

            // Add controls to layout
            statsLayout.Controls.Add(statsTitle, 0, 0);
            statsLayout.SetColumnSpan(statsTitle, 3); // Title spans all columns

            statsLayout.Controls.Add(milesDrivenMonthLabel, 0, 1);
            statsLayout.Controls.Add(fuelUsedLabel, 1, 1);
            statsLayout.Controls.Add(totalMilesDrivenLabel, 2, 1);

            _statsPanel.Controls.Add(statsLayout);            // Track component creation for performance metrics
            TrackComponentCreation(_statsPanel);
            LogDebug("Stats panel positioned at bottom spanning full width with compact 80px height for small statistic boxes");
        }        private Label CreateStatLabel(string title, string value)
        {
            var statLabel = new Label
            {
                Text = $"{title}\n{value}",
                Font = new Font("Segoe UI", 12F),
                ForeColor = ThemeManager.CurrentTheme.PrimaryText,
                BackColor = Color.FromArgb(30, ThemeManager.CurrentTheme.CardText),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(5),
                Padding = new Padding(10),
                AutoSize = false
            };

            // Store label reference for updates
            _statisticsLabels[title] = statLabel;

            // Track component creation for performance metrics
            TrackComponentCreation(statLabel);

            return statLabel;
        }

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                LogDebug("Updating statistics data");
                var updateStopwatch = Stopwatch.StartNew();

                // Get services for data access
                var routeService = _serviceProvider.GetService<IRouteService>();
                var driverService = _serviceProvider.GetService<IDriverService>();
                var vehicleService = _serviceProvider.GetService<IVehicleService>();                // Update statistics labels with real data
                if (routeService != null && _statisticsLabels.TryGetValue("Miles Driven This Month", out var milesLabel))
                {
                    try
                    {
                        var routes = await routeService.GetRoutesAsync();
                        var totalMiles = routes.Sum(r => r.TotalMiles);
                        milesLabel.Text = $"Miles Driven This Month\n🚗 {totalMiles:N0} mi";
                    }
                    catch (Exception ex)
                    {
                        LogError("Error updating route statistics", ex);
                    }
                }                if (vehicleService != null && _statisticsLabels.TryGetValue("Fuel Used", out var fuelLabel))
                {
                    try
                    {
                        var vehicles = await vehicleService.GetAllVehiclesAsync();
                        var activeVehicles = vehicles.Count(v => v.IsActive);
                        fuelLabel.Text = $"Active Vehicles\n⛽ {activeVehicles:N0}";
                    }
                    catch (Exception ex)
                    {
                        LogError("Error updating vehicle statistics", ex);
                    }
                }                if (driverService != null && _statisticsLabels.TryGetValue("Miles Driven", out var driversLabel))
                {
                    try
                    {
                        var drivers = await driverService.GetAllDriversAsync();
                        var totalDrivers = drivers.Count;
                        driversLabel.Text = $"Total Drivers\n👥 {totalDrivers:N0}";
                    }
                    catch (Exception ex)
                    {
                        LogError("Error updating driver statistics", ex);
                    }
                }

                updateStopwatch.Stop();
                LogPerformance("Statistics Update", updateStopwatch.ElapsedMilliseconds, _statisticsLabels.Count);
            }
            catch (Exception ex)
            {
                LogError("Error in UpdateStatisticsAsync", ex);
            }        }private void CreateContentPanel()
        {
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Padding = new Padding(10),
                Tag = "ContentPanelContainer"
            };

            // Apply glassmorphism styling to content panel
            ThemeManager.CurrentTheme.StyleModernCard(_contentPanel);

            // Add all data grids to the content panel (they will be hidden/shown as needed)
            AddDataGridsToContentPanel();

            TrackComponentCreation(_contentPanel);
            LogDebug("Content panel created as container for dynamic data views");
        }        private void AddDataGridsToContentPanel()
        {
            // Add all grids to the content panel
            // They are created in InitializeDataGrids() and will be shown/hidden as needed
            if (_routeDataGrid != null)
            {
                _contentPanel.Controls.Add(_routeDataGrid);
                LogDebug($"Route data grid added to content panel. Grid visible: {_routeDataGrid.Visible}");
            }
            if (_driverDataGrid != null)
            {
                _contentPanel.Controls.Add(_driverDataGrid);
                LogDebug($"Driver data grid added to content panel. Grid visible: {_driverDataGrid.Visible}");
            }
            if (_vehicleDataGrid != null)
            {
                _contentPanel.Controls.Add(_vehicleDataGrid);
                LogDebug($"Vehicle data grid added to content panel. Grid visible: {_vehicleDataGrid.Visible}");
            }

            LogDebug($"Content panel now has {_contentPanel.Controls.Count} controls");
        }private async Task SwitchToViewAsync(string viewType)
        {
            try
            {
                LogDebug($"Switching to view: {viewType}");

                // Ensure we're on the UI thread for all UI operations
                if (InvokeRequired)
                {
                    BeginInvoke(async () => await SwitchToViewAsync(viewType));
                    return;
                }

                // Hide all current data grids
                HideAllDataGrids();

                // Update navigation button states
                UpdateNavigationButtonStates(viewType);

                // Show the appropriate data grid
                switch (viewType)
                {                    case "Routes":
                        if (_routeDataGrid != null)
                        {
                            _currentDataView = _routeDataGrid;
                            _routeDataGrid.Visible = true;
                            _routeDataGrid.BringToFront();
                            LogDebug($"Routes data grid made visible. Control count in content panel: {_contentPanel.Controls.Count}");
                            await _routeDataGrid.LoadDataAsync();
                            LogDebug("Routes data loaded successfully");
                        }
                        else
                        {
                            LogError("Route data grid is null", null);
                        }
                        break;

                    case "Drivers":
                        if (_driverDataGrid != null)
                        {
                            _currentDataView = _driverDataGrid;
                            _driverDataGrid.Visible = true;
                            _driverDataGrid.BringToFront();
                            await _driverDataGrid.LoadDataAsync();
                        }
                        break;

                    case "Vehicles":
                        if (_vehicleDataGrid != null)
                        {
                            _currentDataView = _vehicleDataGrid;
                            _vehicleDataGrid.Visible = true;
                            _vehicleDataGrid.BringToFront();
                            await _vehicleDataGrid.LoadDataAsync();
                        }
                        break;

                    default:
                        // Show placeholder for Reports and Settings
                        var placeholderLabel = new Label
                        {
                            Text = $"{viewType} - Coming Soon",
                            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                            ForeColor = ThemeManager.CurrentTheme.CardText,
                            AutoSize = true,
                            Location = new Point(50, 50),
                            BackColor = Color.Transparent
                        };
                        _contentPanel.Controls.Add(placeholderLabel);
                        _currentDataView = placeholderLabel;
                        break;
                }

                _currentViewType = viewType;
                UpdateCrudButtonStates();

                LogInformation($"Successfully switched to {viewType} view");
            }
            catch (Exception ex)
            {
                LogError($"Failed to switch to {viewType} view", ex);
            }
        }

        private void HideAllDataGrids()
        {
            // Hide all data grids
            if (_routeDataGrid != null) _routeDataGrid.Visible = false;
            if (_driverDataGrid != null) _driverDataGrid.Visible = false;
            if (_vehicleDataGrid != null) _vehicleDataGrid.Visible = false;

            // Clear any placeholder content
            var placeholders = _contentPanel.Controls.OfType<Label>().ToList();
            foreach (var placeholder in placeholders)
            {
                _contentPanel.Controls.Remove(placeholder);
                placeholder.Dispose();
            }
        }

        private void UpdateNavigationButtonStates(string activeView)
        {
            foreach (var (viewType, button) in _navigationButtons)
            {
                if (viewType == activeView)
                {
                    button.BackColor = ThemeManager.CurrentTheme.PrimaryAccent;
                    button.ForeColor = Color.White;
                }
                else
                {
                    button.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                    button.ForeColor = ThemeManager.CurrentTheme.ButtonText;
                }
            }
        }

        private void UpdateCrudButtonStates()
        {
            bool hasDataView = _currentDataView is DynamicDataGridView<Route> || _currentDataView is DynamicDataGridView<Driver> || _currentDataView is DynamicDataGridView<Vehicle>;
            bool hasSelection = false;

            if (_currentDataView is DynamicDataGridView<Route> routeGrid)
                hasSelection = routeGrid.HasSelection;
            else if (_currentDataView is DynamicDataGridView<Driver> driverGrid)
                hasSelection = driverGrid.HasSelection;
            else if (_currentDataView is DynamicDataGridView<Vehicle> vehicleGrid)
                hasSelection = vehicleGrid.HasSelection;

            // Enable/disable CRUD buttons based on current view and selection
            if (_crudButtons.TryGetValue("Create", out var createBtn))
                createBtn.Enabled = hasDataView;
            if (_crudButtons.TryGetValue("Read", out var readBtn))
                readBtn.Enabled = hasDataView;
            if (_crudButtons.TryGetValue("Update", out var updateBtn))
                updateBtn.Enabled = hasDataView && hasSelection;
            if (_crudButtons.TryGetValue("Delete", out var deleteBtn))
                deleteBtn.Enabled = hasDataView && hasSelection;        }

        // Event handlers for data view events
        private void OnDataViewStatusUpdated(object? sender, StatusEventArgs e)
        {            // Forward status to main dashboard
            UpdateStatus(e.Message, e.Type);
        }

        private void OnRouteSelected(object? sender, Route e)
        {
            UpdateCrudButtonStates();
            LogDebug($"Route selected: {e.Name}");
        }

        private void OnDriverSelected(object? sender, Driver e)
        {
            UpdateCrudButtonStates();
            LogDebug($"Driver selected: {e.FirstName} {e.LastName}");
        }

        private void OnVehicleSelected(object? sender, Vehicle e)
        {
            UpdateCrudButtonStates();
            LogDebug($"Vehicle selected: {e.Number}");
        }        private Button CreateNavigationButton(string icon, string title)
        {
            var button = new Button
            {
                Text = $"{icon} {title}",
                Size = new Size(200, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.ButtonText,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                Tag = title
            };            // Modern button styling
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ThemeManager.CurrentTheme.PrimaryAccent;

            // Click event handler
            button.Click += async (sender, e) => await SwitchToViewAsync(title);

            TrackComponentCreation(button);
            return button;
        }

        private void CreateCrudPanel()
        {
            _crudPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Padding = new Padding(15),
                Tag = "CrudPanelContainer"
            };

            // Apply glassmorphism styling to CRUD panel
            ThemeManager.CurrentTheme.StyleModernCard(_crudPanel);

            // Create a layout for CRUD operations
            var crudLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 4,
                BackColor = Color.Transparent
            };

            // Configure row styles
            crudLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Title
            crudLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Buttons

            // Configure column styles - equal distribution for CRUD buttons
            crudLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            crudLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            crudLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            crudLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            // Title for CRUD section
            var crudTitle = new Label
            {
                Text = "Data Operations",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // CRUD operation buttons
            var createButton = CreateCrudButton("➕ Create", "Add new record");
            var readButton = CreateCrudButton("👁️ Read", "Refresh data");
            var updateButton = CreateCrudButton("✏️ Update", "Edit existing record");
            var deleteButton = CreateCrudButton("🗑️ Delete", "Remove record");

            // Store references for state management
            _crudButtons["Create"] = createButton;
            _crudButtons["Read"] = readButton;
            _crudButtons["Update"] = updateButton;
            _crudButtons["Delete"] = deleteButton;

            // Add click handlers
            createButton.Click += async (sender, e) => await HandleCrudOperationAsync("Create");
            readButton.Click += async (sender, e) => await HandleCrudOperationAsync("Read");
            updateButton.Click += async (sender, e) => await HandleCrudOperationAsync("Update");
            deleteButton.Click += async (sender, e) => await HandleCrudOperationAsync("Delete");

            // Add controls to layout
            crudLayout.Controls.Add(crudTitle, 0, 0);
            crudLayout.SetColumnSpan(crudTitle, 4); // Title spans all columns

            crudLayout.Controls.Add(createButton, 0, 1);
            crudLayout.Controls.Add(readButton, 1, 1);
            crudLayout.Controls.Add(updateButton, 2, 1);
            crudLayout.Controls.Add(deleteButton, 3, 1);

            _crudPanel.Controls.Add(crudLayout);

            // Track component creation for performance metrics
            TrackComponentCreation(_crudPanel);
            LogDebug("CRUD panel created with Create, Read, Update, Delete operations positioned above statistics panel");
        }

        private Button CreateCrudButton(string text, string tooltip)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.ButtonText,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(5),
                Enabled = false // Initially disabled until a view is selected
            };

            // Set tooltip using ToolTip control
            var toolTip = new ToolTip();
            toolTip.SetToolTip(button, tooltip);// Modern button styling
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.PrimaryAccent;
            button.FlatAppearance.MouseOverBackColor = ThemeManager.CurrentTheme.PrimaryAccent;

            TrackComponentCreation(button);
            return button;
        }

        private async Task HandleCrudOperationAsync(string operation)
        {
            try
            {
                LogDebug($"Handling CRUD operation: {operation} for view: {_currentViewType}");

                switch (operation)
                {
                    case "Create":
                        await HandleCreateOperationAsync();
                        break;
                    case "Read":
                        await HandleReadOperationAsync();
                        break;
                    case "Update":
                        await HandleUpdateOperationAsync();
                        break;
                    case "Delete":
                        await HandleDeleteOperationAsync();
                        break;
                }
            }
            catch (Exception ex)
            {                LogError($"Error handling {operation} operation", ex);
                UpdateStatus($"Error performing {operation}: {ex.Message}");
            }
        }

        private async Task HandleCreateOperationAsync()
        {
            if (_currentDataView is DynamicDataGridView<Route> routeGrid)
            {
                var newRoute = _routeConfig!.CreateNewEntity();
                await routeGrid.CreateEntityAsync(newRoute);
            }
            else if (_currentDataView is DynamicDataGridView<Driver> driverGrid)
            {
                var newDriver = _driverConfig!.CreateNewEntity();
                await driverGrid.CreateEntityAsync(newDriver);
            }
            else if (_currentDataView is DynamicDataGridView<Vehicle> vehicleGrid)
            {
                var newVehicle = _vehicleConfig!.CreateNewEntity();
                await vehicleGrid.CreateEntityAsync(newVehicle);
            }
        }

        private async Task HandleReadOperationAsync()
        {
            if (_currentDataView is DynamicDataGridView<Route> routeGrid)
            {
                await routeGrid.LoadDataAsync();
            }
            else if (_currentDataView is DynamicDataGridView<Driver> driverGrid)
            {
                await driverGrid.LoadDataAsync();
            }
            else if (_currentDataView is DynamicDataGridView<Vehicle> vehicleGrid)
            {
                await vehicleGrid.LoadDataAsync();
            }
        }

        private async Task HandleUpdateOperationAsync()
        {            // For update operations, the user can edit directly in the grid
            // This method could be extended to open a dedicated edit dialog
            UpdateStatus("Click on a cell to edit values directly, or double-click a row for detailed editing");
            await Task.CompletedTask;
        }

        private async Task HandleDeleteOperationAsync()
        {
            var confirmResult = MessageBox.Show(
                $"Are you sure you want to delete the selected {_currentViewType.TrimEnd('s').ToLower()}?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult == DialogResult.Yes)
            {
                if (_currentDataView is DynamicDataGridView<Route> routeGrid)
                {
                    await routeGrid.DeleteSelectedEntityAsync();
                }
                else if (_currentDataView is DynamicDataGridView<Driver> driverGrid)
                {
                    await driverGrid.DeleteSelectedEntityAsync();
                }
                else if (_currentDataView is DynamicDataGridView<Vehicle> vehicleGrid)
                {
                    await vehicleGrid.DeleteSelectedEntityAsync();
                }
            }        }

        // ...existing code...
        // Use the base class method instead to ensure proper event raising
        private new void UpdateStatus(string message, StatusType statusType = StatusType.Info)
        {
            // Call the base implementation which will raise the StatusUpdated event
            base.UpdateStatus(message, statusType);
        }
        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // Update the status to show loading
            UpdateStatus("Dashboard view activated", StatusType.Info);

            // Additional activation logic would go here
            // This is where we would load initial data, if needed

            await Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync()
        {
            UpdateStatus("Dashboard view deactivated", StatusType.Info);
            return Task.CompletedTask;
        }        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up timer
                if (_statisticsUpdateTimer != null)
                {
                    _statisticsUpdateTimer.Stop();
                    _statisticsUpdateTimer.Dispose();
                    _statisticsUpdateTimer = null;
                    LogDebug("Statistics update timer disposed");
                }

                // Clean up data grid components
                _routeDataGrid?.Dispose();
                _driverDataGrid?.Dispose();
                _vehicleDataGrid?.Dispose();
                _currentDataView?.Dispose();

                // Clear collections
                _navigationButtons.Clear();
                _crudButtons.Clear();
                _statisticsLabels.Clear();
                _timingMetrics.Clear();

                LogInformation("DashboardView resources cleaned up successfully");

                // Ensure application shutdown when the dashboard view is closed
                if (Application.OpenForms.Count > 0)
                {
                    LogInformation("DashboardView is being disposed, initiating application shutdown");

                    // Use BeginInvoke to safely exit from the UI thread
                    if (this.InvokeRequired)
                    {
                        try
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    Application.Exit();
                                }
                                catch (Exception ex)
                                {
                                    LogError("Error during Application.Exit() call", ex);
                                    Environment.Exit(0);
                                }
                            }));
                        }
                        catch (Exception ex)
                        {
                            LogError("Error during BeginInvoke for application shutdown", ex);
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        try
                        {
                            Application.Exit();
                        }
                        catch (Exception ex)
                        {
                            LogError("Error during application shutdown from DashboardView", ex);
                            Environment.Exit(0);
                        }
                    }
                }
            }

            base.Dispose(disposing);
        }
    }
}
