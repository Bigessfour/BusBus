using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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
        private readonly RouteCRUDHelper _crudHelper;
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
        public event EventHandler? CancelButtonClicked;        public RoutePanel(IRouteService routeService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _crudHelper = new RouteCRUDHelper(routeService);
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

            // Initialize the UI
            InitializeUI();

            LoadDriversAndVehicles(); // Load drivers and vehicles
            Console.WriteLine("[RoutePanel] Event handlers hooked");
        }        // Modified constructor to directly initialize from lists (used in tests)
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

            // Initialize the UI to configure control properties (Maximum values, etc.)
            InitializeUI();
        }private void CancelEdit()
        {
            // If it's a new route being edited, invoke the cancel event to close the panel
            if (_currentRoute == null || _currentRoute.Id == Guid.Empty)
            {
                CancelButtonClicked?.Invoke(this, EventArgs.Empty);
            }
            // For existing routes, just revert to view mode
            else
            {
                // Reset form with original route values
                LoadRoute(_currentRoute);
                
                // Update button visibility
                _saveButton.Visible = false;
                _cancelButton.Visible = false;
                _editButton.Visible = true;
                _deleteButton.Visible = true;
                
                // Disable editing
                SetFormEditable(false);
            }
        }// Method to load a route for editing
        public void LoadRoute(Route route)
        {
            ArgumentNullException.ThrowIfNull(route);

            _currentRoute = route;
            _errorLabel.Visible = false;

            // Update the form fields
            _tripDatePicker.Value = route.RouteDate;
            _amStartingMileage.Value = ClampNumeric(_amStartingMileage, route.AMStartingMileage);
            _amEndingMileage.Value = ClampNumeric(_amEndingMileage, route.AMEndingMileage);
            _amRiders.Value = ClampNumeric(_amRiders, route.AMRiders);
            _pmStartMileage.Value = ClampNumeric(_pmStartMileage, route.PMStartMileage);
            _pmEndingMileage.Value = ClampNumeric(_pmEndingMileage, route.PMEndingMileage);
            _pmRiders.Value = ClampNumeric(_pmRiders, route.PMRiders);

            // Update the title
            _titleLabel.Text = route.Id == Guid.Empty ? "Add New Route" : $"Edit Route: {route.Name}";

            // Setup buttons visibility and event handlers
            bool isNewRoute = route.Id == Guid.Empty;

            // For new routes, enable editing and show save/cancel buttons
            if (isNewRoute)
            {
                SetFormEditable(true);
                _saveButton.Visible = true;
                _cancelButton.Visible = true;
                _editButton.Visible = false;
                _deleteButton.Visible = false;
            }
            // For existing routes, show edit button initially
            else
            {
                SetFormEditable(false);
                _saveButton.Visible = false;
                _cancelButton.Visible = false;
                _editButton.Visible = true;
                _deleteButton.Visible = true;

                // Set up delete button click handler
                _deleteButton.Click -= DeleteButton_Click;
                _deleteButton.Click += DeleteButton_Click;
            }

            // Update driver and vehicle combo boxes
            UpdateComboBoxes(route.DriverId, route.VehicleId);
        }

        /// <summary>
        /// Clamps a value to the valid range of a NumericUpDown control.
        /// </summary>
        private static decimal ClampNumeric(NumericUpDown control, int value)
        {
            return Math.Min(Math.Max(value, (int)control.Minimum), (int)control.Maximum);
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
        }        private async Task<bool> SaveRouteAsync()
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
                    null;                // Validate the route
                var (isValid, errorMessage) = RouteCRUDHelper.ValidateRoute(route);
                if (!isValid)
                {
                    _errorLabel.Text = errorMessage;
                    _errorLabel.Visible = true;
                    return false;
                }                // Determine if we're creating or updating
                if (isNewRoute)
                {
                    if (string.IsNullOrWhiteSpace(route.Name))
                    {
                        route.Name = $"Route {DateTime.Now:yyyyMMdd-HHmmss}";
                    }
                    // Create new route
                    var createdRoute = await _crudHelper.CreateRouteAsync(route);
                    _currentRoute = createdRoute;
                    
                    // Notify listeners about the save
                    SaveButtonClicked?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Update existing route
                    var updatedRoute = await _crudHelper.UpdateRouteAsync(route);
                    _currentRoute = updatedRoute;
                    
                    // Switch back to view mode
                    SetFormEditable(false);
                    _saveButton.Visible = false;
                    _cancelButton.Visible = false;
                    _editButton.Visible = true;
                    _deleteButton.Visible = true;
                    
                    // Notify listeners about the update
                    UpdateButtonClicked?.Invoke(this, EventArgs.Empty);
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
        }        private async void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_currentRoute != null && _currentRoute.Id != Guid.Empty)
            {
                try
                {
                    var result = SuppressDialogsForTests ? DialogResult.Yes :
                        MessageBox.Show($"Are you sure you want to delete the route '{_currentRoute.Name}'?",
                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        await _crudHelper.DeleteRouteAsync(_currentRoute.Id);
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

        // Method to initialize the UI
        private void InitializeUI()
        {
            // Create main layout
            _tableLayoutPanel.Dock = DockStyle.Fill;
            _tableLayoutPanel.ColumnCount = 2;
            _tableLayoutPanel.RowCount = 12; // Increased to fit all rows including buttons
            _tableLayoutPanel.BackColor = ThemeManager.CurrentTheme.CardBackground;
            _tableLayoutPanel.Padding = new Padding(10);

            // Configure column styles
            _tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            _tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            // Configure row styles
            // Rows 0 (Title), 1-9 (Fields), 10 (Error), 11 (Buttons)
            for (int i = 0; i < _tableLayoutPanel.RowCount; i++)
            {
                if (i == 11) // Button panel row
                {
                    _tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Fixed height for button row
                }
                else
                {
                    _tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                }
            }

            // Add title label
            _titleLabel.Text = "Route Details";
            _titleLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            _titleLabel.ForeColor = ThemeManager.CurrentTheme.CardText;
            _tableLayoutPanel.Controls.Add(_titleLabel, 0, 0);
            _tableLayoutPanel.SetColumnSpan(_titleLabel, 2);

            // Configure DateTimePicker
            _tripDatePicker.Format = DateTimePickerFormat.Short;
            _tripDatePicker.Width = 200;
            
            // Configure numeric inputs
            _amStartingMileage.Maximum = 999999;
            _amEndingMileage.Maximum = 999999;
            _amRiders.Maximum = 999;
            _pmStartMileage.Maximum = 999999;
            _pmEndingMileage.Maximum = 999999;
            _pmRiders.Maximum = 999;

            // Add form fields with labels
            AddFormRow("Trip Date:", _tripDatePicker, 1);
            AddFormRow("AM Starting Mileage:", _amStartingMileage, 2);
            AddFormRow("AM Ending Mileage:", _amEndingMileage, 3);
            AddFormRow("AM Riders:", _amRiders, 4);
            AddFormRow("PM Starting Mileage:", _pmStartMileage, 5);
            AddFormRow("PM Ending Mileage:", _pmEndingMileage, 6);
            AddFormRow("PM Riders:", _pmRiders, 7);
            AddFormRow("Driver:", _driverComboBox, 8);
            AddFormRow("Vehicle:", _vehicleComboBox, 9);

            // Configure error label
            _errorLabel.ForeColor = System.Drawing.Color.Red;
            _errorLabel.AutoSize = true;
            _errorLabel.Visible = false;
            _tableLayoutPanel.Controls.Add(_errorLabel, 0, 10);
            _tableLayoutPanel.SetColumnSpan(_errorLabel, 2);

            // Create button panel for CRUD operations
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Visible = true // Explicitly set Visible to true
            };
              // Configure CRUD buttons
            ConfigureButton(_saveButton, "Save", ThemeManager.CurrentTheme.ButtonBackground);
            ConfigureButton(_editButton, "Edit", ThemeManager.CurrentTheme.ButtonBackground);
            ConfigureButton(_cancelButton, "Cancel", ThemeManager.CurrentTheme.ButtonBackground);
            ConfigureButton(_deleteButton, "Delete", System.Drawing.Color.FromArgb(220, 53, 69)); // Red for delete
            
            // Set button names for proper identification
            _saveButton.Name = "SaveButton";
            _editButton.Name = "EditButton";
            _cancelButton.Name = "CancelButton";
            _deleteButton.Name = "DeleteButton";

            // Add buttons to panel
            buttonPanel.Controls.Add(_saveButton, 0, 0);
            buttonPanel.Controls.Add(_editButton, 1, 0);
            buttonPanel.Controls.Add(_cancelButton, 2, 0);
            buttonPanel.Controls.Add(_deleteButton, 3, 0);

            // Set equal widths for buttons
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // Add button panel to main layout
            _tableLayoutPanel.Controls.Add(buttonPanel, 0, 11);
            _tableLayoutPanel.SetColumnSpan(buttonPanel, 2);

            // Add the edit button click event handler
            _editButton.Click += EditButton_Click;

            // Add the main layout to the control
            this.Controls.Add(_tableLayoutPanel);
        }

        // Helper method to add a row with label and control
        private void AddFormRow(string labelText, Control control, int rowIndex)
        {
            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = ThemeManager.CurrentTheme.CardText
            };
            
            control.Dock = DockStyle.Fill;
            
            _tableLayoutPanel.Controls.Add(label, 0, rowIndex);
            _tableLayoutPanel.Controls.Add(control, 1, rowIndex);
        }        // Helper method to configure button appearance
        private static void ConfigureButton(Button button, string text, System.Drawing.Color backColor)
        {
            button.Text = text;
            button.BackColor = backColor;
            button.ForeColor = ThemeManager.CurrentTheme.CardText;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(5);
            button.Font = new System.Drawing.Font("Segoe UI", 10F);
        }

        // Edit button click event handler
        private void EditButton_Click(object? sender, EventArgs e)
        {
            if (_currentRoute != null)
            {
                // Enable editing of form fields
                SetFormEditable(true);
                
                // Hide Edit button and show Save/Cancel buttons
                _editButton.Visible = false;
                _saveButton.Visible = true;
                _cancelButton.Visible = true;
                _deleteButton.Visible = true;
            }
        }

        // Helper method to set form fields editable or read-only
        private void SetFormEditable(bool editable)
        {
            _tripDatePicker.Enabled = editable;
            _amStartingMileage.Enabled = editable;
            _amEndingMileage.Enabled = editable;
            _amRiders.Enabled = editable;
            _pmStartMileage.Enabled = editable;
            _pmEndingMileage.Enabled = editable;
            _pmRiders.Enabled = editable;
            _driverComboBox.Enabled = editable;
            _vehicleComboBox.Enabled = editable;
        }
    }
}
// Restore unused event warning
#pragma warning restore CS0067