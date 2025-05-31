using System;
using System.Drawing;
using System.Windows.Forms;

namespace BusBus.Utils
{
    public static class LayoutDebugHelper
    {
        private static bool _debugMode;
        private static readonly Random _random = new Random();

        public static bool DebugMode
        {
            get => _debugMode;
            set => _debugMode = value;
        }

        public static void ToggleDebugMode()
        {
            _debugMode = !_debugMode;
        }
        public static void ApplyDebugBorders(Control control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (!_debugMode) return;

            // Generate a random color for this control
            var color = Color.FromArgb(255, _random.Next(100, 255), _random.Next(100, 255), _random.Next(100, 255));

            // For panels and other containers
            if (control is Panel || control is GroupBox || control is TableLayoutPanel)
            {
                control.Paint += (sender, e) =>
                {
                    if (_debugMode)
                    {
                        using (var pen = new Pen(color, 2))
                        {
                            e.Graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
                        }

                        // Draw control info
                        using (var brush = new SolidBrush(color))
                        {
                            var info = $"{control.GetType().Name}: {control.Name ?? "unnamed"}";
                            e.Graphics.DrawString(info, SystemFonts.DefaultFont, brush, 2, 2);
                        }
                    }
                };
            }

            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyDebugBorders(child);
            }
        }
        public static void PrintLayoutInfo(Control control, int indent = 0)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var indentStr = new string(' ', indent * 2);
            Console.WriteLine($"{indentStr}{control.GetType().Name} '{control.Name ?? "unnamed"}' - " +
                            $"Size: {control.Size}, Location: {control.Location}, Dock: {control.Dock}");

            foreach (Control child in control.Controls)
            {
                PrintLayoutInfo(child, indent + 1);
            }
        }
    }
}
