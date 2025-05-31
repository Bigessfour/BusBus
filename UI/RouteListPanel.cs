#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Core;

namespace BusBus.UI
{
    /// <summary>
    /// Panel for displaying and managing a list of routes with CRUD operations
    /// </summary>
    public partial class RouteListPanel : ThemeableControl, IDisplayable, IView
    {
        private readonly IRouteService _routeService;
        private readonly IDriverService _driverService;
        private readonly IVehicleService _vehicleService;
        private DataGridView _routesGrid = null!;
        private Button _addRouteButton = null!;
        private Button _editRouteButton = null!;
        private Button _deleteRouteButton = null!;
        private Button _prevPageButton = null!;
        private Button _nextPageButton = null!;
        private Label _pageInfoLabel = null!;
        private Label _titleLabel = null!;
        private TableLayoutPanel _mainLayout = null!;
        private Panel _buttonPanel = null!;
        private Panel _paginationPanel = null!;

        private BindingList<RouteDisplayDTO> _routes = new BindingList<RouteDisplayDTO>();
        private List<Driver> _drivers = new List<Driver>();
        private List<Vehicle> _vehicles = new List<Vehicle>();
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalRoutes = 0;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public string ViewName => "routes";
        public string Title => "Routes";
        public Control? Control => this;

        // Required by IView, but not used in this implementation
        public event EventHandler<NavigationEventArgs>? NavigationRequested { add { } remove { } }
        public event EventHandler<StatusEventArgs>? StatusUpdated;
        // Removed unused event RouteEditRequested to resolve CS0067 warning

        public RouteListPanel(IRouteService routeService, IDriverService driverService, IVehicleService vehicleService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));

            InitializeComponent();

            // Apply reader mode if needed
            if (IsReaderMode)
            {
                ConfigureReaderMode();
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Main layout
            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(10)
            };
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Title
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Buttons
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Grid
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Pagination

            // Title
            _titleLabel = new Label
            {
                Text = "Route Management",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 220),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _mainLayout.Controls.Add(_titleLabel, 0, 0);

