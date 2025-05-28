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

namespace BusBus.UI
{
    public partial class DriverListPanel : ThemeableControl, IDisplayable, IStatefulView
    {
        private readonly IDriverService _driverService;
        private DataGridView _driversDataGridView = null!;
        private Button _addButton = null!;
        private Button _editButton = null!;
        private Button _deleteButton = null!;
        private Button _previousPageButton = null!;
        private Button _nextPageButton = null!;
        private Label _pageInfoLabel = null!;
        private Panel _buttonPanel = null!;
        private Panel _paginationPanel;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1; private List<Driver> _currentDrivers = new List<Driver>();

        public event EventHandler<EntityEventArgs<Driver>>? DriverEditRequested;
        public event EventHandler<StatusEventArgs>? StatusUpdated;

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
            mainLayout.Controls.Add(_buttonPanel, 0, 0);            // DataGridView
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
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowHeadersVisible = false,
                RowTemplate = { Height = 32 },
                EditMode = DataGridViewEditMode.EditOnEnter
            };

            // Apply consistent theme styling to the grid
            ThemeManager.CurrentTheme.StyleDataGrid(_driversDataGridView);

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

            // LicenseNumber column
            var licenseColumn = new DataGridViewTextBoxColumn
            {
                Name = "LicenseNumber",
                HeaderText = "License Number",
                DataPropertyName = "LicenseNumber",
                FillWeight = 200,
                MinimumWidth = 150,
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
            var buttonLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 50,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Padding = new Padding(5)
            };
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            _addButton = new Button
            {
                Text = "Add Driver",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                AutoSize = false,
                MinimumSize = new Size(80, 30)
            };
            _addButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            _addButton.FlatAppearance.BorderSize = 1;
            _addButton.Click += AddButton_Click;

            _editButton = new Button
            {
                Text = "Edit",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                AutoSize = false,
                MinimumSize = new Size(80, 30),
                Enabled = false
            };
            _editButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            _editButton.FlatAppearance.BorderSize = 1;
            _editButton.Click += EditButton_Click;

            _deleteButton = new Button
            {
                Text = "Delete",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                AutoSize = false,
                MinimumSize = new Size(80, 30),
                Enabled = false
            };
            _deleteButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            _deleteButton.FlatAppearance.BorderSize = 1;
            _deleteButton.Click += DeleteButton_Click;

            buttonLayout.Controls.Add(_addButton, 0, 0);
            buttonLayout.Controls.Add(_editButton, 1, 0);
            buttonLayout.Controls.Add(_deleteButton, 2, 0);
            _buttonPanel.Controls.Add(buttonLayout);
        }
        private void SetupPagination()
        {
            // Create a horizontal layout for pagination controls
            var paginationLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(5),
                BackColor = ThemeManager.CurrentTheme.CardBackground
            };

            // Previous page button
            _previousPageButton = new Button
            {
                Text = "◀ Previous",
                Size = new Size(100, 35),
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                Enabled = false,
                Margin = new Padding(3)
            };
            _previousPageButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            _previousPageButton.FlatAppearance.BorderSize = 1;
            _previousPageButton.Click += PreviousPageButton_Click;

            // Page info label
            _pageInfoLabel = new Label
            {
                Text = "Page 1",
                Size = new Size(100, 35),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular),
                AutoSize = false,
                Margin = new Padding(10, 3, 10, 3)
            };

            // Next page button
            _nextPageButton = new Button
            {
                Text = "Next ▶",
                Size = new Size(100, 35),
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                Enabled = false,
                Margin = new Padding(3)
            };
            _nextPageButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            _nextPageButton.FlatAppearance.BorderSize = 1;
            _nextPageButton.Click += NextPageButton_Click;

            paginationLayout.Controls.Add(_previousPageButton);
            paginationLayout.Controls.Add(_pageInfoLabel);
            paginationLayout.Controls.Add(_nextPageButton);
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
        #endregion

        public async Task LoadDriversAsync()
        {
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
                StatusUpdated?.Invoke(this, new StatusEventArgs(
                    $"Error loading drivers: {ex.Message}", StatusType.Error));
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
                    case "LicenseNumber":
                        driver.LicenseNumber = newValue;
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

            // Apply theme to main panel
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;

            // Apply theme to data grid
            if (_driversDataGridView != null)
            {
                ThemeManager.CurrentTheme.StyleDataGrid(_driversDataGridView);
            }

            // Apply theme to buttons
            if (_addButton != null)
            {
                _addButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _addButton.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
                _addButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            }

            if (_editButton != null)
            {
                _editButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _editButton.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
                _editButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            }

            if (_deleteButton != null)
            {
                _deleteButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _deleteButton.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
                _deleteButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            }

            if (_previousPageButton != null)
            {
                _previousPageButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _previousPageButton.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
                _previousPageButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                _nextPageButton.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
                _nextPageButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            }

            // Apply theme to labels
            if (_pageInfoLabel != null)
            {
                _pageInfoLabel.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
            }

            // Apply theme to panels
            if (_buttonPanel != null)
            {
                _buttonPanel.BackColor = ThemeManager.CurrentTheme.CardBackground;
            }

            if (_paginationPanel != null)
            {
                _paginationPanel.BackColor = ThemeManager.CurrentTheme.CardBackground;
            }
        }

        public async Task RefreshAsync()
        {
            await LoadDriversAsync();
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
