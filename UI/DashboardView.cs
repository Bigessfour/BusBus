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
using BusBus.UI; // Ensure RouteView and DriverListPanel are found

namespace BusBus.UI
{
    /// <summary>
    /// The DashboardView is the home/main view that appears in the Dashboard's content panel.
    /// This is loaded when the user clicks on the "Dashboard" navigation button in the side panel.
    /// This view contains the header, side panel, statistics panel, and a shared data grid view.
    /// </summary>



    public class DashboardView : UserControl, IView
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DashboardView> _logger;
        private Panel? _sidePanel;
        private Panel? _contentPanel;
        private RouteView? _routeView;
        private DriverListPanel? _driverListPanel;

        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        public event EventHandler<StatusEventArgs>? StatusUpdated;

        // Add more view fields as needed

        public DashboardView(IServiceProvider serviceProvider, ILogger<DashboardView> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            InitializeLayout();
            AddNavigationButtons();
        }

        private void InitializeLayout()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _sidePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 32, 32), Width = 220 };
            _contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 24, 24) };

            mainLayout.Controls.Add(_sidePanel, 0, 0);
            mainLayout.Controls.Add(_contentPanel, 1, 0);
            Controls.Add(mainLayout);
        }

        private void AddNavigationButtons()
        {
            if (_sidePanel == null) return;
            _sidePanel.Controls.Clear();

            var dashboardBtn = CreateNavButton("Dashboard", (s, e) => ShowDashboardHome());
            var routeBtn = CreateNavButton("Routes", (s, e) => ShowRouteView());
            var driverBtn = CreateNavButton("Drivers", (s, e) => ShowDriverListPanel());
            // Add more buttons as needed

            _sidePanel.Controls.Add(dashboardBtn);
            _sidePanel.Controls.Add(routeBtn);
            _sidePanel.Controls.Add(driverBtn);
        }

        private static Button CreateNavButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 48, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            btn.Click += onClick;
            return btn;
        }

        private void ShowDashboardHome()
        {
            if (_contentPanel == null) return;
            _contentPanel.Controls.Clear();
            var label = new Label
            {
                Text = "Dashboard Home",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Height = 60,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 16, 0, 0)
            };
            _contentPanel.Controls.Add(label);
        }

        private void ShowRouteView()
        {
            if (_contentPanel == null)
                return;
            _contentPanel.Controls.Clear();
            if (_routeView == null)
            {
                var routeService = _serviceProvider.GetService(typeof(IRouteService)) as IRouteService;
                if (routeService == null)
                {
                    MessageBox.Show("Route service not available.");
                    return;
                }
                _routeView = new RouteView(routeService);
            }
            if (_routeView != null)
            {
                _routeView.Dock = DockStyle.Fill;
                _contentPanel.Controls.Add(_routeView);
            }
        }

        private void ShowDriverListPanel()
        {
            if (_contentPanel == null)
                return;
            _contentPanel.Controls.Clear();
            if (_driverListPanel == null)
            {
                var driverService = _serviceProvider.GetService(typeof(IDriverService)) as IDriverService;
                if (driverService == null)
                {
                    MessageBox.Show("Driver service not available.");
                    return;
                }
                _driverListPanel = new DriverListPanel(driverService);
            }
            if (_driverListPanel != null)
            {
                _driverListPanel.Dock = DockStyle.Fill;
                _contentPanel.Controls.Add(_driverListPanel);
            }
        }

        // IView interface implementations
        public string ViewName => "Dashboard";
        public string Title => "Dashboard Home";
        public Control? Control => this;

        public Task ActivateAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeactivateAsync()
        {
            return Task.CompletedTask;
        }
    }
}
