using System;
using System.Drawing;
using System.Windows.Forms;
using BusBus.UI;

namespace BusBus
{
    public class ThemeTest : Form
    {
        private Button toggleButton;
        private Label statusLabel;
        private Panel testPanel;

        public ThemeTest()
        {
            InitializeComponents();
            UpdateDisplay();
        }

        private void InitializeComponents()
        {
            this.Text = "Theme Test - BusBus";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));

            // Status label
            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            // Test panel
            testPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Add some test controls to the panel
            var testLabel = new Label
            {
                Text = "This is a test label",
                Location = new Point(20, 20),
                Size = new Size(200, 30)
            };

            var testButton = new Button
            {
                Text = "Test Button",
                Location = new Point(20, 60),
                Size = new Size(100, 30)
            };

            var testTextBox = new TextBox
            {
                Text = "Test text box",
                Location = new Point(20, 100),
                Size = new Size(200, 25)
            };

            testPanel.Controls.AddRange(new Control[] { testLabel, testButton, testTextBox });

            // Toggle button
            toggleButton = new Button
            {
                Text = "Toggle Dark/Light Theme",
                Dock = DockStyle.Fill,
                Height = 40
            };
            toggleButton.Click += ToggleButton_Click;

            layout.Controls.Add(statusLabel, 0, 0);
            layout.Controls.Add(testPanel, 0, 1);
            layout.Controls.Add(toggleButton, 0, 2);

            this.Controls.Add(layout);

            // Subscribe to theme changes
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        }

        private void ToggleButton_Click(object? sender, EventArgs e)
        {
            string newTheme = ThemeManager.CurrentTheme.Name == "Dark" ? "Light" : "Dark";
            Console.WriteLine($"Switching from {ThemeManager.CurrentTheme.Name} to {newTheme}");
            ThemeManager.SwitchTheme(newTheme);
        }

        private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
        {
            Console.WriteLine($"Theme changed to: {ThemeManager.CurrentTheme.Name}");
            UpdateDisplay();
            ThemeManager.RefreshTheme(this);
        }        private void UpdateDisplay()
        {
            var theme = ThemeManager.CurrentTheme;
            statusLabel.Text = $"Current Theme: {theme.Name}";
            
            Console.WriteLine($"=== Theme Details ===");
            Console.WriteLine($"Name: {theme.Name}");
            Console.WriteLine($"MainBackground: {ColorToHex(theme.MainBackground)}");
            Console.WriteLine($"SidePanelBackground: {ColorToHex(theme.SidePanelBackground)}");
            Console.WriteLine($"CardBackground: {ColorToHex(theme.CardBackground)}");
            Console.WriteLine($"CardText: {ColorToHex(theme.CardText)}");
            Console.WriteLine($"ButtonBackground: {ColorToHex(theme.ButtonBackground)}");
            Console.WriteLine($"HeadlineBackground: {ColorToHex(theme.HeadlineBackground)}");
            Console.WriteLine($"HeadlineText: {ColorToHex(theme.HeadlineText)}");
            
            // Test elevation system for dark theme
            if (theme.Name == "Dark")
            {
                Console.WriteLine($"--- Elevation Test ---");
                for (int i = 0; i <= 4; i++)
                {
                    Console.WriteLine($"Elevation {i}: {ColorToHex(theme.GetElevatedBackground(i))}");
                }
            }
            Console.WriteLine($"==================");
        }

        private static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            }
            base.Dispose(disposing);        }

        // Rename from Main to avoid multiple entry points error
        public static void RunThemeTest()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var themeTest = new ThemeTest())
            {
                Application.Run(themeTest);
            }
        }
    }
}
