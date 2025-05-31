using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;

namespace BusBus.UI
{
    public class RouteView : BaseCrudView
    {
        protected DataGridView CrudDataGridView { get; set; }
        private readonly IRouteService _routeService;
        private List<Route> _currentRoutes = new List<Route>();
        public RouteView(IRouteService routeService)
        {
            _routeService = routeService;
            HeaderText = "Route View";
            CrudDataGridView = new DataGridView();
            SetupGrid();
            SetupGrid();
            this.CrudAddClicked += OnAddClicked;
            this.CrudEditClicked += OnEditClicked;
            this.CrudDeleteClicked += OnDeleteClicked;
            _ = LoadRoutesAsync();
        }

        private void SetupGrid()
        {
            var grid = this.CrudDataGridView;
            grid.Columns.Clear();
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RouteNumber", HeaderText = "Route #", DataPropertyName = "RouteNumber" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Origin", HeaderText = "Origin", DataPropertyName = "Origin" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Destination", HeaderText = "Destination", DataPropertyName = "Destination" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
        }

        private async System.Threading.Tasks.Task LoadRoutesAsync()
        {
            _currentRoutes = await _routeService.GetAllAsync(System.Threading.CancellationToken.None);
            this.CrudDataGridView.DataSource = _currentRoutes;
        }

        private void OnAddClicked(object? sender, EventArgs e)
        {
            // Add logic for adding a new route
        }
        private void OnEditClicked(object? sender, EventArgs e)
        {
            // Add logic for editing a route
        }
        private void OnDeleteClicked(object? sender, EventArgs e)
        {
            // Add logic for deleting a route
        }
    }
}
