#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BusBus.UI
{
    /// <summary>
    /// Defines centralized color values used across different themes
    /// </summary>
    public static class ThemeColors
    {
        // Light theme colors - Enhanced for better contrast
        public static readonly Color LightMainBackground = Color.FromArgb(240, 240, 240);
        public static readonly Color LightSidePanelBackground = Color.FromArgb(210, 210, 220);
        public static readonly Color LightHeadlineBackground = Color.FromArgb(200, 200, 220);
        public static readonly Color LightCardBackground = Color.FromArgb(220, 220, 230);
        public static readonly Color LightGridBackground = Color.FromArgb(230, 230, 240);
        public static readonly Color LightButtonBackground = Color.FromArgb(180, 180, 200);
        public static readonly Color LightButtonHoverBackground = Color.FromArgb(160, 160, 180);
        public static readonly Color LightButtonPressedBackground = Color.FromArgb(140, 140, 160);
        public static readonly Color LightButtonDisabledBackground = Color.FromArgb(200, 200, 210);
        public static readonly Color LightCardText = Color.FromArgb(50, 50, 50);        // Darker for better contrast
        public static readonly Color LightSecondaryText = Color.FromArgb(80, 80, 80);   // Darker for better contrast
        public static readonly Color LightHeadlineText = Color.Black;
        public static readonly Color LightTextBoxBackground = Color.White;
        public static readonly Color LightButtonText = Color.Black;
        public static readonly Color LightButtonDisabledText = Color.FromArgb(120, 120, 120); // Darker disabled text

        // Dark theme colors - Enhanced Modern palette with WCAG 2.1 AA compliance and glassmorphism support
        public static readonly Color DarkMainBackground = Color.FromArgb(16, 16, 20);      // #101014 - Enhanced deep background
        public static readonly Color DarkSidePanelBackground = Color.FromArgb(28, 30, 42); // #1C1E2A - Blue-tinted navigation
        public static readonly Color DarkHeadlineBackground = Color.FromArgb(35, 40, 48);  // #232830 - Enhanced header with subtle blue
        public static readonly Color DarkCardBackground = Color.FromArgb(26, 28, 32);      // #1A1C20 - Elevated card surface
        public static readonly Color DarkGridBackground = Color.FromArgb(20, 22, 26);      // #14161A - Enhanced data table background
        public static readonly Color DarkButtonBackground = Color.FromArgb(45, 50, 65);    // #2D3241 - Modern interactive surface
        public static readonly Color DarkButtonHoverBackground = Color.FromArgb(60, 70, 90); // #3C465A - Enhanced hover state
        public static readonly Color DarkButtonPressedBackground = Color.FromArgb(75, 85, 105); // Responsive pressed state
        public static readonly Color DarkButtonDisabledBackground = Color.FromArgb(22, 24, 28); // Subtle disabled state
        public static readonly Color DarkCardText = Color.FromArgb(248, 250, 255);         // #F8FAFF - Brighter for better contrast
        public static readonly Color DarkSecondaryText = Color.FromArgb(200, 205, 210);    // #C8CDD2 - Brighter for better contrast
        public static readonly Color DarkHeadlineText = Color.FromArgb(255, 255, 255);     // #FFFFFF - Pure white for maximum contrast
        public static readonly Color DarkTextBoxBackground = Color.FromArgb(38, 42, 50);   // #262A32 - Enhanced input background
        public static readonly Color DarkButtonText = Color.FromArgb(255, 255, 255);       // #FFFFFF - Pure white for maximum contrast
        public static readonly Color DarkButtonDisabledText = Color.FromArgb(150, 155, 160); // #969B9F - Brighter disabled text
    }

    public class ColorScheme
    {
        public Color Primary { get; set; } = Color.FromArgb(60, 120, 200);
        public Color Secondary { get; set; } = Color.FromArgb(80, 160, 220);
        public Color Background { get; set; } = Color.White;
        public Color Text { get; set; } = Color.Black;
        public Color Accent { get; set; } = Color.FromArgb(240, 100, 50);
        public Color Disabled { get; set; } = Color.Gray;
        public Color Error { get; set; } = Color.Red;
        public Color Success { get; set; } = Color.Green;
        public Color Warning { get; set; } = Color.Orange;
        public static readonly Color DarkButtonDisabledText = Color.DarkGray;
    }

    /// <summary>
    /// Represents a UI theme with color and font definitions
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
        /// Button pressed background color
        /// </summary>
        public abstract Color ButtonPressedBackground { get; }

        /// <summary>
        /// Button disabled background color
        /// </summary>
        public abstract Color ButtonDisabledBackground { get; }

        /// <summary>
        /// Button text color
        /// </summary>
        public abstract Color ButtonText { get; }

        /// <summary>
        /// Button disabled text color
        /// </summary>
        public abstract Color ButtonDisabledText { get; }

        /// <summary>
        /// Card text color
        /// </summary>
        public abstract Color CardText { get; }        /// <summary>
                                                       /// Headline text color
                                                       /// </summary>
        public abstract Color HeadlineText { get; }

        /// <summary>
        /// Secondary text color for less important information
        /// </summary>
        public abstract Color SecondaryText { get; }

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
            Color.FromArgb(128, CardText.R, CardText.G, CardText.B);        /// <summary>
                                                                            /// Gets shadow text color for creating text depth effects in dark UI
                                                                            /// </summary>
        public virtual Color ShadowTextColor =>
            Name == "Dark"
                ? Color.FromArgb(40, 255, 255, 255) // Light shadow for dark backgrounds
                : Color.FromArgb(80, 0, 0, 0);       // Dark shadow for light backgrounds

        /// <summary>
        /// Gets glass effect background color for "floating on glass" appearance
        /// </summary>
        public virtual Color GlassEffectBackground => Color.Transparent;

        /// <summary>
        /// Gets glass effect opacity level (0.0 to 1.0) for glassmorphism effects
        /// </summary>
        public virtual double GlassEffectOpacity => Name == "Dark" ? 0.15 : 0.1;

        /// <summary>
        /// Gets enhanced glass effect opacity level following NN/g guidelines for better accessibility
        /// Increased blur for better content separation and readability
        /// </summary>
        public virtual double EnhancedGlassEffectOpacity => 0.35;

        /// <summary>
        /// Gets accessibility-compliant background blur intensity following NN/g "more blur is better" principle
        /// Higher values ensure text readability over complex backgrounds
        /// </summary>
        public virtual int AccessibilityBlurIntensity => Name == "Dark" ? 15 : 12;        /// <summary>
                                                                                          /// Gets WCAG AA compliant text color for glassmorphic surfaces
                                                                                          /// Ensures 4.5:1 contrast ratio minimum as per NN/g guidelines
                                                                                          /// </summary>
        public virtual Color GlassmorphicTextColor =>
        // Inspired by NN/g and Netguru: Use pure white for dark glass, pure black for light glass for maximum readability and accessibility
        Name == "Dark"
            ? Color.White // #FFFFFF for maximum contrast
            : Color.Black; // #000000 for maximum contrast

        /// <summary>
        /// Gets the button hover text color
        /// </summary>
        public Color ButtonHoverText => this.GetButtonHoverText();

        /// <summary>
        /// Gets the glassmorphic secondary text color
        /// </summary>
        public Color GlassmorphicSecondaryTextColor => this.GetGlassmorphicSecondaryTextColor();

        /// <summary>
        /// Applies glassmorphic text color to a control and its children
        /// </summary>
        public virtual void ApplyGlassmorphicTextColor(Control control)
        {
            if (control == null) return;

            // Apply to this control if it's a text-displaying control
            if (control is Label label)
            {
                label.ForeColor = GlassmorphicTextColor;
            }
            else if (control is Button button)
            {
                button.ForeColor = GlassmorphicTextColor;
            }
            else if (control is LinkLabel linkLabel)
            {
                linkLabel.LinkColor = GlassmorphicTextColor;
                linkLabel.ActiveLinkColor = this.EnsureAccessibleTextColor(GlassmorphicTextColor, CardBackground);
            }

            // Apply to children recursively
            foreach (Control child in control.Controls)
            {
                ApplyGlassmorphicTextColor(child);
            }
        }

        /// <summary>
        /// Ensures that the contrast between two colors meets accessibility standards
        /// </summary>
        public bool EnsureAccessibleContrast(Color foreground, Color background)
        {
            double contrast = this.CalculateContrastRatio(foreground, background);
            return contrast >= 4.5;
        }        /// <summary>
                 /// Gets high-contrast background for glassmorphic elements when accessibility mode is enabled
                 /// Provides fallback solid colors as per NN/g accessibility recommendations
                 /// </summary>
        public virtual Color AccessibilityGlassBackground =>
            Name == "Dark"
                ? Color.FromArgb(240, CardBackground.R, CardBackground.G, CardBackground.B)  // Near-opaque dark
                : Color.FromArgb(240, CardBackground.R, CardBackground.G, CardBackground.B); // Near-opaque light

        /// <summary>
        /// Determines if reduced transparency mode should be used for better accessibility
        /// Can be overridden by user settings or system accessibility preferences
        /// </summary>
        public virtual bool UseReducedTransparency { get; set; }

        /// <summary>
        /// Gets glow color for glassmorphism edge effects and highlights
        /// </summary>
        public virtual Color GlowColor =>
            Name == "Dark"
                ? Color.FromArgb(80, 100, 150, 255)  // Soft blue glow for dark theme
                : Color.FromArgb(60, 120, 120, 120); // Soft gray glow for light theme

        /// <summary>
        /// Gets blur background color for frosted glass effect
        /// </summary>
        public virtual Color BlurBackgroundColor =>
            Name == "Dark"
                ? Color.FromArgb(25, 30, 30, 35)     // Dark translucent backdrop
                : Color.FromArgb(25, 240, 240, 250); // Light translucent backdrop

        /// <summary>
        /// Gets accent gradient colors for glassmorphism effects
        /// </summary>
        public virtual (Color Start, Color End) AccentGradient =>
            Name == "Dark"
                ? (Color.FromArgb(60, 80, 120, 200), Color.FromArgb(30, 50, 80, 150))  // Blue accent gradient
                : (Color.FromArgb(40, 100, 100, 140), Color.FromArgb(20, 80, 80, 120)); // Subtle gray gradient

        /// <summary>
        /// Gets enhanced button minimum size for better text display
        /// </summary>
        public virtual Size EnhancedButtonMinSize => new Size(200, 60);

        /// <summary>
        /// Gets enhanced button padding for better text spacing
        /// </summary>
        public virtual Padding EnhancedButtonPadding => new Padding(16, 12, 16, 12);

        /// <summary>
        /// Gets enhanced button margins for better spacing
        /// </summary>
        public virtual Padding EnhancedButtonMargin => new Padding(2, 6, 2, 6);

        /// <summary>
        /// Gets the border color for subtle UI element separation
        /// </summary>
        public virtual Color SubtleBorderColor =>
            Name == "Dark"
                ? Color.FromArgb(60, Color.Black)   // Subtle dark border for dark theme
                : Color.FromArgb(60, Color.Gray);   // Subtle gray border for light theme

        /// <summary>
        /// Gets gradient overlay color for button effects
        /// </summary>
        public virtual Color GradientOverlayColor =>
            Name == "Dark"
                ? Color.FromArgb(15, Color.White)   // Light overlay for dark theme
                : Color.FromArgb(15, Color.Black);  // Dark overlay for light theme

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
        /// Calculates contrast ratio between two colors (WCAG 2.1 formula)
        /// </summary>
        /// <param name="color1">First color</param>
        /// <param name="color2">Second color</param>
        /// <returns>Contrast ratio (1:1 to 21:1)</returns>
        public static double CalculateContrastRatio(Color color1, Color color2)
        {
            double luminance1 = CalculateRelativeLuminance(color1);
            double luminance2 = CalculateRelativeLuminance(color2);

            // Ensure the lighter color is always in the numerator
            if (luminance1 <= luminance2)
            {
                (luminance1, luminance2) = (luminance2, luminance1);
            }

            return (luminance1 + 0.05) / (luminance2 + 0.05);
        }

        /// <summary>
        /// Calculates relative luminance of a color (WCAG 2.1 formula)
        /// </summary>
        /// <param name="color">Color to calculate</param>
        /// <returns>Relative luminance (0 to 1)</returns>
        private static double CalculateRelativeLuminance(Color color)
        {
            double r = ConvertColorChannel(color.R / 255.0);
            double g = ConvertColorChannel(color.G / 255.0);
            double b = ConvertColorChannel(color.B / 255.0);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        /// <summary>
        /// Converts color channel value according to WCAG 2.1 formula
        /// </summary>
        private static double ConvertColorChannel(double colorChannel)
        {
            return colorChannel <= 0.03928
                ? colorChannel / 12.92
                : Math.Pow((colorChannel + 0.055) / 1.055, 2.4);
        }
        /// <summary>
        /// Ensures WCAG 2.1 AA compliant contrast by adjusting colors if needed
        /// </summary>
        /// <param name="textColor">Text color</param>
        /// <param name="backgroundColor">Background color</param>
        /// <param name="minRatio">Minimum required contrast ratio (4.5:1 for normal text, 3:1 for large text)</param>
        /// <returns>Adjusted text color to meet contrast requirements</returns>
        public static Color EnsureAccessibleContrast(Color textColor, Color backgroundColor, double minRatio = 4.5)
        {
            // Calculate current contrast ratio
            double ratio = CalculateContrastRatio(textColor, backgroundColor);

            // If already compliant, return original color
            if (ratio >= minRatio)
            {
                return textColor;
            }

            // Determine if we're dealing with light or dark background
            bool isDarkBackground = CalculateRelativeLuminance(backgroundColor) < 0.5;

            // Adjust the text color to meet contrast requirements
            Color adjustedColor = textColor;
            int step = isDarkBackground ? 5 : -5; // Lighten for dark backgrounds, darken for light

            while (ratio < minRatio)
            {
                int r = Math.Clamp(adjustedColor.R + step, 0, 255);
                int g = Math.Clamp(adjustedColor.G + step, 0, 255);
                int b = Math.Clamp(adjustedColor.B + step, 0, 255);

                adjustedColor = Color.FromArgb(adjustedColor.A, r, g, b);
                ratio = CalculateContrastRatio(adjustedColor, backgroundColor);

                // Break if we've gone as far as possible but still can't meet requirements
                if ((isDarkBackground && r == 255 && g == 255 && b == 255) ||
                    (!isDarkBackground && r == 0 && g == 0 && b == 0))
                {
                    break;
                }
            }

            return adjustedColor;
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

            // Grid background and basic styling
            grid.BackgroundColor = GridBackground;
            grid.ForeColor = CardText;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            // Header styling with crystal-like theme colors
            grid.ColumnHeadersDefaultCellStyle.BackColor = HeadlineBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = HeadlineText;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersHeight = 35;

            // Cell styling for consistent dark theme appearance
            grid.DefaultCellStyle.BackColor = CardBackground;
            grid.DefaultCellStyle.ForeColor = CardText;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            grid.DefaultCellStyle.SelectionBackColor = ButtonBackground;
            grid.DefaultCellStyle.SelectionForeColor = HeadlineText;
            grid.DefaultCellStyle.Padding = new Padding(6);
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Alternating row colors for better readability in dark theme
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(
                Math.Min(255, CardBackground.R + 8),
                Math.Min(255, CardBackground.G + 8),
                Math.Min(255, CardBackground.B + 8)
            );
            grid.AlternatingRowsDefaultCellStyle.ForeColor = CardText;            // Row and grid appearance
            grid.RowTemplate.Height = 32;
            grid.RowHeadersVisible = false;
            grid.AllowUserToResizeRows = false;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            // Use a solid color for GridColor since transparent colors are not allowed
            // Create a darker version of the card background for subtle grid lines
            var gridLineColor = Color.FromArgb(
                Math.Max(0, CardBackground.R - 30),
                Math.Max(0, CardBackground.G - 30),
                Math.Max(0, CardBackground.B - 30));
            grid.GridColor = gridLineColor;

            // Selection and behavior
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
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
        /// Styles a button with theme colors and hover effects
        /// </summary>
        /// <param name="button">The Button to style</param>
        public virtual void StyleButton(Button button)
        {
            ArgumentNullException.ThrowIfNull(button);

            button.BackColor = ButtonBackground;
            button.ForeColor = ButtonText;
            button.Font = ButtonFont;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ButtonHoverBackground;
            button.FlatAppearance.MouseDownBackColor = ButtonPressedBackground;
            button.Cursor = Cursors.Hand;

            // Handle enabled state
            button.EnabledChanged += (s, e) =>
            {
                if (button.Enabled)
                {
                    button.BackColor = ButtonBackground;
                    button.ForeColor = ButtonText;
                }
                else
                {
                    button.BackColor = ButtonDisabledBackground;
                    button.ForeColor = ButtonDisabledText;
                }
            };
        }

        /// <summary>
        /// Styles a button with enhanced modern appearance following dark UI principles
        /// </summary>
        /// <param name="button">The Button to style</param>
        public virtual void StyleEnhancedButton(Button button)
        {
            ArgumentNullException.ThrowIfNull(button);
            button.BackColor = ButtonBackground;
            button.ForeColor = ButtonText;
            button.Font = ButtonFont;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.MinimumSize = EnhancedButtonMinSize;
            button.Padding = EnhancedButtonPadding;
            button.Margin = EnhancedButtonMargin;
            button.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Styles a side panel button with specific styling
        /// </summary>
        /// <param name="button">The Button to style as a side panel button</param>
        public virtual void StyleSidePanelButton(Button button)
        {
            ArgumentNullException.ThrowIfNull(button);

            StyleButton(button);
            button.Font = MediumButtonFont;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(10, 0, 0, 0);
            button.Height = 40;
        }        /// <summary>
                 /// Styles a panel with enhanced glassmorphism appearance for modern UI
                 /// </summary>
                 /// <param name="panel">The Panel to style</param>
        public virtual void StyleGlassPanel(Panel panel)
        {
            ArgumentNullException.ThrowIfNull(panel);
            panel.BackColor = Color.Transparent;

            // Add glassmorphism paint effects
            panel.Paint += (s, e) =>
            {
                if (s is Panel p)
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Create frosted glass effect background
                    using (var glassBrush = new SolidBrush(BlurBackgroundColor))
                    {
                        var rect = new Rectangle(0, 0, p.Width, p.Height);
                        e.Graphics.FillRectangle(glassBrush, rect);
                    }

                    // Add subtle glow effect around edges
                    var glowRect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                    using (var glowPen = new Pen(GlowColor, 1))
                    {
                        e.Graphics.DrawRectangle(glowPen, glowRect);
                    }

                    // Add inner highlight for glass effect
                    var highlightRect = new Rectangle(1, 1, p.Width - 3, p.Height - 3);
                    var highlightColor = Name == "Dark"
                        ? Color.FromArgb(20, 255, 255, 255)
                        : Color.FromArgb(15, 0, 0, 0);
                    using (var highlightPen = new Pen(highlightColor, 1))
                    {
                        e.Graphics.DrawRectangle(highlightPen, highlightRect);
                    }

                    // Add diagonal shimmer effect for glassmorphism
                    var shimmerGradient = AccentGradient;
                    using (var shimmerBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Point(0, 0),
                        new Point(p.Width, p.Height),
                        shimmerGradient.Start,
                        shimmerGradient.End))
                    {
                        e.Graphics.FillRectangle(shimmerBrush, 2, 2, p.Width - 4, p.Height - 4);
                    }
                }
            };
        }

        /// <summary>
        /// Creates a shadow label for text depth effects
        /// </summary>
        /// <param name="originalLabel">The original label to create shadow for</param>
        /// <param name="offsetX">Shadow offset X (default: 1)</param>
        /// <param name="offsetY">Shadow offset Y (default: 1)</param>
        /// <returns>A shadow label positioned behind the original</returns>
        public virtual Label CreateShadowLabel(Label originalLabel, int offsetX = 1, int offsetY = 1)
        {
            ArgumentNullException.ThrowIfNull(originalLabel);

            return new Label
            {
                Text = originalLabel.Text,
                Font = originalLabel.Font,
                ForeColor = ShadowTextColor,
                Location = new Point(originalLabel.Location.X + offsetX, originalLabel.Location.Y + offsetY),
                Size = originalLabel.Size,
                BackColor = Color.Transparent
            };
        }        /// <summary>
                 /// Applies dark UI design principles to a card panel with glassmorphism effects
                 /// </summary>
                 /// <param name="card">The card panel to enhance</param>
        public virtual void StyleModernCard(Panel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            card.BackColor = CardBackground;

            // Add paint event for glassmorphism effects
            card.Paint += (s, e) =>
            {
                if (s is Panel p)
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Draw glassmorphism backdrop with blur effect simulation
                    using (var backdropBrush = new SolidBrush(BlurBackgroundColor))
                    {
                        e.Graphics.FillRectangle(backdropBrush, 0, 0, p.Width, p.Height);
                    }

                    // Draw subtle glow border for glassmorphism
                    using (var glowPen = new Pen(GlowColor, 2))
                    {
                        e.Graphics.DrawRectangle(glowPen, 1, 1, p.Width - 3, p.Height - 3);
                    }

                    // Draw inner subtle border
                    using (var borderPen = new Pen(SubtleBorderColor, 1))
                    {
                        e.Graphics.DrawRectangle(borderPen, 0, 0, p.Width - 1, p.Height - 1);
                    }

                    // Add glassmorphism gradient overlay
                    var gradient = AccentGradient;
                    using (var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, p.Width, p.Height),
                        gradient.Start,
                        gradient.End,
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillRectangle(gradientBrush, 2, 2, p.Width - 4, p.Height - 4);
                    }
                }
            };
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
                return button.BackColor == ButtonBackground &&
                       button.FlatAppearance.MouseOverBackColor == ButtonHoverBackground;
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

        /// <summary>
        /// Gets elevation-based background colors for dark themes
        /// Following Material Design elevation system for depth perception
        /// </summary>
        public virtual Color GetElevatedBackground(int elevation = 0)
        {
            if (Name != "Dark") return CardBackground;

            // Base dark color (#121212) with increasing lightness for elevation
            var baseColor = Color.FromArgb(18, 18, 18);
            var elevationStep = Math.Min(elevation * 8, 64); // Max 64 steps

            return Color.FromArgb(
                Math.Min(255, baseColor.R + elevationStep),
                Math.Min(255, baseColor.G + elevationStep),
                Math.Min(255, baseColor.B + elevationStep)
            );
        }

        /// <summary>
        /// Gets appropriate text color for elevated surfaces
        /// Ensures proper contrast ratios on different elevation levels
        /// </summary>
        public virtual Color GetElevatedTextColor(int elevation = 0)
        {
            if (Name != "Dark") return CardText;

            // For very light elevated surfaces, use darker text
            if (elevation > 6)
                return Color.FromArgb(33, 33, 33);

            return CardText;
        }

        /// <summary>
        /// Primary accent color for visual hierarchy and interactive elements
        /// </summary>
        public virtual Color PrimaryAccent => Color.FromArgb(60, 120, 215); // Blue

        /// <summary>
        /// Secondary accent color for less important interactive elements
        /// </summary>
        public virtual Color SecondaryAccent => Color.FromArgb(0, 183, 195); // Teal

        /// <summary>
        /// Tertiary accent color for decorative elements
        /// </summary>
        public virtual Color TertiaryAccent => Color.FromArgb(104, 33, 122); // Purple

        /// <summary>
        /// Primary text color for normal content
        /// </summary>
        public virtual Color PrimaryText => CardText;
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
        public override Color ButtonPressedBackground => ThemeColors.LightButtonPressedBackground;
        public override Color ButtonDisabledBackground => ThemeColors.LightButtonDisabledBackground;
        public override Color ButtonText => ThemeColors.LightButtonText;
        public override Color ButtonDisabledText => ThemeColors.LightButtonDisabledText;
        public override Color CardText => ThemeColors.LightCardText;
        public override Color SecondaryText => ThemeColors.LightSecondaryText;
        public override Color HeadlineText => ThemeColors.LightHeadlineText;
        public override Color TextBoxBackground => ThemeColors.LightTextBoxBackground;
    }    /// <summary>
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
        public override Color ButtonPressedBackground => ThemeColors.DarkButtonPressedBackground;
        public override Color ButtonDisabledBackground => ThemeColors.DarkButtonDisabledBackground;
        public override Color ButtonText => ThemeColors.DarkButtonText;
        public override Color ButtonDisabledText => ThemeColors.DarkButtonDisabledText;
        public override Color CardText => ThemeColors.DarkCardText;
        public override Color SecondaryText => ThemeColors.DarkSecondaryText;
        public override Color HeadlineText => ThemeColors.DarkHeadlineText; public override Color TextBoxBackground => ThemeColors.DarkTextBoxBackground;
    }
}
// NOTE: ThemeManager implementation has been moved to ThemeManager.cs to avoid duplication.
