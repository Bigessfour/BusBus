#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using BusBus.UI.Common;
using BusBus.UI;

namespace BusBus
{
    public partial class SidePanel : ThemeableControl
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
            // Apply industry-standard glassmorphism theming with Material Design principles
            this.Width = 200;
            this.Dock = DockStyle.Left;            // Apply glassmorphism styling with enhanced accessibility (WCAG 2.1 AA)
            this.BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(2); // Material Design 2dp elevation

            // Apply glassmorphism properties for modern UI
            if (ThemeManager.CurrentTheme.Name == "Dark")
            {
                // Enhanced glassmorphism effect for dark theme
                this.BackColor = Color.FromArgb(
                    (int)(ThemeManager.CurrentTheme.GlassEffectOpacity * 255),
                    ThemeManager.CurrentTheme.BlurBackgroundColor.R,
                    ThemeManager.CurrentTheme.BlurBackgroundColor.G,
                    ThemeManager.CurrentTheme.BlurBackgroundColor.B
                );
            }

            // Add subtle glass effect border following Nielsen Norman Group guidelines
            this.Paint += (sender, e) =>
            {
                if (ThemeManager.CurrentTheme.Name == "Dark")
                {
                    using (var borderPen = new Pen(ThemeManager.CurrentTheme.GlowColor, 1f))
                    {
                        e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                    }
                }
            };

            // Fleet Management Group - Enhanced spacing following Microsoft Fluent guidelines
            var fleetLabel = CreateGroupLabel("Fleet Management", 20);
            var routesBtn = CreateNavButton("🗺️ Routes", 50, "routes");
            var driversBtn = CreateNavButton("👤 Drivers", 90, "drivers");
            var vehiclesBtn = CreateNavButton("🚌 Vehicles", 130, "vehicles");
            var maintenanceBtn = CreateNavButton("🔧 Maintenance", 170, "maintenance");

            // Operations Group - Industry-standard spacing (8px grid system)
            var opsLabel = CreateGroupLabel("Operations", 220);
            var schedulesBtn = CreateNavButton("📅 Schedules", 250, "schedules");
            var reportsBtn = CreateNavButton("📊 Reports", 290, "reports");

            // Advanced Features Group - Following Apple HIG spacing guidelines
            var advancedLabel = CreateGroupLabel("Advanced Features", 340);
            var analyticsBtn = CreateNavButton("📊 Analytics", 370, "analytics");
            var trackingBtn = CreateNavButton("📍 GPS Tracking", 410, "tracking");
            var performanceBtn = CreateNavButton("⚡ Performance", 450, "performance");

            // AI-Powered Features Group - Enhanced visual hierarchy
            var aiLabel = CreateGroupLabel("AI Features", 500);
            var insightsBtn = CreateNavButton("🤖 AI Insights", 530, "ai-insights");
            var alertsBtn = CreateNavButton("🚨 Smart Alerts", 570, "smart-alerts");
            var chatBtn = CreateNavButton("💬 AI Assistant", 610, "ai-chat");

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
                ForeColor = ThemeManager.CurrentTheme.SecondaryText,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(15, top),
                AutoSize = true,
                Margin = new Padding(8, 4, 8, 8) // Industry-standard 8px grid spacing
            };
        }

        private Button CreateNavButton(string text, int top, string tag)
        {
            var button = new Button
            {
                Text = text,
                Tag = tag,
                Location = new Point(12, top), // Aligned with Material Design spacing
                Size = new Size(176, 36), // Industry-standard touch target (36px+ for accessibility)
                FlatStyle = FlatStyle.Flat,
                ForeColor = ThemeManager.CurrentTheme.ButtonText,
                BackColor = Color.Transparent, // Glassmorphism transparent background
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 8, 0), // Enhanced padding following design systems
                Margin = new Padding(4) // Consistent spacing
            };

            // Apply industry-standard button styling with glassmorphism
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
            button.FlatAppearance.MouseDownBackColor = ThemeManager.CurrentTheme.GetElevatedBackground(1);

            // Enhanced glassmorphism hover effects following NN/g interaction guidelines
            button.MouseEnter += (sender, e) =>
            {
                if (ThemeManager.CurrentTheme.Name == "Dark")
                {
                    button.BackColor = Color.FromArgb(40, ThemeManager.CurrentTheme.GlowColor.R,
                        ThemeManager.CurrentTheme.GlowColor.G, ThemeManager.CurrentTheme.GlowColor.B);
                }
                else
                {
                    button.BackColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
                }
            };

            button.MouseLeave += (sender, e) =>
            {
                button.BackColor = Color.Transparent;
            };

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

        /// <summary>
        /// Renders the SidePanel into the specified container control.
        /// Implementation of IDisplayable interface.
        /// </summary>
        /// <param name="container">The container control to render into</param>
        public override void Render(Control container)
        {
            if (container == null) return;
            container.Controls.Clear();
            container.Controls.Add(this);
            this.Dock = DockStyle.Left;
        }        /// <summary>
                 /// Applies the current theme to the SidePanel and all its child controls.
                 /// Enhanced with industry-standard glassmorphism effects and WCAG 2.1 AA compliance.
                 /// </summary>
        protected override void ApplyTheme()
        {
            base.ApplyTheme();

            // Apply enhanced glassmorphism theming to the SidePanel
            this.BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(2);

            if (ThemeManager.CurrentTheme.Name == "Dark")
            {
                // Apply advanced glassmorphism for dark theme
                this.BackColor = Color.FromArgb(
                    (int)(ThemeManager.CurrentTheme.GlassEffectOpacity * 255),
                    ThemeManager.CurrentTheme.BlurBackgroundColor.R,
                    ThemeManager.CurrentTheme.BlurBackgroundColor.G,
                    ThemeManager.CurrentTheme.BlurBackgroundColor.B
                );
            }

            // Apply theme to all child controls with enhanced styling
            ApplyThemeToControl(this);

            // Update group labels with enhanced typography following design systems
            foreach (Control control in this.Controls)
            {
                if (control is Label label)
                {
                    label.ForeColor = ThemeManager.CurrentTheme.SecondaryText;
                    // Enhanced typography with better contrast for accessibility
                    if (ThemeManager.CurrentTheme.Name == "Dark")
                    {
                        label.ForeColor = Color.FromArgb(230, 230, 230); // WCAG AA compliant contrast
                    }
                }
                else if (control is Button button)
                {
                    // Apply enhanced button styling with glassmorphism
                    button.ForeColor = ThemeManager.CurrentTheme.ButtonText;
                    button.BackColor = Color.Transparent;
                    button.FlatAppearance.MouseOverBackColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
                    button.FlatAppearance.MouseDownBackColor = ThemeManager.CurrentTheme.GetElevatedBackground(1);

                    // Ensure accessibility compliance for button colors
                    if (ThemeManager.CurrentTheme.Name == "Dark")
                    {
                        button.ForeColor = Color.FromArgb(245, 245, 245); // High contrast for dark theme
                    }
                }
            }
        }
    }
}
