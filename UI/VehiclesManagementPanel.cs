using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI
{
    /// <summary>
    /// Comprehensive vehicles management panel with Crystal Dark styling and full CRUD operations
    /// </summary>
    public partial class VehiclesManagementPanel : ThemeableControl, IDisplayable, IView
#pragma warning disable CS0067 // Event is never used
#pragma warning disable CS8622 // Nullability of reference types in event handlers
#pragma warning disable CS8602 // Dereference of a possibly null reference
#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments
#pragma warning disable CA1822 // Member does not access instance data and can be marked as static
    {
        private readonly IVehicleService _vehicleService;
        private DataGridView? _dataGridView;
        private Button? _addButton;
        private Button? _editButton;
        private Button? _deleteButton;
        private Button? _refreshButton;
        private BindingList<VehicleDisplayDTO> _vehicles;
        private readonly CancellationTokenSource _cancellationTokenSource;        // IView implementation
        public string ViewName => "VehiclesManagement";
        public string Title => "Vehicles Management";
        public Control? Control => this;
        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        public event EventHandler<StatusEventArgs>? StatusUpdated;

        public VehiclesManagementPanel(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            _vehicles = new BindingList<VehicleDisplayDTO>();
            _cancellationTokenSource = new CancellationTokenSource();

            InitializeComponent();
            ConfigureDataGridView();
            CreateCrystalDarkButtons();

            // Load data asynchronously
            _ = LoadDataAsync(_cancellationTokenSource.Token);
        }

        private void InitializeComponent()
        {
            // Main panel setup
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(32, 32, 36);
            Padding = new Padding(10);

            // Create main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };

            // Configure rows: buttons panel (auto), data grid (fill)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Create buttons panel
            var buttonsPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 10)
            };

            // Create data grid view
            _dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.FromArgb(45, 45, 48),
                GridColor = Color.FromArgb(70, 70, 74),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(62, 62, 66),
                    ForeColor = Color.FromArgb(241, 241, 241),
                    SelectionBackColor = Color.FromArgb(51, 153, 255),
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.FromArgb(241, 241, 241),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                EnableHeadersVisualStyles = false
            };

            // Add components to layout
            mainLayout.Controls.Add(buttonsPanel, 0, 0);
            mainLayout.Controls.Add(_dataGridView, 0, 1);
            Controls.Add(mainLayout);

            // Event handlers
            _dataGridView!.CellBeginEdit += DataGridView_CellBeginEdit;
            _dataGridView.CellEndEdit += DataGridView_CellEndEdit;
            _dataGridView.DataError += DataGridView_DataError;
            _dataGridView.SelectionChanged += DataGridView_SelectionChanged;
        }

        private void ConfigureDataGridView()
        {
            // Primary Key (hidden)
            _dataGridView!.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "VehicleId",
                HeaderText = "ID",
                DataPropertyName = "VehicleId",
                Visible = false
            });

            // Bus Number
            _dataGridView!.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BusNumber",
                HeaderText = "Bus Number",
                DataPropertyName = "BusNumber",
                Width = 100,
                MinimumWidth = 80
            });

            // Model Year
            _dataGridView!.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ModelYear",
                HeaderText = "Year",
                DataPropertyName = "ModelYear",
                Width = 70,
                MinimumWidth = 60
            });

            // Make
            _dataGridView!.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Make",
                HeaderText = "Make",
                DataPropertyName = "Make",
                Width = 120,
                MinimumWidth = 100
            });

            // Model
            _dataGridView!.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Model",
                HeaderText = "Model",
                DataPropertyName = "Model",
                Width = 120,
                MinimumWidth = 100
            });

            // VIN Number
            _dataGridView!.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "VINNumber",
                HeaderText = "VIN Number",
                DataPropertyName = "VINNumber",
                Width = 150,
                MinimumWidth = 120
            });

            // Capacity
            _dataGridView!.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Capacity",
                HeaderText = "Capacity",
                DataPropertyName = "Capacity",
                Width = 80,
                MinimumWidth = 70
            });

            // Last Inspection Date
            _dataGridView!.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LastInspectionDate",
                HeaderText = "Last Inspection",
                DataPropertyName = "LastInspectionDateFormatted",
                Width = 120,
                MinimumWidth = 100
            });

            // Status ComboBox
            var statusColumn = new DataGridViewComboBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                DataPropertyName = "Status",
                Width = 100,
                MinimumWidth = 90,
                FlatStyle = FlatStyle.Flat
            };

            statusColumn.Items.AddRange(new[] { "Active", "Inactive", "Maintenance", "Out of Service" });
            statusColumn.DefaultCellStyle.BackColor = Color.FromArgb(70, 70, 74);
            statusColumn.DefaultCellStyle.ForeColor = Color.FromArgb(241, 241, 241);
            _dataGridView.Columns.Add(statusColumn);

            // Bind data
            _dataGridView.DataSource = _vehicles;

            // Configure auto-sizing
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _dataGridView.Columns["BusNumber"].FillWeight = 80;
            _dataGridView.Columns["ModelYear"].FillWeight = 60;
            _dataGridView.Columns["Make"].FillWeight = 100;
            _dataGridView.Columns["Model"].FillWeight = 100;
            _dataGridView.Columns["VINNumber"].FillWeight = 120;
            _dataGridView.Columns["Capacity"].FillWeight = 70;
            _dataGridView.Columns["LastInspectionDate"].FillWeight = 100;
            _dataGridView.Columns["Status"].FillWeight = 90;
        }

        private void CreateCrystalDarkButtons()
        {
            var buttonPanel = (Panel)Controls[0].Controls[0];

            // Create buttons with Crystal Dark styling
            _addButton = CreateCrystalDarkButton("Add Vehicle", "Add a new vehicle to the fleet");
            _editButton = CreateCrystalDarkButton("Edit Vehicle", "Edit the selected vehicle");
            _deleteButton = CreateCrystalDarkButton("Delete Vehicle", "Delete the selected vehicle");
            _refreshButton = CreateCrystalDarkButton("Refresh", "Refresh the vehicle list");

            // Position buttons
            _addButton.Location = new Point(10, 15);
            _editButton.Location = new Point(130, 15);
            _deleteButton.Location = new Point(250, 15);
            _refreshButton.Location = new Point(370, 15);

            // Wire up events
            _addButton.Click += AddButton_Click;
            _editButton.Click += EditButton_Click;
            _deleteButton.Click += DeleteButton_Click;
            _refreshButton.Click += RefreshButton_Click;

            // Add to panel
            buttonPanel.Controls.AddRange(new Control[] { _addButton, _editButton, _deleteButton, _refreshButton });

            // Initially disable edit/delete until selection
            _editButton.Enabled = false;
            _deleteButton.Enabled = false;
        }

        private Button CreateCrystalDarkButton(string text, string tooltip)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(70, 130, 180);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 65);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 50, 55);

            // Add subtle hover effect
            button.MouseEnter += (s, e) => button.BackColor = Color.FromArgb(60, 60, 65);
            button.MouseLeave += (s, e) => button.BackColor = Color.FromArgb(40, 40, 45);

            // Add tooltip
            if (!string.IsNullOrEmpty(tooltip))
            {
                var toolTip = new ToolTip();
                toolTip.SetToolTip(button, tooltip);
            }

            return button;
        }

        private async Task LoadDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                var vehicles = await _vehicleService.GetAllVehiclesAsync(cancellationToken);
                var vehicleDisplayDTOs = vehicles.Select(VehicleDisplayDTO.FromVehicle).ToList();

                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        _vehicles.Clear();
                        foreach (var vehicle in vehicleDisplayDTOs)
                        {
                            _vehicles.Add(vehicle);
                        }
                    }));
                }
                else
                {
                    _vehicles.Clear();
                    foreach (var vehicle in vehicleDisplayDTOs)
                    {
                        _vehicles.Add(vehicle);
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                // Fallback to sample data if service is unavailable
                var sampleVehicles = GetSampleVehicles();
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        _vehicles.Clear();
                        foreach (var vehicle in sampleVehicles)
                        {
                            _vehicles.Add(vehicle);
                        }
                    }));
                }
                else
                {
                    _vehicles.Clear();
                    foreach (var vehicle in sampleVehicles)
                    {
                        _vehicles.Add(vehicle);
                    }
                }

                MessageBox.Show($"Could not load vehicles from service. Using sample data.\nError: {ex.Message}",
                    "Service Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => Cursor = Cursors.Default));
                }
                else
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private List<VehicleDisplayDTO> GetSampleVehicles()
#pragma warning restore CS0067
#pragma warning restore CS8622
#pragma warning restore CS8602
#pragma warning restore CA1861
#pragma warning restore CA1822
        {
            return new List<VehicleDisplayDTO>
            {
                new VehicleDisplayDTO
                {
                    VehicleId = 1,
                    BusNumber = "001",
                    ModelYear = 2020,
                    Make = "Blue Bird",
                    Model = "Vision",
                    VINNumber = "1BAKC2CA5LF123456",
                    Capacity = 72,
                    LastInspectionDate = new DateTime(2023, 9, 15),
                    Status = "Active"
                },
                new VehicleDisplayDTO
                {
                    VehicleId = 2,
                    BusNumber = "002",
                    ModelYear = 2019,
                    Make = "Thomas Built",
                    Model = "Saf-T-Liner C2",
                    VINNumber = "4DRAXAA59KB234567",
                    Capacity = 48,
                    LastInspectionDate = new DateTime(2023, 8, 22),
                    Status = "Active"
                },
                new VehicleDisplayDTO
                {
                    VehicleId = 3,
                    BusNumber = "003",
                    ModelYear = 2018,
                    Make = "IC Bus",
                    Model = "CE Series",
                    VINNumber = "1HVBJABN3JH345678",
                    Capacity = 54,
                    LastInspectionDate = new DateTime(2023, 7, 10),
                    Status = "Maintenance"
                },
                new VehicleDisplayDTO
                {
                    VehicleId = 4,
                    BusNumber = "004",
                    ModelYear = 2021,
                    Make = "Blue Bird",
                    Model = "All American",
                    VINNumber = "1BAKD2CA9MF456789",
                    Capacity = 66,
                    LastInspectionDate = new DateTime(2023, 10, 5),
                    Status = "Active"
                },
                new VehicleDisplayDTO
                {
                    VehicleId = 5,
                    BusNumber = "005",
                    ModelYear = 2017,
                    Make = "Thomas Built",
                    Model = "Saf-T-Liner HDX",
                    VINNumber = "4DRAXAA75HB567890",
                    Capacity = 90,
                    LastInspectionDate = new DateTime(2023, 6, 18),
                    Status = "Out of Service"
                }
            };
        }

        private async void AddButton_Click(object? sender, EventArgs e)
        {
            try
            {
                using (var addForm = new VehicleEditForm())
                {
                    if (addForm.ShowDialog() == DialogResult.OK)
                    {
                        var newVehicle = addForm.Vehicle;

                        // Try to add via service
                        try
                        {
                            var addedVehicle = await _vehicleService.CreateAsync(newVehicle, _cancellationTokenSource.Token);
                            var vehicleDisplayDTO = VehicleDisplayDTO.FromVehicle(addedVehicle);
                            _vehicles.Add(vehicleDisplayDTO);
                        }
                        catch (Exception ex)
                        {
                            // Fallback: add to local list with temporary ID
                            var maxId = _vehicles.Any() ? _vehicles.Max(v => v.VehicleId) : 0;
                            var vehicleDisplayDTO = VehicleDisplayDTO.FromVehicle(newVehicle);
                            vehicleDisplayDTO.VehicleId = maxId + 1;
                            _vehicles.Add(vehicleDisplayDTO);

                            MessageBox.Show($"Vehicle added locally. Service error: {ex.Message}",
                                "Service Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding vehicle: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void EditButton_Click(object? sender, EventArgs e)
        {
            if (_dataGridView == null || _dataGridView.SelectedRows.Count == 0) return;

            try
            {
                var selectedVehicle = (VehicleDisplayDTO)_dataGridView.SelectedRows[0].DataBoundItem;
                var vehicle = selectedVehicle.ToVehicle();

                using (var editForm = new VehicleEditForm(vehicle))
                {
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        var updatedVehicle = editForm.Vehicle;

                        // Try to update via service
                        try
                        {
                            await _vehicleService.UpdateAsync(updatedVehicle, _cancellationTokenSource.Token);

                            // Update local list
                            var updatedDisplayDTO = VehicleDisplayDTO.FromVehicle(updatedVehicle);
                            var index = _vehicles.IndexOf(selectedVehicle);
                            if (index >= 0)
                            {
                                _vehicles[index] = updatedDisplayDTO;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Fallback: update local list only
                            var updatedDisplayDTO = VehicleDisplayDTO.FromVehicle(updatedVehicle);
                            var index = _vehicles.IndexOf(selectedVehicle);
                            if (index >= 0)
                            {
                                _vehicles[index] = updatedDisplayDTO;
                            }

                            MessageBox.Show($"Vehicle updated locally. Service error: {ex.Message}",
                                "Service Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing vehicle: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_dataGridView == null || _dataGridView.SelectedRows.Count == 0) return;

            try
            {
                var selectedVehicle = (VehicleDisplayDTO)_dataGridView.SelectedRows[0].DataBoundItem;

                var result = MessageBox.Show(
                    $"Are you sure you want to delete vehicle '{selectedVehicle.BusNumber}'?",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Try to delete via service
                    try
                    {
                        var vehicle = selectedVehicle.ToVehicle();
                        await _vehicleService.DeleteAsync(vehicle.VehicleGuid, _cancellationTokenSource.Token);
                        _vehicles.Remove(selectedVehicle);
                    }
                    catch (Exception ex)
                    {
                        // Fallback: remove from local list only
                        _vehicles.Remove(selectedVehicle);

                        MessageBox.Show($"Vehicle deleted locally. Service error: {ex.Message}",
                            "Service Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting vehicle: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void RefreshButton_Click(object? sender, EventArgs e)
        {
            await LoadDataAsync(_cancellationTokenSource.Token);
        }

        private void DataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Allow editing of all columns except VehicleId
            if (_dataGridView != null && _dataGridView.Columns[e.ColumnIndex].Name == "VehicleId")
            {
                e.Cancel = true;
            }
        }

        private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Validate VIN number format
            if (_dataGridView != null && _dataGridView.Columns[e.ColumnIndex].Name == "VINNumber")
            {
                var cell = _dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
                var vinValue = cell.Value?.ToString();

                if (!string.IsNullOrEmpty(vinValue) && (vinValue.Length < 17 || vinValue.Length > 18))
                {
                    MessageBox.Show("VIN number should be 17-18 characters long.", "Validation Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            // Validate capacity
            if (_dataGridView != null && _dataGridView.Columns[e.ColumnIndex].Name == "Capacity")
            {
                var cell = _dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (int.TryParse(cell.Value?.ToString(), out int capacity))
                {
                    if (capacity <= 0 || capacity > 150)
                    {
                        MessageBox.Show("Capacity should be between 1 and 150.", "Validation Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            // Validate model year
            if (_dataGridView != null && _dataGridView.Columns[e.ColumnIndex].Name == "ModelYear")
            {
                var cell = _dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (int.TryParse(cell.Value?.ToString(), out int year))
                {
                    var currentYear = DateTime.Now.Year;
                    if (year < 1990 || year > currentYear + 1)
                    {
                        MessageBox.Show($"Model year should be between 1990 and {currentYear + 1}.", "Validation Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void DataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Handle data conversion errors gracefully
            e.ThrowException = false;
            string columnHeader = (_dataGridView != null) ? _dataGridView.Columns[e.ColumnIndex].HeaderText : "Unknown Column";
            MessageBox.Show($"Invalid data entered in {columnHeader}.",
                "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            // Enable/disable edit and delete buttons based on selection
            bool hasSelection = _dataGridView != null && _dataGridView.SelectedRows.Count > 0;
            if (_editButton != null) _editButton.Enabled = hasSelection;
            if (_deleteButton != null) _deleteButton.Enabled = hasSelection;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }        // ThemeableControl implementation
        public void Render(Control container)
        {
            if (container != null)
            {
                Dock = DockStyle.Fill;
                container.Controls.Clear();
                container.Controls.Add(this);
            }
        }

        // IView implementation
        public async Task ActivateAsync(CancellationToken cancellationToken)
        {
            await LoadDataAsync(cancellationToken);
        }

        public async Task DeactivateAsync()
        {
            await Task.CompletedTask;
        }

        protected override void ApplyTheme()
        {
            BackColor = ThemeManager.CurrentTheme.MainBackground;
            ForeColor = ThemeManager.CurrentTheme.CardText;

            // Apply theme to data grid view
            if (_dataGridView != null)
            {
                _dataGridView.BackgroundColor = ThemeManager.CurrentTheme.CardBackground;
                _dataGridView.GridColor = ThemeManager.CurrentTheme.BorderColor;
                _dataGridView.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.CardBackground;
                _dataGridView.DefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.CardText;
                _dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 153, 255); // Use default selection color
                _dataGridView.DefaultCellStyle.SelectionForeColor = Color.White; // Use default selection text color
                _dataGridView.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.CurrentTheme.HeadlineBackground;
                _dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
                _dataGridView.EnableHeadersVisualStyles = false;
            }

            // Style buttons
            foreach (var button in new Button?[] { _addButton, _editButton, _deleteButton, _refreshButton })
            {
                if (button != null)
                {
                    button.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                    button.ForeColor = ThemeManager.CurrentTheme.ButtonText;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
                }
            }
        }
    }

    /// <summary>
    /// Data Transfer Object for vehicle display in DataGridView
    /// </summary>
    public class VehicleDisplayDTO
    {
        public int VehicleId { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public int ModelYear { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string VINNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public DateTime? LastInspectionDate { get; set; }
        public string LastInspectionDateFormatted => LastInspectionDate?.ToString("dd-MM-yyyy") ?? "N/A";
        public string Status { get; set; } = "Active";

        public static VehicleDisplayDTO FromVehicle(Vehicle vehicle)
        {
            if (vehicle == null)
                throw new ArgumentNullException(nameof(vehicle));
            return new VehicleDisplayDTO
            {
                VehicleId = vehicle.VehicleId,
                BusNumber = vehicle.BusNumber ?? string.Empty,
                ModelYear = vehicle.ModelYear,
                Make = vehicle.Make ?? string.Empty,
                Model = vehicle.Model ?? string.Empty,
                VINNumber = vehicle.VINNumber ?? string.Empty,
                Capacity = vehicle.Capacity,
                LastInspectionDate = vehicle.LastInspectionDate,
                Status = vehicle.IsActive ? (vehicle.Status ?? "Active") : "Inactive"
            };
        }
        public Vehicle ToVehicle()
        {
            return new Vehicle
            {
                VehicleId = VehicleId,
                BusNumber = BusNumber ?? string.Empty,
                ModelYear = ModelYear,
                Make = Make ?? string.Empty,
                Model = Model ?? string.Empty,
                VINNumber = VINNumber ?? string.Empty,
                Capacity = Capacity,
                LastInspectionDate = LastInspectionDate,
                IsActive = Status != "Inactive",
                Status = Status ?? "Active"
            };
        }
    }

    /// <summary>
    /// Simple vehicle edit form for adding/editing vehicles
    /// </summary>
    public partial class VehicleEditForm : Form
    // CA1861: Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array
#pragma warning disable CA1861
    {
        public Vehicle Vehicle { get; private set; }

        private TextBox? _busNumberTextBox;
        private NumericUpDown? _modelYearNumeric;
        private TextBox? _makeTextBox;
        private TextBox? _modelTextBox;
        private TextBox? _vinTextBox;
        private NumericUpDown? _capacityNumeric;
        private DateTimePicker? _inspectionDatePicker;
        private ComboBox? _statusComboBox;

        public VehicleEditForm(Vehicle? vehicle = null)
        {
            Vehicle = vehicle ?? new Vehicle();
            InitializeComponent();
            PopulateFields();
        }

        private void InitializeComponent()
        {
            Text = Vehicle.VehicleId == 0 ? "Add Vehicle" : "Edit Vehicle";
            Size = new Size(400, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 9,
                Padding = new Padding(10)
            };

            // Configure columns
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Bus Number
            layout.Controls.Add(new Label { Text = "Bus Number:", Anchor = AnchorStyles.Left }, 0, 0);
            _busNumberTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_busNumberTextBox, 1, 0);

            // Model Year
            layout.Controls.Add(new Label { Text = "Model Year:", Anchor = AnchorStyles.Left }, 0, 1);
            _modelYearNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1990, Maximum = DateTime.Now.Year + 1, Value = DateTime.Now.Year };
            layout.Controls.Add(_modelYearNumeric, 1, 1);

            // Make
            layout.Controls.Add(new Label { Text = "Make:", Anchor = AnchorStyles.Left }, 0, 2);
            _makeTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_makeTextBox, 1, 2);

            // Model
            layout.Controls.Add(new Label { Text = "Model:", Anchor = AnchorStyles.Left }, 0, 3);
            _modelTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_modelTextBox, 1, 3);

            // VIN Number
            layout.Controls.Add(new Label { Text = "VIN Number:", Anchor = AnchorStyles.Left }, 0, 4);
            _vinTextBox = new TextBox { Dock = DockStyle.Fill, MaxLength = 18 };
            layout.Controls.Add(_vinTextBox, 1, 4);

            // Capacity
            layout.Controls.Add(new Label { Text = "Capacity:", Anchor = AnchorStyles.Left }, 0, 5);
            _capacityNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 150, Value = 48 };
            layout.Controls.Add(_capacityNumeric, 1, 5);

            // Last Inspection Date
            layout.Controls.Add(new Label { Text = "Last Inspection:", Anchor = AnchorStyles.Left }, 0, 6);
            _inspectionDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            layout.Controls.Add(_inspectionDatePicker, 1, 6);

            // Status
            layout.Controls.Add(new Label { Text = "Status:", Anchor = AnchorStyles.Left }, 0, 7);
            _statusComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _statusComboBox.Items.AddRange(new[] { "Active", "Inactive", "Maintenance", "Out of Service" });
#pragma warning restore CA1861
            layout.Controls.Add(_statusComboBox, 1, 7);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };

            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 23) };
            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 23) };
            okButton.Click += OkButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { cancelButton, okButton });
            layout.Controls.Add(buttonPanel, 1, 8);

            Controls.Add(layout);
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void PopulateFields()
        {
            _busNumberTextBox!.Text = Vehicle.BusNumber ?? string.Empty;
            _modelYearNumeric!.Value = Vehicle.ModelYear > 0 ? Vehicle.ModelYear : DateTime.Now.Year;
            _makeTextBox!.Text = Vehicle.Make ?? string.Empty;
            _modelTextBox!.Text = Vehicle.Model ?? string.Empty;
            _vinTextBox!.Text = Vehicle.VINNumber ?? string.Empty;
            _capacityNumeric!.Value = Vehicle.Capacity > 0 ? Vehicle.Capacity : 48;
            _inspectionDatePicker!.Value = Vehicle.LastInspectionDate ?? DateTime.Now.AddMonths(-3);
            _statusComboBox!.SelectedItem = Vehicle.IsActive ? (Vehicle.Status ?? "Active") : "Inactive";
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_busNumberTextBox!.Text))
            {
                MessageBox.Show("Bus Number is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_makeTextBox!.Text))
            {
                MessageBox.Show("Make is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_modelTextBox!.Text))
            {
                MessageBox.Show("Model is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update vehicle
            Vehicle.BusNumber = _busNumberTextBox!.Text.Trim();
            Vehicle.ModelYear = (int)_modelYearNumeric!.Value;
            Vehicle.Make = _makeTextBox!.Text.Trim();
            Vehicle.Model = _modelTextBox!.Text.Trim();
            Vehicle.VINNumber = _vinTextBox!.Text.Trim();
            Vehicle.Capacity = (int)_capacityNumeric!.Value;
            Vehicle.LastInspectionDate = _inspectionDatePicker!.Value;
            Vehicle.Status = _statusComboBox!.SelectedItem?.ToString() ?? "Active";
            Vehicle.IsActive = Vehicle.Status != "Inactive";
        }
    }
}
