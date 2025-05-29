using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Services;
using BusBus.UI;
using BusBus.UI.Common;
using BusBus.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BusBus.UI
{
    /// <summary>
    /// Route management view that displays routes in a data grid
    /// </summary>
    public class RouteListViewPlaceholder : UserControl, IView
    {
        private readonly IRouteService _routeService;
        private DynamicDataGridView<Route>? _routeDataGrid;
        private RouteDataViewConfiguration? _routeConfig;

        public string ViewName => "Routes";
        public string Title => "Route Management";        public Control? Control => this;

#pragma warning disable CS0067 // Events are never used - required by interface for future functionality
        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        public event EventHandler<StatusEventArgs>? StatusUpdated;
#pragma warning restore CS0067

        public RouteListViewPlaceholder(IRouteService routeService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));

            InitializeRouteView();
        }

        private void InitializeRouteView()
        {
            // Set up the main layout
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };

            // Header panel
            var headerPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = System.Drawing.Color.FromArgb(240, 240, 240)
            };

            var titleLabel = new Label
            {
                Text = "Route Management",
                Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(64, 64, 64),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 15)
            };

            headerPanel.Controls.Add(titleLabel);

            // Create route configuration and data grid
            _routeConfig = new RouteDataViewConfiguration(_routeService);
            _routeDataGrid = new DynamicDataGridView<Route>(_routeConfig);

            var gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            _routeDataGrid.Dock = DockStyle.Fill;
            gridPanel.Controls.Add(_routeDataGrid);

            // Add to main layout
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            tableLayout.Controls.Add(headerPanel, 0, 0);
            tableLayout.Controls.Add(gridPanel, 0, 1);

            Controls.Add(tableLayout);
            BackColor = System.Drawing.Color.White;
        }        public async Task ActivateAsync(CancellationToken cancellationToken)
        {
            if (_routeDataGrid != null)
            {
                await _routeDataGrid.LoadDataAsync(cancellationToken);
            }
        }public Task DeactivateAsync()
        {
            // Nothing special needed for deactivation
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up resources
            }
            base.Dispose(disposing);
        }
    }
}
