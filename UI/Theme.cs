using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BusBus.UI
{
    /// <summary>
    /// Base class for application themes
    /// </summary>
    public abstract class Theme : IDisposable
    {
        // Font cache to prevent repeated font creation
        private Font? _headlineFont;
        private Font? _cardFont;
        private Font? _buttonFont;
        private Font? _mediumButtonFont;
        private Font? _smallButtonFont;
        private Font? _textBoxFont;

        private bool _disposed;

        /// <summary>
        /// Name of the theme
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Main background color
        /// </summary>
        public abstract Color MainBackground { get; }

        /// <summary>
        /// Side panel background color
        /// </summary>
        public abstract Color SidePanelBackground { get; }

        /// <summary>
        /// Headline/header background color
        /// </summary>
        public abstract Color HeadlineBackground { get; }

        /// <summary>
        /// Card background color
        /// </summary>
        public abstract Color CardBackground { get; }

        /// <summary>
        /// Grid background color
        /// </summary>
        public abstract Color GridBackground { get; }

        /// <summary>
        /// Button background color
        /// </summary>
        public abstract Color ButtonBackground { get; }

        /// <summary>
        /// Button hover background color
        /// </summary>
        public abstract Color ButtonHoverBackground { get; }

        /// <summary>
        /// Card text color
        /// </summary>
        public abstract Color CardText { get; }

        /// <summary>
        /// Headline text color
        /// </summary>
        public abstract Color HeadlineText { get; }

        /// <summary>
        /// Text box background color
        /// </summary>
        public abstract Color TextBoxBackground { get; }

        /// <summary>
        /// Light variant of text box background color for better readability
        /// </summary>
        public virtual Color LightTextBoxBackground => 
            Color.FromArgb(255, 
                Math.Min(255, TextBoxBackground.R + 20),
                Math.Min(255, TextBoxBackground.G + 20),
                Math.Min(255, TextBoxBackground.B + 20));

        /// <summary>
        /// Gets border color (derived from other colors if not overridden)
        /// </summary>
        public virtual Color BorderColor => 
            Color.FromArgb(128, CardText.R, CardText.G, CardText.B);

        /// <summary>
        /// Gets disabled text color (derived from CardText if not overridden)
        /// </summary>
        public virtual Color DisabledText => 
            Color.FromArgb(128, CardText.R, CardText.G, CardText.B);

        /// <summary>
        /// Fonts used in the theme
        /// </summary>
        public virtual Font HeadlineFont => _headlineFont ??= TryCreateFont("Segoe UI", 28F, FontStyle.Bold) ?? SystemFonts.DefaultFont;
        public virtual Font CardFont => _cardFont ??= TryCreateFont("Segoe UI", 14F, FontStyle.Regular) ?? SystemFonts.DefaultFont;
        public virtual Font ButtonFont => _buttonFont ??= TryCreateFont("Segoe UI", 12F, FontStyle.Regular) ?? SystemFonts.DefaultFont;
        public virtual Font MediumButtonFont => _mediumButtonFont ??= TryCreateFont("Segoe UI", 10F, FontStyle.Regular) ?? SystemFonts.DefaultFont;
        public virtual Font SmallButtonFont => _smallButtonFont ??= TryCreateFont("Segoe UI", 8F, FontStyle.Regular) ?? SystemFonts.DefaultFont;
        public virtual Font TextBoxFont => _textBoxFont ??= TryCreateFont("Segoe UI", 10F, FontStyle.Regular) ?? SystemFonts.DefaultFont;

        /// <summary>
        /// Creates a font safely with fallbacks if the specified font is not available
        /// </summary>
        /// <param name="familyName">The font family name to create</param>
        /// <param name="emSize">The em-size of the font in points</param>
        /// <param name="style">The font style</param>
        /// <returns>The created font or default font if creation fails</returns>
        protected static Font? TryCreateFont(string familyName, float emSize, FontStyle style)
        {
            if (string.IsNullOrWhiteSpace(familyName))
                return SystemFonts.DefaultFont;

            if (emSize <= 0 || emSize > 72.0f)
                emSize = 12.0f;

            try
            {
                using var families = new System.Drawing.Text.InstalledFontCollection();
                bool fontExists = families.Families.Any(f =>
                    string.Equals(f.Name, familyName, StringComparison.OrdinalIgnoreCase));

                if (!fontExists)
                {
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
                    return new Font(SystemFonts.DefaultFont.Name, emSize, style);
                }

                return new Font(familyName, emSize, style);
            }
            catch (Exception ex)
            {
                // Log the exception instead of silently catching it
                Debug.WriteLine($"Font creation failed: {ex.Message}");
                return SystemFonts.DefaultFont;
            }
        }

        /// <summary>
        /// Styles a ComboBox to match the theme's text box appearance.
        /// </summary>
        /// <param name="comboBox">The ComboBox to style</param>
        public virtual void StyleComboBox(ComboBox comboBox)
        {
            ArgumentNullException.ThrowIfNull(comboBox);
            comboBox.BackColor = TextBoxBackground;
            comboBox.ForeColor = CardText;
            comboBox.Font = TextBoxFont;
            comboBox.FlatStyle = FlatStyle.Flat;
        }

        /// <summary>
        /// Styles a panel as a headline panel
        /// </summary>
        /// <param name="panel">The Panel to style</param>
        public virtual void StyleHeadlinePanel(Panel panel)
        {
            ArgumentNullException.ThrowIfNull(panel);
            panel.Height = 80;
            panel.BackColor = HeadlineBackground;
            panel.Dock = DockStyle.Top;
        }

        /// <summary>
        /// Styles a label as a headline label
        /// </summary>
        /// <param name="label">The Label to style</param>
        public virtual void StyleHeadlineLabel(Label label)
        {
            ArgumentNullException.ThrowIfNull(label);
            label.ForeColor = HeadlineText;
            label.Font = HeadlineFont;
            label.BackColor = Color.Transparent;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
        }

        /// <summary>
        /// Styles a panel as a card panel
        /// </summary>
        /// <param name="panel">The Panel to style</param>
        public virtual void StyleCardPanel(Panel panel)
        {
            ArgumentNullException.ThrowIfNull(panel);
            panel.BackColor = CardBackground;
            panel.BorderStyle = BorderStyle.None;
        }

        /// <summary>
        /// Styles a label as a card label
        /// </summary>
        /// <param name="label">The Label to style</param>
        public virtual void StyleCardLabel(Label label)
        {
            ArgumentNullException.ThrowIfNull(label);
            label.ForeColor = CardText;
            label.Font = CardFont;
            label.BackColor = Color.Transparent;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
        }

        /// <summary>
        /// Styles a DataGridView to match the theme
        /// </summary>
        /// <param name="grid">The DataGridView to style</param>
        public virtual void StyleDataGrid(DataGridView grid)
        {
            ArgumentNullException.ThrowIfNull(grid);
            grid.BackgroundColor = GridBackground;
            grid.ForeColor = CardText;
            grid.BorderStyle = BorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ButtonBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = HeadlineText;
            grid.EnableHeadersVisualStyles = false;
        }

        /// <summary>
        /// Styles a TextBox to match the theme
        /// </summary>
        /// <param name="textBox">The TextBox to style</param>
        public virtual void StyleTextBox(TextBox textBox)
        {
            ArgumentNullException.ThrowIfNull(textBox);
            textBox.BackColor = TextBoxBackground;
            textBox.ForeColor = CardText;
            textBox.Font = TextBoxFont;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        /// <summary>
        /// Determines whether the specified control has already been styled according to this theme
        /// </summary>
        /// <param name="control">The control to check</param>
        /// <returns>True if the control already matches this theme's styling</returns>
        public virtual bool IsControlStyled(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            if (control is Form form)
            {
                return form.BackColor == MainBackground;
            }
            else if (control is Panel panel)
            {
                if (panel.Tag?.ToString() == "HeadlinePanel" ||
                    panel.Name.Contains("Headline", StringComparison.OrdinalIgnoreCase))
                {
                    return panel.BackColor == HeadlineBackground;
                }
                else if (panel.Tag?.ToString() == "SidePanel" ||
                         panel.Name.Contains("SidePanel", StringComparison.OrdinalIgnoreCase))
                {
                    return panel.BackColor == SidePanelBackground;
                }
                else
                {
                    return panel.BackColor == CardBackground;
                }
            }
            else if (control is Button button)
            {
                return button.BackColor == ButtonBackground;
            }
            else if (control is TextBox textBox)
            {
                return textBox.BackColor == TextBoxBackground && textBox.ForeColor == CardText;
            }
            else if (control is ComboBox comboBox)
            {
                return comboBox.BackColor == TextBoxBackground && comboBox.ForeColor == CardText;
            }

            return false;
        }

        /// <summary>
        /// Disposes of all font resources used by this theme
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of all font resources used by this theme
        /// </summary>
        /// <param name="disposing">True if being called from Dispose(), false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources (fonts)
                    _headlineFont?.Dispose();
                    _cardFont?.Dispose();
                    _buttonFont?.Dispose();
                    _mediumButtonFont?.Dispose();
                    _smallButtonFont?.Dispose();
                    _textBoxFont?.Dispose();
                }

                // Set all font references to null
                _headlineFont = null;
                _cardFont = null;
                _buttonFont = null;
                _mediumButtonFont = null;
                _smallButtonFont = null;
                _textBoxFont = null;

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer that ensures resources are released
        /// </summary>
        ~Theme()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Light theme implementation using centralized color definitions
    /// </summary>
    public class LightTheme : Theme
    {
        public override string Name => "Light";
        public override Color MainBackground => ThemeColors.LightMainBackground;
        public override Color SidePanelBackground => ThemeColors.LightSidePanelBackground;
        public override Color HeadlineBackground => ThemeColors.LightHeadlineBackground;
        public override Color CardBackground => ThemeColors.LightCardBackground;
        public override Color GridBackground => ThemeColors.LightGridBackground;
        public override Color ButtonBackground => ThemeColors.LightButtonBackground;
        public override Color ButtonHoverBackground => ThemeColors.LightButtonHoverBackground;
        public override Color CardText => ThemeColors.LightCardText;
        public override Color HeadlineText => ThemeColors.LightHeadlineText;
        public override Color TextBoxBackground => ThemeColors.LightTextBoxBackground;
    }

    /// <summary>
    /// Dark theme implementation using centralized color definitions
    /// </summary>
    public class DarkTheme : Theme
    {
        public override string Name => "Dark";
        public override Color MainBackground => ThemeColors.DarkMainBackground;
        public override Color SidePanelBackground => ThemeColors.DarkSidePanelBackground;
        public override Color HeadlineBackground => ThemeColors.DarkHeadlineBackground;
        public override Color CardBackground => ThemeColors.DarkCardBackground;
        public override Color GridBackground => ThemeColors.DarkGridBackground;
        public override Color ButtonBackground => ThemeColors.DarkButtonBackground;
        public override Color ButtonHoverBackground => ThemeColors.DarkButtonHoverBackground;
        public override Color CardText => ThemeColors.DarkCardText;
        public override Color HeadlineText => ThemeColors.DarkHeadlineText;
        public override Color TextBoxBackground => ThemeColors.DarkTextBoxBackground;
    }

    /// <summary>
    /// Static theme colors for compatibility
    /// </summary>
    public static class ThemeColors
    {
        public static readonly Color LightMainBackground = Color.White;
        public static readonly Color LightSidePanelBackground = Color.FromArgb(248, 249, 250);
        public static readonly Color LightHeadlineBackground = Color.FromArgb(233, 236, 239);
        public static readonly Color LightCardBackground = Color.White;
        public static readonly Color LightGridBackground = Color.White;
        public static readonly Color LightButtonBackground = Color.FromArgb(0, 123, 255);
        public static readonly Color LightButtonHoverBackground = Color.FromArgb(0, 86, 179);
        public static readonly Color LightCardText = Color.Black;
        public static readonly Color LightHeadlineText = Color.FromArgb(33, 37, 41);
        public static readonly Color LightTextBoxBackground = Color.White;

        public static readonly Color DarkMainBackground = Color.FromArgb(33, 37, 41);
        public static readonly Color DarkSidePanelBackground = Color.FromArgb(52, 58, 64);
        public static readonly Color DarkHeadlineBackground = Color.FromArgb(73, 80, 87);
        public static readonly Color DarkCardBackground = Color.FromArgb(52, 58, 64);
        public static readonly Color DarkGridBackground = Color.FromArgb(52, 58, 64);
        public static readonly Color DarkButtonBackground = Color.FromArgb(40, 167, 69);
        public static readonly Color DarkButtonHoverBackground = Color.FromArgb(34, 142, 58);
        public static readonly Color DarkCardText = Color.White;
        public static readonly Color DarkHeadlineText = Color.White;
        public static readonly Color DarkTextBoxBackground = Color.FromArgb(73, 80, 87);
    }
}