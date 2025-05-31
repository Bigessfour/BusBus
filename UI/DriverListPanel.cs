#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
#pragma warning disable CS0169 // The field is never used
#pragma warning disable CA1416 // Platform compatibility (Windows-only)
#pragma warning disable CS1998 // Async method lacks 'await' operators
#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using BusBus.UI.Common;
using BusBus.UI.Core;

namespace BusBus.UI
{
    public class DriverListPanel : BaseCrudView, IDisplayable, IStatefulView
    {
        private readonly IDriverService _driverService;
        // DataGridView and CRUD buttons are now provided by BaseCrudView
        private Button _previousPageButton = null!;
        private Button _nextPageButton = null!;
        private Label _pageInfoLabel = null!;
        private Panel _buttonPanel = null!;
        private Panel _paginationPanel;
        private DataGridView _driversDataGridView = null!;
        private Button _addButton = null!;
        private Button _editButton = null!;
        private Button _deleteButton = null!;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1; private List<Driver> _currentDrivers = new List<Driver>();

        public event EventHandler<EntityEventArgs<Driver>>? DriverEditRequested;
        public event EventHandler<StatusEventArgs>? StatusUpdated; public static string Title => "Drivers"; public DriverListPanel(IDriverService driverService)
        {
            _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
            // Use shared CRUD and grid from BaseCrudView
            HeaderText = "Driver List";
            SetupGrid();
            CrudAddClicked += OnAddClicked;
            CrudEditClicked += OnEditClicked;

            // Initialize UI asynchronously
            _ = Task.Run(async () => await LoadDriversAsync());
        }
        private void SetupGrid()
        {
            if (_driversDataGridView == null)
            {
                InitializeComponent();
                SetupButtons();
                SetupPagination();
            }
            SetupDataGridView();

            // Add columns to the CrudDataGrid from BaseCrudView
            CrudDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "FirstName", HeaderText = "First Name", DataPropertyName = "FirstName" });
            CrudDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastName", HeaderText = "Last Name", DataPropertyName = "LastName" });
            CrudDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PhoneNumber", HeaderText = "Phone", DataPropertyName = "PhoneNumber" });
            CrudDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", DataPropertyName = "Email" });

            // Add a ComboBox column for License Type
            var licenseTypeColumn = new DataGridViewComboBoxColumn
            {
                Name = "LicenseType",
                HeaderText = "License Type",
                DataPropertyName = "LicenseType",
                Items = { "CDL", "Passenger" },
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
            CrudDataGrid.Columns.Add(licenseTypeColumn);

            CrudDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
        }
        private async void OnAddClicked(object? sender, EventArgs e)
        {
            // Create a form for adding a new driver
            using var form = new Form
            {
                Text = "Add New Driver",
                Size = new Size(400, 350),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // Create the input fields
            var firstNameLabel = new Label { Text = "First Name:", Left = 20, Top = 20, Width = 100 };
            var firstNameTextBox = new TextBox { Left = 130, Top = 20, Width = 200 };

            var lastNameLabel = new Label { Text = "Last Name:", Left = 20, Top = 50, Width = 100 };
            var lastNameTextBox = new TextBox { Left = 130, Top = 50, Width = 200 };

            var phoneLabel = new Label { Text = "Phone:", Left = 20, Top = 80, Width = 100 };
            var phoneTextBox = new TextBox { Left = 130, Top = 80, Width = 200 };

            var emailLabel = new Label { Text = "Email:", Left = 20, Top = 110, Width = 100 };
            var emailTextBox = new TextBox { Left = 130, Top = 110, Width = 200 };

            var licenseTypeLabel = new Label { Text = "License Type:", Left = 20, Top = 140, Width = 100 };
            var licenseTypeComboBox = new ComboBox
            {
                Left = 130,
                Top = 140,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            licenseTypeComboBox.Items.AddRange(new object[] { "CDL", "Passenger" });
            licenseTypeComboBox.SelectedIndex = 0;

            // Add save and cancel buttons
            var saveButton = new Button
            {
                Text = "Save",
                Left = 130,
                Top = 200,
                Width = 80,
                DialogResult = DialogResult.OK
            };
            var cancelButton = new Button
            {
                Text = "Cancel",
                Left = 250,
                Top = 200,
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            // Add controls to the form
            form.Controls.AddRange(new Control[]
            {
                firstNameLabel, firstNameTextBox,
                lastNameLabel, lastNameTextBox,
                phoneLabel, phoneTextBox,
                emailLabel, emailTextBox,
                licenseTypeLabel, licenseTypeComboBox,
                saveButton, cancelButton
            });

            form.AcceptButton = saveButton;
            form.CancelButton = cancelButton;

            // Show the form
            if (form.ShowDialog() == DialogResult.OK)
            {
                // Create and save the driver
                var driver = new Driver
                {
                    FirstName = firstNameTextBox.Text.Trim(),
                    LastName = lastNameTextBox.Text.Trim(),
                    PhoneNumber = phoneTextBox.Text.Trim(),
                    Email = emailTextBox.Text.Trim(),
                    LicenseType = licenseTypeComboBox.SelectedItem?.ToString() ?? "CDL"
                }; try
                {
                    await _driverService.CreateAsync(driver);
                    await LoadDriversAsync();
                    StatusUpdated?.Invoke(this, new StatusEventArgs("Driver added successfully"));
                }
                catch (Exception ex)
                {
                    StatusUpdated?.Invoke(this, new StatusEventArgs($"Error adding driver: {ex.Message}"));
                }
            }
        }
        private async void OnEditClicked(object? sender, EventArgs e)
        {
            // Get the selected driver
            if (_driversDataGridView.SelectedRows.Count == 0) return;

            var selectedIndex = _driversDataGridView.SelectedRows[0].Index;
            if (selectedIndex < 0 || selectedIndex >= _currentDrivers.Count) return;

            var driver = _currentDrivers[selectedIndex];

            // Create a form for editing the driver
            using var form = new Form
            {
                Text = $"Edit Driver: {driver.FirstName} {driver.LastName}",
                Size = new Size(400, 350),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // Create the input fields
            var firstNameLabel = new Label { Text = "First Name:", Left = 20, Top = 20, Width = 100 };
            var firstNameTextBox = new TextBox { Left = 130, Top = 20, Width = 200, Text = driver.FirstName };

            var lastNameLabel = new Label { Text = "Last Name:", Left = 20, Top = 50, Width = 100 };
            var lastNameTextBox = new TextBox { Left = 130, Top = 50, Width = 200, Text = driver.LastName };

            var phoneLabel = new Label { Text = "Phone:", Left = 20, Top = 80, Width = 100 };
            var phoneTextBox = new TextBox { Left = 130, Top = 80, Width = 200, Text = driver.PhoneNumber ?? "" };

            var emailLabel = new Label { Text = "Email:", Left = 20, Top = 110, Width = 100 };
            var emailTextBox = new TextBox { Left = 130, Top = 110, Width = 200, Text = driver.Email ?? "" };

            var licenseTypeLabel = new Label { Text = "License Type:", Left = 20, Top = 140, Width = 100 };
            var licenseTypeComboBox = new ComboBox
            {
                Left = 130,
                Top = 140,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            licenseTypeComboBox.Items.AddRange(new object[] { "CDL", "Passenger" });
            licenseTypeComboBox.SelectedItem = driver.LicenseType;

            // Add save and cancel buttons
            var saveButton = new Button
            {
                Text = "Save",
                Left = 130,
                Top = 200,
                Width = 80,
                DialogResult = DialogResult.OK
            };
            var cancelButton = new Button
            {
                Text = "Cancel",
                Left = 250,
                Top = 200,
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            // Add controls to the form
            form.Controls.AddRange(new Control[]
            {
                firstNameLabel, firstNameTextBox,
                lastNameLabel, lastNameTextBox,
                phoneLabel, phoneTextBox,
                emailLabel, emailTextBox,
                licenseTypeLabel, licenseTypeComboBox,
                saveButton, cancelButton
            });

            form.AcceptButton = saveButton;
            form.CancelButton = cancelButton;

            // Show the form
            if (form.ShowDialog() == DialogResult.OK)
            {
                // Update the driver
                driver.FirstName = firstNameTextBox.Text.Trim();
                driver.LastName = lastNameTextBox.Text.Trim();
                driver.PhoneNumber = phoneTextBox.Text.Trim();
                driver.Email = emailTextBox.Text.Trim();
                driver.LicenseType = licenseTypeComboBox.SelectedItem?.ToString() ?? "CDL";

                try
                {
                    await _driverService.UpdateAsync(driver);
                    await LoadDriversAsync();
                    StatusUpdated?.Invoke(this, new StatusEventArgs("Driver updated successfully"));
                }
                catch (Exception ex)
                {
                    StatusUpdated?.Invoke(this, new StatusEventArgs($"Error updating driver: {ex.Message}"));
                }
            }
        }
        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            // Get the selected driver
            if (_driversDataGridView.SelectedRows.Count == 0) return;

            var selectedIndex = _driversDataGridView.SelectedRows[0].Index;
            if (selectedIndex < 0 || selectedIndex >= _currentDrivers.Count) return;

            var driver = _currentDrivers[selectedIndex];

            // Confirm deletion
            var result = MessageBox.Show(
                $"Are you sure you want to delete driver {driver.FirstName} {driver.LastName}?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    await _driverService.DeleteAsync(driver.Id);
                    await LoadDriversAsync();
                    StatusUpdated?.Invoke(this, new StatusEventArgs("Driver deleted successfully"));
                }
                catch (Exception ex)
                {
                    StatusUpdated?.Invoke(this, new StatusEventArgs($"Error deleting driver: {ex.Message}"));
                }
            }
        }

        public void SaveState(object state)
        {
            // Optionally implement saving state here
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Main properties
            Size = new Size(800, 600);
            BackColor = Color.White;
            Padding = new Padding(20);

            // Create main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // DataGridView first
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons in middle
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Pagination last

            Controls.Add(mainLayout);

            // Button panel            // First create and setup the DataGridView
            _driversDataGridView = new DataGridView
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
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                GridColor = System.Drawing.Color.FromArgb(200, 200, 200),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 40, // Fixed height for better visibility
                RowHeadersVisible = false,
                RowTemplate = { Height = 32 },
                EditMode = DataGridViewEditMode.EditOnEnter
            };

            // Apply consistent theme styling to the grid
            BusBus.UI.Core.ThemeManager.CurrentTheme.StyleDataGrid(_driversDataGridView);

            // Enhance header styling for better visibility
            _driversDataGridView.ColumnHeadersDefaultCellStyle.Font = new Font(_driversDataGridView.Font.FontFamily, 9.5F, FontStyle.Bold);
            _driversDataGridView.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 8, 8, 8);
            _driversDataGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Add DataGridView to the top
            mainLayout.Controls.Add(_driversDataGridView, 0, 0);

            // Now create and setup the button panel
            _buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Add button panel to the middle
            mainLayout.Controls.Add(_buttonPanel, 0, 1);

            // Pagination panel
            _paginationPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            mainLayout.Controls.Add(_paginationPanel, 0, 2);

            ResumeLayout(false);
        }
        private void SetupDataGridView()
        {
            _driversDataGridView.Columns.Clear();

            // FirstName column
            var firstNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "FirstName",
                HeaderText = "First Name",
                DataPropertyName = "FirstName",
                FillWeight = 150,
                MinimumWidth = 120,
                ReadOnly = false
            };
            _driversDataGridView.Columns.Add(firstNameColumn);

            // LastName column
            var lastNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "LastName",
                HeaderText = "Last Name",
                DataPropertyName = "LastName",
                FillWeight = 150,
                MinimumWidth = 120,
                ReadOnly = false
            };
            _driversDataGridView.Columns.Add(lastNameColumn);

            // Phone column
            var phoneColumn = new DataGridViewTextBoxColumn
            {
                Name = "PhoneNumber",
                HeaderText = "Phone",
                DataPropertyName = "PhoneNumber",
                FillWeight = 150,
                MinimumWidth = 120,
                ReadOnly = false
            };
            _driversDataGridView.Columns.Add(phoneColumn);

            // Email column
            var emailColumn = new DataGridViewTextBoxColumn
            {
                Name = "Email",
                HeaderText = "Email",
                DataPropertyName = "Email",
                FillWeight = 200,
                MinimumWidth = 150,
                ReadOnly = false
            };
            _driversDataGridView.Columns.Add(emailColumn);

            // LicenseType column (dropdown)
            var licenseTypeColumn = new DataGridViewComboBoxColumn
            {
                Name = "LicenseType",
                HeaderText = "License Type",
                DataPropertyName = "LicenseType",
                FillWeight = 150,
                MinimumWidth = 120,
                Items = { "CDL", "Passenger" },
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                ReadOnly = false
            };
            _driversDataGridView.Columns.Add(licenseTypeColumn);

            // Hidden ID column
            var idColumn = new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = "Id",
                Visible = false
            };
            _driversDataGridView.Columns.Add(idColumn);

            // Event handlers
            _driversDataGridView.CellEndEdit += DriversDataGridView_CellEndEdit;
            _driversDataGridView.SelectionChanged += DriversDataGridView_SelectionChanged;
        }
        private void SetupButtons()
        {
            var buttonLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 50,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(5)
            };
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            // Add button
            _addButton = new Button
            {
                Text = "Add Driver",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            _addButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;
            _addButton.FlatAppearance.BorderSize = 1;
            _addButton.Click += (s, e) => OnCrudAddClicked();

            // Edit button
            _editButton = new Button
            {
                Text = "Edit Driver",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                Enabled = false // Disabled until a row is selected
            };
            _editButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;
            _editButton.FlatAppearance.BorderSize = 1;
            _editButton.Click += (s, e) => OnCrudEditClicked();

            // Delete button
            _deleteButton = new Button
            {
                Text = "Delete Driver",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                Enabled = false // Disabled until a row is selected
            };
            _deleteButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;
            _deleteButton.FlatAppearance.BorderSize = 1;
            _deleteButton.Click += (s, e) => OnCrudDeleteClicked();

            // Add the buttons to the layout
            buttonLayout.Controls.Add(_addButton, 0, 0);
            buttonLayout.Controls.Add(_editButton, 1, 0);
            buttonLayout.Controls.Add(_deleteButton, 2, 0);

            // Add the button layout to the button panel
            _buttonPanel.Controls.Add(buttonLayout);

            // Wire up selection changed event for enabling/disabling buttons
            _driversDataGridView.SelectionChanged += (s, e) =>
            {
                bool hasSelection = _driversDataGridView.SelectedRows.Count > 0;
                _editButton.Enabled = hasSelection;
                _deleteButton.Enabled = hasSelection;
            };
        }
        private void SetupPagination()
        {
            // Create a horizontal layout for pagination controls
            var paginationLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(5)
            };

            // Previous page button
            _previousPageButton = new Button
            {
                Text = "← Previous",
                AutoSize = true,
                Margin = new Padding(3),
                Padding = new Padding(5, 2, 5, 2),
                FlatStyle = FlatStyle.Flat,
                Enabled = false  // Disabled initially (first page)
            };
            _previousPageButton.FlatAppearance.BorderColor = Color.LightGray;
            _previousPageButton.Click += async (s, e) =>
            {
                if (_currentPage > 1)
                {
                    _currentPage--;
                    await LoadDriversAsync();
                }
            };

            // Page info label
            _pageInfoLabel = new Label
            {
                Text = "Page 1 of 1",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(10, 5, 10, 0),
                Padding = new Padding(5)
            };

            // Next page button
            _nextPageButton = new Button
            {
                Text = "Next →",
                AutoSize = true,
                Margin = new Padding(3),
                Padding = new Padding(5, 2, 5, 2),
                FlatStyle = FlatStyle.Flat,
                Enabled = false  // Disabled initially (no pages)
            };
            _nextPageButton.FlatAppearance.BorderColor = Color.LightGray;
            _nextPageButton.Click += async (s, e) =>
            {
                if (_currentPage < _totalPages)
                {
                    _currentPage++;
                    await LoadDriversAsync();
                }
            };

            // Add controls to the pagination layout
            paginationLayout.Controls.Add(_previousPageButton);
            paginationLayout.Controls.Add(_pageInfoLabel);
            paginationLayout.Controls.Add(_nextPageButton);

            // Add the pagination layout to the pagination panel
            _paginationPanel.Controls.Add(paginationLayout);
        }

        #region IStatefulView Implementation
        public object? GetState()
        {
            return new DriverListState
            {
                CurrentPage = _currentPage,
                PageSize = _pageSize,
                SelectedDriverId = _driversDataGridView.SelectedRows.Count > 0
                    ? ((Driver)_driversDataGridView.SelectedRows[0].DataBoundItem).Id
                    : null
            };
        }

        public void RestoreState(object state)
        {
            if (state is DriverListState driverState)
            {
                _currentPage = driverState.CurrentPage;
                _pageSize = driverState.PageSize;

                // Load data with restored state
                _ = LoadDriversAsync().ContinueWith(t =>
                {
                    if (driverState.SelectedDriverId.HasValue)
                    {
                        // Restore selection
                        for (int i = 0; i < _driversDataGridView.Rows.Count; i++)
                        {
                            var driver = (Driver)_driversDataGridView.Rows[i].DataBoundItem;
                            if (driver.Id == driverState.SelectedDriverId.Value)
                            {
                                _driversDataGridView.ClearSelection();
                                _driversDataGridView.Rows[i].Selected = true;
                                break;
                            }
                        }
                    }
                });
            }
        }

        private class DriverListState
        {
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public Guid? SelectedDriverId { get; set; }
        }
        #endregion        /// <summary>
        /// Loads drivers from the service with pagination
        /// </summary>
        public async Task LoadDriversAsync()
        {
            try
            {
                // Show loading state
                StatusUpdated?.Invoke(this, new StatusEventArgs("Loading drivers..."));

                // Get total count for pagination
                int totalCount = await _driverService.GetCountAsync();
                _totalPages = (int)Math.Ceiling(totalCount / (double)_pageSize);

                // Update pagination display
                if (_pageInfoLabel != null)
                {
                    _pageInfoLabel.Text = $"Page {_currentPage} of {_totalPages}";
                    _previousPageButton.Enabled = _currentPage > 1;
                    _nextPageButton.Enabled = _currentPage < _totalPages;
                }

                // Get drivers for current page
                _currentDrivers = await _driverService.GetPagedAsync(_currentPage, _pageSize);

                // Update grid data source
                _driversDataGridView.DataSource = null;
                _driversDataGridView.DataSource = _currentDrivers;

                // Also update the CrudDataGrid if it's being used
                CrudDataGrid.DataSource = null;
                CrudDataGrid.DataSource = _currentDrivers;

                // Update status
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Loaded {_currentDrivers.Count} drivers"));
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error loading drivers: {ex.Message}"));
            }
        }

        private void UpdatePaginationControls()
        {
            _pageInfoLabel.Text = $"Page {_currentPage} of {_totalPages}";
            _previousPageButton.Enabled = _currentPage > 1;
            _nextPageButton.Enabled = _currentPage < _totalPages;
        }

        private async void AddButton_Click(object? sender, EventArgs e)
        {
            var newDriver = new Driver
            {
                FirstName = "New",
                LastName = "Driver",
                LicenseNumber = "LICENSE-" + DateTime.Now.Ticks
            };

            DriverEditRequested?.Invoke(this, new EntityEventArgs<Driver>(newDriver));
        }

        private void EditButton_Click(object? sender, EventArgs e)
        {
            if (_driversDataGridView.SelectedRows.Count > 0)
            {
                var driver = (Driver)_driversDataGridView.SelectedRows[0].DataBoundItem;
                DriverEditRequested?.Invoke(this, new EntityEventArgs<Driver>(driver));
            }
        }

        private async void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_driversDataGridView.SelectedRows.Count > 0)
            {
                var driver = (Driver)_driversDataGridView.SelectedRows[0].DataBoundItem;
                var result = MessageBox.Show($"Delete driver {driver.FirstName} {driver.LastName}?",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await _driverService.DeleteAsync(driver.Id);
                        await LoadDriversAsync();
                        StatusUpdated?.Invoke(this, new StatusEventArgs("Driver deleted successfully"));
                    }
                    catch (Exception ex)
                    {
                        StatusUpdated?.Invoke(this, new StatusEventArgs($"Error deleting driver: {ex.Message}"));
                    }
                }
            }
        }
        private async void DriversDataGridView_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentDrivers.Count) return;

            try
            {
                var driver = _currentDrivers[e.RowIndex];
                var columnName = _driversDataGridView.Columns[e.ColumnIndex].Name;
                var newValue = _driversDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

                switch (columnName)
                {
                    case "FirstName":
                        driver.FirstName = newValue;
                        break;
                    case "LastName":
                        driver.LastName = newValue;
                        break;
                    case "PhoneNumber":
                        driver.PhoneNumber = newValue;
                        break;
                    case "Email":
                        driver.Email = newValue;
                        break;
                    case "LicenseType":
                        driver.LicenseType = newValue;
                        break;
                }

                await _driverService.UpdateAsync(driver);
                StatusUpdated?.Invoke(this, new StatusEventArgs("Driver updated successfully"));
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error updating driver: {ex.Message}"));
            }
        }

        private void DriversDataGridView_SelectionChanged(object? sender, EventArgs e)
        {
            bool hasSelection = _driversDataGridView.SelectedRows.Count > 0;
            _editButton.Enabled = hasSelection;
            _deleteButton.Enabled = hasSelection;
        }

        private async void PreviousPageButton_Click(object? sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadDriversAsync();
            }
        }

        private async void NextPageButton_Click(object? sender, EventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadDriversAsync();
            }
        }
        public void Render(Control container)
        {
            if (container == null) return;
            container.Controls.Clear();
            container.Controls.Add(this);
            Dock = DockStyle.Fill;
        }

        public void RefreshTheme()
        {
            // Apply theme to main panel
            BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardBackground;

            // Apply theme to data grid
            if (_driversDataGridView != null)
            {
                BusBus.UI.Core.ThemeManager.CurrentTheme.StyleDataGrid(_driversDataGridView);
            }

            // Apply theme to buttons
            if (_addButton != null)
            {
                _addButton.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground;
                _addButton.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText;
                _addButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;
            }
            if (_editButton != null)
            {
                _editButton.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground;
                _editButton.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText;
                _editButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;
            }

            if (_deleteButton != null)
            {
                _deleteButton.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground;
                _deleteButton.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText;
                _deleteButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;
            }

            if (_previousPageButton != null)
            {
                _previousPageButton.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground;
                _previousPageButton.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText;
                _previousPageButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground;
                _nextPageButton.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText;
                _nextPageButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;
            }

            // Apply theme to labels
            if (_pageInfoLabel != null)
            {
                _pageInfoLabel.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText;
            }

            // Apply theme to panels
            if (_buttonPanel != null)
            {
                _buttonPanel.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardBackground;
            }

            if (_paginationPanel != null)
            {
                _paginationPanel.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardBackground;
            }
        }

        public async Task RefreshAsync()
        {
            await LoadDriversAsync();
        }

        void IDisplayable.RefreshTheme()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Wire up the button click events to the CRUD operations
        /// </summary>
        private void WireUpButtonEvents()
        {
            // Connect UI buttons to BaseCrudView events
            _addButton.Click += (s, e) => OnCrudAddClicked();
            _editButton.Click += (s, e) => OnCrudEditClicked();
            _deleteButton.Click += (s, e) => OnCrudDeleteClicked();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Wire up event handlers
            CrudAddClicked += OnAddClicked;
            CrudEditClicked += OnEditClicked;
            CrudDeleteClicked += OnDeleteClicked;

            // Load data when control is loaded
            _ = LoadDriversAsync();
        }
    }    // Add missing event args classes
    public class EntityEventArgs<T> : EventArgs
    {
        public T Entity { get; }
        public EntityEventArgs(T entity) => Entity = entity;
    }

    public class DriverListState
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int? SelectedDriverId { get; set; }
    }
}