            // Button panel
            _buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 5)
            };

            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _addRouteButton = CreateCrystalDarkButton("Add Route", Color.FromArgb(70, 130, 180));
            _editRouteButton = CreateCrystalDarkButton("Edit Route", Color.FromArgb(100, 140, 180));
            _deleteRouteButton = CreateCrystalDarkButton("Delete Route", Color.FromArgb(180, 70, 70));

            _addRouteButton.Click += OnAddRouteClick;
            _editRouteButton.Click += OnEditRouteClick;
            _deleteRouteButton.Click += OnDeleteRouteClick;

            buttonLayout.Controls.AddRange(new Control[] { _addRouteButton, _editRouteButton, _deleteRouteButton });
            _buttonPanel.Controls.Add(buttonLayout);
            _mainLayout.Controls.Add(_buttonPanel, 0, 1);

            // Routes grid
            _routesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(45, 45, 48),
                BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(70, 70, 75),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 45),
                    ForeColor = Color.FromArgb(220, 220, 220),
                    SelectionBackColor = Color.FromArgb(70, 130, 180),
                    SelectionForeColor = Color.White
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 50, 55),
                    ForeColor = Color.FromArgb(220, 220, 220),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                },
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false
            };

            _routesGrid.CellDoubleClick += (s, e) => OnEditRouteClick(s, e);
            _mainLayout.Controls.Add(_routesGrid, 0, 2);

            // Pagination panel
            _paginationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 40)
            };

            var paginationLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            paginationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            paginationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            paginationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

            _prevPageButton = CreateCrystalDarkButton("Previous", Color.FromArgb(70, 130, 180));
            _pageInfoLabel = new Label
            {
                Text = "Page 1 of 1",
                ForeColor = Color.FromArgb(220, 220, 220),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            _nextPageButton = CreateCrystalDarkButton("Next", Color.FromArgb(70, 130, 180));

            _prevPageButton.Click += async (s, e) => await ChangePage(-1);
            _nextPageButton.Click += async (s, e) => await ChangePage(1);

            paginationLayout.Controls.Add(_prevPageButton, 0, 0);
            paginationLayout.Controls.Add(_pageInfoLabel, 1, 0);
            paginationLayout.Controls.Add(_nextPageButton, 2, 0);

            _paginationPanel.Controls.Add(paginationLayout);
            _mainLayout.Controls.Add(_paginationPanel, 0, 3);

            Controls.Add(_mainLayout);
            SetupGridColumns();

            ResumeLayout(false);
            PerformLayout();
        }

        private void SetupGridColumns()
        {
            _routesGrid.Columns.Clear();
            _routesGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "RouteNumber",
                    HeaderText = "Route Number",
                    DataPropertyName = "RouteNumber",
                    Width = 120,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "RouteName",
                    HeaderText = "Route Name",
                    DataPropertyName = "RouteName",
                    Width = 200,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "StartLocation",
                    HeaderText = "Start Location",
                    DataPropertyName = "StartLocation",
                    Width = 180,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "EndLocation",
                    HeaderText = "End Location",
                    DataPropertyName = "EndLocation",
                    Width = 180,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Distance",
                    HeaderText = "Distance (miles)",
                    DataPropertyName = "Distance",
                    Width = 120,
                    ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Duration",
                    HeaderText = "Duration",
                    DataPropertyName = "Duration",
                    Width = 100,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "VehicleAssignment",
                    HeaderText = "Vehicle",
                    DataPropertyName = "VehicleAssignment",
                    Width = 120,
                    ReadOnly = true
                }
            });
        }

        private static Button CreateCrystalDarkButton(string text, Color accentColor)
        {
            return new Button
            {
                Text = text,
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.FromArgb(220, 220, 220),
                FlatAppearance =
                {
                    BorderColor = accentColor,
                    BorderSize = 1,
                    MouseOverBackColor = Color.FromArgb(60, 60, 65),
                    MouseDownBackColor = Color.FromArgb(35, 35, 40)
                },
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 5, 0)
            };
        }

        public async Task LoadDataAsync()
        {
            try
            {
                StatusUpdated?.Invoke(this, new StatusEventArgs("Loading routes...", StatusType.Info));

                // Load drivers and vehicles for display
                await LoadDriversAsync();
                await LoadVehiclesAsync();

                // Get routes with pagination
                var pagedResult = await _routeService.GetPagedAsync(_currentPage, _pageSize, _cancellationTokenSource.Token);
                _totalRoutes = pagedResult.TotalCount;

                // Convert to display DTOs
                _routes.Clear();
                foreach (var route in pagedResult.Items)
                {
                    var dto = new RouteDisplayDTO
                    {
                        RouteNumber = route.RouteCode ?? $"RT{route.RouteID:D4}",
                        RouteName = route.Name,
                        StartLocation = route.StartLocation,
                        EndLocation = route.EndLocation,
                        Distance = (decimal)route.TotalMiles,
                        // Duration = route.EstimatedDuration, // Schedule scrubbed
                        VehicleId = route.VehicleId,
                        VehicleAssignment = GetVehicleDisplay(route.VehicleId)
                    };
                    _routes.Add(dto);
                }

                _routesGrid.DataSource = _routes;
                UpdatePaginationControls();

                StatusUpdated?.Invoke(this, new StatusEventArgs($"Loaded {_routes.Count} routes", StatusType.Success));
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error loading routes: {ex.Message}", StatusType.Error));
                LoadSampleData();
            }
        }

        private async Task LoadDriversAsync()
        {
            try
            {
                var pagedDrivers = await _driverService.GetPagedAsync(1, 1000, _cancellationTokenSource.Token);
                _drivers = pagedDrivers.ToList(); // Fixed: List<T> does not have Items property
            }
            catch
            {
                _drivers = GetSampleDrivers();
            }
        }

        private async Task LoadVehiclesAsync()
        {
            try
            {
                var pagedVehicles = await _vehicleService.GetPagedAsync(1, 1000, _cancellationTokenSource.Token);
                _vehicles = pagedVehicles.ToList(); // Fixed: List<T> does not have Items property
            }
            catch
            {
                _vehicles = GetSampleVehicles();
            }
        }

        private string GetVehicleDisplay(Guid? vehicleId)
        {
            if (!vehicleId.HasValue) return "Unassigned";
            var vehicle = _vehicles.FirstOrDefault(v => v.Id == vehicleId.Value);
            return vehicle != null ? $"Bus #{vehicle.Number}" : "Unknown";
        }

        private void LoadSampleData()
        {
            _routes.Clear();
            _routes.Add(new RouteDisplayDTO
            {
                RouteNumber = "RT001",
                RouteName = "North Elementary",
                StartLocation = "Bus Depot",
                EndLocation = "North Elementary School",
                Distance = 15.5m,
                Duration = TimeSpan.FromMinutes(45),
                VehicleAssignment = "Bus #101"
            });
            _routes.Add(new RouteDisplayDTO
            {
                RouteNumber = "RT002",
                RouteName = "South High Route",
                StartLocation = "Bus Depot",
                EndLocation = "South High School",
                Distance = 22.3m,
                Duration = TimeSpan.FromMinutes(60),
                VehicleAssignment = "Bus #102"
            });
            _routesGrid.DataSource = _routes;
        }

        private static List<Driver> GetSampleDrivers()
        {
            return new List<Driver>
            {
                new Driver { Id = Guid.NewGuid(), FirstName = "John", LastName = "Smith" },
                new Driver { Id = Guid.NewGuid(), FirstName = "Mary", LastName = "Johnson" }
            };
        }

        private static List<Vehicle> GetSampleVehicles()
        {
            return new List<Vehicle>
            {
                new Vehicle { Id = Guid.NewGuid(), Number = "101", Model = "Blue Bird" },
                new Vehicle { Id = Guid.NewGuid(), Number = "102", Model = "Thomas C2" }
            };
        }

        private async Task ChangePage(int direction)
        {
            var newPage = _currentPage + direction;
            var totalPages = (_totalRoutes + _pageSize - 1) / _pageSize;

            if (newPage >= 1 && newPage <= totalPages)
            {
                _currentPage = newPage;
                await LoadDataAsync();
            }
        }

        private void UpdatePaginationControls()
        {
            var totalPages = Math.Max(1, (_totalRoutes + _pageSize - 1) / _pageSize);
            _pageInfoLabel.Text = $"Page {_currentPage} of {totalPages}";
            _prevPageButton.Enabled = _currentPage > 1;
            _nextPageButton.Enabled = _currentPage < totalPages;
        }

        private async void OnAddRouteClick(object? sender, EventArgs e)
        {
            using var editForm = new RouteEditForm(_routeService, _driverService, _vehicleService, null);
            if (editForm.ShowDialog(this) == DialogResult.OK)
            {
                await LoadDataAsync();
            }
        }

        private async void OnEditRouteClick(object? sender, EventArgs e)
        {
            if (_routesGrid.SelectedRows.Count == 0) return;

            var selectedDto = (RouteDisplayDTO)_routesGrid.SelectedRows[0].DataBoundItem;

            // Find the actual route by code or name
            var route = (await _routeService.GetAllAsync(_cancellationTokenSource.Token))
                .FirstOrDefault(r => r.RouteCode == selectedDto.RouteNumber || r.Name == selectedDto.RouteName);

            if (route != null)
            {
                using var editForm = new RouteEditForm(_routeService, _driverService, _vehicleService, route);
                if (editForm.ShowDialog(this) == DialogResult.OK)
                {
                    await LoadDataAsync();
                }
            }
        }

        private async void OnDeleteRouteClick(object? sender, EventArgs e)
        {
            if (_routesGrid.SelectedRows.Count == 0) return;

            var selectedDto = (RouteDisplayDTO)_routesGrid.SelectedRows[0].DataBoundItem;

            var result = MessageBox.Show(
                $"Are you sure you want to delete route '{selectedDto.RouteName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Find and delete the actual route
                    var route = (await _routeService.GetAllAsync(_cancellationTokenSource.Token))
                        .FirstOrDefault(r => r.RouteCode == selectedDto.RouteNumber || r.Name == selectedDto.RouteName);

                    if (route != null)
                    {
                        await _routeService.DeleteAsync(route.Id, _cancellationTokenSource.Token);
                        StatusUpdated?.Invoke(this, new StatusEventArgs("Route deleted successfully", StatusType.Success));
                        await LoadDataAsync();
                    }
                }
                catch (Exception ex)
                {
                    StatusUpdated?.Invoke(this, new StatusEventArgs($"Error deleting route: {ex.Message}", StatusType.Error));
                }
            }
        }

        public async Task LoadRoutesAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            _currentPage = page;
            _pageSize = pageSize;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await LoadDataAsync();
        }

        protected override void ApplyTheme()
        {
            // Theme is already applied in InitializeComponent
        }

        public Task ActivateAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return LoadDataAsync();
        }

        public Task DeactivateAsync()
        {
            _cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }
        public void Render(Control parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            parent.Controls.Clear();
            parent.Controls.Add(this);
            Dock = DockStyle.Fill;
        }

        private void ConfigureReaderMode()
        {
            // Configure UI for read-only mode by hiding edit controls
            _editRouteButton.Visible = false;
            _deleteRouteButton.Visible = false;
            _addRouteButton.Visible = false;
            _routesGrid.ReadOnly = true;
            _routesGrid.AllowUserToAddRows = false;
            _routesGrid.AllowUserToDeleteRows = false;
        }

        #region Reader Mode Support

        public bool IsReaderMode { get; set; }

        private bool IsReaderModeEnabled()
        {
            // TODO: Implement logic to determine reader mode
            return IsReaderMode;
        }

        #endregion

        #region Event Handlers

        // ...existing code...

        #endregion

        #region IStatefulView Implementation

        // ...existing code...

        #endregion

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
    }
}
