using BusBus.Models;
using BusBus.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.UI.Common;
using Microsoft.EntityFrameworkCore;

namespace BusBus.UI
{
    // Event args for route edit events (using RouteDisplayDTO for grid context)
    public class RouteEventArgs : EventArgs
    {
        public RouteDisplayDTO RouteDTO { get; set; }

        // Adding Route property for compatibility with existing code
        public Route Route => RouteDTO.ToRoute();

        public RouteEventArgs(RouteDisplayDTO route) => RouteDTO = route;
    }

    public partial class RouteListPanel : ThemeableControl, IDisplayable
    {
        private readonly IRouteService _routeService;
        private List<RouteDisplayDTO> _routes = new List<RouteDisplayDTO>();
        private List<Driver> _drivers = new List<Driver>();
        private List<Vehicle> _vehicles = new List<Vehicle>();
        private int _totalRoutes = 0;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private DataGridView _routesGrid;
        private Button _addRouteButton, _editRouteButton, _deleteRouteButton, _prevPageButton, _nextPageButton;
        private Label _titleLabel, _pageInfoLabel;
        public event EventHandler<RouteEventArgs>? RouteEditRequested;

        // Public accessor for the routes grid
        public DataGridView RoutesGrid => _routesGrid;

        public RouteListPanel(IRouteService routeService)
        {
            ArgumentNullException.ThrowIfNull(routeService);
            _routeService = routeService;
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
            this.Padding = new Padding(10);
            this.Dock = DockStyle.Fill;

            // Title label
            _titleLabel = new Label
            {
                Text = "Route Entries",
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(_titleLabel);

            // Main container
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = ThemeManager.CurrentTheme.CardBackground
            };
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.Controls.Add(mainContainer);

            // Buttons
            _addRouteButton = new Button
            {
                Text = "Add New Route",
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.None,
                Width = 150,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1 },
                Font = new System.Drawing.Font("Segoe UI", 10F)
            };
            _editRouteButton = new Button
            {
                Text = "Edit Selected",
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.None,
                Width = 120,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1 },
                Font = new System.Drawing.Font("Segoe UI", 10F)
            };
            _deleteRouteButton = new Button
            {
                Text = "Delete",
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.None,
                Width = 100,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1 },
                Font = new System.Drawing.Font("Segoe UI", 10F)
            };
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 50,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = ThemeManager.CurrentTheme.CardBackground
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonPanel.Controls.Add(_addRouteButton, 0, 0);
            buttonPanel.Controls.Add(_editRouteButton, 1, 0);
            buttonPanel.Controls.Add(_deleteRouteButton, 2, 0);
            mainContainer.Controls.Add(buttonPanel, 0, 0);

            // DataGridView
            _routesGrid = new DataGridView
            {
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                Visible = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = System.Drawing.Color.FromArgb(200, 200, 200),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular),
                    BackColor = ThemeManager.CurrentTheme.CardBackground,
                    ForeColor = ThemeManager.CurrentTheme.CardText,
                    SelectionBackColor = System.Drawing.Color.FromArgb(100, 150, 255),
                    SelectionForeColor = ThemeManager.CurrentTheme.CardText,
                    Padding = new Padding(3),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = System.Drawing.Color.FromArgb(235, 235, 235)
                },
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                    BackColor = ThemeManager.CurrentTheme.HeadlineBackground,
                    ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = new Padding(3)
                },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowHeadersVisible = false,
                RowTemplate = { Height = 32 },
                EditMode = DataGridViewEditMode.EditOnEnter,
                ReadOnly = false
            };
            mainContainer.Controls.Add(_routesGrid, 0, 1);

            // Columns (updated)
            _routesGrid.Columns.Clear();
            _routesGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "Route Name", DataPropertyName = "Name", FillWeight = 150, MinimumWidth = 120 },
                new DataGridViewTextBoxColumn { HeaderText = "Date", DataPropertyName = "TripDate", FillWeight = 100, MinimumWidth = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "d" } },
                new DataGridViewTextBoxColumn { HeaderText = "AM Start", DataPropertyName = "AMStartingMileage", DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }, FillWeight = 80, MinimumWidth = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "AM End", DataPropertyName = "AMEndingMileage", DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }, FillWeight = 80, MinimumWidth = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "AM Miles", Name = "AMTotalMiles", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N1", Alignment = DataGridViewContentAlignment.MiddleRight }, FillWeight = 70, MinimumWidth = 70 },
                new DataGridViewTextBoxColumn { HeaderText = "PM Start", DataPropertyName = "PMStartMileage", DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }, FillWeight = 80, MinimumWidth = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "PM End", DataPropertyName = "PMEndingMileage", DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }, FillWeight = 80, MinimumWidth = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "PM Miles", Name = "PMTotalMiles", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N1", Alignment = DataGridViewContentAlignment.MiddleRight }, FillWeight = 70, MinimumWidth = 70 },
                new DataGridViewTextBoxColumn { HeaderText = "Driver", Name = "DriverName", ReadOnly = true, FillWeight = 120, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { HeaderText = "Vehicle", Name = "VehicleNumber", ReadOnly = true, FillWeight = 80, MinimumWidth = 80 }
            });

            // Pagination panel
            var paginationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 40,
                BackColor = ThemeManager.CurrentTheme.CardBackground
            };
            mainContainer.Controls.Add(paginationPanel, 0, 2);

            // Previous page button
            _prevPageButton = new Button
            {
                Text = "◀ Previous",
                Dock = DockStyle.Left,
                Width = 100,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1 },
                Enabled = false
            };
            paginationPanel.Controls.Add(_prevPageButton);

            // Page info label
            _pageInfoLabel = new Label
            {
                Text = "Page 1",
                Dock = DockStyle.Left,
                Width = 150,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = new System.Drawing.Font("Segoe UI", 9F)
            };
            paginationPanel.Controls.Add(_pageInfoLabel);

            // Next page button
            _nextPageButton = new Button
            {
                Text = "Next ▶",
                Dock = DockStyle.Left,
                Width = 100,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1 }
            };
            paginationPanel.Controls.Add(_nextPageButton);

            // Event handlers (add as needed)
            _routesGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= _routes.Count) return;
                var route = _routes[e.RowIndex];
                var colName = _routesGrid.Columns[e.ColumnIndex].Name;

                if (colName == "AMTotalMiles")
                {
                    e.Value = Math.Max(0, route.AMEndingMileage - route.AMStartingMileage);
                    e.FormattingApplied = true;
                }
                else if (colName == "PMTotalMiles")
                {
                    e.Value = Math.Max(0, route.PMEndingMileage - route.PMStartMileage);
                    e.FormattingApplied = true;
                }
                else if (colName == "DriverName")
                {
                    e.Value = route.Driver != null ? $"{route.Driver.FirstName} {route.Driver.LastName}".Trim() : "Unassigned";
                    e.FormattingApplied = true;
                }
                else if (colName == "VehicleNumber")
                {
                    e.Value = route.Vehicle?.BusNumber ?? "Unassigned";
                    e.FormattingApplied = true;
                }
            };

            // Handle row selection for better UX
            _routesGrid.SelectionChanged += (s, e) =>
            {
                bool hasSelection = _routesGrid.SelectedRows.Count > 0;
                _editRouteButton.Enabled = hasSelection;
                _deleteRouteButton.Enabled = hasSelection;
            };
            // Double-click to edit
            _routesGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _routes.Count)
                {
                    RouteEditRequested?.Invoke(this, new RouteEventArgs(_routes[e.RowIndex]));
                }
            };

            // Add cell edit handler for updating driver and vehicle assignments
            _routesGrid.CellEndEdit += async (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _routes.Count) return;
                var route = _routes[e.RowIndex];
                if (e.ColumnIndex == _routesGrid.Columns["DriverId"].Index)
                {
                    route.DriverId = (Guid?)_routesGrid[e.ColumnIndex, e.RowIndex].Value;
                }
                else if (e.ColumnIndex == _routesGrid.Columns["VehicleId"].Index)
                {
                    route.VehicleId = (Guid?)_routesGrid[e.ColumnIndex, e.RowIndex].Value;
                }                try
                {
                    await _routeService.UpdateRouteAsync(route.ToRoute());
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show($"Invalid data: {ex.Message}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (DbUpdateException ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Connection error: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Edit button click handler
            _editRouteButton.Click += (s, e) =>
            {
                if (_routesGrid.SelectedRows.Count > 0 && _routesGrid.SelectedRows[0].Index < _routes.Count)
                {
                    RouteEditRequested?.Invoke(this, new RouteEventArgs(_routes[_routesGrid.SelectedRows[0].Index]));
                }
            };

            // Add button click handler
            _addRouteButton.Click += (s, e) =>
            {                var newRoute = new RouteDisplayDTO
                {
                    Id = Guid.Empty, // Mark as new route
                    Name = "New Route",
                    RouteDate = DateTime.Today                };
                RouteEditRequested?.Invoke(this, new RouteEventArgs(newRoute));
            };

            // Delete button click handler
            _deleteRouteButton.Click += async (s, e) =>
            {
                if (_routesGrid.SelectedRows.Count > 0 && _routesGrid.SelectedRows[0].Index < _routes.Count)
                {
                    var route = _routes[_routesGrid.SelectedRows[0].Index];
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete the route '{route.Name}'?",
                        "Confirm Delete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            await _routeService.DeleteRouteAsync(route.Id);
                            await LoadRoutesAsync(_currentPage, _pageSize, CancellationToken.None);
                        }
                        catch (DbUpdateException ex)
                        {
                            MessageBox.Show($"Database error deleting route: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        catch (HttpRequestException ex)
                        {
                            MessageBox.Show($"Network error deleting route: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        catch (InvalidOperationException ex)
                        {
                            MessageBox.Show($"Error deleting route: {ex.Message}", "Operation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };

            // Previous page button
            _prevPageButton.Click += async (s, e) =>
            {
                if (_currentPage > 1)
                {
                    _currentPage--;
                    await LoadRoutesAsync(_currentPage, _pageSize, CancellationToken.None);
                }
            };

            // Next page button
            _nextPageButton.Click += async (s, e) =>
            {
                if (_currentPage * _pageSize < _totalRoutes)
                {
                    _currentPage++;
                    await LoadRoutesAsync(_currentPage, _pageSize, CancellationToken.None);
                }
            };

            // Infinite scroll event
            _routesGrid.Scroll += async (s, e) => await HandleInfiniteScrollAsync();

            // Initial data load
            InitializeDataAsync();
        }

        private async Task HandleInfiniteScrollAsync()
        {
            if (_routes.Count >= _totalRoutes) return;
            var visibleRows = _routesGrid.DisplayedRowCount(false);
            var firstDisplayed = _routesGrid.FirstDisplayedScrollingRowIndex;
            var lastVisible = firstDisplayed + visibleRows;
            if (lastVisible >= _routes.Count - 5)
            {
                int nextPage = (_routes.Count / _pageSize) + 1;                var moreRoutes = await _routeService.GetRoutesAsync(nextPage, _pageSize, CancellationToken.None);
                if (moreRoutes != null && moreRoutes.Count > 0)
                {
                    var moreRouteDTOs = moreRoutes.Select(RouteDisplayDTO.FromRoute).ToList();
                    _routes.AddRange(moreRouteDTOs);
                    _routesGrid.DataSource = null;
                    _routesGrid.DataSource = _routes;
                }
            }
        }

        private async void InitializeDataAsync()
        {
            try
            {
                if (IsDisposed || _routeService == null)
                    return;
                var token = _cancellationTokenSource.Token;
                try
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        _drivers = await _routeService.GetDriversAsync(token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading drivers: {ex.Message}");
                        _drivers = new List<Driver>();
                    }
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        _vehicles = await _routeService.GetVehiclesAsync(token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading vehicles: {ex.Message}");
                        _vehicles = new List<Vehicle>();
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Initial data loading was canceled");
                    return;
                }
                if (!IsDisposed && !token.IsCancellationRequested)
                {
                    await LoadRoutesAsync(1, _pageSize, token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("RouteListPanel initialization canceled");
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("RouteListPanel was disposed during initialization");
            }
            catch (DbUpdateException ex)
            {
                if (!IsDisposed && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show($"Database error loading data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                if (!IsDisposed && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show($"Network error loading data: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            }
            catch (InvalidOperationException ex)
            {
                if (!IsDisposed && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show($"Error loading initial data: {ex.Message}", "Operation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            }
        }

        public async Task LoadRoutesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested || IsDisposed)
                {
                    Console.WriteLine("LoadRoutesAsync canceled or control disposed");
                    return;
                }
                _currentPage = page;
                _pageSize = pageSize;
                if (_drivers == null || _drivers.Count == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _drivers = await _routeService.GetDriversAsync(cancellationToken);
                }
                if (cancellationToken.IsCancellationRequested || IsDisposed)
                {
                    Console.WriteLine("LoadRoutesAsync canceled after loading drivers");
                    return;
                }
                if (_vehicles == null || _vehicles.Count == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _vehicles = await _routeService.GetVehiclesAsync(cancellationToken);
                }
                if (cancellationToken.IsCancellationRequested || IsDisposed)
                {
                    Console.WriteLine("LoadRoutesAsync canceled after loading vehicles");
                    return;
                }
                cancellationToken.ThrowIfCancellationRequested();
                _totalRoutes = await _routeService.GetRoutesCountAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested || IsDisposed)
                {
                    Console.WriteLine("LoadRoutesAsync canceled after getting count");
                    return;
                }                cancellationToken.ThrowIfCancellationRequested();
                var routes = await _routeService.GetRoutesAsync(page, pageSize, cancellationToken);
                _routes = routes.Select(RouteDisplayDTO.FromRoute).ToList();
                if (cancellationToken.IsCancellationRequested || IsDisposed)
                {
                    Console.WriteLine("LoadRoutesAsync canceled before updating UI");
                    return;
                }
                if (IsHandleCreated && !IsDisposed)
                {
                    try
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (IsDisposed) return;
                            _routesGrid.DataSource = null;
                            _routesGrid.DataSource = _routes;
                            _routesGrid.ReadOnly = false;
                            foreach (DataGridViewColumn col in _routesGrid.Columns)
                            {
                                if (col.Name != "DriverId" && col.Name != "VehicleId")
                                {
                                    col.ReadOnly = true;
                                }
                            }
                            int totalPages = (_totalRoutes + pageSize - 1) / pageSize;
                            _pageInfoLabel.Text = $"Page {page} of {totalPages} ({_totalRoutes} routes)";
                            _prevPageButton.Enabled = page > 1;
                            _nextPageButton.Enabled = page < totalPages;
                            _editRouteButton.Enabled = false;
                            _deleteRouteButton.Enabled = false;
                        });                    }
                    catch (ObjectDisposedException)
                    {
                        Console.WriteLine("RouteListPanel disposed during UI refresh");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"UI update error: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("LoadRoutesAsync canceled by OperationCanceledException");
            }
            catch (HttpRequestException ex)
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    try
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (!IsDisposed)
                                MessageBox.Show($"Network error loading routes: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
            }
            catch (InvalidOperationException ex)
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    try
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (!IsDisposed)
                                MessageBox.Show($"Error loading routes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    try
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (!IsDisposed)
                                MessageBox.Show($"Unexpected error loading routes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
            }
        }

        public void RefreshRoutesList()
        {
            LoadRoutesAsync(_currentPage, _pageSize, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override void Render(Control container)
        {
            if (container == null) return;
            container.Controls.Clear();
            container.Controls.Add(this);
            this.Dock = DockStyle.Fill;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _cancellationTokenSource?.Cancel(); } catch { }
                _cancellationTokenSource?.Dispose();
                _routesGrid?.Dispose();
                _prevPageButton?.Dispose();
                _nextPageButton?.Dispose();
                _pageInfoLabel?.Dispose();
                _addRouteButton?.Dispose();
                _editRouteButton?.Dispose();
                _deleteRouteButton?.Dispose();
                _titleLabel?.Dispose();            }
            base.Dispose(disposing);
        }

        // Method added for test compatibility
        private void ShowRoutePanel(Route route)
        {
            // Convert Route to RouteDisplayDTO
            var routeDto = RouteDisplayDTO.FromRoute(route);
            // Trigger the edit event
            RouteEditRequested?.Invoke(this, new RouteEventArgs(routeDto));
        }
    }
}

