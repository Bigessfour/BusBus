#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
#pragma warning disable CS0169 // The field is never used
#pragma warning disable CA1416 // Platform compatibility (Windows-only)
#pragma warning disable CS1998 // Async method lacks 'await' operators
#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BusBus.UI
{
    public enum ThemeType
    {
        Light,
        Dark
    }

    /// <summary>
    /// Manages application themes and provides centralized theme switching functionality
    /// </summary>
    public static class ThemeManager
    {
        private static readonly Dictionary<string, Func<Theme>> _themeRegistry = new()
        {
            ["Light"] = () => new LightTheme(),
            ["Dark"] = () => new DarkTheme()
        };
        private static Theme _currentTheme = new DarkTheme();
        private static ThemeType currentTheme = ThemeType.Light;

        /// <summary>
        /// Event fired when the theme changes
        /// </summary>
        public static event EventHandler<EventArgs> ThemeChanged;

        /// <summary>
        /// Gets or sets the current active theme
        /// </summary>
        public static Theme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme?.Dispose();
                    _currentTheme = value ?? throw new ArgumentNullException(nameof(value));
                    OnThemeChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the current theme type
        /// </summary>
        public static ThemeType CurrentThemeType => currentTheme;

        /// <summary>
        /// Gets the primary color for the current theme
        /// </summary>
        public static Color PrimaryColor => currentTheme == ThemeType.Light
            ? Color.FromArgb(0, 122, 204)
            : Color.FromArgb(30, 30, 30);

        /// <summary>
        /// Gets the background color for the current theme
        /// </summary>
        public static Color BackgroundColor => currentTheme == ThemeType.Light
            ? Color.White
            : Color.FromArgb(45, 45, 48);

        /// <summary>
        /// Gets the text color for the current theme
        /// </summary>
        public static Color TextColor => currentTheme == ThemeType.Light
            ? Color.Black
            : Color.White;

        /// <summary>
        /// Gets the secondary background color for the current theme
        /// </summary>
        public static Color SecondaryBackgroundColor => currentTheme == ThemeType.Light
            ? Color.FromArgb(240, 240, 240)
            : Color.FromArgb(60, 60, 60);

        /// <summary>
        /// Raises the ThemeChanged event
        /// </summary>
        private static void OnThemeChanged(EventArgs e)
        {
            ThemeChanged?.Invoke(null, e);
        }

        /// <summary>
        /// Switches to a theme by name
        /// </summary>
        /// <param name="themeName">Name of the theme to switch to</param>
        public static void SwitchTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
                throw new ArgumentException("Theme name cannot be null or empty", nameof(themeName));

            if (_themeRegistry.TryGetValue(themeName, out var themeFactory))
            {
                CurrentTheme = themeFactory();
                currentTheme = themeName == "Light" ? ThemeType.Light : ThemeType.Dark;
            }
            else
            {
                throw new ArgumentException($"Theme '{themeName}' is not registered", nameof(themeName));
            }
        }

        /// <summary>
        /// Sets the theme by name (alias for SwitchTheme)
        /// </summary>
        /// <param name="themeName">Name of the theme to set</param>
        public static void SetTheme(string themeName)
        {
            SwitchTheme(themeName);
        }

        /// <summary>
        /// Registers a new theme with the theme manager
        /// </summary>
        /// <param name="name">Name of the theme</param>
        /// <param name="themeFactory">Factory function to create the theme</param>
        public static void RegisterTheme(string name, Func<Theme> themeFactory)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(themeFactory);

            _themeRegistry[name] = themeFactory;
        }        /// <summary>
                 /// Gets all available theme names
                 /// </summary>
                 /// <returns>Collection of theme names</returns>
        public static IEnumerable<string> AvailableThemes => _themeRegistry.Keys;

        /// <summary>
        /// Applies the current theme to a form and all its child controls
        /// </summary>
        /// <param name="form">The form to apply the theme to</param>
        public static void RefreshTheme(Form form)
        {
            ArgumentNullException.ThrowIfNull(form);
            ApplyThemeToControl(form);
        }

        /// <summary>
        /// Applies the current theme to a specific control
        /// </summary>
        /// <param name="control">The control to apply the theme to</param>
        public static void RefreshControl(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            ApplyThemeToControl(control);
        }

        /// <summary>
        /// Recursively applies the current theme to a control and all its children
        /// </summary>
        /// <param name="control">The control to apply the theme to</param>
        public static void ApplyThemeToControl(Control control)
        {
            if (control == null) return;

            try
            {
                // Apply theme based on control type and tags
                switch (control)
                {
                    case Form form:
                        form.BackColor = CurrentTheme.MainBackground;
                        break;

                    case Panel panel when panel.Tag?.ToString() == "HeadlinePanel":
                        panel.BackColor = CurrentTheme.HeadlineBackground;
                        break;

                    case Panel panel when panel.Tag?.ToString() == "SidePanel":
                        panel.BackColor = CurrentTheme.SidePanelBackground;
                        break;
                    case Panel panel when panel.Tag?.ToString()?.StartsWith("Elevation", StringComparison.Ordinal) == true:
                        if (int.TryParse(panel.Tag.ToString()!.Replace("Elevation", "", StringComparison.Ordinal), out int elevation))
                        {
                            panel.BackColor = CurrentTheme.GetElevatedBackground(elevation);
                        }
                        else
                        {
                            panel.BackColor = CurrentTheme.CardBackground;
                        }
                        break;

                    case Panel panel:
                        panel.BackColor = CurrentTheme.CardBackground;
                        break;

                    case Button button:
                        button.BackColor = CurrentTheme.ButtonBackground;
                        button.ForeColor = CurrentTheme.ButtonText;
                        button.Font = CurrentTheme.ButtonFont;
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderSize = 0;
                        break;

                    case Label label:
                        label.ForeColor = CurrentTheme.CardText;
                        label.Font = CurrentTheme.CardFont;
                        break;

                    case TextBox textBox:
                        textBox.BackColor = CurrentTheme.TextBoxBackground;
                        textBox.ForeColor = CurrentTheme.CardText;
                        textBox.Font = CurrentTheme.TextBoxFont;
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        break;

                    case ComboBox comboBox:
                        comboBox.BackColor = CurrentTheme.TextBoxBackground;
                        comboBox.ForeColor = CurrentTheme.CardText;
                        comboBox.Font = CurrentTheme.TextBoxFont;
                        comboBox.FlatStyle = FlatStyle.Flat;
                        break;

                    case NumericUpDown numericUpDown:
                        numericUpDown.BackColor = CurrentTheme.TextBoxBackground;
                        numericUpDown.ForeColor = CurrentTheme.CardText;
                        numericUpDown.Font = CurrentTheme.TextBoxFont;
                        break;

                    case DataGridView grid:
                        grid.BackgroundColor = CurrentTheme.GridBackground;
                        grid.ForeColor = CurrentTheme.CardText;
                        grid.BorderStyle = BorderStyle.None;
                        grid.ColumnHeadersDefaultCellStyle.BackColor = CurrentTheme.HeadlineBackground;
                        grid.ColumnHeadersDefaultCellStyle.ForeColor = CurrentTheme.HeadlineText;
                        grid.DefaultCellStyle.BackColor = CurrentTheme.CardBackground;
                        grid.DefaultCellStyle.ForeColor = CurrentTheme.CardText;
                        grid.EnableHeadersVisualStyles = false;
                        break;
                }

                // Recursively apply theme to all child controls
                foreach (Control child in control.Controls)
                {
                    ApplyThemeToControl(child);
                }
            }
            catch (ObjectDisposedException)
            {
                // Control was disposed while applying theme
                // This is normal during application shutdown
            }
        }

        /// <summary>
        /// Applies a theme to a control and all its child controls
        /// </summary>
        /// <param name="control">The control to apply the theme to</param>
        /// <param name="theme">The theme to apply</param>
        public static void ApplyTheme(Control control, Theme theme)
        {
            if (control == null || theme == null) return;

            control.BackColor = theme.MainBackground;
            control.ForeColor = theme.CardText;

            foreach (Control child in control.Controls)
            {
                ApplyTheme(child, theme);
            }
        }

        /// <summary>
        /// Determines if a control has been styled according to the current theme
        /// </summary>
        /// <param name="control">The control to check</param>
        /// <returns>True if the control matches the current theme styling</returns>
        private static bool IsControlStyled(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            return control switch
            {
                Form form => form.BackColor == CurrentTheme.MainBackground,
                Panel panel when panel.Tag?.ToString() == "HeadlinePanel" =>
                    panel.BackColor == CurrentTheme.HeadlineBackground,
                Panel panel when panel.Tag?.ToString() == "SidePanel" =>
                    panel.BackColor == CurrentTheme.SidePanelBackground,
                Panel panel => panel.BackColor == CurrentTheme.CardBackground,
                Button button => button.BackColor == CurrentTheme.ButtonBackground,
                TextBox textBox => textBox.BackColor == CurrentTheme.TextBoxBackground &&
                                   textBox.ForeColor == CurrentTheme.CardText,
                ComboBox comboBox => comboBox.BackColor == CurrentTheme.TextBoxBackground &&
                                     comboBox.ForeColor == CurrentTheme.CardText,
                _ => false
            };
        }
    }
}
