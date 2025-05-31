#nullable enable

using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Common;

namespace BusBus.UI
{
    /// <summary>
    /// Form for adding or editing route information
    /// </summary>
    public partial class RouteEditForm : Form
    {
        private readonly IRouteService _routeService;
        private readonly IDriverService _driverService;
        private readonly IVehicleService _vehicleService;
        private Route? _route;
        private readonly bool _isNewRoute;

        private TextBox _routeNumberTextBox = null!;
        private TextBox _routeNameTextBox = null!;
        private TextBox _startLocationTextBox = null!;
        private TextBox _endLocationTextBox = null!;
        private NumericUpDown _distanceNumeric = null!;
        private NumericUpDown _durationHoursNumeric = null!;
        private NumericUpDown _durationMinutesNumeric = null!;
        private ComboBox _vehicleComboBox = null!;
        private ComboBox _driverComboBox = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        public RouteEditForm(IRouteService routeService, IDriverService driverService,
            IVehicleService vehicleService, Route? route = null)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            _route = route;
            _isNewRoute = route == null;

            InitializeComponent();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Text = _isNewRoute ? "Add New Route" : "Edit Route";
            Size = new Size(500, 550);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(40, 40, 45);
            ForeColor = Color.FromArgb(220, 220, 220);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 10,
                ColumnCount = 2,
                Padding = new Padding(20)
            };

            // Configure rows
            for (int i = 0; i < 9; i++)
            {
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            }
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Buttons row

            // Configure columns
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            int row = 0;

            // Route Number
            mainLayout.Controls.Add(CreateLabel("Route Number:"), 0, row);
            _routeNumberTextBox = CreateTextBox();
            mainLayout.Controls.Add(_routeNumberTextBox, 1, row++);

            // Route Name
            mainLayout.Controls.Add(CreateLabel("Route Name:"), 0, row);
            _routeNameTextBox = CreateTextBox();
            mainLayout.Controls.Add(_routeNameTextBox, 1, row++);

            // Start Location
            mainLayout.Controls.Add(CreateLabel("Start Location:"), 0, row);
            _startLocationTextBox = CreateTextBox();
            mainLayout.Controls.Add(_startLocationTextBox, 1, row++);

            // End Location
            mainLayout.Controls.Add(CreateLabel("End Location:"), 0, row);
            _endLocationTextBox = CreateTextBox();
            mainLayout.Controls.Add(_endLocationTextBox, 1, row++);

            // Distance
            mainLayout.Controls.Add(CreateLabel("Distance (miles):"), 0, row);
            _distanceNumeric = CreateNumericUpDown(0, 1000, 1);
            mainLayout.Controls.Add(_distanceNumeric, 1, row++);

            // Duration
            mainLayout.Controls.Add(CreateLabel("Duration:"), 0, row);
            var durationPanel = new Panel { Dock = DockStyle.Fill };
            _durationHoursNumeric = CreateNumericUpDown(0, 24, 0);
            _durationHoursNumeric.Width = 60;
            _durationMinutesNumeric = CreateNumericUpDown(0, 59, 0);
            _durationMinutesNumeric.Width = 60;
            durationPanel.Controls.Add(_durationHoursNumeric);
            durationPanel.Controls.Add(new Label { Text = "hrs", Left = 65, Top = 3, Width = 25 });
            durationPanel.Controls.Add(_durationMinutesNumeric);
            _durationMinutesNumeric.Left = 90;
            durationPanel.Controls.Add(new Label { Text = "min", Left = 155, Top = 3, Width = 30 });
            mainLayout.Controls.Add(durationPanel, 1, row++);

            // Vehicle Assignment
            mainLayout.Controls.Add(CreateLabel("Vehicle:"), 0, row);
            _vehicleComboBox = CreateComboBox();
            mainLayout.Controls.Add(_vehicleComboBox, 1, row++);

            // Driver Assignment
            mainLayout.Controls.Add(CreateLabel("Driver:"), 0, row);
            _driverComboBox = CreateComboBox();
            mainLayout.Controls.Add(_driverComboBox, 1, row++);

            // Buttons
            var buttonPanel = new Panel { Dock = DockStyle.Fill };
            mainLayout.SetColumnSpan(buttonPanel, 2);

            _saveButton = CreateButton("Save", Color.FromArgb(70, 130, 180));
            _saveButton.Click += SaveButton_Click;

            _cancelButton = CreateButton("Cancel", Color.FromArgb(100, 100, 100));
            _cancelButton.Click += (s, e) => DialogResult = DialogResult.Cancel;

            _saveButton.Location = new Point(100, 10);
            _cancelButton.Location = new Point(230, 10);

            buttonPanel.Controls.Add(_saveButton);
            buttonPanel.Controls.Add(_cancelButton);
            mainLayout.Controls.Add(buttonPanel, 0, 9);

            Controls.Add(mainLayout);

