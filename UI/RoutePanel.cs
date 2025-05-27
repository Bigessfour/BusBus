// Suppress unassigned and unused field warnings for WinForms designer fields
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
#pragma warning disable CS0169 // The field is never used
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
#pragma warning disable CA1416 // Platform compatibility (Windows-only)
#pragma warning disable CS1998 // Async method lacks 'await' operators
// <auto-added>
#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Common;
using Microsoft.EntityFrameworkCore;

// Suppress unused event warning for this file
#pragma warning disable CS0067

namespace BusBus.UI
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
        private readonly IRouteService _routeService;
        private Route? _currentRoute;
        private TextBox? _nameTextBox;
        private DateTimePicker? _datePicker;
        private NumericUpDown? _amStartMileage, _amEndMileage, _amRiders;
        private NumericUpDown? _pmStartMileage, _pmEndMileage, _pmRiders;
        private ComboBox? _driverComboBox, _vehicleComboBox;
        private Button? _saveButton, _cancelButton;
        private Label? _titleLabel;
        // Additional UI controls for advanced functionality
        private DateTimePicker? _tripDatePicker;
        private DateTimePicker? _scheduledTimePicker;
        private TextBox? _startLocationTextBox;
        private TextBox? _endLocationTextBox;
        private Label? _errorLabel;
        private TableLayoutPanel? _tableLayoutPanel;
        private Button? _editButton;
        private Button? _deleteButton;

        // Mileage fields
        private NumericUpDown? _amStartingMileage;
        private NumericUpDown? _amEndingMileage;
        private NumericUpDown? _pmEndingMileage;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _disposedValue; public event EventHandler? SaveButtonClicked;
        public event EventHandler? CancelButtonClicked;
        public event EventHandler? DeleteButtonClicked;

        public RoutePanel(IRouteService routeService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
            this.Padding = new Padding(20);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 11,
                AutoSize = true
            };

            // Title
            _titleLabel = new Label
            {
                Text = "Route Details",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainLayout.Controls.Add(_titleLabel, 0, 0);
            mainLayout.SetColumnSpan(_titleLabel, 2);

            // Route Name
            var nameLabel = CreateLabel("Route Name:");
            _nameTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };
            mainLayout.Controls.Add(nameLabel, 0, 1);
            mainLayout.Controls.Add(_nameTextBox, 1, 1);

            // Date
            var dateLabel = CreateLabel("Date:");
            _datePicker = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short
            };
            mainLayout.Controls.Add(dateLabel, 0, 2);
            mainLayout.Controls.Add(_datePicker, 1, 2);

            // Driver
            var driverLabel = CreateLabel("Driver:");
            _driverComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            mainLayout.Controls.Add(driverLabel, 0, 3);
            mainLayout.Controls.Add(_driverComboBox, 1, 3);

            // Vehicle
            var vehicleLabel = CreateLabel("Vehicle:");
            _vehicleComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            mainLayout.Controls.Add(vehicleLabel, 0, 4);
            mainLayout.Controls.Add(_vehicleComboBox, 1, 4);

            // AM Section
            var amLabel = CreateLabel("AM Route:");
            amLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            mainLayout.Controls.Add(amLabel, 0, 5);
            mainLayout.SetColumnSpan(amLabel, 2);

            // AM Start Mileage
            var amStartLabel = CreateLabel("Start Mileage:");
            _amStartMileage = CreateNumericUpDown();
            mainLayout.Controls.Add(amStartLabel, 0, 6);
            mainLayout.Controls.Add(_amStartMileage, 1, 6);

            // AM End Mileage
            var amEndLabel = CreateLabel("End Mileage:");
            _amEndMileage = CreateNumericUpDown();
            mainLayout.Controls.Add(amEndLabel, 0, 7);
            mainLayout.Controls.Add(_amEndMileage, 1, 7);

            // AM Riders
            var amRidersLabel = CreateLabel("Riders:");
            _amRiders = CreateNumericUpDown();
            mainLayout.Controls.Add(amRidersLabel, 0, 8);
            mainLayout.Controls.Add(_amRiders, 1, 8);

            // PM Section
            var pmLabel = CreateLabel("PM Route:");
            pmLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            mainLayout.Controls.Add(pmLabel, 0, 9);
            mainLayout.SetColumnSpan(pmLabel, 2);

            // PM Start Mileage
            var pmStartLabel = CreateLabel("Start Mileage:");
            _pmStartMileage = CreateNumericUpDown();
            mainLayout.Controls.Add(pmStartLabel, 0, 10);
            mainLayout.Controls.Add(_pmStartMileage, 1, 10);

            // PM End Mileage
            var pmEndLabel = CreateLabel("End Mileage:");
            _pmEndMileage = CreateNumericUpDown();
            mainLayout.Controls.Add(pmEndLabel, 0, 11);
            mainLayout.Controls.Add(_pmEndMileage, 1, 11);

            // PM Riders
            var pmRidersLabel = CreateLabel("Riders:");
            _pmRiders = CreateNumericUpDown();
            mainLayout.Controls.Add(pmRidersLabel, 0, 12);
            mainLayout.Controls.Add(_pmRiders, 1, 12);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            _cancelButton.Click += (s, e) => CancelButtonClicked?.Invoke(this, EventArgs.Empty);

            _saveButton = new Button
            {
                Text = "Save",
                Size = new Size(100, 35),
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            _saveButton.Click += SaveButton_Click;

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_saveButton);

            this.Controls.Add(mainLayout);
            this.Controls.Add(buttonPanel);

            // Load initial data
            LoadDropdownData();
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill
            };
        }

        private static NumericUpDown CreateNumericUpDown()
        {
            return new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Maximum = 999999,
                Minimum = 0
            };
        }
        /// <summary>
        /// Creates a Route object from the current form fields.
        /// </summary>
        private Route CreateRouteFromForm()
        {
            var route = _currentRoute ?? new Route();
            route.Name = _nameTextBox!.Text;
            route.RouteDate = _datePicker!.Value;
            route.AMStartingMileage = (int)_amStartMileage!.Value;
            route.AMEndingMileage = (int)_amEndMileage!.Value;
            route.AMRiders = (int)_amRiders!.Value;
            route.PMStartMileage = (int)_pmStartMileage!.Value;
            route.PMEndingMileage = (int)_pmEndMileage!.Value;
            route.PMRiders = (int)_pmRiders!.Value;

            // Driver
            if (_driverComboBox!.SelectedItem != null)
            {
                var driverId = _driverComboBox.SelectedItem.GetType().GetProperty("Id")?.GetValue(_driverComboBox.SelectedItem) as Guid?;
                route.DriverId = driverId;
            }
            else
            {
                route.DriverId = null;
            }

            // Vehicle
            if (_vehicleComboBox!.SelectedItem != null)
            {
                var vehicleId = _vehicleComboBox.SelectedItem.GetType().GetProperty("Id")?.GetValue(_vehicleComboBox.SelectedItem) as Guid?;
                route.VehicleId = vehicleId;
            }
            else
            {
                route.VehicleId = null;
            }

            return route;
        }
        private async void LoadDropdownData()
        {
            try
            {
                if (_routeService == null || IsDisposed)
                    return;

                var drivers = await _routeService.GetDriversAsync();
                if (IsDisposed) return;
                _driverComboBox!.Items.Clear();
                _driverComboBox!.Items.Add(new { Id = (Guid?)null, Display = "-- No Driver --" });
                foreach (var driver in drivers)
                {
                    _driverComboBox!.Items.Add(new { Id = (Guid?)driver.Id, Display = $"{driver.FirstName} {driver.LastName}" });
                }
                _driverComboBox!.DisplayMember = "Display";
                _driverComboBox!.ValueMember = "Id";

                var vehicles = await _routeService.GetVehiclesAsync();
                if (IsDisposed) return;
                _vehicleComboBox!.Items.Clear();
                _vehicleComboBox!.Items.Add(new { Id = (Guid?)null, Display = "-- No Vehicle --" });
                foreach (var vehicle in vehicles)
                {
                    _vehicleComboBox!.Items.Add(new { Id = (Guid?)vehicle.Id, Display = vehicle.BusNumber });
                }
                _vehicleComboBox!.DisplayMember = "Display";
                _vehicleComboBox!.ValueMember = "Id";
            }
            catch (ObjectDisposedException)
            {
                // Swallow: control or service disposed during async load
                Console.WriteLine("[RoutePanel] Service disposed during LoadDropdownData");
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    Console.WriteLine($"[RoutePanel] Error loading dropdown data: {ex.Message}");
                    // Only show message box if not disposed and not a disposal-related error
                    if (!ex.Message.Contains("disposed", StringComparison.OrdinalIgnoreCase))
                        MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void LoadRoute(Route route)
        {
            _currentRoute = route;
            if (route != null)
            {
                _nameTextBox!.Text = route.Name;
                _datePicker!.Value = route.RouteDate;
                _amStartMileage!.Value = route.AMStartingMileage;
                _amEndMileage!.Value = route.AMEndingMileage;
                _amRiders!.Value = route.AMRiders;
                _pmStartMileage!.Value = route.PMStartMileage;
                _pmEndMileage!.Value = route.PMEndingMileage;
                _pmRiders!.Value = route.PMRiders;
                // Set driver
                foreach (var item in _driverComboBox!.Items)
                {
                    if (item is object obj && obj.GetType().GetProperty("Id")?.GetValue(obj) is Guid id && id == route.DriverId)
                    {
                        _driverComboBox!.SelectedItem = item;
                        break;
                    }
                }

                // Set vehicle
                foreach (var item in _vehicleComboBox!.Items)
                {
                    if (item is object obj && obj.GetType().GetProperty("Id")?.GetValue(obj) is Guid id && id == route.VehicleId)
                    {
                        _vehicleComboBox!.SelectedItem = item;
                        break;
                    }
                }

                _titleLabel!.Text = route.Id == Guid.Empty ? "New Route" : "Edit Route";
            }
        }

        private async void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_currentRoute == null)
                {
                    _currentRoute = new Route { Id = Guid.NewGuid() };
                }

                _currentRoute.Name = _nameTextBox!.Text;
                _currentRoute.RouteDate = _datePicker!.Value;
                _currentRoute.AMStartingMileage = (int)_amStartMileage!.Value;
                _currentRoute.AMEndingMileage = (int)_amEndMileage!.Value;
                _currentRoute.AMRiders = (int)_amRiders!.Value;
                _currentRoute.PMStartMileage = (int)_pmStartMileage!.Value;
                _currentRoute.PMEndingMileage = (int)_pmEndMileage!.Value;
                _currentRoute.PMRiders = (int)_pmRiders!.Value;

                if (_driverComboBox!.SelectedItem != null)
                {
                    var driverId = _driverComboBox!.SelectedItem.GetType().GetProperty("Id")?.GetValue(_driverComboBox!.SelectedItem) as Guid?;
                    _currentRoute.DriverId = driverId;
                }

                if (_vehicleComboBox!.SelectedItem != null)
                {
                    var vehicleId = _vehicleComboBox!.SelectedItem.GetType().GetProperty("Id")?.GetValue(_vehicleComboBox!.SelectedItem) as Guid?;
                    _currentRoute.VehicleId = vehicleId;
                }

                if (_currentRoute.Id == Guid.Empty)
                {
                    await _routeService.CreateRouteAsync(_currentRoute);
                }
                else
                {
                    await _routeService.UpdateRouteAsync(_currentRoute);
                }

                SaveButtonClicked?.Invoke(this, EventArgs.Empty);
                MessageBox.Show("Route saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Invalid data: {ex.Message}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (DbUpdateConcurrencyException)
            {
                MessageBox.Show("The route was modified or deleted by another user. Please reload and try again.", "Concurrency Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving route: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _saveButton!.Click -= SaveButton_Click;
                    _cancelButton!.Click -= (s, e) => CancelButtonClicked?.Invoke(this, EventArgs.Empty);
                }
                _disposedValue = true;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Saves the current route data.
        /// </summary>
        private async Task<bool> SaveRouteAsync()
        {
            try
            {
                var route = CreateRouteFromForm();

                if (_currentRoute?.Id == Guid.Empty)
                {
                    // Create new route
                    var createdRoute = await _routeService.CreateRouteAsync(route);
                    _currentRoute = createdRoute;

                    // Notify listeners about the save
                    SaveButtonClicked?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Update existing route
                    var updatedRoute = await _routeService.UpdateRouteAsync(route);
                    _currentRoute = updatedRoute;

                    // Switch back to view mode
                    SetFormEditable(false);
                    _saveButton!.Visible = false;
                    _cancelButton!.Visible = false;
                    _editButton!.Visible = true;
                    _deleteButton!.Visible = true;

                    // Notify listeners about the update
                    SaveButtonClicked?.Invoke(this, EventArgs.Empty);
                }

                return true;
            }
            catch (InvalidOperationException ex)
            {
                _errorLabel!.Text = $"Error saving route: {ex.Message}";
                _errorLabel!.Visible = true;
                return false;
            }
            catch (HttpRequestException ex)
            {
                _errorLabel!.Text = $"Network error: {ex.Message}";
                _errorLabel!.Visible = true;
                return false;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                _errorLabel!.Text = $"Unexpected error: {ex.Message}";
                _errorLabel!.Visible = true;
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
            if (_currentRoute != null && _currentRoute.Id != Guid.Empty)
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
                    _errorLabel!.Text = $"Failed to delete route: {ex.Message}";
                    _errorLabel!.Visible = true;
                }
            }
        }

        // Method to initialize the UI
        private void InitializeUI()
        {            // Create main layout
            _tableLayoutPanel!.Dock = DockStyle.Fill;
            _tableLayoutPanel.ColumnCount = 2;
            _tableLayoutPanel.RowCount = 16; // Increased to fit all rows including new fields and buttons
            _tableLayoutPanel.BackColor = ThemeManager.CurrentTheme.CardBackground;
            _tableLayoutPanel.Padding = new Padding(10);

            // Configure column styles
            _tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            _tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            // Configure row styles
            // Rows 0 (Title), 1-13 (Fields), 14 (Error), 15 (Buttons)
            for (int i = 0; i < _tableLayoutPanel.RowCount; i++)
            {
                if (i == 15) // Button panel row
                {
                    _tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Fixed height for button row
                }
                else
                {
                    _tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                }
            }

            // Add title label
            _titleLabel!.Text = "Route Details";
            _titleLabel!.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            _titleLabel!.ForeColor = ThemeManager.CurrentTheme.CardText;
            _tableLayoutPanel!.Controls.Add(_titleLabel, 0, 0);
            _tableLayoutPanel!.SetColumnSpan(_titleLabel, 2);            // Configure DateTimePicker
            _tripDatePicker!.Format = DateTimePickerFormat.Short;
            _tripDatePicker!.Width = 200;

            // Configure ScheduledTime DateTimePicker
            _scheduledTimePicker!.Format = DateTimePickerFormat.Custom;
            _scheduledTimePicker!.CustomFormat = "MM/dd/yyyy hh:mm tt";
            _scheduledTimePicker!.Width = 200;

            // Configure numeric inputs
            _amStartingMileage!.Maximum = 999999;
            _amEndingMileage!.Maximum = 999999;
            _amRiders!.Maximum = 999;
            _pmStartMileage!.Maximum = 999999;
            _pmEndingMileage!.Maximum = 999999;
            _pmRiders!.Maximum = 999;

            // Add form fields with labels
            AddFormRow("Route Name:", _nameTextBox!, 1);
            AddFormRow("Trip Date:", _tripDatePicker!, 2);
            AddFormRow("Start Location:", _startLocationTextBox!, 3);
            AddFormRow("End Location:", _endLocationTextBox!, 4);
            AddFormRow("Scheduled Time:", _scheduledTimePicker!, 5);
            AddFormRow("AM Starting Mileage:", _amStartingMileage!, 6);
            AddFormRow("AM Ending Mileage:", _amEndingMileage!, 7);
            AddFormRow("AM Riders:", _amRiders!, 8);
            AddFormRow("PM Starting Mileage:", _pmStartMileage!, 9);
            AddFormRow("PM Ending Mileage:", _pmEndingMileage!, 10);
            AddFormRow("PM Riders:", _pmRiders!, 11);
            AddFormRow("Driver:", _driverComboBox!, 12);
            AddFormRow("Vehicle:", _vehicleComboBox!, 13);            // Configure error label
            _errorLabel!.ForeColor = System.Drawing.Color.Red;
            _errorLabel!.AutoSize = true;
            _errorLabel!.Visible = false;
            _tableLayoutPanel!.Controls.Add(_errorLabel, 0, 14);
            _tableLayoutPanel!.SetColumnSpan(_errorLabel, 2);

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
            ConfigureButton(_saveButton!, "Save", ThemeManager.CurrentTheme.ButtonBackground);
            ConfigureButton(_editButton!, "Edit", ThemeManager.CurrentTheme.ButtonBackground);
            ConfigureButton(_cancelButton!, "Cancel", ThemeManager.CurrentTheme.ButtonBackground);
            ConfigureButton(_deleteButton!, "Delete", System.Drawing.Color.FromArgb(220, 53, 69)); // Red for delete

            // Set button names for proper identification
            _saveButton!.Name = "SaveButton";
            _editButton!.Name = "EditButton";
            _cancelButton!.Name = "CancelButton";
            _deleteButton!.Name = "DeleteButton";

            // Add buttons to panel
            buttonPanel.Controls.Add(_saveButton!, 0, 0);
            buttonPanel.Controls.Add(_editButton!, 1, 0);
            buttonPanel.Controls.Add(_cancelButton!, 2, 0);
            buttonPanel.Controls.Add(_deleteButton!, 3, 0);

            // Set equal widths for buttons
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));            // Add button panel to main layout
            _tableLayoutPanel!.Controls.Add(buttonPanel, 0, 15);
            _tableLayoutPanel!.SetColumnSpan(buttonPanel, 2);

            // Add the edit button click event handler
            _editButton!.Click += EditButton_Click;

            // Add the main layout to the control
            this.Controls.Add(_tableLayoutPanel!);
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

            _tableLayoutPanel!.Controls.Add(label, 0, rowIndex);
            _tableLayoutPanel!.Controls.Add(control, 1, rowIndex);
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
                _editButton!.Visible = false;
                _saveButton!.Visible = true;
                _cancelButton!.Visible = true;
                _deleteButton!.Visible = true;
            }
        }        // Helper method to set form fields editable or read-only
        private void SetFormEditable(bool editable)
        {
            _nameTextBox!.Enabled = editable;
            _tripDatePicker!.Enabled = editable;
            _startLocationTextBox!.Enabled = editable;
            _endLocationTextBox!.Enabled = editable;
            _scheduledTimePicker!.Enabled = editable;
            _amStartingMileage!.Enabled = editable;
            _amEndingMileage!.Enabled = editable;
            _amRiders!.Enabled = editable;
            _pmStartMileage!.Enabled = editable;
            _pmEndingMileage!.Enabled = editable;
            _pmRiders!.Enabled = editable;
            _driverComboBox!.Enabled = editable;
            _driverComboBox!.Enabled = editable;
            _vehicleComboBox!.Enabled = editable;
        }
    }


    // Restore unused event warning
#pragma warning restore CS0067
}
