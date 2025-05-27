#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
#pragma warning disable CS0169 // The field is never used
#pragma warning disable CA1416 // Platform compatibility (Windows-only)
#pragma warning disable CS1998 // Async method lacks 'await' operators
#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BusBus.UI
{
    public partial class SampleSidePanelForm : Form
    {
        private Panel sidePanel;
        private Panel mainPanel;
        private Panel headlinePanel;
        private Label headlineLabel;

        public SampleSidePanelForm()
        {
            InitializeComponent();
            ThemeManager.ApplyThemeToControl(this);
        }

        private void InitializeComponent()
        {
            this.Text = "BusBus - Side Panel Demo";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create headline panel
            headlinePanel = new Panel
            {
                Name = "HeadlinePanel",
                Tag = "HeadlinePanel",
                Dock = DockStyle.Top,
                Height = 80
            };

            headlineLabel = new Label
            {
                Name = "HeadlineLabel",
                Tag = "HeadlineLabel",
                Text = "BusBus Application",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            headlinePanel.Controls.Add(headlineLabel);

            // Create side panel
            sidePanel = new Panel
            {
                Name = "SidePanel",
                Tag = "SidePanel",
                Dock = DockStyle.Left,
                Width = 200
            };

            // Create side panel buttons
            var dashboardButton = CreateSidePanelButton("Dashboard", 0);
            var routesButton = CreateSidePanelButton("Routes", 1);
            var scheduleButton = CreateSidePanelButton("Schedule", 2);
            var settingsButton = CreateSidePanelButton("Settings", 3);
            var themeButton = CreateSidePanelButton("Toggle Theme", 4);

            // Add click handlers
            dashboardButton.Click += (s, e) => ShowContent("Dashboard");
            routesButton.Click += (s, e) => ShowContent("Routes");
            scheduleButton.Click += (s, e) => ShowContent("Schedule");
            settingsButton.Click += (s, e) => ShowContent("Settings");
            themeButton.Click += (s, e) => ToggleTheme();

            sidePanel.Controls.AddRange(new Control[] {
                dashboardButton,
                routesButton,
                scheduleButton,
                settingsButton,
                themeButton
            });

            // Create main content panel
            mainPanel = new Panel
            {
                Name = "MainPanel",
                Dock = DockStyle.Fill
            };

            // Add controls to form
            this.Controls.Add(mainPanel);
            this.Controls.Add(sidePanel);
            this.Controls.Add(headlinePanel);
        }

        private static Button CreateSidePanelButton(string text, int index)
        {
            var button = new Button
            {
                Name = $"sidePanelButton{index}",
                Text = text,
                Dock = DockStyle.Top,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            // The theme will be applied by ThemeManager
            return button;
        }

        private void ShowContent(string content)
        {
            mainPanel.Controls.Clear();

            var contentLabel = new Label
            {
                Text = $"{content} Content",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 24f, FontStyle.Regular)
            };

            mainPanel.Controls.Add(contentLabel);
            ThemeManager.ApplyThemeToControl(contentLabel);
        }

        private void ToggleTheme()
        {
            var currentThemeName = ThemeManager.CurrentTheme.Name;
            var newTheme = currentThemeName == "Light" ? "Dark" : "Light";
            ThemeManager.SwitchTheme(newTheme);
            ThemeManager.RefreshTheme(this);
        }
    }
}
