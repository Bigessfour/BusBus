using System;
using System.Drawing;
using System.Windows.Forms;

namespace BusBus.UI
{
    /// <summary>
    /// Helps visualize UI components for debugging purposes.
    /// This class adds subtle visual indicators to help identify different parts of the UI.
    /// </summary>
    public static class VisualDebugHelper
    {
        // Whether visual debugging is enabled
        private static bool _enabled = false;

        /// <summary>
        /// Enables or disables visual debugging features
        /// </summary>
        public static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }        /// <summary>
                 /// Adds a subtle border to a control to make it visually identifiable
                 /// </summary>
        public static void HighlightControl(Control control, string label, Color color)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (!_enabled)
                return;

            // Only proceed if we're in debug mode
#if DEBUG
            try
            {
                // Create a thin border around the control
                control.Paint += (sender, e) =>
                {
                    using (var pen = new Pen(color, 2))
                    {
                        var rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
                        e.Graphics.DrawRectangle(pen, rect);

                        // Draw the label in the top-left corner
                        using (var brush = new SolidBrush(color))
                        using (var font = new Font("Arial", 8, FontStyle.Bold))
                        {
                            e.Graphics.DrawString(label, font, brush, 4, 2);
                        }
                    }
                };

                // Force a redraw
                control.Invalidate();
            }
            catch (Exception)
            {
                // Silently fail - this is just a debug helper
            }
#endif
        }        /// <summary>
                 /// Adds labels to the main Dashboard components
                 /// </summary>
        public static void HighlightDashboardComponents(Dashboard dashboard)
        {
            if (dashboard == null)
                throw new ArgumentNullException(nameof(dashboard));

            if (!_enabled)
                return;

#if DEBUG
            try
            {
                // Use reflection to access private fields (only for debugging)
                var headerPanel = dashboard.GetType().GetField("_headerPanel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(dashboard) as Panel;

                var sidePanel = dashboard.GetType().GetField("_sidePanel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(dashboard) as Panel;

                var contentPanel = dashboard.GetType().GetField("_contentPanel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(dashboard) as Panel;

                // Highlight each component with a different color
                if (headerPanel != null)
                    HighlightControl(headerPanel, "MAIN HEADER", Color.FromArgb(100, Color.Blue));

                if (sidePanel != null)
                    HighlightControl(sidePanel, "NAVIGATION", Color.FromArgb(100, Color.Green));

                if (contentPanel != null)
                    HighlightControl(contentPanel, "CONTENT AREA", Color.FromArgb(100, Color.Orange));
            }
            catch (Exception)
            {
                // Silently fail - this is just a debug helper
            }
#endif
        }
    }
}
