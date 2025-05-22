
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;

namespace BusBus.UI
{
    /// <summary>
    /// A reusable UserControl for displaying and editing route mileage log information.
    /// </summary>
    public class RoutePanel : Panel, IDisplayable
    {
        /// <summary>
        /// Set to true in unit tests to suppress validation dialogs.
        /// </summary>
        /// <summary>
        /// Gets or sets a value indicating whether validation dialogs are suppressed (for unit testing).
        /// </summary>
        public static bool SuppressDialogsForTests { get; set; }
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
        private readonly DateTimePicker _tripDatePicker;
        private readonly NumericUpDown _amStartingMileage;
        private readonly NumericUpDown _amEndingMileage;
        private readonly NumericUpDown _amRiders;
        private readonly NumericUpDown _pmStartMileage;
        private readonly NumericUpDown _pmEndingMileage;
        private readonly NumericUpDown _pmRiders;
        private readonly ComboBox _driverComboBox;
        private readonly ComboBox _vehicleComboBox;
        private readonly Button _saveButton;
        private readonly Button _editButton;
        private readonly Button _deleteButton;
        private readonly TableLayoutPanel _tableLayoutPanel;
        private readonly IRouteService _routeService;
        private readonly DataGridView _routesGrid;
        private readonly Button _prevPageButton;
        private readonly Button _nextPageButton;
        private readonly Label _pageInfoLabel;
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRoutes;
        private bool _disposedValue;
        private List<Driver> _drivers;
        private List<Vehicle> _vehicles;

        public event EventHandler? SaveButtonClicked;
        public event EventHandler? UpdateButtonClicked;
        public event EventHandler? DeleteButtonClicked;

        public RoutePanel(IRouteService routeService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _drivers = new List<Driver>();
            _vehicles = new List<Vehicle>();

            _tripDatePicker = new DateTimePicker();
            _amStartingMileage = new NumericUpDown();
            _amEndingMileage = new NumericUpDown();
            _amRiders = new NumericUpDown();
            _pmStartMileage = new NumericUpDown();
            _pmEndingMileage = new NumericUpDown();
            _pmRiders = new NumericUpDown();
            _driverComboBox = new ComboBox();
            _vehicleComboBox = new ComboBox();
            _saveButton = new Button();
            _editButton = new Button();
            _deleteButton = new Button();
            _tableLayoutPanel = new TableLayoutPanel();
            _routesGrid = new DataGridView();
            _prevPageButton = new Button();
            _nextPageButton = new Button();
            _pageInfoLabel = new Label();

            InitializeComponent();
        }

        public RoutePanel(IReadOnlyList<Driver> drivers, IReadOnlyList<Vehicle> vehicles)
        {
            _tripDatePicker = new DateTimePicker();
            _amStartingMileage = new NumericUpDown();
            _amEndingMileage = new NumericUpDown();
            _amRiders = new NumericUpDown();
            _pmStartMileage = new NumericUpDown();
            _pmEndingMileage = new NumericUpDown();
            _pmRiders = new NumericUpDown();
            _driverComboBox = new ComboBox();
            _vehicleComboBox = new ComboBox();
            _saveButton = new Button();
            _editButton = new Button();
            _deleteButton = new Button();
            _tableLayoutPanel = new TableLayoutPanel();
            _routesGrid = new DataGridView();
            _prevPageButton = new Button();
            _nextPageButton = new Button();
            _pageInfoLabel = new Label();
            _routeService = null!; // Suppress CS8618 for this constructor

            _drivers = drivers?.ToList() ?? new List<Driver>();
            _vehicles = vehicles?.ToList() ?? new List<Vehicle>();

            InitializeComponent();
        }

        private void InitializeComponent()

