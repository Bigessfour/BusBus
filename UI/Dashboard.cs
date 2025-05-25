using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using BusBus.UI.Common;

namespace BusBus.UI
{
    public partial class Dashboard : Form, IDisplayable
    {        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Panel _headerPanel, _sidebarPanel, _mainContentPanel, _footerPanel;
        private Label _titleLabel, _statusLabel;
        private Button _themeToggleButton, _refreshButton, _routesButton, _driversButton, _vehiclesButton;
        private RouteListPanel _routeListPanel;
        private DriverListPanel _driverListPanel;
        private Control? _currentPanel;

        public Dashboard(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            InitializeComponent();
            InitializeUI();
            LoadRoutes();

            this.FormClosing += Dashboard_FormClosing;
        }        private void Dashboard_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Disposal is already handled in the Dispose method
            // No additional cleanup needed here to prevent double-disposal
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1200, 800);
            this.MinimumSize = new Size(800, 600);
            this.Name = "Dashboard";
            this.Text = "BusBus - Management Dashboard";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.ResumeLayout(false);
        }

        private void InitializeUI()
        {
            // Header Panel
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ThemeManager.CurrentTheme.SidePanelBackground
            };

            _titleLabel = new Label
            {
                Text = "BusBus Management Dashboard",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                Dock = DockStyle.Left,
                AutoSize = true,
                Padding = new Padding(10)
            };

            var headerButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(5)
            };            _themeToggleButton = CreateButton("Toggle Theme", ThemeToggleButton_Click, 120, 30);
            _refreshButton = CreateButton("Refresh", RefreshButton_Click, 80, 30);

            headerButtonPanel.Controls.AddRange(new Control[] { _refreshButton, _themeToggleButton });
            _headerPanel.Controls.AddRange(new Control[] { _titleLabel, headerButtonPanel });

            // Sidebar Panel
            _sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = ThemeManager.CurrentTheme.SidePanelBackground
            };

            var sidebarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            _routesButton = CreateButton("Routes", RoutesButton_Click, 180, 30);
            _driversButton = CreateButton("Drivers", DriversButton_Click, 180, 30);
            _vehiclesButton = CreateButton("Vehicles", VehiclesButton_Click, 180, 30);

            sidebarLayout.Controls.Add(_routesButton, 0, 0);
            sidebarLayout.Controls.Add(_driversButton, 0, 1);
            sidebarLayout.Controls.Add(_vehiclesButton, 0, 2);
            _sidebarPanel.Controls.Add(sidebarLayout);

            // Main Content Panel
            _mainContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = ThemeManager.CurrentTheme.MainBackground
            };

            // Footer Panel
            _footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = ThemeManager.CurrentTheme.SidePanelBackground,
                Padding = new Padding(10, 5, 10, 5)
            };

            _statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Left,
                AutoSize = true,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _footerPanel.Controls.Add(_statusLabel);

            // Add Panels to Form
            this.Controls.AddRange(new[] { _mainContentPanel, _sidebarPanel, _headerPanel, _footerPanel });

            // Initialize Route List Panel
            var routeService = _serviceProvider.GetRequiredService<IRouteService>();
            _routeListPanel = new RouteListPanel(routeService)
            {
                Dock = DockStyle.Fill
            };
            _routeListPanel.RouteEditRequested += RouteListPanel_RouteSelected;
            _currentPanel = _routeListPanel;
            _mainContentPanel.Controls.Add(_routeListPanel);

            RefreshTheme();
        }

        private static Button CreateButton(string text, EventHandler clickHandler, int width, int height)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(width, height),
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.ButtonFont,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = ThemeManager.CurrentTheme.BorderColor, BorderSize = 1 }
            };
            button.Click += clickHandler;
            return button;
        }

        private async void LoadRoutes()
        {
            try
            {
                UpdateStatus("Loading routes...");
                await _routeListPanel.LoadRoutesAsync(1, 20, _cancellationTokenSource.Token);
                UpdateStatus("Routes loaded successfully");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading routes: {ex.Message}");
                MessageBox.Show($"Failed to load routes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void RouteListPanel_RouteSelected(object? sender, RouteEventArgs e)
        {
            try
            {
                UpdateStatus($"Opening route: {e.Route.Name}");                await WithScopedServiceAsync<IRouteService>(routeService =>
                {
                    var routePanel = new RouteModalPanel(routeService, RouteDisplayDTO.FromRoute(e.Route));
                    routePanel.ShowDialog(this);
                    if (routePanel.IsSaved)
                    {
                        LoadRoutes();
                        UpdateStatus("Route updated successfully");
                    }
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error opening route: {ex.Message}");
                MessageBox.Show($"Failed to open route: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RoutesButton_Click(object? sender, EventArgs e)
        {
            SwitchPanel(_routeListPanel);
            LoadRoutes();
            UpdateStatus("Showing routes");
        }

        private void DriversButton_Click(object? sender, EventArgs e)
        {
            if (_driverListPanel == null)
            {
                var driverService = _serviceProvider.GetRequiredService<IDriverService>();
                _driverListPanel = new DriverListPanel(driverService)
                {
                    Dock = DockStyle.Fill
                };
                _driverListPanel.DriverEditRequested += DriverListPanel_DriverSelected;
            }
            SwitchPanel(_driverListPanel);
            LoadDrivers();
            UpdateStatus("Showing drivers");
        }

        private async void DriverListPanel_DriverSelected(object? sender, EntityEventArgs<Driver> e)
        {
            try
            {
                UpdateStatus($"Opening driver: {e.Entity.FirstName} {e.Entity.LastName}");                await WithScopedServiceAsync<IDriverService>(driverService =>
                {
                    var driverPanel = new DriverPanel(driverService, e.Entity);
                    driverPanel.ShowDialog(this);
                    if (driverPanel.IsSaved)
                    {
                        LoadDrivers();
                        UpdateStatus("Driver updated successfully");
                    }
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error opening driver: {ex.Message}");
                MessageBox.Show($"Failed to open driver: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LoadDrivers()
        {
            if (_driverListPanel != null)
            {
                try
                {
                    UpdateStatus("Loading drivers...");
                    await _driverListPanel.LoadDriversAsync();
                    UpdateStatus("Drivers loaded successfully");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error loading drivers: {ex.Message}");
                    MessageBox.Show($"Failed to load drivers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void VehiclesButton_Click(object? sender, EventArgs e)
        {
            SwitchPanel(null);
            UpdateStatus("Vehicles view not implemented yet");
            // TODO: Implement VehicleListPanel similar to DriverListPanel
        }        private void SwitchPanel(Control? newPanel)
        {
            _mainContentPanel.Controls.Clear();
            if (newPanel != null)
            {
                _currentPanel = newPanel;
                _mainContentPanel.Controls.Add(newPanel);
                if (newPanel is IDisplayable displayable)
                {
                    displayable.RefreshTheme();
                }
            }
        }

        private void RefreshButton_Click(object? sender, EventArgs e)
        {
            if (_currentPanel == _routeListPanel)
                LoadRoutes();
            else if (_currentPanel == _driverListPanel)
                LoadDrivers();
            else
                UpdateStatus("Refresh not applicable");
        }

        private void ThemeToggleButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var currentTheme = ThemeManager.CurrentTheme.Name;
                ThemeManager.SwitchTheme(currentTheme == "Dark" ? "Light" : "Dark");
                RefreshTheme();
                UpdateStatus("Theme switched successfully");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error switching theme: {ex.Message}");
                MessageBox.Show($"Failed to switch theme: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RefreshTheme()
        {
            var theme = ThemeManager.CurrentTheme;
            this.BackColor = theme.MainBackground;
            this.ForeColor = theme.CardText;

            _headerPanel.BackColor = theme.SidePanelBackground;
            _titleLabel.ForeColor = theme.HeadlineText;
            _sidebarPanel.BackColor = theme.SidePanelBackground;
            _mainContentPanel.BackColor = theme.MainBackground;
            _footerPanel.BackColor = theme.SidePanelBackground;
            _statusLabel.ForeColor = theme.CardText;
            _statusLabel.Font = theme.CardFont;

            foreach (var button in new[] { _themeToggleButton, _refreshButton, _routesButton, _driversButton, _vehiclesButton })
            {
                if (button != null)
                {
                    button.BackColor = theme.ButtonBackground;
                    button.ForeColor = theme.CardText;
                    button.Font = theme.ButtonFont;
                    button.FlatAppearance.BorderColor = theme.BorderColor;
                }
            }

            _routeListPanel?.RefreshTheme();
            _driverListPanel?.RefreshTheme();
            this.Invalidate(true);
        }

        private void UpdateStatus(string message)
        {
            if (_statusLabel != null && !_statusLabel.IsDisposed)
            {
                if (_statusLabel.InvokeRequired)
                    _statusLabel.Invoke(new Action(() => _statusLabel.Text = message));
                else
                    _statusLabel.Text = message;
            }
        }

        public void Render(Control container)
        {
            if (container != null)
            {
                this.TopLevel = false;
                this.Parent = container;
                this.Dock = DockStyle.Fill;
                this.Show();
            }
            else
            {
                this.Show();
            }
        }

        private async Task WithScopedServiceAsync<TService>(Func<TService, Task> action) where TService : notnull
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            await action(service);
        }        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }
                _cancellationTokenSource = null!;
                _routeListPanel?.Dispose();
                _driverListPanel?.Dispose();
                _mainContentPanel?.Dispose();
                _headerPanel?.Dispose();
                _sidebarPanel?.Dispose();
                _footerPanel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
