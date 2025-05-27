#nullable enable
using System;
using System.Windows.Forms;
using BusBus.UI;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Base class for controls that support automatic theme management.
    /// Provides centralized theme application and automatic theme change handling.
    /// </summary>
    public abstract class ThemeableControl : UserControl, IDisplayable
    {
        private bool _themeSubscribed = false;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_themeSubscribed)
            {
                ApplyTheme();
                // Subscribe to theme changes for automatic updates
                ThemeManager.ThemeChanged += OnThemeChanged;
                _themeSubscribed = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _themeSubscribed)
            {
                // Unsubscribe from theme changes to prevent memory leaks
                ThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Applies the current theme to this control and all its children.
        /// Called automatically on load and when theme changes.
        /// </summary>
        public virtual void RefreshTheme()
        {
            ApplyTheme();
        }

        /// <summary>
        /// Event handler for theme changes. Automatically refreshes the theme.
        /// </summary>
        private void OnThemeChanged(object? sender, EventArgs e)
        {
            if (!IsDisposed && IsHandleCreated)
            {
                try
                {
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(RefreshTheme));
                    }
                    else
                    {
                        RefreshTheme();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Control was disposed while trying to update theme - ignore
                }
            }
        }

        /// <summary>
        /// Applies the current theme to this control and all its children.
        /// Override this method to customize theme application for specific controls.
        /// </summary>
        protected virtual void ApplyTheme()
        {
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
        }        /// <summary>
        /// Recursively applies theme to a control and all its children.
        /// This is the centralized theme application logic.
        /// </summary>
        protected static void ApplyThemeToControl(Control control)
        {
            if (control == null)
                return;

            try
            {
                // Apply theme based on control type
                switch (control)
                {
                    case TextBox textBox:
                        textBox.BackColor = ThemeManager.CurrentTheme.TextBoxBackground;
                        textBox.ForeColor = ThemeManager.CurrentTheme.CardText;
                        break;
                    case ComboBox comboBox:
                        comboBox.BackColor = ThemeManager.CurrentTheme.TextBoxBackground;
                        comboBox.ForeColor = ThemeManager.CurrentTheme.CardText;
                        break;
                    case NumericUpDown numericUpDown:
                        numericUpDown.BackColor = ThemeManager.CurrentTheme.TextBoxBackground;
                        numericUpDown.ForeColor = ThemeManager.CurrentTheme.CardText;
                        break;
                    default:
                        // Apply default theme to the control itself
                        control.BackColor = ThemeManager.CurrentTheme.CardBackground;
                        control.ForeColor = ThemeManager.CurrentTheme.CardText;
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
        }/// <summary>
        /// Renders the control into the specified container.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <param name="container">The container control to render into</param>
        public abstract void Render(Control container);
    }
}
