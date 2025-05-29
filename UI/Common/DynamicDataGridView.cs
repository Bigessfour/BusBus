#nullable enable
#pragma warning disable CA1848 // Use LoggerMessage delegates for performance - minimal impact for debug/error logging in UI components
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.UI.Common;
using Microsoft.Extensions.Logging;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Dynamic DataGridView component that can switch between different entity types
    /// Used in DashboardView to provide a shared CRUD interface for Routes, Drivers, and Vehicles
    /// </summary>
    public class DynamicDataGridView<T> : UserControl where T : class
    {
        private readonly IDataViewConfiguration<T> _configuration;
        private readonly ILogger<DynamicDataGridView<T>> _logger;

        private EnhancedDataGridView _dataGrid = null!;
        private Panel _paginationPanel = null!;
        private Button _previousPageButton = null!;
        private Button _nextPageButton = null!;
        private Label _pageInfoLabel = null!;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;
        private int _totalRecords = 0;
        private List<T> _currentData = new();

        private CancellationTokenSource _cancellationTokenSource = new();

        public event EventHandler<T>? EntitySelected;
        public event EventHandler<StatusEventArgs>? StatusUpdated;
        public event EventHandler<T>? EntityCreateRequested;
        public event EventHandler<T>? EntityUpdateRequested;
        public event EventHandler<T>? EntityDeleteRequested;

        public T? SelectedEntity => _dataGrid.SelectedRows.Count > 0 ? (T)_dataGrid.SelectedRows[0].DataBoundItem : null;
        public bool HasSelection => _dataGrid.SelectedRows.Count > 0;
        public string ViewName => _configuration.ViewName;

        public DynamicDataGridView(IDataViewConfiguration<T> configuration, ILogger<DynamicDataGridView<T>>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<DynamicDataGridView<T>>();

            InitializeComponent();
            SetupDataGrid();
            SetupPagination();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Size = new Size(800, 600);
            BackColor = ThemeManager.CurrentTheme.CardBackground;
            Padding = new Padding(10);

            // Main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // DataGridView
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Pagination

            Controls.Add(mainLayout);

            // DataGridView
            _dataGrid = new EnhancedDataGridView
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            mainLayout.Controls.Add(_dataGrid, 0, 0);

            // Pagination panel
            _paginationPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            mainLayout.Controls.Add(_paginationPanel, 0, 1);

            ResumeLayout(false);
        }

        private void SetupDataGrid()
        {
            // Configure columns using the data view configuration
            _configuration.ConfigureColumns(_dataGrid);

            // Apply consistent theme styling
            ThemeManager.CurrentTheme.StyleDataGrid(_dataGrid);

            // Event handlers
            _dataGrid.CellEndEdit += DataGrid_CellEndEdit;
            _dataGrid.SelectionChanged += DataGrid_SelectionChanged;
            _dataGrid.CellValueChanged += DataGrid_CellValueChanged;
            _dataGrid.KeyDown += DataGrid_KeyDown;
        }

        private void SetupPagination()
        {
            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Anchor = AnchorStyles.None
            };

            _previousPageButton = new Button
            {
                Text = "◀ Previous",
                Size = new Size(80, 30),
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.ButtonText,
                Margin = new Padding(5)
            };
            _previousPageButton.Click += PreviousPageButton_Click;

            _pageInfoLabel = new Label
            {
                Text = "Page 1 of 1 (0 records)",
                Size = new Size(200, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Margin = new Padding(10, 5, 10, 5)
            };

            _nextPageButton = new Button
            {
                Text = "Next ▶",
                Size = new Size(80, 30),
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.ButtonText,
                Margin = new Padding(5)
            };
            _nextPageButton.Click += NextPageButton_Click;

            layout.Controls.AddRange(new Control[] { _previousPageButton, _pageInfoLabel, _nextPageButton });
            _paginationPanel.Controls.Add(layout);
        }

        public async Task LoadDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                using var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

                StatusUpdated?.Invoke(this, new StatusEventArgs($"Loading {_configuration.PluralName}..."));

                // Load data and total count in parallel
                var dataTask = _configuration.LoadDataAsync(_currentPage, _pageSize, combinedToken.Token);
                var countTask = _configuration.GetTotalCountAsync(combinedToken.Token);

                await Task.WhenAll(dataTask, countTask);

                _currentData = await dataTask;
                _totalRecords = await countTask;
                _totalPages = Math.Max(1, (int)Math.Ceiling((double)_totalRecords / _pageSize));

                // Update UI on main thread
                _dataGrid.DataSource = _currentData;
                UpdatePaginationControls();

                StatusUpdated?.Invoke(this, new StatusEventArgs($"Loaded {_currentData.Count} {_configuration.PluralName}"));

                _logger.LogDebug("Loaded {Count} {EntityType} records (Page {Page} of {TotalPages})",
                    _currentData.Count, _configuration.PluralName, _currentPage, _totalPages);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading {EntityType} data", _configuration.PluralName);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error loading {_configuration.PluralName}: {ex.Message}"));
            }
        }

        private void UpdatePaginationControls()
        {
            _previousPageButton.Enabled = _currentPage > 1;
            _nextPageButton.Enabled = _currentPage < _totalPages;
            _pageInfoLabel.Text = $"Page {_currentPage} of {_totalPages} ({_totalRecords} records)";
        }

        private async void PreviousPageButton_Click(object? sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadDataAsync();
            }
        }

        private async void NextPageButton_Click(object? sender, EventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadDataAsync();
            }
        }

        private async void DataGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentData.Count) return;

            try
            {
                var entity = _currentData[e.RowIndex];
                await _configuration.UpdateAsync(entity);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"{_configuration.ViewName} updated successfully"));
                EntityUpdateRequested?.Invoke(this, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {EntityType}", _configuration.ViewName);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error updating {_configuration.ViewName}: {ex.Message}"));
                await LoadDataAsync(); // Refresh to revert changes
            }
        }

        private void DataGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_dataGrid.SelectedRows.Count > 0)
            {
                var entity = (T)_dataGrid.SelectedRows[0].DataBoundItem;
                EntitySelected?.Invoke(this, entity);
            }
        }

        private async void DataGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                try
                {
                    var entity = _currentData[e.RowIndex];
                    await _configuration.UpdateAsync(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating {EntityType} cell value", _configuration.ViewName);
                    await LoadDataAsync(); // Refresh to revert changes
                }
            }
        }

        private void DataGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && HasSelection)
            {
                var entity = SelectedEntity!;
                EntityDeleteRequested?.Invoke(this, entity);
            }
            else if (e.KeyCode == Keys.Insert)
            {
                var newEntity = _configuration.CreateNewEntity();
                EntityCreateRequested?.Invoke(this, newEntity);
            }
        }

        public async Task CreateEntityAsync(T entity)
        {
            try
            {
                await _configuration.CreateAsync(entity);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"{_configuration.ViewName} created successfully"));
                await LoadDataAsync(); // Refresh to show new entity
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating {EntityType}", _configuration.ViewName);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error creating {_configuration.ViewName}: {ex.Message}"));
            }
        }

        public async Task DeleteSelectedEntityAsync()
        {
            if (!HasSelection) return;

            var entity = SelectedEntity!;
            try
            {
                var success = await _configuration.DeleteAsync(entity);
                if (success)
                {
                    StatusUpdated?.Invoke(this, new StatusEventArgs($"{_configuration.ViewName} deleted successfully"));
                    await LoadDataAsync(); // Refresh to remove deleted entity
                }
                else
                {
                    StatusUpdated?.Invoke(this, new StatusEventArgs($"Failed to delete {_configuration.ViewName}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {EntityType}", _configuration.ViewName);
                StatusUpdated?.Invoke(this, new StatusEventArgs($"Error deleting {_configuration.ViewName}: {ex.Message}"));
            }
        }        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Cancel();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Ignore if already disposed
                }
                finally
                {
                    _cancellationTokenSource?.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
