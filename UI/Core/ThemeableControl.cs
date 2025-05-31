#nullable enable
using System;
using System.Windows.Forms;
using BusBus.UI;

namespace BusBus.UI.Core
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
                BusBus.UI.Core.ThemeManager.ThemeChanged += OnThemeChanged;
                _themeSubscribed = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _themeSubscribed)
            {
                // Unsubscribe from theme changes to prevent memory leaks
                BusBus.UI.Core.ThemeManager.ThemeChanged -= OnThemeChanged;
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
                }
                catch (ObjectDisposedException) { }
            }
        }

        /// <summary>
        /// Applies the current theme to this control. Override to customize.
        /// </summary>
        protected abstract void ApplyTheme();

        void IDisplayable.Render(Control container)
        {
            throw new NotImplementedException();
        }
    }
}