        {
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Padding = new Padding(10);

            _tableLayoutPanel.ColumnCount = 2;
            _tableLayoutPanel.RowCount = 10; // 7 fields + 3 buttons
            _tableLayoutPanel.Dock = DockStyle.Fill;
            _tableLayoutPanel.BackColor = Color.Transparent;
            _tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            _tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            for (int i = 0; i < _tableLayoutPanel.RowCount; i++)
            {
                _tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            }
            this.Controls.Add(_tableLayoutPanel);

            // Trip Date
            var tripDateLabel = new Label
            {
                Text = "Trip Date:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _tableLayoutPanel.Controls.Add(tripDateLabel, 0, 0);
            _tripDatePicker.Format = DateTimePickerFormat.Short;
            _tripDatePicker.Dock = DockStyle.Fill;
            _tableLayoutPanel.Controls.Add(_tripDatePicker, 1, 0);

            // AM Starting Mileage
            var amStartLabel = new Label
            {
                Text = "AM Start Mileage:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _tableLayoutPanel.Controls.Add(amStartLabel, 0, 1);
            _amStartingMileage.Minimum = 0;
            _amStartingMileage.Maximum = int.MaxValue;
            _amStartingMileage.DecimalPlaces = 0;
            _amStartingMileage.ThousandsSeparator = true;
            _amStartingMileage.Dock = DockStyle.Fill;
            _tableLayoutPanel.Controls.Add(_amStartingMileage, 1, 1);

            // AM Ending Mileage
            var amEndLabel = new Label
            {
                Text = "AM End Mileage:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _tableLayoutPanel.Controls.Add(amEndLabel, 0, 2);
            _amEndingMileage.Minimum = 0;
            _amEndingMileage.Maximum = int.MaxValue;
            _amEndingMileage.DecimalPlaces = 0;
            _amEndingMileage.ThousandsSeparator = true;
            _amEndingMileage.Dock = DockStyle.Fill;
            _tableLayoutPanel.Controls.Add(_amEndingMileage, 1, 2);

            // AM Riders
            var amRidersLabel = new Label
            {
                Text = "AM Riders:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _tableLayoutPanel.Controls.Add(amRidersLabel, 0, 3);
            _amRiders.Minimum = 0;
            _amRiders.Maximum = int.MaxValue;
            _amRiders.DecimalPlaces = 0;
            _amRiders.ThousandsSeparator = true;
            _amRiders.Dock = DockStyle.Fill;
            _tableLayoutPanel.Controls.Add(_amRiders, 1, 3);

            // PM Start Mileage
            var pmStartLabel = new Label
            {
                Text = "PM Start Mileage:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _tableLayoutPanel.Controls.Add(pmStartLabel, 0, 4);
            _pmStartMileage.Minimum = 0;
            _pmStartMileage.Maximum = int.MaxValue;
            _pmStartMileage.DecimalPlaces = 0;
            _pmStartMileage.ThousandsSeparator = true;
            _pmStartMileage.Dock = DockStyle.Fill;
            _tableLayoutPanel.Controls.Add(_pmStartMileage, 1, 4);

            // PM End Mileage
            var pmEndLabel = new Label
            {
                Text = "PM End Mileage:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _tableLayoutPanel.Controls.Add(pmEndLabel, 0, 5);
            _pmEndingMileage.Minimum = 0;
            _pmEndingMileage.Maximum = int.MaxValue;
            _pmEndingMileage.DecimalPlaces = 0;
            _pmEndingMileage.ThousandsSeparator = true;
            _pmEndingMileage.Dock = DockStyle.Fill;
            _tableLayoutPanel.Controls.Add(_pmEndingMileage, 1, 5);

            // PM Riders
            var pmRidersLabel = new Label
            {
                Text = "PM Riders:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _tableLayoutPanel.Controls.Add(pmRidersLabel, 0, 6);
            _pmRiders.Minimum = 0;
            _pmRiders.Maximum = int.MaxValue;
            _pmRiders.DecimalPlaces = 0;
            _pmRiders.ThousandsSeparator = true;
            _pmRiders.Dock = DockStyle.Fill;
            _tableLayoutPanel.Controls.Add(_pmRiders, 1, 6);

            // Driver
            var driverLabel = new Label
            {
                Text = "Driver:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _tableLayoutPanel.Controls.Add(driverLabel, 0, 7);
            _driverComboBox.Dock = DockStyle.Fill;
            _driverComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _driverComboBox.Items.Clear();
            _driverComboBox.Items.Add("Unassigned");
            _driverComboBox.SelectedIndex = 0;
            _driverComboBox.Enabled = true;
            _tableLayoutPanel.Controls.Add(_driverComboBox, 1, 7);

            // Vehicle
            var vehicleLabel = new Label
            {
                Text = "Vehicle:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.CardFont
            };
            _tableLayoutPanel.Controls.Add(vehicleLabel, 0, 8);
            _vehicleComboBox.Dock = DockStyle.Fill;
            _vehicleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _vehicleComboBox.Items.Clear();
            _vehicleComboBox.Items.Add("Unassigned");
            _vehicleComboBox.SelectedIndex = 0;
            _vehicleComboBox.Enabled = true;
            _tableLayoutPanel.Controls.Add(_vehicleComboBox, 1, 8);

            // ToolTips for mileage fields (after controls are created)
            using (var toolTip = new ToolTip())
            {
                toolTip.SetToolTip(_amStartingMileage, "Enter starting mileage in miles");
                toolTip.SetToolTip(_amEndingMileage, "Enter ending mileage in miles");
                toolTip.SetToolTip(_pmStartMileage, "Enter starting mileage in miles");
                toolTip.SetToolTip(_pmEndingMileage, "Enter ending mileage in miles");
            }

            // Set tab order for efficient data entry (input controls only)
            tripDateLabel.TabIndex = 0;
            _tripDatePicker.TabIndex = 1;
            amStartLabel.TabIndex = 2;
            _amStartingMileage.TabIndex = 3;
            amEndLabel.TabIndex = 4;
            _amEndingMileage.TabIndex = 5;
            amRidersLabel.TabIndex = 6;
            _amRiders.TabIndex = 7;
            pmStartLabel.TabIndex = 8;
            _pmStartMileage.TabIndex = 9;
            pmEndLabel.TabIndex = 10;
            _pmEndingMileage.TabIndex = 11;
            pmRidersLabel.TabIndex = 12;
            _pmRiders.TabIndex = 13;
            driverLabel.TabIndex = 14;
            _driverComboBox.TabIndex = 15;
            vehicleLabel.TabIndex = 16;
            _vehicleComboBox.TabIndex = 17;
            _saveButton.TabIndex = 18;
            _editButton.TabIndex = 19;
            _deleteButton.TabIndex = 20;

            // Buttons Row
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(3, 5, 3, 3),
                Margin = new Padding(0)
            };

            // Apply button styling and add to panel
            StyleButton(_saveButton, "Save Route");
            _saveButton.Click += new EventHandler(SaveButton_Click);
            buttonPanel.Controls.Add(_saveButton);

            StyleButton(_editButton, "Edit Route");
            _editButton.Click += new EventHandler(EditButton_Click);
            buttonPanel.Controls.Add(_editButton);

            StyleButton(_deleteButton, "Delete Route", Color.FromArgb(210, 70, 70));
            _deleteButton.Click += new EventHandler(DeleteButton_Click);
            buttonPanel.Controls.Add(_deleteButton);

            _tableLayoutPanel.Controls.Add(buttonPanel, 1, 9);
        }

        private static void StyleButton(Button button, string text, Color? customColor = null)
        {
            button.Text = text;
            button.BackColor = customColor ?? ThemeManager.CurrentTheme.ButtonBackground;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = ThemeManager.CurrentTheme.ButtonFont;
            button.Margin = new Padding(0, 0, 10, 0); // Add space between buttons
            button.Padding = new Padding(5, 3, 5, 3); // Add internal padding
            button.MinimumSize = new Size(100, 30); // Consistent button size
            button.Cursor = Cursors.Hand; // Show hand cursor on hover

            // Add hover effect handlers
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = ControlPaint.Light(button.BackColor, 0.1f);
            };
            button.MouseLeave += (s, e) =>
            {
                button.BackColor = customColor ?? ThemeManager.CurrentTheme.ButtonBackground;
            };

            // Add pressed effect
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(button.BackColor, 0.1f);
            button.FlatAppearance.BorderColor = ControlPaint.Dark(button.BackColor, 0.2f);
            button.FlatAppearance.BorderSize = 1;
        }

        private void SaveButton_Click(object? sender, EventArgs e) => OnSaveButtonClicked(EventArgs.Empty);
        private void EditButton_Click(object? sender, EventArgs e) => OnUpdateButtonClicked(EventArgs.Empty);
        private void DeleteButton_Click(object? sender, EventArgs e) => OnDeleteButtonClicked(EventArgs.Empty);

        protected virtual void OnSaveButtonClicked(EventArgs e) => SaveButtonClicked?.Invoke(this, e);
        protected virtual void OnUpdateButtonClicked(EventArgs e) => UpdateButtonClicked?.Invoke(this, e);
        protected virtual void OnDeleteButtonClicked(EventArgs e) => DeleteButtonClicked?.Invoke(this, e);

        public void SetRouteData(Route route)
        {
            ArgumentNullException.ThrowIfNull(route);
            _tripDatePicker.Value = route.RouteDate;
            _amStartingMileage.Value = route.AMStartingMileage;
            _amEndingMileage.Value = route.AMEndingMileage;
            _amRiders.Value = route.AMRiders;
            _pmStartMileage.Value = route.PMStartMileage;
            _pmEndingMileage.Value = route.PMEndingMileage;
            _pmRiders.Value = route.PMRiders;

            if (route.Driver != null)
            {
                var idx = _driverComboBox.Items.Cast<object>().ToList().FindIndex(x => x is Driver d && d.Id == route.Driver.Id);
                _driverComboBox.SelectedIndex = idx > 0 ? idx : 0;
            }
            else
            {
                _driverComboBox.SelectedIndex = 0;
            }

            if (route.Vehicle != null)
            {
                var idx = _vehicleComboBox.Items.Cast<object>().ToList().FindIndex(x => x is Vehicle v && v.Id == route.Vehicle.Id);
                _vehicleComboBox.SelectedIndex = idx > 0 ? idx : 0;
            }
            else
            {
                _vehicleComboBox.SelectedIndex = 0;
            }
        }
        private bool ValidateRouteData(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (_amEndingMileage.Value < _amStartingMileage.Value)
            {
                errorMessage = "AM Ending Mileage must be greater than or equal to AM Starting Mileage.";
                return false;
            }

            if (_pmEndingMileage.Value < _pmStartMileage.Value)
            {
                errorMessage = "PM Ending Mileage must be greater than or equal to PM Starting Mileage.";
                return false;
            }

            return true;
        }

        public Route? GetRouteData()
        {
            if (!ValidateRouteData(out string errorMessage))
            {
                // Suppress dialog if running in test mode
                if (!SuppressDialogsForTests && System.Environment.UserInteractive)
                {
                    MessageBox.Show(errorMessage, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return null;
            }

            var selectedDriver = _driverComboBox.SelectedItem as Driver;
            var selectedVehicle = _vehicleComboBox.SelectedItem as Vehicle;
            var route = new Route
            {
                RouteDate = _tripDatePicker.Value,
                AMStartingMileage = (int)_amStartingMileage.Value,
                AMEndingMileage = (int)_amEndingMileage.Value,
                AMRiders = (int)_amRiders.Value,
                PMStartMileage = (int)_pmStartMileage.Value,
                PMEndingMileage = (int)_pmEndingMileage.Value,
                PMRiders = (int)_pmRiders.Value,
                Driver = selectedDriver,
                DriverId = selectedDriver?.Id,
                Vehicle = selectedVehicle,
                VehicleId = selectedVehicle?.Id
            };
            return route;
        }

        public void ClearFields()
        {
            _tripDatePicker.Value = DateTime.Today;
            _amStartingMileage.Value = 0;
            _amEndingMileage.Value = 0;
            _amRiders.Value = 0;
            _pmStartMileage.Value = 0;
            _pmEndingMileage.Value = 0;
            _pmRiders.Value = 0;
            if (_drivers.Count > 0) _driverComboBox.SelectedIndex = 0;
            if (_vehicles.Count > 0) _vehicleComboBox.SelectedIndex = 0;
        }
        public void ApplyVisualRefinements()
        {
            foreach (Control control in this.Controls)
            {
                ApplyControlStyle(control);

                if (control.HasChildren)
                {
                    foreach (Control childControl in control.Controls)
                    {
                        ApplyControlStyle(childControl);
                    }
                }
            }

            this.BorderStyle = BorderStyle.FixedSingle;
            this.Padding = new Padding(1);
        }

        private static void ApplyControlStyle(Control control)
        {
            switch (control)
            {
                case TextBox textBox:
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.Margin = new Padding(3);
                    break;
                case Button button:
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _tripDatePicker?.Dispose();
                    _amStartingMileage?.Dispose();
                    _amEndingMileage?.Dispose();
                    _amRiders?.Dispose();
                    _pmStartMileage?.Dispose();
                    _pmEndingMileage?.Dispose();
                    _pmRiders?.Dispose();
                    _driverComboBox?.Dispose();
                    _vehicleComboBox?.Dispose();
                    _saveButton?.Dispose();
                    _editButton?.Dispose();
                    _deleteButton?.Dispose();
                    _tableLayoutPanel?.Dispose();
                    _routesGrid?.Dispose();
                    _prevPageButton?.Dispose();
                    _nextPageButton?.Dispose();
                    _pageInfoLabel?.Dispose();
                }
                _disposedValue = true;
            }
            base.Dispose(disposing);
        }

        public void Render(Control container)
        {
            ArgumentNullException.ThrowIfNull(container);
            container.Controls.Clear();
            container.Controls.Add(this);
        }

        public async Task LoadRoutesAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_routeService);

            try
            {
                _currentPage = page;
                _pageSize = pageSize;

                _drivers = await _routeService.GetDriversAsync(cancellationToken);
                _vehicles = await _routeService.GetVehiclesAsync(cancellationToken);

                UpdateDriversComboBox();
                UpdateVehiclesComboBox();

                if (_routesGrid != null && _routesGrid.IsHandleCreated)
                {
                    _totalRoutes = await _routeService.GetRoutesCountAsync(cancellationToken);
                    var routes = await _routeService.GetRoutesAsync(page, pageSize, cancellationToken);

                    // Use RouteDisplayDTO to ensure property mapping for grid
                    var routeDisplayList = routes.Select(RouteDisplayDTO.FromRoute).ToList();
                    _routesGrid.DataSource = routeDisplayList;

                    UpdatePaginationControls();
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            // Let all other exceptions bubble up (CA1031)
        }

        private void UpdateDriversComboBox()
        {
            _driverComboBox.BeginUpdate();
            _driverComboBox.DataSource = null;
            _driverComboBox.Items.Clear();

            if (_drivers.Count > 0)
            {
                _driverComboBox.DataSource = _drivers;
                _driverComboBox.DisplayMember = "Name";
                _driverComboBox.ValueMember = "Id";
                _driverComboBox.Enabled = true;
            }
            else
            {
                _driverComboBox.Items.Add("No drivers available");
                _driverComboBox.SelectedIndex = 0;
                _driverComboBox.Enabled = false;
            }

            _driverComboBox.EndUpdate();
        }

        private void UpdateVehiclesComboBox()
        {
            _vehicleComboBox.BeginUpdate();
            _vehicleComboBox.DataSource = null;
            _vehicleComboBox.Items.Clear();

            if (_vehicles.Count > 0)
            {
                _vehicleComboBox.DataSource = _vehicles;
                _vehicleComboBox.DisplayMember = "Name";
                _vehicleComboBox.ValueMember = "Id";
                _vehicleComboBox.Enabled = true;
            }
            else
            {
                _vehicleComboBox.Items.Add("No vehicles available");
                _vehicleComboBox.SelectedIndex = 0;
                _vehicleComboBox.Enabled = false;
            }

            _vehicleComboBox.EndUpdate();
        }

        private void UpdateRoutesGrid(List<Route> routes)
        {
            // This is used when showing a grid of routes rather than editing a single route
            if (_routesGrid == null || !_routesGrid.IsHandleCreated)
                return;

            _routesGrid.SuspendLayout();
            _routesGrid.Rows.Clear();

            foreach (var route in routes)
            {
                var dto = RouteDisplayDTO.FromRoute(route);
                _routesGrid.Rows.Add(
                    dto.Id,
                    dto.Name,
                    dto.RouteDate.ToShortDateString(),
                    dto.DriverName,
                    dto.VehicleName,
                    dto.AMRiders + dto.PMRiders,
                    (dto.AMEndingMileage - dto.AMStartingMileage) +
                    (dto.PMEndingMileage - dto.PMStartMileage)
                );
            }

            _routesGrid.ResumeLayout();
        }

        private void UpdatePaginationControls()
        {
            if (_pageInfoLabel == null || !_pageInfoLabel.IsHandleCreated ||
                _prevPageButton == null || _nextPageButton == null)
                return;

            int totalPages = (_totalRoutes + _pageSize - 1) / _pageSize;
            _pageInfoLabel.Text = $"Page {_currentPage} of {totalPages}";

            _prevPageButton.Enabled = _currentPage > 1;
            _nextPageButton.Enabled = _currentPage < totalPages;
        }

        private static void OptimizeDataGridViewPerformance(DataGridView grid)
        {
            // Double buffering significantly improves rendering performance
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)?
                .SetValue(grid, true);

            // Set performance-related properties
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.VirtualMode = true;

            // Additional optimizations
            grid.RowHeadersVisible = false;
            grid.EnableHeadersVisualStyles = false;
        }
    }
}