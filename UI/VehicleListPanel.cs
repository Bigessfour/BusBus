// Enable nullable reference types for this file
#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using BusBus.UI.Common;

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

        public static string Title => "Vehicles";

        public VehicleListPanel(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            InitializeComponent();
            SetupDataGridView();
            SetupButtons();
            SetupPagination();
            _ = LoadVehiclesAsync();
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

            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // DataGridView
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Pagination

            Controls.Add(mainLayout);

            // Button panel
            _buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            mainLayout.Controls.Add(_buttonPanel, 0, 0);

            // DataGridView
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
                EnableHeadersVisualStyles = false
            };
            mainLayout.Controls.Add(_vehiclesDataGridView, 0, 1);

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
                Text = "Add Vehicle",
                Size = new Size(100, 40),
                Margin = new Padding(0, 10, 10, 10),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(5, 10, 5, 10),
                AutoSize = false
            };
            _addButton.FlatAppearance.BorderSize = 0;
            _addButton.Click += AddButton_Click;

            _editButton = new Button
            {
                Text = "Edit",
                Size = new Size(80, 40),
                Margin = new Padding(0, 10, 10, 10),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Padding = new Padding(5, 10, 5, 10),
                AutoSize = false
            };
            _editButton.FlatAppearance.BorderSize = 0;
            _editButton.Click += EditButton_Click;

            _deleteButton = new Button
            {
                Text = "Delete",
                Size = new Size(80, 30),
                Margin = new Padding(0, 10, 10, 10),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _deleteButton.FlatAppearance.BorderSize = 0;
            _deleteButton.Click += DeleteButton_Click;

            buttonLayout.Controls.Add(_addButton);
            buttonLayout.Controls.Add(_editButton);
            buttonLayout.Controls.Add(_deleteButton);
            _buttonPanel.Controls.Add(buttonLayout);
        }

        private void SetupPagination()
        {
            var paginationLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _previousPageButton = new Button
            {
                Text = "Previous",
                Size = new Size(80, 40),
                Margin = new Padding(0, 5, 10, 5),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Padding = new Padding(5, 10, 5, 10),
                AutoSize = false
            };
            _previousPageButton.FlatAppearance.BorderSize = 0;
            _previousPageButton.Click += PreviousPageButton_Click;

            _pageInfoLabel = new Label
            {
                Text = "Page 1 of 1",
                AutoSize = true,
                Margin = new Padding(10, 8, 10, 5),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            _nextPageButton = new Button
            {
                Text = "Next",
                Size = new Size(80, 40),
                Margin = new Padding(0, 5, 10, 5),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Padding = new Padding(5, 10, 5, 10),
                AutoSize = false
            };
            _nextPageButton.FlatAppearance.BorderSize = 0;
            _nextPageButton.Click += NextPageButton_Click;

            paginationLayout.Controls.Add(_previousPageButton);
            paginationLayout.Controls.Add(_pageInfoLabel);
            paginationLayout.Controls.Add(_nextPageButton);
            _paginationPanel!.Controls.Add(paginationLayout);
        }

        public async Task LoadVehiclesAsync()
        {            try
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
            _previousPageButton.Enabled = _currentPage > 1;
            _nextPageButton.Enabled = _currentPage < _totalPages;
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
        }        private void EditButton_Click(object? sender, EventArgs e)
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
        }        private async void VehiclesDataGridView_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
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
                var columnName = _vehiclesDataGridView.Columns[e.ColumnIndex].Name;                if (columnName == "IsActive")
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
            _editButton.Enabled = hasSelection;
            _deleteButton.Enabled = hasSelection;
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
            }        }        public override void Render(Control container)
        {
            ArgumentNullException.ThrowIfNull(container);
            container.Controls.Add(this);
        }        protected override void ApplyTheme()
        {
            base.ApplyTheme();

            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
            ThemeManager.CurrentTheme.StyleDataGrid(_vehiclesDataGridView);
            _pageInfoLabel.ForeColor = ThemeManager.CurrentTheme.CardText;
        }
    }
}
