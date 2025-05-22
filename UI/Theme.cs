using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BusBus.UI
{
    public class Theme
    {
        public string Name { get; }
        public Color MainBackground { get; }
        public Color HeadlineBackground { get; }
        public Color CardBackground { get; }
        public Color HeadlineText { get; }
        public Color CardText { get; }
        public Color SidePanelBackground { get; }
        public Color ButtonBackground { get; }
        public Color ButtonHoverBackground { get; }
        public Color GridBackground { get; }
        public Color TextBoxBackground { get; }

        public Font HeadlineFont { get; }
        public Font CardFont { get; }
        public Font ButtonFont { get; }
        public Font MediumButtonFont { get; }
        public Font SmallButtonFont { get; }
        public Font TextBoxFont { get; }

        public Theme(string name)
        {
            Name = name;
            if (name == "Light")
            {
                MainBackground = Color.FromArgb(240, 240, 240);
                HeadlineBackground = Color.FromArgb(200, 200, 220);
                CardBackground = Color.FromArgb(220, 220, 230);
                HeadlineText = Color.Black;
                CardText = Color.DarkGray;
                SidePanelBackground = Color.FromArgb(210, 210, 220);
                ButtonBackground = Color.FromArgb(180, 180, 200);
                ButtonHoverBackground = Color.FromArgb(160, 160, 180);
                GridBackground = Color.FromArgb(230, 230, 240);
                TextBoxBackground = Color.FromArgb(255, 255, 255);
            }
            else // Default (Dark)
            {
                MainBackground = Color.FromArgb(30, 30, 30);
                HeadlineBackground = Color.FromArgb(44, 51, 73);
                CardBackground = Color.FromArgb(40, 40, 50);
                HeadlineText = Color.White;
                CardText = Color.Gainsboro;
                SidePanelBackground = Color.FromArgb(36, 36, 46);
                ButtonBackground = Color.FromArgb(44, 51, 73);
                ButtonHoverBackground = Color.FromArgb(60, 70, 100);
                GridBackground = Color.FromArgb(35, 35, 45);
                TextBoxBackground = Color.FromArgb(50, 50, 60);
            }

            HeadlineFont = TryCreateFont("Segoe UI", 28F, FontStyle.Bold) ?? SystemFonts.DefaultFont;
            CardFont = TryCreateFont("Segoe UI", 14F, FontStyle.Regular) ?? SystemFonts.DefaultFont;
            ButtonFont = TryCreateFont("Segoe UI Symbol", 24F, FontStyle.Bold) ?? SystemFonts.DefaultFont;
            MediumButtonFont = TryCreateFont("Segoe UI Symbol", 20F, FontStyle.Bold) ?? SystemFonts.DefaultFont;
            SmallButtonFont = TryCreateFont("Segoe UI Symbol", 16F, FontStyle.Bold) ?? SystemFonts.DefaultFont;
            TextBoxFont = TryCreateFont("Segoe UI", 12F, FontStyle.Regular) ?? SystemFonts.DefaultFont;
        }

        private static Font TryCreateFont(string familyName, float emSize, FontStyle style)
        {
            if (string.IsNullOrWhiteSpace(familyName))
            {

                return SystemFonts.DefaultFont;
            }


            if (emSize <= 0 || emSize > 72.0f)
            {
                emSize = 12.0f;
            }


            try
            {
                using var families = new System.Drawing.Text.InstalledFontCollection();
                bool fontExists = families.Families.Any(f =>
                    string.Equals(f.Name, familyName, StringComparison.OrdinalIgnoreCase));

                if (!fontExists)
                {
                    // Try a series of common system fonts as fallbacks
                    string[] fallbackFonts = new[] {
                        "Segoe UI", "Arial", "Tahoma", "Verdana", "Calibri", "Microsoft Sans Serif"
                    };

                    foreach (string fallbackFont in fallbackFonts)
                    {
                        if (families.Families.Any(f =>
                            string.Equals(f.Name, fallbackFont, StringComparison.OrdinalIgnoreCase)))
                        {
                            return new Font(fallbackFont, emSize, style);
                        }
                    }

                    // If none of the fallbacks work, use the system default
                    return new Font(SystemFonts.DefaultFont.Name, emSize, style);
                }

                return new Font(familyName, emSize, style);
            }
            catch (ArgumentException)
            {
                // Font creation issues - fall back to system default
                return new Font(SystemFonts.DefaultFont.FontFamily, emSize, style);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // Font creation issues related to GDI+ - fall back to system default
                return SystemFonts.DefaultFont;
            }
        }

        /// <summary>
        /// Styles a ComboBox to match the theme’s text box appearance.
        /// </summary>
        /// <param name="comboBox">The ComboBox to style.</param>
        public void StyleComboBox(ComboBox comboBox)
        {
            ArgumentNullException.ThrowIfNull(comboBox);
            comboBox.BackColor = TextBoxBackground;
            comboBox.ForeColor = CardText;
            comboBox.Font = TextBoxFont;
            comboBox.FlatStyle = FlatStyle.Flat;
        }

        public void StyleHeadlinePanel(Panel panel)
        {
            ArgumentNullException.ThrowIfNull(panel);
            panel.Height = 80;
            panel.BackColor = HeadlineBackground;
            panel.Dock = DockStyle.Top;
        }

        public void StyleHeadlineLabel(Label label)
        {
            ArgumentNullException.ThrowIfNull(label);
            label.ForeColor = HeadlineText;
            label.Font = HeadlineFont;
            label.BackColor = Color.Transparent;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
        }

        public void StyleCardPanel(Panel panel)
        {
            ArgumentNullException.ThrowIfNull(panel);
            panel.BackColor = CardBackground;
            panel.BorderStyle = BorderStyle.None;
        }

        public void StyleCardLabel(Label label)
        {
            ArgumentNullException.ThrowIfNull(label);
            label.ForeColor = CardText;
            label.Font = CardFont;
            label.BackColor = Color.Transparent;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
        }

        public void StyleDataGrid(DataGridView grid)
        {
            ArgumentNullException.ThrowIfNull(grid);
            grid.BackgroundColor = GridBackground;
            grid.ForeColor = CardText;
            grid.BorderStyle = BorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ButtonBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = HeadlineText;
            grid.EnableHeadersVisualStyles = false;
        }

        public void StyleTextBox(TextBox textBox)
        {
            ArgumentNullException.ThrowIfNull(textBox);
            textBox.BackColor = TextBoxBackground;
            textBox.ForeColor = CardText;
            textBox.Font = TextBoxFont;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }
    }

    public static class ThemeManager
    {
        private static Theme _currentTheme = new Theme("Dark");

        /// <summary>
        /// Event triggered when the theme changes
        /// </summary>
        public static event EventHandler? ThemeChanged;

        public static Theme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                ArgumentNullException.ThrowIfNull(value);


                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnThemeChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Raises the ThemeChanged event
        /// </summary>
        private static void OnThemeChanged(EventArgs e)
        {
            ThemeChanged?.Invoke(null, e);
        }

        public static void SwitchTheme(string themeName)
        {
            CurrentTheme = new Theme(themeName);
        }

        /// <summary>
        /// Refreshes the theme for the given form
        /// </summary>
        public static void RefreshTheme(Form form)
        {
            ArgumentNullException.ThrowIfNull(form);
            ApplyThemeToControl(form);
            form.Refresh();
        }

        /// <summary>
        /// Applies the current theme to a control and its children
        /// </summary>
        private static void ApplyThemeToControl(Control control)
        {
            // Apply theme based on control type
            switch (control)
            {
                case Form form:
                    form.BackColor = CurrentTheme.MainBackground;
                    break;

                case Panel panel:
                    panel.BackColor = CurrentTheme.CardBackground;
                    break;

                case Label label:
                    label.ForeColor = CurrentTheme.CardText;
                    label.Font = CurrentTheme.CardFont;
                    break;

                case Button button:
                    button.BackColor = CurrentTheme.ButtonBackground;
                    button.ForeColor = CurrentTheme.HeadlineText;
                    button.FlatStyle = FlatStyle.Flat;
                    break;

                case TextBox textBox:
                    textBox.BackColor = CurrentTheme.TextBoxBackground;
                    textBox.ForeColor = CurrentTheme.CardText;
                    textBox.Font = CurrentTheme.TextBoxFont;
                    break;

                case ComboBox comboBox:
                    CurrentTheme.StyleComboBox(comboBox);
                    break;
            }

            // Apply to children
            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child);
            }
        }
    }
}