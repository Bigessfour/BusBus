using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusBus.UI.Templates;
using BusBus.Utils;

namespace BusBus.UI.Forms
{
    /// <summary>
    /// Sample high-quality form with excellent text rendering
    /// </summary>
    public class TransportFormView : HighQualityFormTemplate
    {
        private new readonly ILogger<TransportFormView> _logger;

        public override string ViewName => "transport";
        public override string Title => "Transport Management";

        public TransportFormView(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<TransportFormView>>();
            InitializeView();
        }

        protected override void InitializeView()
        {
            base.InitializeView();

            // Configure main layout
            _mainLayout.RowCount = 3;
            _mainLayout.ColumnCount = 2;

            // Set up rows and columns
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Create header that spans both columns
            var header = CreateHeaderSection(
                "Transport Management",
                "View and manage transportation routes and schedules");
            _mainLayout.Controls.Add(header, 0, 0);
            _mainLayout.SetColumnSpan(header, 2);

            // Left side - Routes List
            var routesPanel = CreateContentCard("Available Routes");
            _mainLayout.Controls.Add(routesPanel, 0, 1);

            // Create routes content
            var routesLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };

            routesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            routesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            routesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Create search box
            var searchLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, UiConstants.SmallPadding)
            };

            searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Search routes...",
                Font = ThemeManager.CurrentTheme.CardFont,
                BackColor = ThemeManager.CurrentTheme.TextBoxBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText
            };

            var searchButton = CreateStyledButton("Search", (s, e) =>
            {
                MessageBox.Show($"Searching for: {searchBox.Text}", "Search",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            searchLayout.Controls.Add(searchBox, 0, 0);
            searchLayout.Controls.Add(searchButton, 1, 0);

            // Create routes grid
            var routesGrid = CreateStyledDataGrid();
            routesGrid.Columns.AddRange(new[]
            {
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "Route ID",
                    Name = "RouteId",
                    Width = 80
                },
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "Name",
                    Name = "RouteName",
                    Width = 150
                },
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "Status",
                    Name = "Status",
                    Width = 100
                }
            });

            // Add some sample data
            routesGrid.Rows.Add("R-1001", "Downtown Express", "Active");
            routesGrid.Rows.Add("R-1002", "Airport Shuttle", "Active");
            routesGrid.Rows.Add("R-1003", "North-South Connector", "Inactive");
            routesGrid.Rows.Add("R-1004", "East-West Line", "Active");
            routesGrid.Rows.Add("R-1005", "Central Station Loop", "Active");

            routesLayout.Controls.Add(searchLayout, 0, 0);
            routesLayout.Controls.Add(routesGrid, 0, 1);

            routesPanel.Controls.Add(routesLayout);

            // Right side - Route Details
            var detailsPanel = CreateContentCard("Route Details");
            _mainLayout.Controls.Add(detailsPanel, 1, 1);

            // Create details layout
            var detailsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                BackColor = Color.Transparent
            };

            // Set column styles
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));

            // Add rows - all auto-size for better text rendering
            for (int i = 0; i < 6; i++)
            {
                detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Add field labels and values
            AddDetailField(detailsLayout, "Route ID:", "R-1001", 0);
            AddDetailField(detailsLayout, "Name:", "Downtown Express", 1);
            AddDetailField(detailsLayout, "Description:", "Express service connecting downtown area with business district and residential zones", 2);
            AddDetailField(detailsLayout, "Schedule:", "Weekdays: 6:00 AM - 11:00 PM\nWeekends: 7:00 AM - 10:00 PM", 3);
            AddDetailField(detailsLayout, "Status:", "Active", 4);

            // Add action buttons
            var actionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };

            var editButton = CreateStyledButton("Edit Route", (s, e) =>
            {
                MessageBox.Show("Edit route functionality", "Edit",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            var deactivateButton = CreateStyledButton("Deactivate", (s, e) =>
            {
                MessageBox.Show("Route deactivated", "Status Change",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            actionsPanel.Controls.Add(editButton);
            actionsPanel.Controls.Add(deactivateButton);

            detailsLayout.Controls.Add(actionsPanel, 0, 5);
            detailsLayout.SetColumnSpan(actionsPanel, 2);

            detailsPanel.Controls.Add(detailsLayout);

            // Bottom panel with actions - spans both columns
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(UiConstants.DefaultPadding)
            };

            var bottomLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var newRouteButton = CreateStyledButton("Add New Route", (s, e) =>
            {
                MessageBox.Show("Add new route functionality", "New Route",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            var exportButton = CreateStyledButton("Export Routes", (s, e) =>
            {
                MessageBox.Show("Export routes functionality", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            var settingsButton = CreateStyledButton("Settings", (s, e) =>
            {
                // Toggle high-accessibility mode for demonstration
                _highAccessibilityMode = !_highAccessibilityMode;
                SetHighAccessibilityMode(_highAccessibilityMode);
                MessageBox.Show($"Accessibility mode {(_highAccessibilityMode ? "enabled" : "disabled")}",
                    "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            bottomLayout.Controls.Add(newRouteButton);
            bottomLayout.Controls.Add(exportButton);
            bottomLayout.Controls.Add(settingsButton);

            bottomPanel.Controls.Add(bottomLayout);

            _mainLayout.Controls.Add(bottomPanel, 0, 2);
            _mainLayout.SetColumnSpan(bottomPanel, 2);

            // Enable layout debugging to detect text truncation
            LayoutDebugger.DetectTextTruncation(this);

            // Optimize table layouts to prevent truncation
            OptimizeTableLayoutForText(detailsLayout);

            // Apply high-quality text rendering again to catch any new controls
            TextRenderingManager.RegisterForHighQualityTextRendering(this);
        }

        private void AddDetailField(TableLayoutPanel layout, string label, string value, int row)
        {
            var labelControl = CreateStyledLabel(label, false, true);
            labelControl.Font = new Font(labelControl.Font, FontStyle.Bold);
            labelControl.Dock = DockStyle.Fill;

            var valueControl = CreateStyledLabel(value);
            valueControl.Dock = DockStyle.Fill;
            valueControl.AutoSize = true;

            // For multiline text, use maxwidth but unlimited height
            if (value.Contains('\n'))
            {
                valueControl.MaximumSize = new Size(layout.Width / 2, 0);
            }

            layout.Controls.Add(labelControl, 0, row);
            layout.Controls.Add(valueControl, 1, row);
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // No need for async operations in this sample
            await Task.CompletedTask;
        }

        protected override async Task OnDeactivateAsync()
        {
            // No need for async operations in this sample
            await Task.CompletedTask;
        }
    }
}
