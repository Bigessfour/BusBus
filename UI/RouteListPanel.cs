using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI
{
    public class RouteEventArgs : EventArgs
    {
        public Route Route { get; set; }

        public RouteEventArgs(Route route)
        {
            Route = route;
        }
    }

#pragma warning disable CA2213 // Disposable fields should be disposed
    public partial class RouteListPanel : ThemeableControl, IDisplayable
    // Removed unused field _disposedValue and related pragma
    {
        // Add event for route editing with proper EventArgs pattern
        public event EventHandler<RouteEventArgs>? RouteEditRequested;

        private bool _disposed = false;

#pragma warning disable CS0649 // Field is never assigned
        private Task? _initialLoadTask;
#pragma warning restore CS0649
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly EventHandler _selectionChangedHandler;        private readonly List<Task> _backgroundTasks = new List<Task>();
        private readonly DataGridViewCellFormattingEventHandler _cellFormattingHandler;
        private readonly DataGridViewCellEventHandler _cellDoubleClickHandler;
        private readonly DataGridViewCellEventHandler _cellEndEditHandler;
        private readonly IRouteService _routeService;
        private readonly DataGridView _routesGrid;

        // For test/mocking: allow injection of details panel and map view
        public IRouteDetailsPanel? RouteDetailsPanel { get; set; }
        public IMapView? MapView { get; set; }

        /// <summary>
        /// Exposes the routes DataGridView for testing purposes.
        /// </summary>
        public DataGridView RoutesGrid => _routesGrid;
        private readonly Button _prevPageButton;
        private readonly Button _nextPageButton;
        private readonly Label _pageInfoLabel;
        private readonly Button _addRouteButton;
        private readonly Button _editRouteButton;
        private readonly Button _deleteRouteButton; // Properly name the button
        private readonly Label _titleLabel; // Add title label
        private bool _disposedValue;
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRoutes;
        private List<BusBus.Models.Route> _routes = new List<BusBus.Models.Route>();
        private List<Driver>? _drivers;
        private List<Vehicle>? _vehicles;

        public RouteListPanel(IRouteService routeService)

        {
            ArgumentNullException.ThrowIfNull(routeService);
            _routeService = routeService;
            _selectionChangedHandler = (s, e) => { };            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
            this.Padding = new Padding(0); // Remove padding for edge-to-edge layout

            // Create title label
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

            // Create a container for the grid and pagination
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = ThemeManager.CurrentTheme.CardBackground
            };
            // Using AutoSize for the button row for better scaling
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Grid
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Pagination
            this.Controls.Add(mainContainer);

            // Initialize all buttons before adding to layout
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

            // Create button panel with centered buttons
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

            // Setup DataGridView with optimized settings
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
                GridColor = System.Drawing.Color.FromArgb(200, 200, 200), // Lighter grid lines
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular),
                    BackColor = System.Drawing.Color.FromArgb(245, 245, 245), // Light gray background
                    ForeColor = System.Drawing.Color.Black,
                    SelectionBackColor = System.Drawing.Color.FromArgb(100, 150, 255), // Soft blue selection
                    SelectionForeColor = System.Drawing.Color.Black,
                    Padding = new Padding(3),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = System.Drawing.Color.FromArgb(235, 235, 235) // Slightly darker alternating rows
                },
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                    BackColor = System.Drawing.Color.FromArgb(100, 130, 180), // Blue header background
                    ForeColor = System.Drawing.Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = new Padding(3)
                },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowHeadersVisible = false, // Hide row headers for cleaner look
                RowTemplate = { Height = 32 }, // Slightly taller rows for better readability
                EditMode = DataGridViewEditMode.EditOnEnter, // Start edit when cell is entered
                ReadOnly = false // Allow editing
            };
            mainContainer.Controls.Add(_routesGrid, 0, 1);

            // Optimized columns for the grid
            var nameCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "Route Name",
                DataPropertyName = "Name",
                FillWeight = 150,
                MinimumWidth = 120
            };

            var dateCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "Date",
                DataPropertyName = "RouteDate",
                FillWeight = 100,
                MinimumWidth = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "d" }
            };

            var amStartCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "AM Start",
                DataPropertyName = "AMStartingMileage",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N0",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                },
                FillWeight = 80,
                MinimumWidth = 80
            };

            var amEndCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "AM End",
                DataPropertyName = "AMEndingMileage",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N0",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                },
                FillWeight = 80,
                MinimumWidth = 80
            };

            var pmStartCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "PM Start",
                DataPropertyName = "PMStartMileage",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N0",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                },
                FillWeight = 80,
                MinimumWidth = 80
            };

            var pmEndCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "PM End",
                DataPropertyName = "PMEndingMileage",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N0",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                },
                FillWeight = 80,
                MinimumWidth = 80
            };

            var amMilesCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "AM Miles",
                Name = "AMTotalMiles",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N1",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                },
                FillWeight = 70,
                MinimumWidth = 70
            };

            var pmMilesCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "PM Miles",
                Name = "PMTotalMiles",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N1",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                },
                FillWeight = 70,
                MinimumWidth = 70
            };

            var driverCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "Driver",
                Name = "DriverName",
                FillWeight = 120,
                MinimumWidth = 100,
                ReadOnly = true
            };

            var vehicleCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "Vehicle",
                Name = "VehicleNumber",
                FillWeight = 80,
                MinimumWidth = 80,
                ReadOnly = true
            };

            // Add columns to grid
            _routesGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                nameCol, dateCol, amStartCol, amEndCol, amMilesCol,
                pmStartCol, pmEndCol, pmMilesCol, driverCol, vehicleCol
            });

            // Create pagination panel
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

            // Initialize the event handlers
            _cellFormattingHandler = OnCellFormatting;
            _cellDoubleClickHandler = OnCellDoubleClick;
            _cellEndEditHandler = OnCellEndEdit;

            // Event handlers
            _routesGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                var colName = _routesGrid.Columns[e.ColumnIndex].Name;

                // Calculate and display AM miles
                if (colName == "AMTotalMiles" && e.RowIndex < _routes.Count)
                {
                    var route = _routes[e.RowIndex];
                    e.Value = Math.Max(0, route.AMEndingMileage - route.AMStartingMileage);
                    e.FormattingApplied = true;
                }
                // Calculate and display PM miles
                else if (colName == "PMTotalMiles" && e.RowIndex < _routes.Count)
                {
                    var route = _routes[e.RowIndex];
                    e.Value = Math.Max(0, route.PMEndingMileage - route.PMStartMileage);
                    e.FormattingApplied = true;
                }
                // Display driver name
                else if (colName == "DriverName" && e.RowIndex < _routes.Count)
                {
                    var route = _routes[e.RowIndex];
                    if (route.Driver != null)
                    {
                        e.Value = $"{route.Driver.FirstName} {route.Driver.LastName}".Trim();
                    }
                    else
                    {
                        e.Value = "Unassigned";
                    }
                    e.FormattingApplied = true;
                }
                // Display vehicle number
                else if (colName == "VehicleNumber" && e.RowIndex < _routes.Count)
                {
                    var route = _routes[e.RowIndex];
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
            };            // Double-click to edit
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
                }
                try
                {
                    await _routeService.UpdateRouteAsync(route);
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
                // No general catch (Exception) after specific ones to avoid CS0160
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
            {
                var newRoute = new Route
                {
                    Id = Guid.Empty, // Mark as new route
                    Name = "New Route",
                    RouteDate = DateTime.Today
                };
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
            {                if (_currentPage * _pageSize < _totalRoutes)
                {
                    _currentPage++;
                    await LoadRoutesAsync(_currentPage, _pageSize, CancellationToken.None);
                }            };

            // Initialize data loading properly without background Task.Run
            InitializeDataAsync();
        }

        private async void InitializeDataAsync()
        {
            try
            {
                // Check if we've been disposed before starting work
                if (IsDisposed || _routeService == null)
                    return;

                var token = _cancellationTokenSource.Token;

                try
                {
                    // Check for cancellation frequently
                    token.ThrowIfCancellationRequested();

                    // Get drivers and vehicles info with null checks
                    try
                    {
                        _drivers = await _routeService.GetDriversAsync(token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading drivers: {ex.Message}");
                        _drivers = new List<Driver>(); // Fallback to empty list
                    }

                    token.ThrowIfCancellationRequested();

                    try
                    {
                        _vehicles = await _routeService.GetVehiclesAsync(token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading vehicles: {ex.Message}");
                        _vehicles = new List<Vehicle>(); // Fallback to empty list
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Initial data loading was canceled");
                    return; // Exit early if canceled
                }

                // Only continue if the control is not disposed and not canceled
                if (!IsDisposed && !token.IsCancellationRequested)
                {
                    await LoadRoutesAsync(1, _pageSize, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Task was canceled, normal behavior during shutdown
                Console.WriteLine("RouteListPanel initialization canceled");
            }
            catch (ObjectDisposedException)
            {
                // Object was disposed, normal behavior during shutdown
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

        // Method to load routes with pagination
        public async Task LoadRoutesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                // Early check for cancellation or disposal
                if (cancellationToken.IsCancellationRequested || IsDisposed)
                {
                    Console.WriteLine("LoadRoutesAsync canceled or control disposed");
                    return;
                }

                _currentPage = page;
                _pageSize = pageSize;

                // Lazy load drivers and vehicles if they're not already loaded
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
                }

                cancellationToken.ThrowIfCancellationRequested();
                _routes = await _routeService.GetRoutesAsync(page, pageSize, cancellationToken);

                if (cancellationToken.IsCancellationRequested || IsDisposed)
                {
                    Console.WriteLine("LoadRoutesAsync canceled before updating UI");
                    return;
                }

                // Update UI on UI thread, but only if handle is created and not disposed
                if (IsHandleCreated && !IsDisposed)
                {
                    try
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (IsDisposed) return;
                            // Clear and reload grid
                            _routesGrid.DataSource = null;
                            _routesGrid.DataSource = _routes;

                            // Enable editing only for driver and vehicle columns
                            _routesGrid.ReadOnly = false;
                            foreach (DataGridViewColumn col in _routesGrid.Columns)
                            {
                                if (col.Name != "DriverId" && col.Name != "VehicleId")
                                {
                                    col.ReadOnly = true;
                                }
                            }

                            // Update pagination UI
                            int totalPages = (_totalRoutes + pageSize - 1) / pageSize;
                            _pageInfoLabel.Text = $"Page {page} of {totalPages} ({_totalRoutes} routes)";

                            // Enable/disable navigation buttons
                            _prevPageButton.Enabled = page > 1;
                            _nextPageButton.Enabled = page < totalPages;

                            // Initialize button states
                            _editRouteButton.Enabled = false;
                            _deleteRouteButton.Enabled = false;
                        });
                    }
                    catch (ObjectDisposedException)
                    {
                        Console.WriteLine("RouteListPanel was disposed during UI update");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"InvalidOperationException during UI update: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Task was canceled, normal behavior during shutdown
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

        // Method to show route panel for a selected route
        private void ShowRoutePanel(Route route)
        {
            RouteEditRequested?.Invoke(this, new RouteEventArgs(route));
        }

        /// <summary>
        /// Synchronously refreshes the routes list for test and UI purposes.
        /// </summary>
        public void RefreshRoutesList()
        {
            // Use default pagination values or current ones
            // Wait for async method to complete (for test scenarios)
            LoadRoutesAsync(_currentPage, _pageSize, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override void Render(Control container)
        {
            if (container == null) return;
            container.Controls.Clear();
            container.Controls.Add(this);
            this.Dock = DockStyle.Fill;
        }        // Override dispose to properly clean up resources        private bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                Console.WriteLine("RouteListPanel being disposed...");
                
                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }

                // Wait for tasks with timeout
                if (_initialLoadTask != null && !_initialLoadTask.IsCompleted)
                {
                    try
                    {
                        _initialLoadTask.Wait(TimeSpan.FromMilliseconds(500));
                    }
                    catch (AggregateException)
                    {
                        // Task was cancelled or timed out
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed, ignore
                    }
                }

                foreach (var task in _backgroundTasks.ToList())
                {
                    if (!task.IsCompleted)
                    {
                        try
                        {
                            task.Wait(TimeSpan.FromMilliseconds(500));
                        }
                        catch (AggregateException)
                        {
                            // Task was cancelled or timed out
                        }
                    }
                }

                _cancellationTokenSource?.Dispose();
                _routesGrid?.Dispose();
                _prevPageButton?.Dispose();
                _nextPageButton?.Dispose();
                _pageInfoLabel?.Dispose();
                _addRouteButton?.Dispose();
                _editRouteButton?.Dispose();
                _deleteRouteButton?.Dispose();
                _titleLabel?.Dispose();
            }
            base.Dispose(disposing);
        }

        private static void LogError(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[RouteListPanel ERROR] {message}");
        }

        private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // Handle cell formatting
            if (e.Value != null && e.ColumnIndex >= 0)
            {
                // Add formatting logic as needed
            }
        }

        private void OnCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Handle cell double click
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // Add double click logic as needed
            }
        }

        private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            // Handle cell end edit
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // Add end edit logic as needed
            }
        }
    }
}