#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BusBus
{
    public partial class SidePanel : UserControl
    {
        public event EventHandler<string>? NavigationButtonClicked;

        public SidePanel()
        {
            InitializeComponent();
            SetupPanel();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "SidePanel";
            this.ResumeLayout(false);
        }

        private void SetupPanel()
        {
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.Width = 200;
            this.Dock = DockStyle.Left;

            // Fleet Management Group
            var fleetLabel = CreateGroupLabel("Fleet Management", 10);
            var routesBtn = CreateNavButton("🗺️ Routes", 40, "routes");
            var driversBtn = CreateNavButton("👤 Drivers", 80, "drivers");
            var vehiclesBtn = CreateNavButton("🚌 Vehicles", 120, "vehicles");
            var maintenanceBtn = CreateNavButton("🔧 Maintenance", 160, "maintenance");

            // Operations Group
            var opsLabel = CreateGroupLabel("Operations", 210);
            var schedulesBtn = CreateNavButton("📅 Schedules", 240, "schedules");
            var reportsBtn = CreateNavButton("📊 Reports", 280, "reports");

            // Add advanced features group
            var advancedLabel = CreateGroupLabel("Advanced Features", 320);
            var analyticsBtn = CreateNavButton("📊 Analytics", 350, "analytics");
            var trackingBtn = CreateNavButton("📍 GPS Tracking", 390, "tracking");
            var performanceBtn = CreateNavButton("⚡ Performance", 430, "performance");

            // AI-Powered Features Group
            var aiLabel = CreateGroupLabel("AI Features", 480);
            var insightsBtn = CreateNavButton("🤖 AI Insights", 510, "ai-insights");
            var alertsBtn = CreateNavButton("🚨 Smart Alerts", 550, "smart-alerts");
            var chatBtn = CreateNavButton("💬 AI Assistant", 590, "ai-chat");

            this.Controls.AddRange(new Control[] {
                fleetLabel, routesBtn, driversBtn, vehiclesBtn, maintenanceBtn,
                opsLabel, schedulesBtn, reportsBtn,
                advancedLabel, analyticsBtn, trackingBtn, performanceBtn,
                aiLabel, insightsBtn, alertsBtn, chatBtn
            });
        }

        private static Label CreateGroupLabel(string text, int top)
        {
            return new Label
            {
                Text = text.ToUpper(),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(15, top),
                AutoSize = true
            };
        }

        private Button CreateNavButton(string text, int top, string tag)
        {
            var button = new Button
            {
                Text = text,
                Tag = tag,
                Location = new Point(10, top),
                Size = new Size(180, 35),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 65),
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 85);
            button.Click += NavButton_Click;

            return button;
        }

        private void NavButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                NavigationButtonClicked?.Invoke(this, button.Tag.ToString()!);
            }
            else
            {
                NavigationButtonClicked?.Invoke(this, string.Empty);
            }
        }
    }
}
