using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BusBus.UI
{
    /// <summary>
    /// Manages application themes and theme switching
    /// </summary>
    public static class ThemeManager
    {
        private static readonly Dictionary<string, Func<Theme>> _themes = new();
        private static Theme _currentTheme = new LightTheme();

        /// <summary>
        /// Event fired when theme changes
        /// </summary>
        public static event EventHandler? ThemeChanged;

        static ThemeManager()
        {
            // Register built-in themes
            RegisterTheme("Light", () => new LightTheme());
            RegisterTheme("Dark", () => new DarkTheme());
        }

        /// <summary>
        /// Gets the current active theme
        /// </summary>
        public static Theme CurrentTheme => _currentTheme;

        /// <summary>
        /// Registers a new theme
        /// </summary>
        /// <param name="name">Theme name</param>
        /// <param name="themeFactory">Factory function to create theme instance</param>
        public static void RegisterTheme(string name, Func<Theme> themeFactory)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(themeFactory);
            
            _themes[name] = themeFactory;
        }

        /// <summary>
        /// Switches to the specified theme
        /// </summary>
        /// <param name="themeName">Name of the theme to switch to</param>
        public static void SwitchTheme(string themeName)
        {
            ArgumentNullException.ThrowIfNull(themeName);

            if (!_themes.TryGetValue(themeName, out var themeFactory))
            {
                throw new ArgumentException($"Theme '{themeName}' not found. Available themes: {string.Join(", ", _themes.Keys)}");
            }

            var oldTheme = _currentTheme;
            _currentTheme = themeFactory();
            oldTheme?.Dispose();

            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the theme (alias for SwitchTheme for compatibility)
        /// </summary>
        /// <param name="themeName">Name of the theme to set</param>
        public static void SetTheme(string themeName) => SwitchTheme(themeName);

        /// <summary>
        /// Gets available theme names
        /// </summary>
        /// <returns>Array of theme names</returns>
        public static string[] GetAvailableThemes()
        {
            var themes = new string[_themes.Keys.Count];
            _themes.Keys.CopyTo(themes, 0);
            return themes;
        }

        /// <summary>
        /// Refreshes theme for the specified control and its children
        /// </summary>
        /// <param name="control">Control to refresh</param>
        public static void RefreshTheme(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            ApplyThemeToControl(control);
        }

        /// <summary>
        /// Refreshes theme for a specific control based on its properties
        /// </summary>
        /// <param name="control">Control to refresh</param>
        public static void RefreshControl(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            // Apply theme based on control tag or type
            if (control.Tag?.ToString() == "HeadlinePanel")
            {
                control.BackColor = CurrentTheme.HeadlineBackground;
                control.ForeColor = CurrentTheme.HeadlineText;
            }
            else
            {
                ApplyThemeToControl(control);
            }
        }

        /// <summary>
        /// Applies current theme to a control and its children
        /// </summary>
        /// <param name="control">Control to apply theme to</param>
        private static void ApplyThemeToControl(Control control)
        {
            switch (control)
            {
                case Form form:
                    form.BackColor = CurrentTheme.MainBackground;
                    form.ForeColor = CurrentTheme.CardText;
                    break;

                case Panel panel:
                    panel.BackColor = CurrentTheme.CardBackground;
                    panel.ForeColor = CurrentTheme.CardText;
                    break;

                case DataGridView grid:
                    grid.BackgroundColor = CurrentTheme.GridBackground;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = CurrentTheme.HeadlineBackground;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = CurrentTheme.HeadlineText;
                    grid.DefaultCellStyle.BackColor = CurrentTheme.CardBackground;
                    grid.DefaultCellStyle.ForeColor = CurrentTheme.CardText;
                    grid.DefaultCellStyle.SelectionBackColor = CurrentTheme.ButtonBackground;
                    grid.DefaultCellStyle.SelectionForeColor = Color.White;
                    break;

                case Button button:
                    button.BackColor = CurrentTheme.ButtonBackground;
                    button.ForeColor = Color.White;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 0;
                    break;

                case Label label:
                    label.ForeColor = CurrentTheme.CardText;
                    if (label.Parent != null)
                    {
                        label.BackColor = Color.Transparent;
                    }
                    break;

                case TextBox textBox:
                    textBox.BackColor = CurrentTheme.TextBoxBackground;
                    textBox.ForeColor = CurrentTheme.CardText;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ComboBox comboBox:
                    comboBox.BackColor = CurrentTheme.TextBoxBackground;
                    comboBox.ForeColor = CurrentTheme.CardText;
                    break;

                case NumericUpDown numericUpDown:
                    numericUpDown.BackColor = CurrentTheme.TextBoxBackground;
                    numericUpDown.ForeColor = CurrentTheme.CardText;
                    break;

                default:
                    // For unknown controls, apply basic theming
                    control.BackColor = CurrentTheme.CardBackground;
                    control.ForeColor = CurrentTheme.CardText;
                    break;
            }

            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child);
            }        }
    }
}
