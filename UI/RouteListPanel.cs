using BusBus.Models;
using BusBus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI
{
    public class RouteListPanel : Panel, IDisplayable
    {
        public void RefreshTheme()
        {
            // Apply theme to this panel and its children
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
            foreach (Control control in this.Controls)
            {
                ApplyThemeToControl(control);
            }
        }

        private static void ApplyThemeToControl(Control control)
        {
            switch (control)
            {
                case DataGridView grid:
                    grid.BackgroundColor = ThemeManager.CurrentTheme.GridBackground;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.CurrentTheme.HeadlineBackground;
                    grid.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.CardBackground;
                    grid.DefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.CardText;
                    break;
                case Button button:
                    button.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                    button.ForeColor = ThemeManager.CurrentTheme.CardText;
                    break;
                case Label label:
                    label.ForeColor = ThemeManager.CurrentTheme.CardText;
                    break;
            }
            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child);
            }
        }
        private readonly IRouteService _routeService;
        private readonly DataGridView _routesGrid;
        private readonly Button _prevPageButton;
        private readonly Button _nextPageButton;
        private readonly Label _pageInfoLabel;
        private readonly Button _addRouteButton;
        private readonly Button _editRouteButton;
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRoutes;
        private List<BusBus.Models.Route> _routes = new List<BusBus.Models.Route>();

        public RouteListPanel(IRouteService routeService)
        {
            ArgumentNullException.ThrowIfNull(routeService);
            _routeService = routeService;

            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;

            _routesGrid = new DataGridView
            {
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                Visible = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSize = true,
                Anchor = AnchorStyles.None // Center the grid
            };

            _routesGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            var nameCol = new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name" };
            var dateCol = new DataGridViewTextBoxColumn { HeaderText = "Date", DataPropertyName = "RouteDate" };
            var amStartCol = new DataGridViewTextBoxColumn { HeaderText = "AM Start", DataPropertyName = "AMStartingMileage" };
            var amEndCol = new DataGridViewTextBoxColumn { HeaderText = "AM End", DataPropertyName = "AMEndingMileage" };
            var pmStartCol = new DataGridViewTextBoxColumn { HeaderText = "PM Start", DataPropertyName = "PMStartMileage" };
            var pmEndCol = new DataGridViewTextBoxColumn { HeaderText = "PM End", DataPropertyName = "PMEndingMileage" };
            var driverCol = new DataGridViewTextBoxColumn { HeaderText = "Driver", DataPropertyName = "DriverName" };
            var vehicleCol = new DataGridViewTextBoxColumn { HeaderText = "Vehicle", DataPropertyName = "VehicleName" };

            nameCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dateCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            amStartCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            amEndCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            pmStartCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            pmEndCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            driverCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            vehicleCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _routesGrid.Columns.Add(nameCol);
            _routesGrid.Columns.Add(dateCol);
            _routesGrid.Columns.Add(amStartCol);
            _routesGrid.Columns.Add(amEndCol);
            _routesGrid.Columns.Add(pmStartCol);
            _routesGrid.Columns.Add(pmEndCol);
            _routesGrid.Columns.Add(driverCol);
            _routesGrid.Columns.Add(vehicleCol);

            _routesGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _routes.Count)
                {
                    ShowRoutePanel(_routes[e.RowIndex]);
                }
            };

            _addRouteButton = new Button { Text = "Add Route", Dock = DockStyle.Left, Width = 100 };
            _editRouteButton = new Button { Text = "Edit Route", Dock = DockStyle.Left, Width = 100 };
            _prevPageButton = new Button { Text = "Previous", Dock = DockStyle.Left, Width = 80 };
            _nextPageButton = new Button { Text = "Next", Dock = DockStyle.Left, Width = 80 };
            _pageInfoLabel = new Label { Text = "Page 1", Dock = DockStyle.Left, Width = 100, TextAlign = ContentAlignment.MiddleLeft };

            _addRouteButton.Click += (s, e) => ShowRoutePanel(null);
            _editRouteButton.Click += (s, e) =>
            {
                if (_routesGrid.SelectedRows.Count > 0)
                {
                    int idx = _routesGrid.SelectedRows[0].Index;
                    if (idx >= 0 && idx < _routes.Count)
                        ShowRoutePanel(_routes[idx]);
                }
            };
            _prevPageButton.Click += async (s, e) =>
            {
                if (_currentPage > 1)
                {
                    await LoadRoutesAsync(_currentPage - 1, _pageSize, CancellationToken.None);
                }
            };
            _nextPageButton.Click += async (s, e) =>
            {
                int totalPages = (_totalRoutes + _pageSize - 1) / _pageSize;
                if (_currentPage < totalPages)
                {
                    await LoadRoutesAsync(_currentPage + 1, _pageSize, CancellationToken.None);
                }
            };

            var topPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight };
            topPanel.Controls.Add(_addRouteButton);
            topPanel.Controls.Add(_editRouteButton);

            var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.LeftToRight };
            bottomPanel.Controls.Add(_prevPageButton);
            bottomPanel.Controls.Add(_nextPageButton);
            bottomPanel.Controls.Add(_pageInfoLabel);

            // Center the DataGridView using FlowLayoutPanel
            var gridLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10)
            };
            gridLayout.Controls.Add(_routesGrid);

            this.Controls.Add(topPanel);
            this.Controls.Add(gridLayout);
            this.Controls.Add(bottomPanel);
        }

        public async Task LoadRoutesAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            _currentPage = page;
            _pageSize = pageSize;

            using (var loadingCursor = new CursorScope(Cursors.WaitCursor))
            {
                try
                {
                    _totalRoutes = await _routeService.GetRoutesCountAsync(cancellationToken);
                    var routes = await _routeService.GetRoutesAsync(page, pageSize, cancellationToken);

                    var routeDisplayList = routes.Select(r => RouteDisplayDTO.FromRoute(r)).ToList();

                    _routes = routes.ToList();

                    _routesGrid.DataSource = routeDisplayList;

                    foreach (DataGridViewColumn column in _routesGrid.Columns)
                    {
                        column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }

                    int totalPages = (_totalRoutes + _pageSize - 1) / _pageSize;
                    _pageInfoLabel.Text = $"Page {_currentPage} of {totalPages}";
                    _prevPageButton.Enabled = _currentPage > 1;
                    _nextPageButton.Enabled = _currentPage < totalPages;
                }
                catch (OperationCanceledException)
                {
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Error retrieving data: {ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Network error: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
                {
                    Console.WriteLine($"Exception in LoadRoutesAsync: {ex}");
                    MessageBox.Show($"Failed to load routes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowRoutePanel(BusBus.Models.Route? route)
        {
            using (var form = new Form
            {
                Text = route == null ? "Add Route" : "Edit Route",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent
            })
            {
                var routePanel = new RoutePanel(_routeService);
                routePanel.Dock = DockStyle.Fill;
                if (route != null)
                    routePanel.SetRouteData(route);
                form.Controls.Add(routePanel);

                routePanel.SaveButtonClicked += async (s, e) =>
                {
                    try
                    {
                        var routeData = routePanel.GetRouteData();
                        if (routeData == null) return;
                        if (route == null)
                            await _routeService.CreateRouteAsync(routeData);
                        else
                        {
                            routeData.Id = route.Id;
                            await _routeService.UpdateRouteAsync(routeData);
                        }
                        await LoadRoutesAsync(_currentPage, _pageSize, CancellationToken.None);
                        form.Close();
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show($"Operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show($"Invalid data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                    {
                        Console.WriteLine($"Critical error in SaveButtonClicked: {ex}");
                        MessageBox.Show($"Failed to save route: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                form.ShowDialog();
            }
        }

        public void Render(Control container)
        {
            ArgumentNullException.ThrowIfNull(container);
            container.Controls.Clear();
            container.Controls.Add(this);
        }

        // Only one Dispose method should exist: override for Panel
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _routesGrid?.Dispose();
                _prevPageButton?.Dispose();
                _nextPageButton?.Dispose();
                _pageInfoLabel?.Dispose();
                _addRouteButton?.Dispose();
                _editRouteButton?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}