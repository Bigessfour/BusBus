#nullable enable
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;


namespace BusBus.UI
{
    public class DashboardView : BaseView
    {
        private readonly IServiceProvider _serviceProvider;
        private TableLayoutPanel _layout = null!;
        private Label _welcomeLabel = null!;
        private Panel _statsPanel = null!;

        public override string ViewName => "dashboard";
        public override string Title => "Dashboard";

        public DashboardView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void InitializeView()
        {
            base.InitializeView();

            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(20)
            };

            // Configure layout
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Welcome section
            _welcomeLabel = new Label
            {
                Text = $"Welcome back, {Environment.UserName}!",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };

            _layout.Controls.Add(_welcomeLabel, 0, 0);
            _layout.SetColumnSpan(_welcomeLabel, 2);

            // Create stat cards
            CreateStatCards();

            this.Controls.Add(_layout);
        }

        private void CreateStatCards()
        {
            var cards = new[]
            {
                CreateStatCard("Active Routes", "12", "🚌", Color.FromArgb(66, 165, 245)),
                CreateStatCard("Available Drivers", "8", "👥", Color.FromArgb(102, 187, 106)),
                CreateStatCard("Vehicles", "15", "🚗", Color.FromArgb(255, 167, 38)),
                CreateStatCard("Today's Trips", "24", "📊", Color.FromArgb(239, 83, 80))
            };

            _statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true
            };

            foreach (var card in cards)
            {
                _statsPanel.Controls.Add(card);
            }

            _layout.Controls.Add(_statsPanel, 0, 1);
            _layout.SetColumnSpan(_statsPanel, 2);
        }

        private Panel CreateStatCard(string title, string value, string icon, Color color)
        {

            var card = new Panel
            {
                Size = new Size(250, 150),
                Margin = new Padding(10),
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Tag = "Elevation1"
            };

            var iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 36F),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = color
            };


            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                Location = new Point(100, 30),
                AutoSize = true,
                ForeColor = ThemeManager.CurrentTheme.CardText
            };


            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12F),
                Location = new Point(20, 100),
                AutoSize = true,
                ForeColor = ThemeManager.CurrentTheme.SecondaryText
            };

            card.Controls.AddRange(new Control[] { iconLabel, valueLabel, titleLabel });


            // Add hover effect
            card.MouseEnter += (s, e) => card.BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(2);
            card.MouseLeave += (s, e) => card.BackColor = ThemeManager.CurrentTheme.CardBackground;

            // Add click handler
            card.Click += (s, e) =>
            {
                switch (title)
                {
                    case "Active Routes":
                        NavigateTo("routes");
                        break;
                    case "Available Drivers":
                        NavigateTo("drivers");
                        break;
                    case "Vehicles":
                        NavigateTo("vehicles");
                        break;
                    case "Today's Trips":
                        NavigateTo("reports");
                        break;
                }
            };

            return card;
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            UpdateStatus("Loading dashboard...");

            // Simulate loading stats
            await Task.Delay(500, cancellationToken);

            UpdateStatus("Dashboard loaded", StatusType.Success);
        }

        protected override Task OnDeactivateAsync()
        {
            // Save any state if needed
            return Task.CompletedTask;
        }
    }
}
