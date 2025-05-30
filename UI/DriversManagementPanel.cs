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
    /// Drivers management tab with Crystal Dark styling and CRUD operations
    /// </summary>
    public partial class DriversManagementPanel : ThemeableControl, IDisplayable, IView
    {
        private readonly IDriverService _driverService;

        // Data collections
        private BindingList<DriverDisplayDTO> _drivers = new BindingList<DriverDisplayDTO>();

        // UI Controls
        private DataGridView _driversGrid = null!;
        private Button _addDriverButton = null!;
        private Button _editDriverButton = null!;
        private Button _deleteDriverButton = null!;
        private Label _titleLabel = null!;

        // Pagination and state
        private int _totalDrivers = 0;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // IView interface implementation
        public string ViewName => "drivers";
        public string Title => "Drivers";
        public Control? Control => this;

        public event EventHandler<NavigationEventArgs>? NavigationRequested;
#pragma warning disable CS0067 // Event is never used
#pragma warning disable CS0414 // Field is assigned but its value is never used
        public event EventHandler<StatusEventArgs>? StatusUpdated;

        public DriversManagementPanel(IDriverService driverService)
        {
            ArgumentNullException.ThrowIfNull(driverService);
            _driverService = driverService;

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
                Text = "Driver Management",
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
            mainContainer.Controls.Add(_driversGrid, 0, 0);

            // Create CRUD buttons
            CreateCrudButtons();
            var buttonPanel = CreateButtonPanel();
            mainContainer.Controls.Add(buttonPanel, 0, 1);
        }

        private void CreateDataGridView()
        {
            _driversGrid = new DataGridView
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
            ThemeManager.CurrentTheme.StyleDataGrid(_driversGrid);

            // Enhance header styling
            _driversGrid.ColumnHeadersDefaultCellStyle.Font = new Font(_driversGrid.Font.FontFamily, 9.5F, FontStyle.Bold);
            _driversGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 8, 8, 8);
            _driversGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Define columns
            _driversGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                // Hidden Primary Key
                new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false },

                // Driver Name (combined First + Last)
                new DataGridViewTextBoxColumn {
                    HeaderText = "Driver Name",
                    DataPropertyName = "FullName",
                    FillWeight = 200,
                    MinimumWidth = 150,
                    ReadOnly = true
                },

                // Phone Number
                new DataGridViewTextBoxColumn {
                    HeaderText = "Phone",
                    DataPropertyName = "PhoneNumber",
                    FillWeight = 120,
                    MinimumWidth = 100
                },

                // Email Address
                new DataGridViewTextBoxColumn {
                    HeaderText = "Email",
                    DataPropertyName = "Email",
                    FillWeight = 180,
                    MinimumWidth = 150
                },

                // License Type ComboBox
                new DataGridViewComboBoxColumn {
                    HeaderText = "License Type",
                    DataPropertyName = "LicenseType",
                    Name = "LicenseTypeColumn",
                    FillWeight = 120,
                    MinimumWidth = 100,
                    Items = { "CDL-A", "CDL-B", "CDL-C", "Regular", "Commercial" }
                },

                // License Number
                new DataGridViewTextBoxColumn {
                    HeaderText = "License Number",
                    DataPropertyName = "LicenseNumber",
                    FillWeight = 130,
                    MinimumWidth = 120
                },

                // Hire Date
                new DataGridViewTextBoxColumn {
                    HeaderText = "Hire Date",
                    DataPropertyName = "HireDate",
                    FillWeight = 100,
                    MinimumWidth = 100,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "dd-MM-yy" }
                },

                // Status (Active/Inactive)
                new DataGridViewComboBoxColumn {
                    HeaderText = "Status",
                    DataPropertyName = "Status",
                    Name = "StatusColumn",
                    FillWeight = 80,
                    MinimumWidth = 80,
                    Items = { "Active", "Inactive", "Suspended" }
                }
            });

            // Bind data
            _driversGrid.DataSource = _drivers;
        }

        private void CreateCrudButtons()
        {
            _addDriverButton = CreateCrystalDarkButton("Add New Driver", new Size(120, 35));
            _editDriverButton = CreateCrystalDarkButton("Edit Selected", new Size(100, 35));
            _deleteDriverButton = CreateCrystalDarkButton("Delete", new Size(80, 35));
        }

        /// <summary>
        /// Creates a Crystal Dark glass-like button with specified text and size
        /// </summary>
        private static Button CreateCrystalDarkButton(string text, Size size)
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

        private TableLayoutPanel CreateButtonPanel()
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

            buttonPanel.Controls.Add(_addDriverButton, 0, 0);
            buttonPanel.Controls.Add(_editDriverButton, 1, 0);
            buttonPanel.Controls.Add(_deleteDriverButton, 2, 0);

            return buttonPanel;
        }

        private void SetupEventHandlers()
        {
            // Data validation
            _driversGrid.CellValidating += OnCellValidating;

            // CRUD button events
            _addDriverButton.Click += OnAddDriverClick;
            _editDriverButton.Click += OnEditDriverClick;
            _deleteDriverButton.Click += OnDeleteDriverClick;
        }

        private void OnCellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            var columnName = _driversGrid.Columns[e.ColumnIndex].DataPropertyName;
            var value = e.FormattedValue?.ToString() ?? string.Empty;

            switch (columnName)
            {
                case "Email":
                    if (!string.IsNullOrEmpty(value) && !IsValidEmail(value))
                    {
                        e.Cancel = true;
                        MessageBox.Show("Please enter a valid email address.", "Invalid Email",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    break;

                case "PhoneNumber":
                    if (!string.IsNullOrEmpty(value) && !IsValidPhoneNumber(value))
                    {
                        e.Cancel = true;
                        MessageBox.Show("Please enter a valid phone number (e.g., (555) 123-4567).", "Invalid Phone Number",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    break;
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidPhoneNumber(string phone)
        {
            // Simple phone validation - allows various formats
            var cleaned = new string(phone.Where(char.IsDigit).ToArray());
            return cleaned.Length >= 10 && cleaned.Length <= 11;
        }

        private async void OnAddDriverClick(object? sender, EventArgs e)
        {
            try
            {
                // Create new driver with default values
                var newDriver = new DriverDisplayDTO
                {
                    Id = Guid.NewGuid(),
                    FirstName = "New",
                    LastName = "Driver",
                    FullName = "New Driver",
                    PhoneNumber = "",
                    Email = "",
                    LicenseType = "CDL-B", // Default selection
                    LicenseNumber = "",
                    HireDate = DateTime.Today,
                    Status = "Active"
                };

                // Add to service
                var driver = newDriver.ToDriver();
                var createdDriver = await _driverService.CreateAsync(driver);

                // Add to local collection
                var displayDriver = DriverDisplayDTO.FromDriver(createdDriver);
                _drivers.Add(displayDriver);

                // Select the new row
                _driversGrid.ClearSelection();
                var newIndex = _drivers.Count - 1;
                if (newIndex >= 0)
                {
                    _driversGrid.Rows[newIndex].Selected = true;
                    _driversGrid.CurrentCell = _driversGrid.Rows[newIndex].Cells[1]; // Select first editable cell
                }

                StatusUpdated?.Invoke(this, new StatusEventArgs("New driver added successfully.", StatusType.Success));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding driver: {ex.Message}", "Add Driver Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error adding driver: {ex.Message}", StatusType.Error));
            }
        }

        private async void OnEditDriverClick(object? sender, EventArgs e)
        {
            if (_driversGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a driver to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var selectedIndex = _driversGrid.SelectedRows[0].Index;
                if (selectedIndex >= 0 && selectedIndex < _drivers.Count)
                {
                    var driver = _drivers[selectedIndex];

                    // Update driver in service
                    await _driverService.UpdateAsync(driver.ToDriver());

                    StatusUpdated?.Invoke(this, new StatusEventArgs("Driver updated successfully.", StatusType.Success));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating driver: {ex.Message}", "Update Driver Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error updating driver: {ex.Message}", StatusType.Error));
            }
        }

        private async void OnDeleteDriverClick(object? sender, EventArgs e)
        {
            if (_driversGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a driver to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedIndex = _driversGrid.SelectedRows[0].Index;
            if (selectedIndex >= 0 && selectedIndex < _drivers.Count)
            {
                var driver = _drivers[selectedIndex];
                var result = MessageBox.Show(
                    $"Are you sure you want to delete driver '{driver.FullName}'?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await _driverService.DeleteAsync(driver.Id);
                        _drivers.RemoveAt(selectedIndex);

                        StatusUpdated?.Invoke(this, new StatusEventArgs("Driver deleted successfully.", StatusType.Success));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting driver: {ex.Message}", "Delete Driver Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        StatusUpdated?.Invoke(this, new StatusEventArgs($"Error deleting driver: {ex.Message}", StatusType.Error));
                    }
                }
            }
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                await LoadDriversAsync(_currentPage, _pageSize, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error loading data: {ex.Message}", StatusType.Error));
            }
        }

        private async Task LoadDriversAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var drivers = await _driverService.GetDriversAsync(page, pageSize, cancellationToken);
                var displayDrivers = drivers.Select(DriverDisplayDTO.FromDriver).ToList();

                _drivers.Clear();
                foreach (var driver in displayDrivers)
                {
                    _drivers.Add(driver);
                }

                StatusUpdated?.Invoke(this, new StatusEventArgs($"Loaded {drivers.Count} drivers.", StatusType.Success));
            }
            catch (Exception ex)
            {
                // Fallback to sample data if service fails
                var sampleDrivers = new List<DriverDisplayDTO>
                {
                    new DriverDisplayDTO
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "John",
                        LastName = "Smith",
                        FullName = "John Smith",
                        PhoneNumber = "(555) 123-4567",
                        Email = "john.smith@busbus.com",
                        LicenseType = "CDL-B",
                        LicenseNumber = "CDL123456",
                        HireDate = DateTime.Today.AddMonths(-6),
                        Status = "Active"
                    },
                    new DriverDisplayDTO
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "Jane",
                        LastName = "Doe",
                        FullName = "Jane Doe",
                        PhoneNumber = "(555) 987-6543",
                        Email = "jane.doe@busbus.com",
                        LicenseType = "CDL-A",
                        LicenseNumber = "CDL789012",
                        HireDate = DateTime.Today.AddYears(-1),
                        Status = "Active"
                    },
                    new DriverDisplayDTO
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "Mike",
                        LastName = "Johnson",
                        FullName = "Mike Johnson",
                        PhoneNumber = "(555) 456-7890",
                        Email = "mike.johnson@busbus.com",
                        LicenseType = "CDL-B",
                        LicenseNumber = "CDL345678",
                        HireDate = DateTime.Today.AddMonths(-3),
                        Status = "Active"
                    }
                };

                _drivers.Clear();
                foreach (var driver in sampleDrivers)
                {
                    _drivers.Add(driver);
                }

                StatusUpdated?.Invoke(this, new StatusEventArgs($"Loaded sample data (service error): {ex.Message}", StatusType.Warning));
            }
        }

        public async Task RefreshAsync()
        {
            await LoadDriversAsync(_currentPage, _pageSize, _cancellationTokenSource.Token);
        }

        public new void Show()
        {
            this.Visible = true;
        }

        // CS0108: Hides inherited member 'Control.Hide()'. Use the new keyword if hiding was intended.
        public new void Hide()
#pragma warning disable CS0067 // Event is never used
#pragma warning disable CS0414 // Field is assigned but its value is never used
        {
            this.Visible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ThemeableControl implementation
        public override void Render(Control container)
        {
            if (container != null)
            {
                this.Dock = DockStyle.Fill;
                container.Controls.Clear();
                container.Controls.Add(this);
            }
        }        // IView implementation
        public async Task ActivateAsync(CancellationToken cancellationToken)
        {
            await LoadDriversAsync(_currentPage, _pageSize, cancellationToken);
        }

        public async Task DeactivateAsync()
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// DTO for displaying drivers in the DataGridView
    /// </summary>
    public class DriverDisplayDTO
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LicenseType { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public string Status { get; set; } = "Active";

        public static DriverDisplayDTO FromDriver(Driver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));

            return new DriverDisplayDTO
            {
                Id = driver.Id,
                FirstName = driver.FirstName,
                LastName = driver.LastName,
                FullName = $"{driver.FirstName} {driver.LastName}",
                PhoneNumber = driver.PhoneNumber ?? string.Empty,
                Email = driver.Email ?? string.Empty,
                LicenseType = driver.LicenseType ?? "CDL-B",
                LicenseNumber = driver.LicenseNumber ?? string.Empty,
                HireDate = driver.HireDate,
                Status = driver.IsActive ? "Active" : "Inactive"
            };
        }

        public Driver ToDriver()
        {
            return new Driver
            {
                Id = this.Id,
                FirstName = this.FirstName,
                LastName = this.LastName,
                PhoneNumber = this.PhoneNumber,
                Email = this.Email,
                LicenseType = this.LicenseType,
                LicenseNumber = this.LicenseNumber,
                HireDate = this.HireDate,
                IsActive = this.Status == "Active"
            };
        }
    }
}
