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
using BusBus.UI.Common;

namespace BusBus.UI
// Suppress unused event warning for this file
#pragma warning disable CS0067
{
    /// <summary>
    /// A reusable UserControl for displaying and editing route mileage log information.
    /// </summary>
    public partial class RoutePanel : ThemeableControl, IDisplayable
    {
        /// <summary>
        /// Set to true in unit tests to suppress validation dialogs.
        /// </summary>
        /// <summary>        /// Gets or sets a value indicating whether validation dialogs are suppressed (for unit testing).
        /// </summary>
        public static bool SuppressDialogsForTests { get; set; }
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
        private readonly Button _cancelButton;
        private readonly Button _editButton;
        private readonly Button _deleteButton;
        private readonly TableLayoutPanel _tableLayoutPanel;
        private readonly IRouteService _routeService;
        private readonly Label _titleLabel;
        private readonly Label _errorLabel;
#pragma warning disable CS0169, CA1823 // Unused field
        private bool _disposedValue;
#pragma warning restore CS0169, CA1823
        private List<Driver> _drivers;
        private List<Vehicle> _vehicles;
        private Route? _currentRoute; // Track the current route being edited
#pragma warning disable CA1823 // Unused field
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
#pragma warning restore CA1823
        private readonly EventHandler _saveButtonHandler;
        private readonly EventHandler _cancelButtonHandler;

        public event EventHandler? SaveButtonClicked;
        public event EventHandler? UpdateButtonClicked;
        public event EventHandler? DeleteButtonClicked;
        public event EventHandler? CancelButtonClicked;

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
            _cancelButton = new Button();
            _editButton = new Button();
            _deleteButton = new Button();
            _tableLayoutPanel = new TableLayoutPanel();
            _titleLabel = new Label();
            _errorLabel = new Label();

            // Store event handlers as fields so we can unhook them
            _saveButtonHandler = async (s, e) => await SaveRouteAsync();
            _cancelButtonHandler = (s, e) => CancelEdit();

            _saveButton.Click += _saveButtonHandler;
            _cancelButton.Click += _cancelButtonHandler;

            LoadDriversAndVehicles(); // Load drivers and vehicles
            Console.WriteLine("[RoutePanel] Event handlers hooked");
        }

        // Modified constructor to directly initialize from lists (used in tests)
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
            _cancelButton = new Button();
            _editButton = new Button();
            _deleteButton = new Button();
            _tableLayoutPanel = new TableLayoutPanel();
            _titleLabel = new Label();
            _errorLabel = new Label();
            _routeService = null!; // Suppress CS8618 for this constructor

            _drivers = drivers?.ToList() ?? new List<Driver>();
            _vehicles = vehicles?.ToList() ?? new List<Vehicle>();

            // Assign event handlers for test constructor
            _saveButtonHandler = async (s, e) => await SaveRouteAsync();
            _cancelButtonHandler = (s, e) => CancelEdit();
            _saveButton.Click += _saveButtonHandler;
            _cancelButton.Click += _cancelButtonHandler;
        }

        private void CancelEdit()
        {
            CancelButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        // Method to load a route for editing
        public void LoadRoute(Route route)
        {
            ArgumentNullException.ThrowIfNull(route);

            _currentRoute = route;
            _errorLabel.Visible = false;

            // Update the form fields
            _tripDatePicker.Value = route.RouteDate;
            _amStartingMileage.Value = route.AMStartingMileage;
            _amEndingMileage.Value = route.AMEndingMileage;
            _amRiders.Value = route.AMRiders;
            _pmStartMileage.Value = route.PMStartMileage;
            _pmEndingMileage.Value = route.PMEndingMileage;
            _pmRiders.Value = route.PMRiders;            // Update the title
            _titleLabel.Text = route.Id == Guid.Empty ? "Add New Route" : $"Edit Route: {route.Name}";

            // Enable/disable delete button
            _deleteButton.Visible = route.Id != Guid.Empty;
            _deleteButton.Click -= DeleteButton_Click;
            _deleteButton.Click += DeleteButton_Click;

            // Update driver and vehicle combo boxes
            UpdateComboBoxes(route.DriverId, route.VehicleId);
        }

        // Async method to load drivers and vehicles
        private async void LoadDriversAndVehicles()
        {
            if (_routeService == null) return;

            try
            {
                _drivers = await _routeService.GetDriversAsync();
                _vehicles = await _routeService.GetVehiclesAsync();

                UpdateComboBoxes(null, null);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error loading drivers and vehicles: {ex.Message}");
                _errorLabel.Text = $"Network error: {ex.Message}";
                _errorLabel.Visible = true;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error loading drivers and vehicles: {ex.Message}");
                _errorLabel.Text = $"Error: {ex.Message}";
                _errorLabel.Visible = true;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Console.WriteLine($"Unexpected error loading drivers and vehicles: {ex.Message}");
                _errorLabel.Text = $"Unexpected error: {ex.Message}";
                _errorLabel.Visible = true;
            }
        }

        // Method to update the combo boxes
        private void UpdateComboBoxes(Guid? driverId, Guid? vehicleId)
        {
            // Update driver combo box
            _driverComboBox.Items.Clear();
            _driverComboBox.Items.Add("Unassigned");
            foreach (var driver in _drivers)
            {
                _driverComboBox.Items.Add($"{driver.FirstName} {driver.LastName}");
            }

            // Set selected driver
            if (driverId.HasValue && _drivers.Count > 0)
            {
                var driverIndex = _drivers.FindIndex(d => d.Id == driverId.Value);
                if (driverIndex >= 0)
                {
                    _driverComboBox.SelectedIndex = driverIndex + 1; // +1 because of "Unassigned"
                }
                else
                {
                    _driverComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                _driverComboBox.SelectedIndex = 0;
            }

            // Update vehicle combo box
            _vehicleComboBox.Items.Clear();
            _vehicleComboBox.Items.Add("Unassigned");
            foreach (var vehicle in _vehicles)
            {
                _vehicleComboBox.Items.Add(vehicle.BusNumber);
            }

            // Set selected vehicle
            if (vehicleId.HasValue && _vehicles.Count > 0)
            {
                var vehicleIndex = _vehicles.FindIndex(v => v.Id == vehicleId.Value);
                if (vehicleIndex >= 0)
                {
                    _vehicleComboBox.SelectedIndex = vehicleIndex + 1; // +1 because of "Unassigned"
                }
                else
                {
                    _vehicleComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                _vehicleComboBox.SelectedIndex = 0;
            }
        }

        private async Task<bool> SaveRouteAsync()
        {
            try
            {
                _errorLabel.Text = "";
                _errorLabel.Visible = false;

                if (_routeService == null)
                {
                    _errorLabel.Text = "Route service is not available";
                    _errorLabel.Visible = true;
                    return false;
                }

                var isNewRoute = _currentRoute == null || _currentRoute.Id == Guid.Empty;
                var route = _currentRoute ?? new Route { Id = Guid.Empty };

                // Validate data
                if (_amEndingMileage.Value < _amStartingMileage.Value)
                {
                    _errorLabel.Text = "AM ending mileage cannot be less than AM starting mileage";
                    _errorLabel.Visible = true;
                    return false;
                }

                if (_pmEndingMileage.Value < _pmStartMileage.Value)
                {
                    _errorLabel.Text = "PM ending mileage cannot be less than PM starting mileage";
                    _errorLabel.Visible = true;
                    return false;
                }

                // Update route properties
                route.RouteDate = _tripDatePicker.Value;
                route.AMStartingMileage = (int)_amStartingMileage.Value;
                route.AMEndingMileage = (int)_amEndingMileage.Value;
                route.AMRiders = (int)_amRiders.Value;
                route.PMStartMileage = (int)_pmStartMileage.Value;
                route.PMEndingMileage = (int)_pmEndingMileage.Value;
                route.PMRiders = (int)_pmRiders.Value;

                // Get selected driver and vehicle
                route.DriverId = _driverComboBox.SelectedIndex > 0 ?
                    _drivers[_driverComboBox.SelectedIndex - 1].Id :
                    null;

                route.VehicleId = _vehicleComboBox.SelectedIndex > 0 ?
                    _vehicles[_vehicleComboBox.SelectedIndex - 1].Id :
                    null;

                // Determine if we're creating or updating
                if (isNewRoute)
                {
                    route.Name = $"Route {DateTime.Now:yyyyMMdd-HHmmss}";
                    await _routeService.CreateRouteAsync(route);
                }
                else
                {
                    await _routeService.UpdateRouteAsync(route);
                }

                return true;
            }
            catch (InvalidOperationException ex)
            {
                _errorLabel.Text = $"Error saving route: {ex.Message}";
                _errorLabel.Visible = true;
                return false;
            }
            catch (HttpRequestException ex)
            {
                _errorLabel.Text = $"Network error: {ex.Message}";
                _errorLabel.Visible = true;
                return false;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                _errorLabel.Text = $"Unexpected error: {ex.Message}";
                _errorLabel.Visible = true;
                return false;
            }
        }

        public override void Render(Control container)
        {
            if (container == null) return;
            container.Controls.Clear();
            container.Controls.Add(this);
            this.Dock = DockStyle.Fill;
        }

        private async void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_currentRoute != null && _currentRoute.Id != Guid.Empty && _routeService != null)
            {
                try
                {
                    var result = SuppressDialogsForTests ? DialogResult.Yes :
                        MessageBox.Show($"Are you sure you want to delete the route '{_currentRoute.Name}'?",
                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        await _routeService.DeleteRouteAsync(_currentRoute.Id);
                        DeleteButtonClicked?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    _errorLabel.Text = $"Failed to delete route: {ex.Message}";
                    _errorLabel.Visible = true;
                }
            }
        }
    }
}
// Restore unused event warning
#pragma warning restore CS0067