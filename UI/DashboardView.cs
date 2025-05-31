// Suppress warnings for unused fields and variables in this view
#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0414 // Field is assigned but its value is never used
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

        // Constructor to satisfy DI and field requirements
        public DashboardView(IServiceProvider serviceProvider, ILogger<DashboardView> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        // Required abstract property implementations
        public override string ViewName => "Dashboard";
        public override string Title => "Dashboard Home";

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // TODO: Add activation logic
            return Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync()
        {
            // TODO: Add deactivation logic
            return Task.CompletedTask;
        }

        // ...existing code...
    }
}
