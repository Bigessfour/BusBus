#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace BusBus.UI.Core
{
    /// <summary>
    /// Helper class to ensure consistent text rendering quality across the application
    /// </summary>
    public static class TextRenderingManager
    {
        private static readonly Action<ILogger, string, string, Exception?> _logFixTruncation =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(1, "FixTruncation"),
            "Fixing potential text truncation in label: {LabelName} - '{LabelText}'");
        private static readonly Action<ILogger, Exception?> _logOptimizeTableLayout =
            LoggerMessage.Define(LogLevel.Debug, new EventId(2, "OptimizeTableLayout"),
            "Optimizing TableLayoutPanel for text rendering - adding AutoSize row");
        private static ILogger? _logger;

        /// <summary>
        /// Initializes the TextRenderingManager with a logger
        /// </summary>
        public static void Initialize(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Applies high-quality text rendering settings to a Graphics object
        /// </summary>
        /// <param name="graphics">The Graphics object to configure</param>
        public static void ApplyHighQualityTextRendering(Graphics graphics)
        {
            if (graphics == null) return;

            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
        }

        /// <summary>
        /// Registers Paint events for consistent text rendering quality in a control and its children
        /// </summary>
        /// <param name="control">The root control to enhance</param>
        public static void RegisterForHighQualityTextRendering(Control control)
        {
            if (control == null) return;

            // For controls that handle their own painting
            control.Paint += (sender, e) => ApplyHighQualityTextRendering(e.Graphics);

            // Special handling for specific control types
            if (control is DataGridView grid)
            {
                ConfigureDataGridView(grid);
            }
            else if (control is Label label)
            {
                ConfigureLabel(label);
            }
            else if (control is Button button)
            {
                ConfigureButton(button);
            }
            else if (control is TextBox textBox)
            {
                ConfigureTextBox(textBox);
            }
            else if (control is ComboBox comboBox)
            {
                ConfigureComboBox(comboBox);
            }
            else if (control is Panel panel)
            {
                // Apply glassmorphic text optimizations if it's a glass panel
                if ((panel.Tag?.ToString()?.Contains("Glass", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (panel.Name?.Contains("Glass", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (panel.Tag?.ToString()?.Contains("ModernCard", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    panel.Paint += (sender, e) =>
                    {
                        ApplyHighQualityTextRendering(e.Graphics);
                        // Additional glassmorphic optimizations can be added here
                    };
                }
            }

            // Special handling for TableLayoutPanel and FlowLayoutPanel
            if (control is TableLayoutPanel || control is FlowLayoutPanel)
            {
                EnsureProperSizingForLayout(control);
            }

            // Apply to child controls recursively
            foreach (Control child in control.Controls)
            {
                RegisterForHighQualityTextRendering(child);
            }
        }

        /// <summary>
        /// Configures a DataGridView for optimal text rendering
        /// </summary>
        private static void ConfigureDataGridView(DataGridView grid)
        {
            grid.CellPainting += (sender, e) => { if (e.Graphics != null) ApplyHighQualityTextRendering(e.Graphics); };
            grid.RowPrePaint += (sender, e) => { if (e.Graphics != null) ApplyHighQualityTextRendering(e.Graphics); };
            grid.RowPostPaint += (sender, e) => { if (e.Graphics != null) ApplyHighQualityTextRendering(e.Graphics); };
            grid.EnableHeadersVisualStyles = false; // Required for consistent rendering
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font.FontFamily, grid.Font.Size, FontStyle.Bold);

            // Improve cell padding to prevent text from touching borders
            grid.DefaultCellStyle.Padding = new Padding(5);

            // Allow text wrapping and prevent truncation
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // Ensure rows adjust to accommodate wrapped text
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Add minimum row height to improve readability
            grid.RowTemplate.MinimumHeight = 30;

            // Prevent column header truncation
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.ColumnHeadersHeight = 35; // Minimum height for headers
        }

        /// <summary>
        /// Configures a Label for optimal text rendering and truncation prevention
        /// </summary>
        private static void ConfigureLabel(Label label)
        {
            // Ensure labels are sized properly to prevent truncation
            label.AutoEllipsis = true;

            // Use proper padding to prevent text from touching borders
            label.Padding = new Padding(2);

            // For multi-line labels
            if (label.Text.Contains(Environment.NewLine) || label.Text.Length > 50)
            {
                label.AutoSize = true;
                label.MaximumSize = new Size(
                    label.MaximumSize.Width > 0 ? label.MaximumSize.Width : 500,
                    0); // Unlimited height, constrained width
            }

            // Fix for certain common use cases where labels get truncated
            if (label.Text.Length > 100 && !label.AutoSize)
            {
                label.AutoSize = true;
            }

            // Improve readability with proper letter spacing and anti-aliasing
            label.Paint += (sender, e) =>
            {
                e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                // Apply pixel offset to prevent blurry text
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            };
        }

        /// <summary>
        /// Configures a Button for optimal text rendering
        /// </summary>
        private static void ConfigureButton(Button button)
        {
            button.UseVisualStyleBackColor = false; // Required for custom rendering
            button.TextAlign = ContentAlignment.MiddleCenter;

            // Ensure button text doesn't get cut off
            button.AutoEllipsis = true;

            // Add padding inside buttons to prevent text from touching borders
            button.Padding = new Padding(5, 2, 5, 2);

            // Ensure minimum button width to prevent truncation
            if (button.Width < button.PreferredSize.Width)
            {
                button.Width = button.PreferredSize.Width + 20; // Add extra space
            }

            // Improve button text clarity
            button.Paint += (sender, e) =>
            {
                e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality; // Prevent blurry text
            };
        }

        /// <summary>
        /// Configures a TextBox for optimal text rendering
        /// </summary>
        private static void ConfigureTextBox(TextBox textBox)
        {
            // Improve text rendering in textboxes
            textBox.BorderStyle = BorderStyle.FixedSingle;

            // For multiline textboxes, ensure proper sizing
            if (textBox.Multiline)
            {
                textBox.ScrollBars = ScrollBars.Vertical;
            }
        }

        /// <summary>
        /// Configures a ComboBox for optimal text rendering
        /// </summary>
        private static void ConfigureComboBox(ComboBox comboBox)
        {
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox.DrawItem += (sender, e) =>
            {
                if (e.Index < 0) return;

                e.DrawBackground();
                ApplyHighQualityTextRendering(e.Graphics);

                if (sender is ComboBox combo && e.Index < combo.Items.Count)
                {
                    string text = combo.Items[e.Index]?.ToString() ?? string.Empty;
                    using var brush = new SolidBrush(e.ForeColor);
                    e.Graphics.DrawString(text, e.Font ?? combo.Font, brush, e.Bounds);
                }

                e.DrawFocusRectangle();
            };
        }

        /// <summary>
        /// Ensures layout panels properly size their children to prevent truncation
        /// </summary>
        private static void EnsureProperSizingForLayout(Control layoutPanel)
        {
            if (layoutPanel is TableLayoutPanel tableLayout)
            {
                // Add minimum spacing between cells to prevent text from being too close
                tableLayout.Padding = new Padding(5);
                tableLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
                tableLayout.Margin = new Padding(5);

                // Check if there are any AutoSize rows or columns
                bool hasAutoSizeRows = false;
                for (int i = 0; i < tableLayout.RowStyles.Count; i++)
                {
                    if (tableLayout.RowStyles[i].SizeType == SizeType.AutoSize)
                    {
                        hasAutoSizeRows = true;
                        break;
                    }
                }

                // If no AutoSize rows, add one to prevent truncation
                if (!hasAutoSizeRows && tableLayout.RowStyles.Count > 0)
                {
                    if (_logger != null)
                        _logOptimizeTableLayout(_logger, null);
                    tableLayout.RowStyles[tableLayout.RowStyles.Count - 1] = new RowStyle(SizeType.AutoSize);
                }
            }
            else if (layoutPanel is FlowLayoutPanel flowLayout)
            {
                // Optimize FlowLayoutPanel for text rendering
                if (!flowLayout.AutoSize)
                {
                    flowLayout.AutoScroll = true;
                }
            }
        }

        /// <summary>
        /// Checks if a control's text is likely to be truncated
        /// </summary>
        public static bool IsTextLikelyTruncated(Control control)
        {
            if (control is Label label && !label.AutoSize && !label.AutoEllipsis)
            {
                // Measure the text
                using (var g = label.CreateGraphics())
                {
                    SizeF textSize = g.MeasureString(label.Text, label.Font);
                    return textSize.Width > label.Width || textSize.Height > label.Height;
                }
            }

            return false;
        }

        /// <summary>
        /// Fixes potential text truncation issues
        /// </summary>
        public static void FixPotentialTruncation(Control control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (control is Label label && IsTextLikelyTruncated(label))
            {
                if (_logger != null)
                {
                    var textSpan = label.Text.AsSpan(0, Math.Min(20, label.Text.Length));
                    var concat = string.Concat(textSpan, "...");
                    _logFixTruncation(_logger, label.Name, concat, null);
                }
                label.AutoEllipsis = true;

                // For important labels, try to adjust sizing to prevent truncation
                if (label.Tag?.ToString()?.Contains("important", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    label.AutoSize = true;
                    label.MaximumSize = new Size(label.Parent?.Width ?? 500, 0);
                }
            }

            // Process child controls
            foreach (Control child in control.Controls)
            {
                FixPotentialTruncation(child);
            }
        }
    }
}
