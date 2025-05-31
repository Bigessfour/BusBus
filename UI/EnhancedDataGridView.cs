using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.ComponentModel;
using BusBus.UI;
#pragma warning disable CS8618 // Non-nullable field/property must contain a non-null value when exiting constructor
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate
#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace BusBus.UI
{
    public class EnhancedDataGridView : DataGridView
    {
        private Dashboard? GetDashboardParent()
        {
            Control? parent = this.Parent;
            while (parent != null)
            {
                if (parent is Dashboard dashboard)
                    return dashboard;
                parent = parent.Parent;
            }
            return null;
        }
        private TextBox filterTextBox;
        private ComboBox filterColumnCombo;
        private Panel filterPanel;

        public EnhancedDataGridView()
        {
            SetupEnhancedFeatures();
        }

        private void SetupEnhancedFeatures()
        {
            // Modern styling
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            GridColor = Color.FromArgb(224, 224, 224);
            BackgroundColor = Color.White;
            RowHeadersVisible = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = true;

            // Enhanced appearance
            DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            DefaultCellStyle.SelectionForeColor = Color.White;
            AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);

            // Modern header style
            EnableHeadersVisualStyles = false;
            ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(68, 68, 68);
            ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 240, 240);
            ColumnHeadersHeight = 40;

            // Row styling
            RowTemplate.Height = 35;
            DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            DefaultCellStyle.Padding = new Padding(5, 0, 5, 0);

            // Enable sorting
            ColumnHeaderMouseClick += OnColumnHeaderMouseClick;

            // Add context menu
            SetupContextMenu();
        }

        private void SetupContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            var copyItem = new ToolStripMenuItem("Copy", null, (s, e) => CopySelectedRows());
            var exportItem = new ToolStripMenuItem("Export to CSV", null, (s, e) => ExportToCSV());
            var refreshItem = new ToolStripMenuItem("Refresh", null, (s, e) => RefreshData());

            contextMenu.Items.AddRange(new ToolStripItem[] { copyItem, exportItem, new ToolStripSeparator(), refreshItem });
            ContextMenuStrip = contextMenu;
        }

        public Panel CreateFilterPanel()
        {
            filterPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(5)
            };

            var lblFilter = new Label
            {
                Text = "🔍 Filter:",
                Location = new Point(10, 10),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10)
            };

            filterColumnCombo = new ComboBox
            {
                Location = new Point(75, 8),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };

            filterTextBox = new TextBox
            {
                Location = new Point(235, 8),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9)
            };
            filterTextBox.TextChanged += ApplyFilter;

            var btnClear = new Button
            {
                Text = "✖",
                Location = new Point(440, 8),
                Size = new Size(30, 25),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => { filterTextBox.Clear(); };

            filterPanel.Controls.AddRange(new Control[] { lblFilter, filterColumnCombo, filterTextBox, btnClear });

            return filterPanel;
        }

        protected override void OnDataSourceChanged(EventArgs e)
        {
            base.OnDataSourceChanged(e);

            // Auto-size columns intelligently
            if (DataSource != null)
            {
                AutoSizeColumns();
                PopulateFilterColumns();
                ApplyConditionalFormatting();
            }
        }

        private void AutoSizeColumns()
        {
            foreach (DataGridViewColumn column in Columns)
            {
                // Set minimum widths based on data type
                if (column.ValueType == typeof(DateTime))
                {
                    column.Width = 120;
                }
                else if (column.ValueType == typeof(int) || column.ValueType == typeof(decimal))
                {
                    column.Width = 100;
                }
                else
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }

                // Add sorting glyph space
                column.SortMode = DataGridViewColumnSortMode.Programmatic;
            }
        }

        private void PopulateFilterColumns()
        {
            if (filterColumnCombo != null)
            {
                filterColumnCombo.Items.Clear();
                filterColumnCombo.Items.Add("All Columns");

                foreach (DataGridViewColumn column in Columns)
                {
                    if (column.Visible)
                    {
                        filterColumnCombo.Items.Add(column.HeaderText);
                    }
                }

                filterColumnCombo.SelectedIndex = 0;
            }
        }

        private void ApplyFilter(object sender, EventArgs e)
        {
            if (DataSource == null) return;

            var filterText = filterTextBox.Text.ToLower();

            foreach (DataGridViewRow row in Rows)
            {
                if (string.IsNullOrEmpty(filterText))
                {
                    row.Visible = true;
                    continue;
                }

                bool visible = false;

                if (filterColumnCombo.SelectedIndex == 0) // All columns
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.Value?.ToString().Contains(filterText, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            visible = true;
                            break;
                        }
                    }
                }
                else // Specific column
                {
                    var columnIndex = filterColumnCombo.SelectedIndex - 1;
                    if (columnIndex >= 0 && columnIndex < row.Cells.Count)
                    {
                        visible = row.Cells[columnIndex].Value?.ToString().Contains(filterText, StringComparison.OrdinalIgnoreCase) == true;
                    }
                }

                row.Visible = visible;
            }

            // Update status
            UpdateFilterStatus();
        }

        private void UpdateFilterStatus()
        {
            var visibleRows = Rows.Cast<DataGridViewRow>().Count(r => r.Visible);
            var totalRows = Rows.Count;

            var dashboard = GetDashboardParent();
            if (dashboard != null)
            {
                dashboard.UpdateStatusMessage($"Showing {visibleRows} of {totalRows} records");
            }
        }

        private void ApplyConditionalFormatting()
        {
            // Example: Highlight status columns
            foreach (DataGridViewRow row in Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn.Name.Contains("status", StringComparison.OrdinalIgnoreCase))
                    {
                        switch (cell.Value?.ToString().ToLower())
                        {
                            case "available":
                            case "active":
                                cell.Style.ForeColor = Color.Green;
                                cell.Style.Font = new Font(Font, FontStyle.Bold);
                                break;
                            case "maintenance":
                            case "inactive":
                                cell.Style.ForeColor = Color.Orange;
                                break;
                            case "out of service":
                                cell.Style.ForeColor = Color.Red;
                                break;
                        }
                    }
                }
            }
        }

        private void OnColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;

            var column = Columns[e.ColumnIndex];
            var sortDirection = column.HeaderCell.SortGlyphDirection;

            // Clear other columns' sort glyphs
            foreach (DataGridViewColumn col in Columns)
            {
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            // Toggle sort direction
            if (sortDirection == SortOrder.Ascending)
            {
                Sort(column, ListSortDirection.Descending);
                column.HeaderCell.SortGlyphDirection = SortOrder.Descending;
            }
            else
            {
                Sort(column, ListSortDirection.Ascending);
                column.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
            }
        }

        private void CopySelectedRows()
        {
            if (SelectedRows.Count > 0)
            {
                var data = string.Join("\n", SelectedRows.Cast<DataGridViewRow>()
                    .Select(r => string.Join("\t", r.Cells.Cast<DataGridViewCell>()
                        .Select(c => c.Value?.ToString() ?? ""))));

                Clipboard.SetText(data);
            }
        }

        private static void ExportToCSV()

        {
            // Implementation for CSV export
        }

        private void RefreshData()
        {
            var dashboard = GetDashboardParent();
            if (dashboard != null)
            {
                dashboard.RefreshCurrentView();
            }
        }
    }
}
