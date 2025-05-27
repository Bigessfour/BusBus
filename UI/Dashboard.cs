#nullable enable
// <auto-added>
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CA1848 // Use LoggerMessage delegates for logging performance
#pragma warning disable CA2254 // LoggerMessage delegates for logging performance
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Suppressed because fields are initialized in SetupLayout.
using BusBus.Services;
using BusBus.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusBus.UI
{
    /// <summary>
    /// Main application hub that manages navigation, state, and view lifecycle
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

        private TableLayoutPanel _mainLayout;
        private Panel _sidePanel;
        private Panel _contentPanel;
        private Panel _headerPanel;
        private Panel _footerPanel;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripProgressBar _progressBar;

        private IView? _currentView;
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

            _logger.LogDebug("[DEBUG] Dashboard constructor called. serviceProvider: {ServiceProvider}, routeService: {RouteService}, logger: {Logger}", serviceProvider, routeService, logger);

            InitializeComponent();
            SetupLayout();
            RegisterViews();
            SubscribeToEvents();

            _logger.LogInformation("Dashboard initialized successfully");
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
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create main table layout
            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // Configure layout proportions
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Header
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // Status

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

            this.Controls.Add(_mainLayout);

            // Apply theme
            ThemeManager.ApplyTheme(this, ThemeManager.CurrentTheme);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CreateHeaderPanel()
        {
            _headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Tag = "HeaderPanel",
                Height = 60
            };

            var titleLabel = new Label
            {
                Text = "BusBus Transport Management",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                Location = new Point(20, 15),
                AutoSize = true
            };

            var userInfoLabel = new Label
            {
                Text = $"Welcome, {Environment.UserName}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = ThemeManager.CurrentTheme.SecondaryText,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_headerPanel.Width - 200, 20),
                AutoSize = true
            };

            var themeToggle = new Button
            {
                Text = "🌙",
                Font = new Font("Segoe UI", 12F),
                Size = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_headerPanel.Width - 50, 10),
                Tag = "ThemeToggle"
            };

            themeToggle.Click += (s, e) => ToggleTheme();

            _headerPanel.Controls.AddRange(new Control[] { titleLabel, userInfoLabel, themeToggle });
        }

        private void CreateSidePanel()
        {
            _sidePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Tag = "SidePanel",
                AutoScroll = true
            };

            var navItems = new[]
            {
                new NavigationItem("🏠", "Dashboard", "dashboard", true),
                new NavigationItem("🚌", "Routes", "routes"),
                new NavigationItem("👥", "Drivers", "drivers"),
                new NavigationItem("🚗", "Vehicles", "vehicles"),
                new NavigationItem("📊", "Reports", "reports"),
                new NavigationItem("⚙️", "Settings", "settings")
            };

            int yPos = 20;
            foreach (var item in navItems)
            {
                var navButton = CreateNavigationButton(item);
                navButton.Location = new Point(10, yPos);
                _sidePanel.Controls.Add(navButton);
                yPos += 50;
            }
        }

        private Button CreateNavigationButton(NavigationItem item)
        {
            var button = new Button
            {
                Text = $"{item.Icon} {item.Text}",
                Size = new Size(230, 40),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F),
                Tag = item.ViewName,
                Padding = new Padding(10, 0, 0, 0)
            };

            button.FlatAppearance.BorderSize = 0;
            button.Click += async (s, e) => await NavigateToAsync(item.ViewName);

            if (item.IsDefault)
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
                Padding = new Padding(20)
            };
        }

        private void CreateStatusBar()
        {
            _statusStrip = new StatusStrip
            {
                Tag = "StatusBar"
            };

            _statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _progressBar = new ToolStripProgressBar
            {
                Visible = false,
                Style = ProgressBarStyle.Marquee
            };

            var connectionLabel = new ToolStripStatusLabel
            {
                Text = "Connected",
                ForeColor = Color.Green
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
        public async Task NavigateToAsync(string viewName)
        {
            if (viewName == null)
            {
                throw new ArgumentNullException(nameof(viewName));
            }

            try
            {
                _logger.LogInformation($"Navigating to view: {viewName}");
                // pragma disables above

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
                    // pragma disables above
                    ShowStatus($"View '{viewName}' not found", StatusType.Warning);
                    return;
                }

                // Load view
                _contentPanel.Controls.Clear();

                if (view.Control != null)
                {
                    view.Control.Dock = DockStyle.Fill;
                    _contentPanel.Controls.Add(view.Control);
                }

                // Activate view
                await view.ActivateAsync(_cancellationTokenSource.Token);
                _currentView = view;

                // Update UI
                UpdateNavigationButtons(viewName);
                UpdateTitle(view.Title);

                HideProgress();
                ShowStatus($"{view.Title} loaded", StatusType.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error navigating to {viewName}");
                // pragma disables above
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

        private IView? GetOrCreateView(string viewName)
        {
            if (_viewCache.TryGetValue(viewName, out var cachedView))
            {
                return cachedView;
            }

            IView? view = viewName.ToLower() switch
            {
                "dashboard" => new DashboardView(_serviceProvider),
                "routes" => new RouteListView(_routeService),
                "drivers" => new DriverListView(_serviceProvider),
                "vehicles" => new VehicleListView(_serviceProvider),
                "reports" => new ReportsView(_serviceProvider),
                "settings" => new SettingsView(_serviceProvider),
                _ => null
            };

            if (view != null)
            {
                _viewCache[viewName] = view;
                view.NavigationRequested += OnViewNavigationRequested;
                view.StatusUpdated += OnViewStatusUpdated;
            }

            return view;
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
        private void RegisterViews()
        {
            // Pre-register critical views for faster initial load
            _viewCache["dashboard"] = new DashboardView(_serviceProvider);
            _viewCache["routes"] = new RouteListView(_routeService);
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
            await NavigateToAsync(_state.LastView ?? "dashboard");
        }

        private async void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveState();

            // Cleanup
            _cancellationTokenSource.Cancel();

            // Deactivate current view
            if (_currentView != null)
            {
                await _currentView.DeactivateAsync();
            }

            // Dispose all cached views
            foreach (var view in _viewCache.Values)
            {
                view.Dispose();
            }

            _cancellationTokenSource.Dispose();
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

        public void ShowNotification(string message, NotificationType type)
        {
            ShowStatus(message, type switch
            {
                NotificationType.Success => StatusType.Success,
                NotificationType.Warning => StatusType.Warning,
                NotificationType.Error => StatusType.Error,
                _ => StatusType.Info
            });
        }

        public async Task<bool> ShowConfirmationAsync(string message, string title)
        {
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return await Task.FromResult(result == DialogResult.Yes);
        }

        public void ShowBusyIndicator(string message)
        {
            ShowProgress(message);
        }

        public void HideBusyIndicator()
        {
            HideProgress();
        }
        #endregion

        #region Helper Classes
        private class NavigationItem
        {
            public string Icon { get; }
            public string Text { get; }
            public string ViewName { get; }
            public bool IsDefault { get; }

            public NavigationItem(string icon, string text, string viewName, bool isDefault = false)
            {
                Icon = icon;
                Text = text;
                ViewName = viewName;
                IsDefault = isDefault;
            }
        }

        private class DashboardState
        {
            public string? CurrentTheme { get; set; }
            public string? LastView { get; set; }
            public Dictionary<string, object> ViewStates { get; } = new();
        }
        #endregion

        private void InitializeComponent()
        {
            // Initialize layout panels
            this._mainLayout = new TableLayoutPanel();
            this._sidePanel = new Panel();
            this._contentPanel = new Panel();
            this._headerPanel = new Panel();
            this._footerPanel = new Panel();
            this._statusStrip = new StatusStrip();
            this._statusLabel = new ToolStripStatusLabel();
            this._progressBar = new ToolStripProgressBar();

            // Configure main layout
            this._mainLayout.Dock = DockStyle.Fill;
            this._mainLayout.ColumnCount = 2;
            this._mainLayout.RowCount = 3;

            // Add columns and rows
            this._mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            this._mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            this._mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            this._mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            // Configure panels
            this._headerPanel.Dock = DockStyle.Fill;
            this._headerPanel.BackColor = System.Drawing.Color.FromArgb(50, 100, 180);

            this._sidePanel.Dock = DockStyle.Fill;
            this._sidePanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);

            this._contentPanel.Dock = DockStyle.Fill;
            this._contentPanel.BackColor = System.Drawing.Color.White;

            this._footerPanel.Dock = DockStyle.Fill;
            this._footerPanel.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);

            // Configure status strip
            this._statusStrip.Items.Add(this._statusLabel);
            this._statusStrip.Items.Add(this._progressBar);
            this._progressBar.Visible = false;

            // Add controls to layout
            this._mainLayout.Controls.Add(this._headerPanel, 0, 0);
            this._mainLayout.SetColumnSpan(this._headerPanel, 2);

            this._mainLayout.Controls.Add(this._sidePanel, 0, 1);
            this._mainLayout.Controls.Add(this._contentPanel, 1, 1);

            this._mainLayout.Controls.Add(this._statusStrip, 0, 2);
            this._mainLayout.SetColumnSpan(this._statusStrip, 2);

            // Configure form
            this.Controls.Add(this._mainLayout);
            this.Size = new System.Drawing.Size(1024, 768);
            this.Text = "BusBus Management System";
        }
    }

    #region Interfaces
    public interface IApplicationHub
    {
        IServiceProvider ServiceProvider { get; }
        Task NavigateToAsync(string viewName);
        Task NavigateBackAsync();
        void ShowNotification(string message, NotificationType type);
        Task<bool> ShowConfirmationAsync(string message, string title);
        void ShowBusyIndicator(string message);
        void HideBusyIndicator();
    }

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
