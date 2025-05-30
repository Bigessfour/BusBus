// Enable nullable reference types for this file
#nullable enable
using BusBus.Models;
using BusBus.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.UI.Common;
using System.Drawing;
using System.Linq;
using System.ComponentModel;

namespace BusBus.UI
{
    /// <summary>
    /// Routes management tab with Crystal Dark styling and CRUD operations
    /// </summary>
    public partial class RoutesManagementPanel : ThemeableControl, IView
    {
        private readonly IRouteService _routeService;
        private readonly IDriverService? _driverService;
        private readonly IVehicleService? _vehicleService;

        // Data collections
        private BindingList<RouteDisplayDTO> _routes = new BindingList<RouteDisplayDTO>();
        private List<Driver> _drivers = new List<Driver>();
        private List<Vehicle> _vehicles = new List<Vehicle>();

        // UI Controls
        private DataGridView _routesGrid = null!;
        private Button _addRouteButton = null!;
        private Button _editRouteButton = null!;
        private Button _deleteRouteButton = null!;
        private Label _titleLabel = null!;

        // Pagination and state
        private int _totalRoutes = 0;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // IView interface implementation
        public string ViewName => "routes";
        public string Title => "Routes";
        public Control? Control => this;

        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        public event EventHandler<StatusEventArgs>? StatusUpdated;

        public RoutesManagementPanel(IRouteService routeService, IDriverService? driverService = null, IVehicleService? vehicleService = null)
        {
            ArgumentNullException.ThrowIfNull(routeService);
            _routeService = routeService;
            _driverService = driverService;
            _vehicleService = vehicleService;

            InitializeComponent();
            SetupEventHandlers();
            _ = InitializeDataAsync();
        }

        private void InitializeComponent()
        {
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
            this.Padding = new Padding(10);
            this.Dock = DockStyle.Fill;
            ThemeManager.EnforceGlassmorphicTextColor(this);

            // Title label
            _titleLabel = new Label
            {
                Text = "Route Management",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
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
                RowCount = 2,
                BackColor = ThemeManager.CurrentTheme.CardBackground
            };
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.Controls.Add(mainContainer);

            // Create DataGridView
            CreateDataGridView();
            mainContainer.Controls.Add(_routesGrid, 0, 0);

            // Create CRUD buttons
            CreateCrudButtons();
            var buttonPanel = CreateButtonPanel();
            mainContainer.Controls.Add(buttonPanel, 0, 1);
        }

        private void CreateDataGridView()
        {
            _routesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(200, 200, 200),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 40,
                RowHeadersVisible = false,
                RowTemplate = { Height = 32 },
                EditMode = DataGridViewEditMode.EditOnEnter
            };

            // Apply theme styling
            ThemeManager.CurrentTheme.StyleDataGrid(_routesGrid);

