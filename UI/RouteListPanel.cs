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
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                Visible = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Height = 400,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = System.Drawing.Color.FromArgb(85, 85, 85),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new System.Drawing.Font("Impact", 12F, System.Drawing.FontStyle.Bold),
                    BackColor = ThemeManager.CurrentTheme.GridBackground,
                    ForeColor = System.Drawing.Color.White,
                    SelectionBackColor = System.Drawing.Color.FromArgb(80, 80, 90),
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = System.Drawing.Color.FromArgb(60, 60, 60)
                },
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Raised,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };

            var nameCol = new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name" };
            var dateCol = new DataGridViewTextBoxColumn { HeaderText = "Date", DataPropertyName = "RouteDate" };
            var amStartCol = new DataGridViewTextBoxColumn { HeaderText = "AM Start", DataPropertyName = "AMStartingMileage", DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } };
            var amEndCol = new DataGridViewTextBoxColumn { HeaderText = "AM End", DataPropertyName = "AMEndingMileage", DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } };
            var pmStartCol = new DataGridViewTextBoxColumn { HeaderText = "PM Start", DataPropertyName = "PMStartMileage", DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } };
            var pmEndCol = new DataGridViewTextBoxColumn { HeaderText = "PM End", DataPropertyName = "PMEndingMileage", DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } };
            var driverCol = new DataGridViewComboBoxColumn
            {
                HeaderText = "Driver",
                DataPropertyName = "DriverId",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat,
                Name = "DriverId"
            };
            var vehicleCol = new DataGridViewComboBoxColumn
            {
                HeaderText = "Vehicle",
                DataPropertyName = "VehicleId",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat,
                Name = "VehicleId"
            };

            _routesGrid.Columns.Add(nameCol);
            _routesGrid.Columns.Add(dateCol);
            _routesGrid.Columns.Add(amStartCol);
            _routesGrid.Columns.Add(amEndCol);
            _routesGrid.Columns.Add(pmStartCol);
            _routesGrid.Columns.Add(pmEndCol);
            _routesGrid.Columns.Add(driverCol);
            _routesGrid.Columns.Add(vehicleCol);

            // Load drivers and vehicles for ComboBox columns
            Task.Run(async () =>
            {
                var drivers = await _routeService.GetDriversAsync();
                var vehicles = await _routeService.GetVehiclesAsync();
                this.Invoke((MethodInvoker)delegate
                {
                    var driverList = new List<object> { new { Id = (Guid?)null, Name = "Unassigned" } };
                    driverList.AddRange(drivers.Select(d => new { Id = (Guid?)d.Id, Name = $"{d.FirstName} {d.LastName}".Trim() }));
                    ((DataGridViewComboBoxColumn)driverCol).DataSource = driverList;
                    ((DataGridViewComboBoxColumn)driverCol).ValueMember = "Id";
                    ((DataGridViewComboBoxColumn)driverCol).DisplayMember = "Name";

                    var vehicleList = new List<object> { new { Id = (Guid?)null, BusNumber = "Unassigned" } };
                    vehicleList.AddRange(vehicles.Select(v => new { Id = (Guid?)v.Id, BusNumber = v.BusNumber }));
                    ((DataGridViewComboBoxColumn)vehicleCol).DataSource = vehicleList;
                    ((DataGridViewComboBoxColumn)vehicleCol).ValueMember = "Id";
                    ((DataGridViewComboBoxColumn)vehicleCol).DisplayMember = "BusNumber";
                });
            });

            // Hover effect for rows
            _routesGrid.RowEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _routesGrid.Rows.Count)
                {
                    _routesGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(70, 70, 90);
                }
            };
            _routesGrid.RowLeave += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _routesGrid.Rows.Count)
                {
                    _routesGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.GridBackground;
                }
            };

            // Rider count color accent threshold
            _routesGrid.CellFormatting += (s, e) =>
            {
                if (_routesGrid.Columns[e.ColumnIndex].DataPropertyName == "AMRiders" || _routesGrid.Columns[e.ColumnIndex].DataPropertyName == "PMRiders")
                {
                    if (e.Value is int riders && riders > 50)
                    {
                        e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(255, 215, 0); // Soft yellow
                    }
                    else
                    {
                        e.CellStyle.ForeColor = System.Drawing.Color.White;
                    }
                    e.CellStyle.Format = "D2";
                }
            };

            _routesGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _routes.Count)
                {
                    ShowRoutePanel(_routes[e.RowIndex]);
                }
            };

            _addRouteButton = new Button { Text = "New Route Entry", Dock = DockStyle.Left, Width = 120 };
            _editRouteButton = new Button { Text = "Edit", Dock = DockStyle.Left, Width = 80 };
            _updateRouteButton = new Button { Text = "Update", Dock = DockStyle.Left, Width = 80 };
            _deleteRouteButton = new Button { Text = "Delete", Dock = DockStyle.Left, Width = 80 };
            _prevPageButton = new Button { Text = "Previous", Dock = DockStyle.Left, Width = 80 };
            _nextPageButton = new Button { Text = "Next", Dock = DockStyle.Left, Width = 80 };
            _pageInfoLabel = new Label { Text = "Page 1", Dock = DockStyle.Left, Width = 100, TextAlign = ContentAlignment.MiddleLeft };

            _addRouteButton.Click += (s, e) =>
            {
                // Add a blank row to the DataGridView
                var list = _routesGrid.DataSource as List<RouteDisplayDTO>;
                if (list == null)
                {
                    list = new List<RouteDisplayDTO>();
                }
                var newDto = new RouteDisplayDTO
                {
                    Id = Guid.NewGuid(),
                    Name = "New Route",
                    RouteDate = DateTime.Today
                };
                list.Add(newDto);
                _routesGrid.DataSource = null;
                _routesGrid.DataSource = list;
                _routesGrid.Refresh();
                // Select the new row
                if (_routesGrid.Rows.Count > 0)
                {
                    _routesGrid.ClearSelection();
                    _routesGrid.Rows[_routesGrid.Rows.Count - 2].Selected = true; // -2 because of the new row placeholder
                }
            };

            _editRouteButton.Click += (s, e) =>
            {
                if (_routesGrid.SelectedRows.Count > 0)
                {
                    var row = _routesGrid.SelectedRows[0];
                    // Highlight the selected row
                    foreach (DataGridViewRow r in _routesGrid.Rows)
                    {
                        r.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.CardBackground;
                    }
                    row.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
                }
            };

            _updateRouteButton.Click += async (s, e) =>
            {
                if (_routesGrid.SelectedRows.Count > 0)
                {
                    var row = _routesGrid.SelectedRows[0];
                    var dto = row.DataBoundItem as RouteDisplayDTO;
                    if (dto != null)
                    {
                        // Fetch the tracked entity from the database
                        var trackedRoute = await _routeService.GetRouteByIdAsync(dto.Id);
                        if (trackedRoute != null)
                        {
                            // Update properties from DTO
                            trackedRoute.Name = dto.Name;
                            trackedRoute.RouteDate = dto.RouteDate;
                            trackedRoute.AMStartingMileage = dto.AMStartingMileage;
                            trackedRoute.AMEndingMileage = dto.AMEndingMileage;
                            trackedRoute.AMRiders = dto.AMRiders;
                            trackedRoute.PMStartMileage = dto.PMStartMileage;
                            trackedRoute.PMEndingMileage = dto.PMEndingMileage;
                            trackedRoute.PMRiders = dto.PMRiders;
                            // Note: Driver and Vehicle updates would require lookup by name if editable
                            await _routeService.UpdateRouteAsync(trackedRoute);
                            await LoadRoutesAsync(_currentPage, _pageSize, CancellationToken.None);
                        }
                        else
                        {
                            MessageBox.Show($"Route with ID {dto.Id} not found in the database.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };

            _deleteRouteButton.Click += async (s, e) =>
            {
                if (_routesGrid.SelectedRows.Count > 0)
                {
                    var row = _routesGrid.SelectedRows[0];
                    var dto = row.DataBoundItem as RouteDisplayDTO;
                    if (dto != null && dto.Id != Guid.Empty)
                    {
                        await _routeService.DeleteRouteAsync(dto.Id);
                        var result = MessageBox.Show("Would you like to enter a new route?", "Add New Route", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            _addRouteButton.PerformClick();
                        }
                        else
                        {
                            await LoadRoutesAsync(_currentPage, _pageSize, CancellationToken.None);
                        }
                    }
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

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = ThemeManager.CurrentTheme.CardBackground
            };
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5F)); // Top spacer
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 85F)); // Grid
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Bottom panel (increased height)

            // Top row: empty panel for spacing
            tableLayout.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.CurrentTheme.CardBackground }, 0, 0);

            // Middle row: grid
            tableLayout.Controls.Add(_routesGrid, 0, 1);

            // Bottom row: CRUD and pagination
            var bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                RowCount = 1,
                ColumnCount = 3,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Visible = true,
                AutoSize = false,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            // Debug logging for CRUD button visibility and size
            Console.WriteLine($"[RouteListPanel] CRUD Button Visible: {_addRouteButton.Visible}, Size: {_addRouteButton.Size}");
            Console.WriteLine($"[RouteListPanel] CRUD Button Visible: {_editRouteButton.Visible}, Size: {_editRouteButton.Size}");
            Console.WriteLine($"[RouteListPanel] CRUD Button Visible: {_updateRouteButton.Visible}, Size: {_updateRouteButton.Size}");
            Console.WriteLine($"[RouteListPanel] CRUD Button Visible: {_deleteRouteButton.Visible}, Size: {_deleteRouteButton.Size}");
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Pagination left
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Centered CRUD
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Page info right

            // Pagination controls (left)
            var paginationPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            paginationPanel.Controls.Add(_prevPageButton);
            paginationPanel.Controls.Add(_nextPageButton);
            bottomPanel.Controls.Add(paginationPanel, 0, 0);

            // Centered CRUD buttons (center)
            var crudPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None };
            crudPanel.Controls.Add(_addRouteButton);
            crudPanel.Controls.Add(_editRouteButton);
            crudPanel.Controls.Add(_updateRouteButton);
            crudPanel.Controls.Add(_deleteRouteButton);
            bottomPanel.Controls.Add(crudPanel, 1, 0);

            // Page info (right)
            var pageInfoPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            pageInfoPanel.Controls.Add(_pageInfoLabel);
            bottomPanel.Controls.Add(pageInfoPanel, 2, 0);

            tableLayout.Controls.Add(bottomPanel, 0, 2);
            bottomPanel.BringToFront();

            this.Controls.Add(tableLayout);
            // Inline editing event
            _routesGrid.CellEndEdit += async (s, e) =>
            {
                // Validate referential integrity for DriverId and VehicleId
                var row = _routesGrid.Rows[e.RowIndex];
                var dto = row.DataBoundItem as RouteDisplayDTO;
                if (dto != null)
                {
                    if (e.ColumnIndex == _routesGrid.Columns["DriverId"].Index)
                    {
                        var driverId = dto.DriverId;
                        if (driverId != null)
                        {
                            var driver = (await _routeService.GetDriversAsync()).FirstOrDefault(d => d.Id == driverId);
                            if (driver == null)
                            {
                                MessageBox.Show("Selected driver does not exist", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                dto.DriverId = null;
                                row.Cells[e.ColumnIndex].Value = null;
                            }
                        }
                    }
                    if (e.ColumnIndex == _routesGrid.Columns["VehicleId"].Index)
                    {
                        var vehicleId = dto.VehicleId;
                        if (vehicleId != null)
                        {
                            var vehicle = (await _routeService.GetVehiclesAsync()).FirstOrDefault(v => v.Id == vehicleId);
                            if (vehicle == null)
                            {
                                MessageBox.Show("Selected vehicle does not exist", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                dto.VehicleId = null;
                                row.Cells[e.ColumnIndex].Value = null;
                            }
                        }
                    }
                }
                _routesGrid.Refresh();
            };
        }

        private readonly Button _updateRouteButton;
        private readonly Button _deleteRouteButton;

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

                    Console.WriteLine($"[RouteListPanel] Retrieved {routes.Count} routes for page {page}");
                    var routeDisplayList = routes.Select(r => RouteDisplayDTO.FromRoute(r)).ToList();

                    _routes = routes.ToList();
                    _routesGrid.DataSource = routeDisplayList;
                    _routesGrid.Refresh();

                    foreach (DataGridViewColumn column in _routesGrid.Columns)
                    {
                        column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }

                    int totalPages = (_totalRoutes + _pageSize - 1) / _pageSize;
                    _pageInfoLabel.Text = $"Page {_currentPage} of {totalPages}";
                    _prevPageButton.Enabled = _currentPage > 1;
                    _nextPageButton.Enabled = _currentPage < totalPages;

                    // Debug: Check grid state
                    Console.WriteLine($"[RouteListPanel] _routesGrid.DataSource count: {routeDisplayList.Count}");
                    Console.WriteLine($"[RouteListPanel] _routesGrid.Controls.Count: {_routesGrid.Controls.Count}");
                    Console.WriteLine($"[RouteListPanel] _routesGrid.Visible: {_routesGrid.Visible}, Size: {_routesGrid.Size}");

                    if (routeDisplayList.Count == 0)
                    {
                        MessageBox.Show("No routes found in the database.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
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
            Console.WriteLine($"[RouteListPanel] Render called. Container: {container.GetType().Name}, Controls before: {container.Controls.Count}");
            container.Controls.Clear();
            container.Controls.Add(this);
            Console.WriteLine($"[RouteListPanel] Render complete. Controls after: {container.Controls.Count}");
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
                _updateRouteButton?.Dispose();
                _deleteRouteButton?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}