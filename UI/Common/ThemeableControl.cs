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

            // Recursively apply theme to children
            ApplyThemeToChildControls(this);
        }

        /// <summary>
        /// Recursively applies theme to a control and all its children.
        /// This is the centralized theme application logic.
        /// </summary>
        protected static void ApplyThemeToChildControls(Control parentControl)
        {
            if (parentControl == null)
                return;

            foreach (Control control in parentControl.Controls)
            {
                ApplyThemeToControl(control); // Apply to direct child
                if (control.Controls.Count > 0)
                {
                    ApplyThemeToChildControls(control); // Recurse for grandchildren
                }
            }
        }

        /// <summary>
        /// Applies theme to a single control based on its type.
        /// </summary>
        protected static void ApplyThemeToControl(Control control)
        {
            if (control == null)
                return;

            // Use ThemeManager's static method to apply theme to individual controls
            // This leverages the detailed styling logic within ThemeManager and Theme classes
            ThemeManager.ApplyThemeToControl(control);
        }

        /// <summary>
        /// Renders the control into the specified container.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <param name="container">The container control to render into</param>
        public abstract void Render(Control container);
    }
}