            if (!_isNewRoute && _route != null)
            {
                PopulateFields();
            }
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(220, 220, 220),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 10, 0)
            };
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private static NumericUpDown CreateNumericUpDown(decimal min, decimal max, decimal value)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private static ComboBox CreateComboBox()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(220, 220, 220),
                FlatStyle = FlatStyle.Flat
            };
        }

        private static Button CreateButton(string text, Color accentColor)
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
                Cursor = Cursors.Hand
            };
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load vehicles
                var vehicles = await _vehicleService.GetAllVehiclesAsync();
                _vehicleComboBox.Items.Clear();
                _vehicleComboBox.Items.Add(new ComboBoxItem { Text = "Unassigned", Value = null });
                foreach (var vehicle in vehicles.Where(v => v.IsActive))
                {
                    _vehicleComboBox.Items.Add(new ComboBoxItem
                    {
                        Text = $"Bus #{vehicle.Number} - {vehicle.Model}",
                        Value = vehicle.Id
                    });
                }
                _vehicleComboBox.SelectedIndex = 0;

                // Load drivers
                var drivers = await _driverService.GetAllDriversAsync();
                _driverComboBox.Items.Clear();
                _driverComboBox.Items.Add(new ComboBoxItem { Text = "Unassigned", Value = null });
                foreach (var driver in drivers.Where(d => d.IsActive))
                {
                    _driverComboBox.Items.Add(new ComboBoxItem
                    {
                        Text = $"{driver.FirstName} {driver.LastName}",
                        Value = driver.Id
                    });
                }
                _driverComboBox.SelectedIndex = 0;
            }
            catch
            {
                // Use sample data on error
                _vehicleComboBox.Items.Add(new ComboBoxItem { Text = "Sample Bus #101", Value = Guid.NewGuid() });
                _driverComboBox.Items.Add(new ComboBoxItem { Text = "Sample Driver", Value = Guid.NewGuid() });
            }
        }

        private void PopulateFields()
        {
            if (_route == null) return;

            _routeNumberTextBox.Text = _route.RouteCode ?? $"RT{_route.RouteID:D4}";
            _routeNameTextBox.Text = _route.Name;
            _startLocationTextBox.Text = _route.StartLocation;
            _endLocationTextBox.Text = _route.EndLocation;
            _distanceNumeric.Value = (decimal)_route.TotalMiles;

            // var duration = _route.EstimatedDuration; // Schedule scrubbed
            _durationHoursNumeric.Value = 0;
            _durationMinutesNumeric.Value = 0;

            // Select vehicle
            if (_route.VehicleId.HasValue)
            {
                foreach (ComboBoxItem item in _vehicleComboBox.Items)
                {
                    if (item.Value is Guid guid && guid == _route.VehicleId.Value)
                    {
                        _vehicleComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Select driver
            if (_route.DriverId.HasValue)
            {
                foreach (ComboBoxItem item in _driverComboBox.Items)
                {
                    if (item.Value is Guid guid && guid == _route.DriverId.Value)
                    {
                        _driverComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private async void SaveButton_Click(object? sender, EventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(_routeNameTextBox.Text))
            {
                MessageBox.Show("Please enter a route name.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_startLocationTextBox.Text) ||
                string.IsNullOrWhiteSpace(_endLocationTextBox.Text))
            {
                MessageBox.Show("Please enter both start and end locations.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_distanceNumeric.Value <= 0)
            {
                MessageBox.Show("Please enter a valid distance.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _saveButton.Enabled = false;

                var route = _route ?? new Route { Id = Guid.NewGuid() };

                route.RouteCode = _routeNumberTextBox.Text;
                route.Name = _routeNameTextBox.Text;
                route.StartLocation = _startLocationTextBox.Text;
                route.EndLocation = _endLocationTextBox.Text;
                route.Distance = (int)_distanceNumeric.Value;
                // Schedule fields scrubbed: removed ScheduledTime and AddMinutes

                route.VehicleId = (_vehicleComboBox.SelectedItem as ComboBoxItem)?.Value as Guid?;
                route.DriverId = (_driverComboBox.SelectedItem as ComboBoxItem)?.Value as Guid?;

                if (_isNewRoute)
                {
                    route.CreatedDate = DateTime.Now;
                    route.RouteDate = DateTime.Today;
                    route.IsActive = true;
                    await _routeService.CreateAsync(route, System.Threading.CancellationToken.None);
                }
                else
                {
                    route.ModifiedDate = DateTime.Now;
                    await _routeService.UpdateAsync(route, System.Threading.CancellationToken.None);
                }

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving route: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _saveButton.Enabled = true;
            }
        }

        private class ComboBoxItem
        {
            public string Text { get; set; } = string.Empty;
            public object? Value { get; set; }
            public override string ToString() => Text;
        }

        /// <summary>
        /// Handles cancel button click
        /// </summary>
        private void OnCancelClick(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #region Validation

        /// <summary>
        /// Validates user input
        /// </summary>
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_routeNumberTextBox.Text))
            {
                MessageBox.Show("Please enter a route number.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _routeNumberTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_routeNameTextBox.Text))
            {
                MessageBox.Show("Please enter a route name.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _routeNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_startLocationTextBox.Text))
            {
                MessageBox.Show("Please enter a start location.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _startLocationTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_endLocationTextBox.Text))
            {
                MessageBox.Show("Please enter an end location.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _endLocationTextBox.Focus();
                return false;
            }

            // Validate distance
            if (_distanceNumeric.Value <= 0)
            {
                var result = MessageBox.Show(
                    "Distance is set to 0. Routes typically have a positive distance.\n\n" +
                    "Do you want to continue anyway?",
                    "Distance Validation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    _distanceNumeric.Focus();
                    return false;
                }
            }

            // Validate duration
            if (_durationHoursNumeric.Value == 0 && _durationMinutesNumeric.Value == 0)
            {
                var result = MessageBox.Show(
                    "Duration is set to 0. Routes typically have a positive duration.\n\n" +
                    "Do you want to continue anyway?",
                    "Duration Validation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    _durationHoursNumeric.Focus();
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Helper Classes

        private class VehicleItem
        {
            public Guid? Id { get; set; }
            public string Display { get; set; } = string.Empty;

            public override string ToString() => Display;
        }

        #endregion
    }
}