            // Enhance header styling
            _routesGrid.ColumnHeadersDefaultCellStyle.Font = new Font(_routesGrid.Font.FontFamily, 9.5F, FontStyle.Bold);
            _routesGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 8, 8, 8);
            _routesGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Define columns
            _routesGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                // Hidden Primary Key
                new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false },

                // Date with DD-MM-YY format
                new DataGridViewTextBoxColumn {
                    HeaderText = "Date",
                    DataPropertyName = "RouteDate",
                    FillWeight = 100,
                    MinimumWidth = 100,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "dd-MM-yy" }
                },

                // Route Name ComboBox
                new DataGridViewComboBoxColumn {
                    HeaderText = "Route Name",
                    DataPropertyName = "Name",
                    Name = "RouteNameColumn",
                    FillWeight = 150,
                    MinimumWidth = 120,
                    Items = { "Truck Plaza", "East", "West", "SPED" }
                },

                // Bus ComboBox (active Vehicles)
                new DataGridViewComboBoxColumn {
                    HeaderText = "Bus",
                    Name = "BusColumn",
                    DataPropertyName = "VehicleId",
                    FillWeight = 100,
                    MinimumWidth = 80,
                    DisplayMember = "BusNumber",
                    ValueMember = "VehicleId"
                },

                // AM Begin Mileage
                new DataGridViewTextBoxColumn {
                    HeaderText = "AM Begin",
                    DataPropertyName = "AMStartingMileage",
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight },
                    FillWeight = 80,
                    MinimumWidth = 80
                },

                // AM End Mileage
                new DataGridViewTextBoxColumn {
                    HeaderText = "AM End",
                    DataPropertyName = "AMEndingMileage",
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight },
                    FillWeight = 80,
                    MinimumWidth = 80
                },

                // AM Riders
                new DataGridViewTextBoxColumn {
                    HeaderText = "AM Riders",
                    DataPropertyName = "AMRiders",
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight },
                    FillWeight = 70,
                    MinimumWidth = 70
                },

                // AM Driver ComboBox
                new DataGridViewComboBoxColumn {
                    HeaderText = "AM Driver",
                    Name = "AMDriverColumn",
                    DataPropertyName = "AMDriverId",
                    FillWeight = 120,
                    MinimumWidth = 100,
                    DisplayMember = "Name",
                    ValueMember = "Id"
                },

                // PM Begin Mileage
                new DataGridViewTextBoxColumn {
                    HeaderText = "PM Begin",
                    DataPropertyName = "PMStartMileage",
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight },
                    FillWeight = 80,
                    MinimumWidth = 80
                },

                // PM End Mileage
                new DataGridViewTextBoxColumn {
                    HeaderText = "PM End",
                    DataPropertyName = "PMEndingMileage",
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight },
                    FillWeight = 80,
                    MinimumWidth = 80
                },

                // PM Riders
                new DataGridViewTextBoxColumn {
                    HeaderText = "PM Riders",
                    DataPropertyName = "PMRiders",
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight },
                    FillWeight = 70,
                    MinimumWidth = 70
                },

                // PM Driver ComboBox
                new DataGridViewComboBoxColumn {
                    HeaderText = "PM Driver",
                    Name = "PMDriverColumn",
                    DataPropertyName = "PMDriverId",
                    FillWeight = 120,
                    MinimumWidth = 100,
                    DisplayMember = "Name",
                    ValueMember = "Id"
                }
            });

            // Bind data
            _routesGrid.DataSource = _routes;
        }

        private void CreateCrudButtons()
        {
            _addRouteButton = CreateCrystalDarkButton("Add New Route", new Size(120, 35));
            _editRouteButton = CreateCrystalDarkButton("Edit Selected", new Size(100, 35));
            _deleteRouteButton = CreateCrystalDarkButton("Delete", new Size(80, 35));
        }

        /// <summary>
        /// Creates a Crystal Dark glass-like button with specified text and size
        /// </summary>
        private Button CreateCrystalDarkButton(string text, Size size)
        {
            var button = new Button
            {
                Text = text,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 45),              // Dark translucent background
                ForeColor = Color.FromArgb(220, 220, 220),           // Light text
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(70, 130, 180);  // Steel blue accent

            // Hover effects
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = Color.FromArgb(60, 60, 70);       // Lighter on hover
                button.FlatAppearance.BorderColor = Color.FromArgb(100, 150, 200);
            };

            button.MouseLeave += (s, e) =>
            {
                button.BackColor = Color.FromArgb(40, 40, 45);       // Return to original
                button.FlatAppearance.BorderColor = Color.FromArgb(70, 130, 180);
            };

            return button;
        }

        private Panel CreateButtonPanel()
        {
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 50,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Padding = new Padding(5)
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            buttonPanel.Controls.Add(_addRouteButton, 0, 0);
            buttonPanel.Controls.Add(_editRouteButton, 1, 0);
            buttonPanel.Controls.Add(_deleteRouteButton, 2, 0);

            return buttonPanel;
        }

        private void SetupEventHandlers()
        {
            // Mileage validation
            _routesGrid.CellEndEdit += OnCellEndEdit;

            // CRUD button events
            _addRouteButton.Click += OnAddRouteClick;
            _editRouteButton.Click += OnEditRouteClick;
            _deleteRouteButton.Click += OnDeleteRouteClick;
        }

        private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            ValidateMileageEntry(e);
        }

        private async void OnAddRouteClick(object? sender, EventArgs e)
        {
            try
            {
                // Create new route with default values
                var newRoute = new RouteDisplayDTO
                {
                    Id = Guid.NewGuid(),
                    RouteDate = DateTime.Today,
                    Name = "Truck Plaza", // Default selection
                    VehicleId = _vehicles.FirstOrDefault()?.VehicleId ?? 0,
                    AMStartingMileage = 0,
                    AMEndingMileage = 0,
                    AMRiders = 0,
                    PMStartMileage = 0,
                    PMEndingMileage = 0,
                    PMRiders = 0
                };

                // Add to service
                var route = newRoute.ToRoute();
                var createdRoute = await _routeService.CreateRouteAsync(route);

                // Add to local collection
                var displayRoute = RouteDisplayDTO.FromRoute(createdRoute);
                _routes.Add(displayRoute);

                // Select the new row
                _routesGrid.ClearSelection();
                var newIndex = _routes.Count - 1;
                if (newIndex >= 0)
                {
                    _routesGrid.Rows[newIndex].Selected = true;
                    _routesGrid.CurrentCell = _routesGrid.Rows[newIndex].Cells[1]; // Select first editable cell
                }

                StatusUpdated?.Invoke(this, new StatusEventArgs("New route added successfully.", StatusType.Success));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding route: {ex.Message}", "Add Route Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error adding route: {ex.Message}", StatusType.Error));
            }
        }

        private async void OnEditRouteClick(object? sender, EventArgs e)
        {
            if (_routesGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a route to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var selectedIndex = _routesGrid.SelectedRows[0].Index;
                if (selectedIndex >= 0 && selectedIndex < _routes.Count)
                {
                    var route = _routes[selectedIndex];

                    // Update route in service
                    await _routeService.UpdateRouteAsync(route.ToRoute());

                    StatusUpdated?.Invoke(this, new StatusEventArgs("Route updated successfully.", StatusType.Success));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating route: {ex.Message}", "Update Route Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error updating route: {ex.Message}", StatusType.Error));
            }
        }

        private async void OnDeleteRouteClick(object? sender, EventArgs e)
        {
            if (_routesGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a route to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedIndex = _routesGrid.SelectedRows[0].Index;
            if (selectedIndex >= 0 && selectedIndex < _routes.Count)
            {
                var route = _routes[selectedIndex];
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the route '{route.Name}' on {route.RouteDate:dd-MM-yy}?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await _routeService.DeleteRouteAsync(route.Id);
                        _routes.RemoveAt(selectedIndex);

                        StatusUpdated?.Invoke(this, new StatusEventArgs("Route deleted successfully.", StatusType.Success));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting route: {ex.Message}", "Delete Route Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        StatusUpdated?.Invoke(this, new StatusEventArgs($"Error deleting route: {ex.Message}", StatusType.Error));
                    }
                }
            }
        }

        /// <summary>
        /// Validates mileage entries and shows warnings for invalid data
        /// </summary>
        private void ValidateMileageEntry(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _routes.Count) return;

            var route = _routes[e.RowIndex];
            var columnName = _routesGrid.Columns[e.ColumnIndex].DataPropertyName;

            switch (columnName)
            {
                case "AMEndingMileage":
                    if (route.AMEndingMileage > 0 && route.AMStartingMileage > 0 &&
                        route.AMEndingMileage < route.AMStartingMileage)
                    {
                        MessageBox.Show("Warning: AM Ending mileage should be greater than AM Starting mileage.",
                            "Mileage Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    break;

                case "PMEndingMileage":
                    if (route.PMEndingMileage > 0 && route.PMStartMileage > 0 &&
                        route.PMEndingMileage < route.PMStartMileage)
                    {
                        MessageBox.Show("Warning: PM Ending mileage should be greater than PM Starting mileage.",
                            "Mileage Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    break;

                case "AMRiders":
                case "PMRiders":
                    var ridersValue = columnName == "AMRiders" ? route.AMRiders : route.PMRiders;
                    if (ridersValue < 0)
                    {
                        MessageBox.Show("Warning: Rider count cannot be negative.",
                            "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    break;
            }
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                await LoadComboBoxDataAsync(_cancellationTokenSource.Token);
                await LoadRoutesAsync(_currentPage, _pageSize, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error loading data: {ex.Message}", StatusType.Error));
            }
        }

        private async Task LoadComboBoxDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Load drivers
                if (_driverService != null)
                {
                    try
                    {
                        _drivers = await _driverService.GetAllDriversAsync(cancellationToken);
                    }
                    catch
                    {
                        // Fallback to sample data
                        _drivers = new List<Driver>
                        {
                            new Driver { Id = Guid.NewGuid(), FirstName = "John", LastName = "Smith" },
                            new Driver { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" },
                            new Driver { Id = Guid.NewGuid(), FirstName = "Mike", LastName = "Johnson" }
                        };
                    }
                }
                else
                {
                    _drivers = new List<Driver>
                    {
                        new Driver { Id = Guid.NewGuid(), FirstName = "John", LastName = "Smith" },
                        new Driver { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" },
                        new Driver { Id = Guid.NewGuid(), FirstName = "Mike", LastName = "Johnson" }
                    };
                }

                // Load vehicles (only active ones)
                if (_vehicleService != null)
                {
                    try
                    {
                        var allVehicles = await _vehicleService.GetAllVehiclesAsync(cancellationToken);
                        _vehicles = allVehicles.Where(v => v.Status == "Active").ToList();
                    }
                    catch
                    {
                        // Fallback to sample data
                        _vehicles = new List<Vehicle>
                        {
                            new Vehicle { VehicleId = 1, BusNumber = "Bus 001", Status = "Active" },
                            new Vehicle { VehicleId = 2, BusNumber = "Bus 002", Status = "Active" },
                            new Vehicle { VehicleId = 3, BusNumber = "Bus 003", Status = "Active" }
                        };
                    }
                }
                else
                {
                    _vehicles = new List<Vehicle>
                    {
                        new Vehicle { VehicleId = 1, BusNumber = "Bus 001", Status = "Active" },
                        new Vehicle { VehicleId = 2, BusNumber = "Bus 002", Status = "Active" },
                        new Vehicle { VehicleId = 3, BusNumber = "Bus 003", Status = "Active" }
                    };
                }

                // Update ComboBox data sources
                UpdateComboBoxDataSources();
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Warning: Could not load ComboBox data: {ex.Message}", StatusType.Warning));
            }
        }

        private void UpdateComboBoxDataSources()
        {
            // Update Bus ComboBox
            var busColumn = _routesGrid.Columns["BusColumn"] as DataGridViewComboBoxColumn;
            if (busColumn != null)
            {
                busColumn.DataSource = _vehicles.ToList();
                busColumn.DisplayMember = "BusNumber";
                busColumn.ValueMember = "VehicleId";
            }

            // Update Driver ComboBoxes
            var driversWithFullName = _drivers.Select(d => new
            {
                Id = d.Id,
                Name = $"{d.FirstName} {d.LastName}"
            }).ToList();

            var amDriverColumn = _routesGrid.Columns["AMDriverColumn"] as DataGridViewComboBoxColumn;
            if (amDriverColumn != null)
            {
                amDriverColumn.DataSource = driversWithFullName.ToList();
                amDriverColumn.DisplayMember = "Name";
                amDriverColumn.ValueMember = "Id";
            }

            var pmDriverColumn = _routesGrid.Columns["PMDriverColumn"] as DataGridViewComboBoxColumn;
            if (pmDriverColumn != null)
            {
                pmDriverColumn.DataSource = driversWithFullName.ToList();
                pmDriverColumn.DisplayMember = "Name";
                pmDriverColumn.ValueMember = "Id";
            }
        }

        private async Task LoadRoutesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var routes = await _routeService.GetRoutesAsync(page, pageSize, cancellationToken);
                var displayRoutes = routes.Select(RouteDisplayDTO.FromRoute).ToList();

                _routes.Clear();
                foreach (var route in displayRoutes)
                {
                    _routes.Add(route);
                }

                StatusUpdated?.Invoke(this, new StatusEventArgs($"Loaded {routes.Count} routes.", StatusType.Success));
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error loading routes: {ex.Message}", StatusType.Error));
            }
        }

        public async Task RefreshAsync()
        {
            await LoadRoutesAsync(_currentPage, _pageSize, _cancellationTokenSource.Token);
        }

        public void Show()
        {
            this.Visible = true;
        }

        public void Hide()
        {
            this.Visible = false;
        }

        // Add IView interface implementation
        public async Task ActivateAsync(CancellationToken cancellationToken)
        {
            await RefreshAsync();
        }

        public Task DeactivateAsync()
        {
            _cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }

        public override void Render(Control parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            parent.Controls.Clear();
            parent.Controls.Add(this);
            Dock = DockStyle.Fill;
        }

        #region IDisposable Support
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion


