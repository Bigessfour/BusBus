
#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Common;
using BusBus.UI.Core;

namespace BusBus.UI
{
    public partial class VehicleListPanel : ThemeableControl, IDisplayable
    {
        private readonly IVehicleService _vehicleService;
        private DataGridView _vehiclesDataGridView = null!;
        private Button _addButton = null!;
        private Button _editButton = null!;
        private Button _deleteButton = null!;
        private Button _previousPageButton = null!;
        private Button _nextPageButton = null!;
        private Label _pageInfoLabel = null!;
        private Panel _buttonPanel = null!;
        private Panel? _paginationPanel;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;
        private List<Vehicle> _currentVehicles = new List<Vehicle>();

        public event EventHandler<EntityEventArgs<Vehicle>>? VehicleEditRequested;

        public static string Title => "Vehicles"; public VehicleListPanel(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            InitializeComponent();
            SetupDataGridView();
            SetupButtons();
            SetupPagination();

            // Ensure the current theme is applied
            ThemeManager.ApplyThemeToControl(this);

            _ = LoadVehiclesAsync();
        }

        public override void RefreshTheme()
        {
            ApplyTheme();
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
            }; mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // DataGridView first
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons in middle
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Pagination last

            Controls.Add(mainLayout);

            // Button panel
            _buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            mainLayout.Controls.Add(_buttonPanel, 0, 0);            // DataGridView
            _vehiclesDataGridView = new DataGridView
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
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 40, // Fixed height for better visibility
                RowHeadersVisible = false,
                RowTemplate = { Height = 32 }
            };
            mainLayout.Controls.Add(_vehiclesDataGridView, 0, 1);

            // Enhance header styling for better visibility
            _vehiclesDataGridView.ColumnHeadersDefaultCellStyle.Font = new Font(_vehiclesDataGridView.Font.FontFamily, 9.5F, FontStyle.Bold);
            _vehiclesDataGridView.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 8, 8, 8);
            _vehiclesDataGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

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
            _vehiclesDataGridView.Columns.Clear();

            // Number column
            var numberColumn = new DataGridViewTextBoxColumn
            {
                Name = "Number",
                HeaderText = "Number",
                DataPropertyName = "Number",
                Width = 100,
                ReadOnly = false
            };
            _vehiclesDataGridView.Columns.Add(numberColumn);

            // Capacity column
            var capacityColumn = new DataGridViewTextBoxColumn
            {
                Name = "Capacity",
                HeaderText = "Capacity",
                DataPropertyName = "Capacity",
                Width = 100,
                ReadOnly = false
            };
            _vehiclesDataGridView.Columns.Add(capacityColumn);

            // Model column
            var modelColumn = new DataGridViewTextBoxColumn
            {
                Name = "Model",
                HeaderText = "Model",
                DataPropertyName = "Model",
                Width = 150,
                ReadOnly = false
            };
            _vehiclesDataGridView.Columns.Add(modelColumn);

            // LicensePlate column
            var licensePlateColumn = new DataGridViewTextBoxColumn
            {
                Name = "LicensePlate",
                HeaderText = "License Plate",
                DataPropertyName = "LicensePlate",
                Width = 150,
                ReadOnly = false
            };
            _vehiclesDataGridView.Columns.Add(licensePlateColumn);

            // IsActive column (checkbox)
            var isActiveColumn = new DataGridViewCheckBoxColumn
            {
                Name = "IsActive",
                HeaderText = "Active",
                DataPropertyName = "IsActive",
                Width = 80,
                ReadOnly = false
            };
            _vehiclesDataGridView.Columns.Add(isActiveColumn);

            // Hidden ID column
            var idColumn = new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = "Id",
                Visible = false
            };
            _vehiclesDataGridView.Columns.Add(idColumn);

            // Event handlers
            _vehiclesDataGridView.CellEndEdit += VehiclesDataGridView_CellEndEdit;
            _vehiclesDataGridView.SelectionChanged += VehiclesDataGridView_SelectionChanged;
            _vehiclesDataGridView.CellValueChanged += VehiclesDataGridView_CellValueChanged;
        }
        private void SetupButtons()
        {
            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _addButton = new Button
            {
                Text = "Add",
                Size = new Size(100, 40),
                Margin = new Padding(0, 10, 10, 10),
                Padding = new Padding(5, 10, 5, 10),
                AutoSize = false,
                Tag = "ActionButton"
            };
            _addButton.Click += AddButton_Click;

            _editButton = new Button
            {
                Text = "Edit",
                Size = new Size(80, 40),
                Margin = new Padding(0, 10, 10, 10),
                Padding = new Padding(5, 10, 5, 10),
                AutoSize = false,
                Enabled = false,
                Tag = "ActionButton"
            };
            _editButton.Click += EditButton_Click;

            _deleteButton = new Button
            {
                Text = "Delete",
                Size = new Size(80, 40),
                Margin = new Padding(0, 10, 10, 10),
                Padding = new Padding(5, 10, 5, 10),
                AutoSize = false,
                Enabled = false,
                Tag = "ActionButton"
            };
            _deleteButton.Click += DeleteButton_Click;

            // Apply theme to buttons
            ApplyThemeToButtons();
            // Subscribe to theme changed event
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

            buttonLayout.Controls.Add(_addButton);
            buttonLayout.Controls.Add(_editButton);
            buttonLayout.Controls.Add(_deleteButton); _buttonPanel.Controls.Add(buttonLayout);
        }

        private void ApplyThemeToButtons()
        {
            if (_addButton != null)
            {
                _addButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _addButton.ForeColor = ThemeManager.CurrentTheme.ButtonText;
                _addButton.FlatStyle = FlatStyle.Flat;
                _addButton.FlatAppearance.BorderSize = 0;
                _addButton.Font = ThemeManager.CurrentTheme.ButtonFont;
            }

            if (_editButton != null)
            {
                _editButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _editButton.ForeColor = ThemeManager.CurrentTheme.ButtonText;
                _editButton.FlatStyle = FlatStyle.Flat;
                _editButton.FlatAppearance.BorderSize = 0;
                _editButton.Font = ThemeManager.CurrentTheme.ButtonFont;

                if (!_editButton.Enabled)
                {
                    _editButton.BackColor = ThemeManager.CurrentTheme.ButtonDisabledBackground;
                    _editButton.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                }
            }

            if (_deleteButton != null)
            {
                _deleteButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _deleteButton.ForeColor = ThemeManager.CurrentTheme.ButtonText;
                _deleteButton.FlatStyle = FlatStyle.Flat;
                _deleteButton.FlatAppearance.BorderSize = 0;
                _deleteButton.Font = ThemeManager.CurrentTheme.ButtonFont;

                if (!_deleteButton.Enabled)
                {
                    _deleteButton.BackColor = ThemeManager.CurrentTheme.ButtonDisabledBackground;
                    _deleteButton.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                }
            }
        }

        private void SetupPagination()
        {
            var paginationLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Color.Transparent
            }; _previousPageButton = new Button
            {
                Text = "Previous",
                Size = new Size(80, 40),
                Margin = new Padding(0, 5, 10, 5),
                Enabled = false,
                Padding = new Padding(5, 10, 5, 10),
                AutoSize = false,
                Tag = "NavigationButton"
            };
            _previousPageButton.Click += PreviousPageButton_Click;

            _pageInfoLabel = new Label
            {
                Text = "Page 1 of 1",
                AutoSize = true,
                Margin = new Padding(10, 8, 10, 5)
            };

            _nextPageButton = new Button
            {
                Text = "Next",
                Size = new Size(80, 40),
                Margin = new Padding(0, 5, 10, 5),
                Enabled = false,
                Padding = new Padding(5, 10, 5, 10),
                AutoSize = false,
                Tag = "NavigationButton"
            };
            _nextPageButton.Click += NextPageButton_Click; paginationLayout.Controls.Add(_previousPageButton);
            paginationLayout.Controls.Add(_pageInfoLabel);
            paginationLayout.Controls.Add(_nextPageButton);
            _paginationPanel!.Controls.Add(paginationLayout);

            // Apply theme to pagination buttons
            ApplyThemeToPaginationButtons();

            // Apply theme to pagination buttons
            ApplyThemeToPaginationButtons();
        }

        public async Task LoadVehiclesAsync()
        {
            try
            {
                var totalCount = await _vehicleService.GetCountAsync();
                _totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);

                _currentVehicles = await _vehicleService.GetPagedAsync(_currentPage, _pageSize);

                _vehiclesDataGridView.DataSource = _currentVehicles;
                UpdatePaginationControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading vehicles: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdatePaginationControls()
        {
            _pageInfoLabel.Text = $"Page {_currentPage} of {_totalPages}";

            bool prevWasEnabled = _previousPageButton.Enabled;
            bool nextWasEnabled = _nextPageButton.Enabled;

            _previousPageButton.Enabled = _currentPage > 1;
            _nextPageButton.Enabled = _currentPage < _totalPages;

            // Update button visuals if enabled state changed
            if (prevWasEnabled != _previousPageButton.Enabled || nextWasEnabled != _nextPageButton.Enabled)
            {
                ApplyThemeToPaginationButtons();
            }
        }

        private async void AddButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var newVehicle = new Vehicle
                {
                    Id = Guid.NewGuid(),
                    Number = "V001",
                    Capacity = 40,
                    Model = "New Model",
                    LicensePlate = "ABC-123",
                    IsActive = true
                };

                await _vehicleService.CreateAsync(newVehicle);
                await LoadVehiclesAsync();

                // Select the new row
                var newRowIndex = _currentVehicles.FindIndex(v => v.Id == newVehicle.Id);
                if (newRowIndex >= 0)
                {
                    _vehiclesDataGridView.ClearSelection();
                    _vehiclesDataGridView.Rows[newRowIndex].Selected = true;
                    _vehiclesDataGridView.CurrentCell = _vehiclesDataGridView.Rows[newRowIndex].Cells[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding vehicle: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void EditButton_Click(object? sender, EventArgs e)
        {
            if (_vehiclesDataGridView.SelectedRows.Count > 0)
            {
                var selectedVehicle = (Vehicle)_vehiclesDataGridView.SelectedRows[0].DataBoundItem;
                VehicleEditRequested?.Invoke(this, new EntityEventArgs<Vehicle>(selectedVehicle));
            }
        }

        private async void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_vehiclesDataGridView.SelectedRows.Count > 0)
            {
                var selectedVehicle = (Vehicle)_vehiclesDataGridView.SelectedRows[0].DataBoundItem;
                var result = MessageBox.Show(
                    $"Are you sure you want to delete vehicle {selectedVehicle.Number} ({selectedVehicle.Model})?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await _vehicleService.DeleteAsync(selectedVehicle.Id);
                        await LoadVehiclesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting vehicle: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private async void VehiclesDataGridView_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var vehicle = (Vehicle)_vehiclesDataGridView.Rows[e.RowIndex].DataBoundItem;
                await _vehicleService.UpdateAsync(vehicle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating vehicle: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                await LoadVehiclesAsync(); // Refresh to revert changes
            }
        }

        private async void VehiclesDataGridView_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            // Handle checkbox changes for IsActive column
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var columnName = _vehiclesDataGridView.Columns[e.ColumnIndex].Name; if (columnName == "IsActive")
                {
                    try
                    {
                        var vehicle = (Vehicle)_vehiclesDataGridView.Rows[e.RowIndex].DataBoundItem;
                        await _vehicleService.UpdateAsync(vehicle);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating vehicle: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        await LoadVehiclesAsync(); // Refresh to revert changes
                    }
                }
            }
        }
        private void VehiclesDataGridView_SelectionChanged(object? sender, EventArgs e)
        {
            bool hasSelection = _vehiclesDataGridView.SelectedRows.Count > 0;
            bool editWasEnabled = _editButton.Enabled;
            bool deleteWasEnabled = _deleteButton.Enabled;

            _editButton.Enabled = hasSelection;
            _deleteButton.Enabled = hasSelection;

            // Update button visuals if enabled state changed
            if (editWasEnabled != hasSelection || deleteWasEnabled != hasSelection)
            {
                ApplyThemeToButtons();
            }
        }

        private async void PreviousPageButton_Click(object? sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadVehiclesAsync();
            }
        }

        private async void NextPageButton_Click(object? sender, EventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadVehiclesAsync();
            }
        }
        public void Render(Control container)
        {
            ArgumentNullException.ThrowIfNull(container);
            container.Controls.Add(this);
        }
        protected override void ApplyTheme()
        {
            // Do not call base.ApplyTheme() to avoid abstract call error
            BackColor = ThemeManager.CurrentTheme.CardBackground;
            ThemeManager.CurrentTheme.StyleDataGrid(_vehiclesDataGridView);
            _pageInfoLabel.ForeColor = ThemeManager.CurrentTheme.CardText;
        }

        private void ApplyThemeToPaginationButtons()
        {
            if (_previousPageButton != null)
            {
                _previousPageButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _previousPageButton.ForeColor = ThemeManager.CurrentTheme.ButtonText;
                _previousPageButton.FlatStyle = FlatStyle.Flat;
                _previousPageButton.FlatAppearance.BorderSize = 0;
                _previousPageButton.Font = ThemeManager.CurrentTheme.ButtonFont;

                if (!_previousPageButton.Enabled)
                {
                    _previousPageButton.BackColor = ThemeManager.CurrentTheme.ButtonDisabledBackground;
                    _previousPageButton.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                }
            }

            if (_pageInfoLabel != null)
            {
                _pageInfoLabel.ForeColor = ThemeManager.CurrentTheme.CardText;
                _pageInfoLabel.Font = ThemeManager.CurrentTheme.CardFont;
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _nextPageButton.ForeColor = ThemeManager.CurrentTheme.ButtonText;
                _nextPageButton.FlatStyle = FlatStyle.Flat;
                _nextPageButton.FlatAppearance.BorderSize = 0;
                _nextPageButton.Font = ThemeManager.CurrentTheme.ButtonFont;

                if (!_nextPageButton.Enabled)
                {
                    _nextPageButton.BackColor = ThemeManager.CurrentTheme.ButtonDisabledBackground;
                    _nextPageButton.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                }
            }
        }

        private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
        {
            ApplyThemeToButtons();
            ApplyThemeToPaginationButtons();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from the theme changed event
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;

                // Unsubscribe from other events
                if (_addButton != null) _addButton.Click -= AddButton_Click;
                if (_editButton != null) _editButton.Click -= EditButton_Click;
                if (_deleteButton != null) _deleteButton.Click -= DeleteButton_Click;
                if (_previousPageButton != null) _previousPageButton.Click -= PreviousPageButton_Click;
                if (_nextPageButton != null) _nextPageButton.Click -= NextPageButton_Click;
                if (_vehiclesDataGridView != null)
                {
                    _vehiclesDataGridView.CellEndEdit -= VehiclesDataGridView_CellEndEdit;
                    _vehiclesDataGridView.SelectionChanged -= VehiclesDataGridView_SelectionChanged;
                    _vehiclesDataGridView.CellValueChanged -= VehiclesDataGridView_CellValueChanged;
                }
            }
            base.Dispose(disposing);
        }

        // Implement IDisposable explicitly to satisfy interface
        void IDisposable.Dispose()
        {
            Dispose();
        }
    }
}
