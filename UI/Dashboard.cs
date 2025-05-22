using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;

namespace BusBus.UI
{
    public class Dashboard : Form
    {
        private readonly IRouteService _routeService;
        private readonly Panel _sidePanel;
        private readonly Panel _mainPanel;
        private Button _settingsButton;
        private readonly Panel _settingsPanel;
        private readonly Panel _footerPanel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Panel _contentPanel;
        private IDisplayable? _currentView;

        public Dashboard(IRouteService routeService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _sidePanel = new Panel();
            _mainPanel = new Panel();
            _settingsButton = new Button();
            _settingsPanel = new Panel();
            _footerPanel = new Panel();
            _cancellationTokenSource = new CancellationTokenSource();
            _contentPanel = new Panel();
            _currentView = null;

            this.Size = new Size(UiConstants.FormWidth, UiConstants.FormHeight);
            this.MinimumSize = new Size(UiConstants.MinPanelWidth + 300, 300);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimizeBox = true;
            this.MaximizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.FormClosing += Dashboard_FormClosing;
            Console.WriteLine($"[Dashboard] Constructor - Form Width: {this.Width}, ClientSize Width: {this.ClientSize.Width}");



            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

            InitializeComponents();
        }

        // Expose a method for test to load routes into the grid (for test reflection)
        internal async Task LoadRoutesAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            // Find the RouteListPanel instance (if any) or create one for test
            RouteListPanel? routeListPanel = null;
            foreach (Control c in _mainPanel.Controls)
            {
                if (c is TableLayoutPanel table)
                {
                    foreach (Control inner in table.Controls)
                    {
                        if (inner is RouteListPanel rlp)
                        {
                            routeListPanel = rlp;
                            break;
                        }
                    }
                }
                if (routeListPanel != null) break;
            }
            if (routeListPanel == null)
            {
                routeListPanel = new RouteListPanel(_routeService);
                _mainPanel.Controls.Add(routeListPanel);
            }
            await routeListPanel.LoadRoutesAsync(page, pageSize, cancellationToken);
        }
        private void Dashboard_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing)
                return;

            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Console.WriteLine($"Error during application shutdown: {ex.Message}");
            }
        }

        private void InitializeComponents()
        {
            try
            {
                this.Text = "BusBus Dashboard";
                this.BackColor = ThemeManager.CurrentTheme.MainBackground;

                var mainTableLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = ThemeManager.CurrentTheme.MainBackground,
                    RowCount = 2,
                    ColumnCount = 2,
                    Padding = new Padding(0),
                    Margin = new Padding(0)
                };
                mainTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
                mainTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
                mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 95F));
                mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));

                _sidePanel.Dock = DockStyle.Fill;
                _sidePanel.BackColor = ThemeManager.CurrentTheme.SidePanelBackground;
                mainTableLayout.Controls.Add(_sidePanel, 0, 0);

                _mainPanel.Dock = DockStyle.Fill;
                _mainPanel.BackColor = ThemeManager.CurrentTheme.MainBackground;
                mainTableLayout.Controls.Add(_mainPanel, 1, 0);

                _footerPanel.Dock = DockStyle.Fill;
                _footerPanel.BackColor = ThemeManager.CurrentTheme.SidePanelBackground;
                mainTableLayout.SetColumnSpan(_footerPanel, 2);
                mainTableLayout.Controls.Add(_footerPanel, 0, 1);

                this.Controls.Add(mainTableLayout);

                SetupSidePanel(_sidePanel);
                SetupMainPanel(_mainPanel);
                SetupFooterPanel(_footerPanel);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Dashboard] UI initialization error: {ex}");
                var fallbackPanel = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.CurrentTheme.MainBackground };
                this.Controls.Add(fallbackPanel);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Console.WriteLine($"[Dashboard] Critical error: {ex}");
                var fallbackPanel = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.CurrentTheme.MainBackground };
                this.Controls.Add(fallbackPanel);
            }
        }

        private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
        {
            try
            {
                ThemeManager.RefreshTheme(this);

                if (_currentView != null)
                {
                    _contentPanel.Controls.Clear();
                    _currentView.Render(_contentPanel);
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Invalid operation when applying theme: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Invalid argument when applying theme: {ex.Message}");
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"Null reference when applying theme: {ex.Message}");
            }
        }

        private void SetupSidePanel(Panel sidePanel)
        {
            sidePanel.BackColor = ThemeManager.CurrentTheme.SidePanelBackground;
            sidePanel.BorderStyle = BorderStyle.FixedSingle;
            sidePanel.Padding = new Padding(1);

            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 300,
                BackColor = ThemeManager.CurrentTheme.SidePanelBackground,
                Margin = new Padding(0, 5, 0, 0),
                RowCount = 4,
                ColumnCount = 1
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            var routesButton = CreateSidebarButton("🚌 Routes", ThemeManager.CurrentTheme.ButtonFont);
            routesButton.Dock = DockStyle.Fill;
            routesButton.TextAlign = ContentAlignment.MiddleCenter;
            routesButton.ImageAlign = ContentAlignment.MiddleCenter;
            routesButton.Click += async (s, e) =>
            {
                _settingsPanel.Visible = false;
                try
                {
                    var routeListPanel = new RouteListPanel(_routeService);
                    LoadView(routeListPanel);
                    await routeListPanel.LoadRoutesAsync(1, 10, _cancellationTokenSource.Token);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    MessageBox.Show($"Error loading routes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            buttonPanel.Controls.Add(routesButton, 0, 0);

            var vehiclesButton = CreateSidebarButton("🚐 Vehicles", ThemeManager.CurrentTheme.ButtonFont);
            vehiclesButton.Dock = DockStyle.Fill;
            vehiclesButton.TextAlign = ContentAlignment.MiddleCenter;
            vehiclesButton.ImageAlign = ContentAlignment.MiddleCenter;
            vehiclesButton.Click += (s, e) =>
            {
                _settingsPanel.Visible = false;
                MessageBox.Show("Vehicles functionality will be implemented in a future update.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            buttonPanel.Controls.Add(vehiclesButton, 0, 1);

            var driversButton = CreateSidebarButton("👤 Drivers", ThemeManager.CurrentTheme.ButtonFont);
            driversButton.Dock = DockStyle.Fill;
            driversButton.TextAlign = ContentAlignment.MiddleCenter;
            driversButton.ImageAlign = ContentAlignment.MiddleCenter;
            driversButton.Click += (s, e) =>
            {
                _settingsPanel.Visible = false;
                MessageBox.Show("Drivers functionality will be implemented in a future update.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            buttonPanel.Controls.Add(driversButton, 0, 2);

            _settingsButton = CreateSidebarButton("⚙ Settings", ThemeManager.CurrentTheme.SmallButtonFont);
            _settingsButton.Dock = DockStyle.Fill;
            _settingsButton.TextAlign = ContentAlignment.MiddleCenter;
            _settingsButton.ImageAlign = ContentAlignment.MiddleCenter;
            _settingsButton.Click += (s, e) =>
            {
                _settingsPanel.Visible = !_settingsPanel.Visible;
                _contentPanel.Visible = !_settingsPanel.Visible;
            };
            buttonPanel.Controls.Add(_settingsButton, 0, 3);

            sidePanel.Controls.Add(buttonPanel);

            _settingsPanel.BackColor = ThemeManager.CurrentTheme.SidePanelBackground;
            _settingsPanel.Dock = DockStyle.Fill;
            _settingsPanel.Visible = false;
            var settingsLabel = new Label
            {
                Text = "Settings",
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 40
            };
            _settingsPanel.Controls.Add(settingsLabel);

            var themePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10)
            };

            var themeLabel = new Label
            {
                Text = "Theme:",
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            var themeToggle = new Button
            {
                Text = "Toggle Dark/Light",
                ForeColor = ThemeManager.CurrentTheme.CardText,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                FlatAppearance = { BorderSize = 1 }
            };

            themeToggle.Click += (s, e) =>
            {
                string newTheme = ThemeManager.CurrentTheme.GetType().GetField("Name")?.GetValue(ThemeManager.CurrentTheme)?.ToString() == "Dark"
                    ? "Light"
                    : "Dark";
                ThemeManager.SwitchTheme(newTheme);
            };

            themePanel.Controls.Add(themeLabel);
            themePanel.Controls.Add(themeToggle);
            _settingsPanel.Controls.Add(themePanel);

            sidePanel.Controls.Add(_settingsPanel);
        }

        private static Button CreateSidebarButton(string text, Font font)
        {
            return new Button
            {
                Text = text,
                Font = font,
                ForeColor = Color.Gainsboro,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                FlatStyle = FlatStyle.Flat,
                Width = 160,
                Height = 40,
                TabStop = false,
                Anchor = AnchorStyles.None,
                FlatAppearance = {
                    BorderSize = 1,
                    BorderColor = Color.FromArgb(80, 80, 100),
                    MouseOverBackColor = ThemeManager.CurrentTheme.ButtonHoverBackground
                }
            };
        }

        private void SetupMainPanel(Panel mainPanel)
        {
            mainPanel.BackColor = ThemeManager.CurrentTheme.MainBackground;
            mainPanel.Padding = new Padding(2);

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = ThemeManager.CurrentTheme.MainBackground
            };
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var headlinePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                Margin = new Padding(2)
            };
            ThemeManager.CurrentTheme.StyleHeadlinePanel(headlinePanel);
            var headlineLabel = new Label
            {
                Text = "Welcome to BusBus!",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            ThemeManager.CurrentTheme.StyleHeadlineLabel(headlineLabel);
            headlinePanel.Controls.Add(headlineLabel);
            contentLayout.Controls.Add(headlinePanel, 0, 0);

            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = ThemeManager.CurrentTheme.MainBackground;
            contentLayout.Controls.Add(_contentPanel, 0, 1);

            mainPanel.Controls.Add(contentLayout);

            var welcomeLabel = new Label
            {
                Text = "Welcome to BusBus Management System",
                Font = new Font(ThemeManager.CurrentTheme.HeadlineFont.FontFamily, 16, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            _contentPanel.Controls.Add(welcomeLabel);
        }

        private void LoadView(IDisplayable view)
        {
            try
            {
                _currentView?.Dispose();
                _contentPanel.Controls.Clear();
                _currentView = view;
                _currentView.Render(_contentPanel);
                _contentPanel.Visible = true; // Ensure content panel is visible after loading a view
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Loaded view: {_currentView?.GetType().Name}, Controls in _contentPanel: {_contentPanel.Controls.Count}, Visible: {_contentPanel.Visible}, Size: {_contentPanel.Size}");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Console.WriteLine($"Error loading view: {ex.Message}");

                var errorLabel = new Label
                {
                    Text = "Error loading the requested view. Please try again or contact support.",
                    ForeColor = Color.Red,
                    Font = ThemeManager.CurrentTheme.CardFont,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                _contentPanel.Controls.Clear();
                _contentPanel.Controls.Add(errorLabel);
            }
        }

        private static void OptimizeDataGridViewPerformance(DataGridView grid)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)?
                .SetValue(grid, true);

            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.VirtualMode = true;

            grid.MinimumSize = new Size(50, 50);

            grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            grid.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex < grid.FirstDisplayedScrollingRowIndex ||
                    e.RowIndex > grid.FirstDisplayedScrollingRowIndex + grid.DisplayedRowCount(true))
                {
                    e.Handled = true;
                }
            };
        }

        private static void SetupFooterPanel(Panel footerPanel)
        {
            footerPanel.BackColor = Color.FromArgb(40, 40, 55);

            var versionLabel = new Label
            {
                Text = "BusBus v1.0.0",
                ForeColor = Color.Silver,
                Font = new Font(ThemeManager.CurrentTheme.SmallButtonFont.FontFamily, 8),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            var copyrightLabel = new Label
            {
                Text = "© 2025 BusBus Inc. All rights reserved.",
                ForeColor = Color.Silver,
                Font = new Font(ThemeManager.CurrentTheme.SmallButtonFont.FontFamily, 8),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 10, 0)
            };

            footerPanel.Controls.Add(versionLabel);
            footerPanel.Controls.Add(copyrightLabel);
        }

        public static void SetRouteInput(BusBus.Models.Route route)
        {
            ArgumentNullException.ThrowIfNull(route);
        }

        public static void SimulateSaveButtonClick()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;

                _settingsButton?.Dispose();
                _settingsPanel?.Dispose();
                _sidePanel?.Dispose();
                _mainPanel?.Dispose();
                _footerPanel?.Dispose();
                _cancellationTokenSource?.Dispose();
                _contentPanel?.Dispose();
                if (_currentView is IDisposable disposableView)
                {
                    disposableView.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }

    // Removed duplicate RouteListPanel, IDisplayable, and RouteDisplayDTO definitions. Use the canonical definitions from their respective files.
}