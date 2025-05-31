// <auto-added>
#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.UI.Common;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Generic data grid panel for CRUD operations on any entity type
    /// Provides consistent UI pattern for all entity management forms
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    public class GenericDataGridPanel<T, TKey> : UserControl, IDisplayable where T : class
    {
        private readonly GenericCrudHelper<T, TKey> _crudHelper;
        private readonly Func<T, TKey> _getIdFunc;
        private readonly Action<DataGridView> _setupColumnsAction;
        private readonly string _entityName;
        private readonly string _entityPluralName;

        private DataGridView? _dataGrid;
        private Button? _addButton;
        private Button? _editButton;
        private Button? _deleteButton;
        private Button? _refreshButton;
        private Button? _prevPageButton;
        private Button? _nextPageButton;
        private Label? _pageInfoLabel;
        private Label? _titleLabel;

        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalItems;
        private List<T> _entities = new List<T>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public event EventHandler<EntityEventArgs<T>>? EntitySelected;
        public event EventHandler<EntityEventArgs<T>>? EntityDoubleClicked;

        /// <summary>
        /// Gets the underlying DataGridView for advanced customization
        /// </summary>
        public DataGridView DataGrid => _dataGrid!;

        /// <summary>
        /// Gets or sets the page size for pagination
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value > 0)
                {
                    _pageSize = value;
                    _ = LoadEntitiesAsync();
                }
            }
        }

        public GenericDataGridPanel(
            GenericCrudHelper<T, TKey> crudHelper,
            Func<T, TKey> getIdFunc,
            Action<DataGridView> setupColumnsAction,
            string entityName,
            string? entityPluralName = null)
        {
            _crudHelper = crudHelper ?? throw new ArgumentNullException(nameof(crudHelper));
            _getIdFunc = getIdFunc ?? throw new ArgumentNullException(nameof(getIdFunc));
            _setupColumnsAction = setupColumnsAction ?? throw new ArgumentNullException(nameof(setupColumnsAction));
            _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
            _entityPluralName = entityPluralName ?? _entityName + "s";

            InitializeComponents();
            SetupDataGrid();
            SetupEventHandlers();
            ApplyTheme();
        }

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeManager.CurrentTheme.CardBackground;
            Padding = new Padding(0);

            // Title
            _titleLabel = new Label
            {
                Text = $"{_entityPluralName} Management",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Main container
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = ThemeManager.CurrentTheme.CardBackground
            };
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Buttons
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Grid
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Pagination

            // Buttons panel
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 60,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Padding = new Padding(10)
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            _addButton = CreateButton($"Add {_entityName}", "Add a new " + _entityName.ToUpperInvariant());
            _editButton = CreateButton("Edit", "Edit selected " + _entityName.ToUpperInvariant());
            _deleteButton = CreateButton("Delete", "Delete selected " + _entityName.ToUpperInvariant());
            _refreshButton = CreateButton("Refresh", "Refresh the list");

            _editButton.Enabled = false;
            _deleteButton.Enabled = false;

            buttonPanel.Controls.Add(_addButton, 0, 0);
            buttonPanel.Controls.Add(_editButton, 1, 0);
            buttonPanel.Controls.Add(_deleteButton, 2, 0);
            buttonPanel.Controls.Add(_refreshButton, 3, 0);

            // Data grid
            _dataGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = Color.FromArgb(200, 200, 200),
                BackgroundColor = ThemeManager.CurrentTheme.GridBackground,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 10F),
                    BackColor = ThemeManager.CurrentTheme.CardBackground,
                    ForeColor = ThemeManager.CurrentTheme.CardText,
                    SelectionBackColor = ThemeManager.CurrentTheme.ButtonBackground,
                    SelectionForeColor = Color.White,
                    Padding = new Padding(4),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    BackColor = ThemeManager.CurrentTheme.HeadlineBackground,
                    ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = new Padding(4)
                },
                RowHeadersVisible = false,
                RowTemplate = { Height = 32 },
                AllowUserToResizeRows = false
            };

            // Pagination panel
            var paginationPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 50,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = ThemeManager.CurrentTheme.CardBackground
            };
            paginationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            paginationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            paginationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            _prevPageButton = CreateButton("Previous", "Go to previous page");
            _nextPageButton = CreateButton("Next", "Go to next page");
            _pageInfoLabel = new Label
            {
                Text = "Page 1 of 1",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = new Font("Segoe UI", 10F)
            };

            paginationPanel.Controls.Add(_prevPageButton, 0, 0);
            paginationPanel.Controls.Add(_pageInfoLabel, 1, 0);
            paginationPanel.Controls.Add(_nextPageButton, 2, 0);

            // Add controls to main container
            mainContainer.Controls.Add(buttonPanel, 0, 0);
            mainContainer.Controls.Add(_dataGrid, 0, 1);
            mainContainer.Controls.Add(paginationPanel, 0, 2);

            Controls.Add(_titleLabel);
            Controls.Add(mainContainer);
        }

        private static Button CreateButton(string text, string tooltip)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(2),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;

            if (!string.IsNullOrEmpty(tooltip))
            {
                using (var toolTip = new ToolTip())
                {
                    toolTip.SetToolTip(button, tooltip);
                }
            }

            return button;
        }

        private void SetupDataGrid()
        {
            _setupColumnsAction(_dataGrid!);
        }

        private void SetupEventHandlers()
        {
            _addButton!.Click += async (s, e) => await OnAddEntityAsync();
            _editButton!.Click += async (s, e) => await OnEditEntityAsync();
            _deleteButton!.Click += async (s, e) => await OnDeleteEntityAsync();
            _refreshButton!.Click += async (s, e) => await LoadEntitiesAsync();
            _prevPageButton!.Click += async (s, e) => await GoToPreviousPageAsync();
            _nextPageButton!.Click += async (s, e) => await GoToNextPageAsync();

            _dataGrid!.SelectionChanged += OnDataGridSelectionChanged;
            _dataGrid!.CellDoubleClick += OnDataGridCellDoubleClick;

            // Handle data errors gracefully
            _dataGrid!.DataError += OnDataGridDataError;
        }
        /// <summary>
        /// Handles DataGridView data errors to provide user-friendly feedback and suppress default dialogs.
        /// </summary>
        private void OnDataGridDataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            // Show a user-friendly message for format/conversion errors
            string columnName = e.ColumnIndex >= 0 && _dataGrid!.Columns.Count > e.ColumnIndex
                ? _dataGrid!.Columns[e.ColumnIndex].HeaderText
                : "Unknown";
            string message = $"Invalid value for column '{columnName}'. Please enter a value of the correct type.";
            MessageBox.Show(message, "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.ThrowException = false;
            e.Cancel = true;
        }

        private void OnDataGridSelectionChanged(object? sender, EventArgs e)
        {
            bool hasSelection = _dataGrid!.SelectedRows.Count > 0;
            _editButton!.Enabled = hasSelection;
            _deleteButton!.Enabled = hasSelection;

            if (hasSelection && _dataGrid!.SelectedRows[0].DataBoundItem is T selectedEntity)
            {
                EntitySelected?.Invoke(this, new EntityEventArgs<T>(selectedEntity));
            }
        }

        private void OnDataGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _entities.Count)
            {
                EntityDoubleClicked?.Invoke(this, new EntityEventArgs<T>(_entities[e.RowIndex]));
            }
        }

        private static async Task OnAddEntityAsync()
        {
            // Override in derived classes or handle via events
            await Task.CompletedTask;
        }

        private async Task OnEditEntityAsync()
        {
            if (_dataGrid == null)
                return;
            if (_dataGrid.SelectedRows.Count > 0 && _dataGrid.SelectedRows[0].DataBoundItem is T selectedEntity)
            {
                // Override in derived classes or handle via events
                EntityDoubleClicked?.Invoke(this, new EntityEventArgs<T>(selectedEntity));
            }
            await Task.CompletedTask;
        }

        private async Task OnDeleteEntityAsync()
        {
            if (_dataGrid == null)
                return;
            if (_dataGrid.SelectedRows.Count > 0 && _dataGrid.SelectedRows[0].DataBoundItem is T selectedEntity)
            {
                try
                {
                    var id = _getIdFunc(selectedEntity);
                    await _crudHelper.DeleteEntityAsync(id, _entityName.ToUpperInvariant());
                    await LoadEntitiesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting {_entityName.ToUpperInvariant()}: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task GoToPreviousPageAsync()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadEntitiesAsync();
            }
        }

        private async Task GoToNextPageAsync()
        {
            var totalPages = (int)Math.Ceiling((double)_totalItems / _pageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                await LoadEntitiesAsync();
            }
        }

        public async Task LoadEntitiesAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                // Load total count
                _totalItems = await _crudHelper.GetEntitiesCountAsync();

                // Load entities for current page
                _entities = await _crudHelper.GetEntitiesAsync(_currentPage, _pageSize);

                // Update data grid
                _dataGrid!.DataSource = _entities;

                // Update pagination info
                var totalPages = Math.Max(1, (int)Math.Ceiling((double)_totalItems / _pageSize));
                _pageInfoLabel!.Text = $"Page {_currentPage} of {totalPages} ({_totalItems} total)";

                _prevPageButton!.Enabled = _currentPage > 1;
                _nextPageButton!.Enabled = _currentPage < totalPages;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading {_entityPluralName.ToUpperInvariant()}: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
        protected virtual void ApplyTheme()
        {
            BackColor = ThemeManager.CurrentTheme.CardBackground;
            _titleLabel!.ForeColor = ThemeManager.CurrentTheme.CardText;
            _pageInfoLabel!.ForeColor = ThemeManager.CurrentTheme.CardText;

            // Apply consistent data grid styling
            if (_dataGrid != null)
            {
                ThemeManager.CurrentTheme.StyleDataGrid(_dataGrid);
            }
        }

        public virtual void Render(Control container)
        {
            ArgumentNullException.ThrowIfNull(container);
            container.Controls.Clear();
            container.Controls.Add(this);
            _ = LoadEntitiesAsync();
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

        void IDisplayable.RefreshTheme()
        {
            throw new NotImplementedException();
        }
    }
}
