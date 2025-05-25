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

namespace BusBus.UI
{
    public partial class DriverListPanel : ThemeableControl, IDisplayable
    {
        private readonly IDriverService _driverService;
        private DataGridView _driversDataGridView;
        private Button _addButton;
        private Button _editButton;
        private Button _deleteButton;
        private Button _previousPageButton;
        private Button _nextPageButton;
        private Label _pageInfoLabel;
        private Panel _buttonPanel;
        private Panel _paginationPanel;
        
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;        private List<Driver> _currentDrivers = new List<Driver>();

        public event EventHandler<EntityEventArgs<Driver>>? DriverEditRequested;

        public static string Title => "Drivers";

        public DriverListPanel(IDriverService driverService)
        {
            _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
            InitializeComponent();
            SetupDataGridView();
            SetupButtons();
            SetupPagination();
            _ = LoadDriversAsync();
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
                EnableHeadersVisualStyles = false
            };
            mainLayout.Controls.Add(_driversDataGridView, 0, 1);

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
                Width = 150,
                ReadOnly = false
            };
            _driversDataGridView.Columns.Add(firstNameColumn);

            // LastName column
            var lastNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "LastName",
                HeaderText = "Last Name",
                DataPropertyName = "LastName",
                Width = 150,
                ReadOnly = false
            };
            _driversDataGridView.Columns.Add(lastNameColumn);

            // LicenseNumber column
            var licenseColumn = new DataGridViewTextBoxColumn
            {
                Name = "LicenseNumber",
                HeaderText = "License Number",
                DataPropertyName = "LicenseNumber",
                Width = 200,
                ReadOnly = false
            };
            _driversDataGridView.Columns.Add(licenseColumn);

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
            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _addButton = new Button
            {
                Text = "Add Driver",
                Size = new Size(100, 30),
                Margin = new Padding(0, 10, 10, 10),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _addButton.FlatAppearance.BorderSize = 0;
            _addButton.Click += AddButton_Click;

            _editButton = new Button
            {
                Text = "Edit",
                Size = new Size(80, 30),
                Margin = new Padding(0, 10, 10, 10),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
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
                Size = new Size(80, 30),
                Margin = new Padding(0, 5, 10, 5),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
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
                Size = new Size(80, 30),
                Margin = new Padding(0, 5, 10, 5),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _nextPageButton.FlatAppearance.BorderSize = 0;
            _nextPageButton.Click += NextPageButton_Click;

            paginationLayout.Controls.Add(_previousPageButton);
            paginationLayout.Controls.Add(_pageInfoLabel);
            paginationLayout.Controls.Add(_nextPageButton);
            _paginationPanel.Controls.Add(paginationLayout);
        }

        public async Task LoadDriversAsync()        {
            try
            {
                var totalCount = await _driverService.GetCountAsync();
                _totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);
                
                _currentDrivers = await _driverService.GetPagedAsync(_currentPage, _pageSize);

                _driversDataGridView.DataSource = _currentDrivers;
                UpdatePaginationControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading drivers: {ex.Message}", "Error", 
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
                var newDriver = new Driver
                {
                    Id = Guid.NewGuid(),
                    FirstName = "New",
                    LastName = "Driver",
                    LicenseNumber = "DL000000"
                };

                await _driverService.CreateAsync(newDriver);
                await LoadDriversAsync();

                // Select the new row
                var newRowIndex = _currentDrivers.FindIndex(d => d.Id == newDriver.Id);
                if (newRowIndex >= 0)
                {
                    _driversDataGridView.ClearSelection();
                    _driversDataGridView.Rows[newRowIndex].Selected = true;
                    _driversDataGridView.CurrentCell = _driversDataGridView.Rows[newRowIndex].Cells[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding driver: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditButton_Click(object? sender, EventArgs e)        {
            if (_driversDataGridView.SelectedRows.Count > 0)
            {
                var selectedDriver = (Driver)_driversDataGridView.SelectedRows[0].DataBoundItem;
                DriverEditRequested?.Invoke(this, new EntityEventArgs<Driver>(selectedDriver));
            }
        }

        private async void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_driversDataGridView.SelectedRows.Count > 0)
            {
                var selectedDriver = (Driver)_driversDataGridView.SelectedRows[0].DataBoundItem;
                var result = MessageBox.Show(
                    $"Are you sure you want to delete driver {selectedDriver.FirstName} {selectedDriver.LastName}?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await _driverService.DeleteAsync(selectedDriver.Id);
                        await LoadDriversAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting driver: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void DriversDataGridView_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var driver = (Driver)_driversDataGridView.Rows[e.RowIndex].DataBoundItem;
                await _driverService.UpdateAsync(driver);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating driver: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                await LoadDriversAsync(); // Refresh to revert changes
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
            if (_currentPage < _totalPages)            {
                _currentPage++;
                await LoadDriversAsync();
            }
        }

        public override void Render(Control container)
        {
            if (container == null) return;
            container.Controls.Clear();
            container.Controls.Add(this);
            this.Dock = DockStyle.Fill;
        }

        protected override void ApplyTheme()
        {
            base.ApplyTheme();
            
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
            _driversDataGridView.BackgroundColor = ThemeManager.CurrentTheme.GridBackground;
            _driversDataGridView.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.CardBackground;
            _driversDataGridView.DefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.CardText;
            _driversDataGridView.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.CurrentTheme.HeadlineBackground;
            _driversDataGridView.ColumnHeadersDefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
            _pageInfoLabel.ForeColor = ThemeManager.CurrentTheme.CardText;
        }
    }
}
