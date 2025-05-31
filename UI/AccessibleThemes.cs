#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace BusBus.UI.Core
{
    /// <summary>
    /// Enhanced implementation of Light and Dark themes with accessibility support
    /// </summary>
    public class AccessibleLightTheme : LightTheme, IAccessibleTheme
    {
        private static readonly Action<ILogger, bool, Exception?> _logHighContrastModeLight =
            LoggerMessage.Define<bool>(LogLevel.Information, new EventId(1, "HighContrastModeLight"),
            "High contrast mode {Enabled} for Light theme");
        private bool _isHighContrastMode = false;
        private readonly ILogger? _logger;

        public AccessibleLightTheme(ILogger? logger = null)
        {
            _logger = logger;
        }

        public bool IsHighContrastModeEnabled => _isHighContrastMode;

        public void SetHighContrastMode(bool enabled)
        {
            _isHighContrastMode = enabled;
            if (_logger != null)
                _logHighContrastModeLight(_logger, enabled, null);
        }

        public new bool EnsureAccessibleContrast(Color foreground, Color background)
        {
            return this.CalculateContrastRatio(foreground, background) >= 4.5;
        }

        public override Color GlassmorphicTextColor => _isHighContrastMode
            ? Color.Black
            : base.GlassmorphicTextColor;

        public override double GlassEffectOpacity => _isHighContrastMode
            ? 0.9 // Almost solid for high contrast mode
            : base.GlassEffectOpacity;

        public override void StyleModernCard(Panel panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            base.StyleModernCard(panel);
            if (_isHighContrastMode)
            {
                panel.BackColor = Color.White;
                panel.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        public override void ApplyGlassmorphicTextColor(Control parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            base.ApplyGlassmorphicTextColor(parent);
            if (_isHighContrastMode)
            {
                // Apply higher contrast colors
                foreach (Control ctrl in parent.Controls)
                {
                    if (ctrl is Label || ctrl is Button || ctrl is LinkLabel)
                    {
                        ctrl.ForeColor = Color.Black;
                    }
                    if (ctrl.Controls.Count > 0)
                    {
                        ApplyGlassmorphicTextColor(ctrl);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Enhanced implementation of Dark theme with accessibility support
    /// </summary>
    public class AccessibleDarkTheme : DarkTheme, IAccessibleTheme
    {
        private static readonly Action<ILogger, bool, Exception?> _logHighContrastModeDark =
            LoggerMessage.Define<bool>(LogLevel.Information, new EventId(2, "HighContrastModeDark"),
            "High contrast mode {Enabled} for Dark theme");
        private bool _isHighContrastMode = false;
        private readonly ILogger? _logger;

        public AccessibleDarkTheme(ILogger? logger = null)
        {
            _logger = logger;
        }

        public bool IsHighContrastModeEnabled => _isHighContrastMode; public void SetHighContrastMode(bool enabled)
        {
            _isHighContrastMode = enabled;
            if (_logger != null)
                _logHighContrastModeDark(_logger, enabled, null);
        }

        public new bool EnsureAccessibleContrast(Color foreground, Color background)
        {
            return this.CalculateContrastRatio(foreground, background) >= 4.5;
        }

        public override Color GlassmorphicTextColor => _isHighContrastMode
            ? Color.White
            : base.GlassmorphicTextColor;

        public override Color CardText => _isHighContrastMode
            ? Color.White
            : base.CardText;

        public override double GlassEffectOpacity => _isHighContrastMode
            ? 0.9 // Almost solid for high contrast mode
            : base.GlassEffectOpacity;

        public override void StyleModernCard(Panel panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            base.StyleModernCard(panel);
            if (_isHighContrastMode)
            {
                panel.BackColor = Color.Black;
                panel.BorderStyle = BorderStyle.FixedSingle;
                // Add a high-visibility border
                panel.Paint += (sender, e) =>
                {
                    Rectangle rect = new Rectangle(1, 1, panel.Width - 3, panel.Height - 3);
                    using (var pen = new Pen(Color.FromArgb(100, 100, 100), 1))
                    {
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                };
            }
        }

        public override void ApplyGlassmorphicTextColor(Control parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            base.ApplyGlassmorphicTextColor(parent);
            if (_isHighContrastMode)
            {
                // Apply higher contrast colors
                foreach (Control ctrl in parent.Controls)
                {
                    if (ctrl is Label || ctrl is Button || ctrl is LinkLabel)
                    {
                        ctrl.ForeColor = Color.White;
                    }
                    if (ctrl.Controls.Count > 0)
                    {
                        ApplyGlassmorphicTextColor(ctrl);
                    }
                }
            }
        }
    }
}
// File removed. See UI/Core/AccessibleThemes.cs for the canonical implementation.
