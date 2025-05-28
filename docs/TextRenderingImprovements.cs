// TextRenderingImprovements.cs
// Sample implementation of text rendering improvements for BusBus
// Created: May 27, 2025

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BusBus.UI
{
    /// <summary>
    /// Helper class to ensure consistent text rendering quality across the application
    /// </summary>
    public static class TextRenderingManager
    {
        /// <summary>
        /// Applies high-quality text rendering settings to a Graphics object
        /// </summary>
        public static void ApplyHighQualityTextRendering(Graphics graphics)
        {
            if (graphics == null) return;

            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        /// <summary>
        /// Registers Paint events for consistent text rendering quality in a control and its children
        /// </summary>
        public static void RegisterForHighQualityTextRendering(Control control)
        {
            if (control == null) return;

            // For controls that handle their own painting
            control.Paint += (sender, e) => ApplyHighQualityTextRendering(e.Graphics);

            // Special handling for specific control types
            if (control is DataGridView grid)
            {
                grid.CellPainting += (sender, e) => ApplyHighQualityTextRendering(e.Graphics);
                grid.RowPrePaint += (sender, e) => ApplyHighQualityTextRendering(e.Graphics);
                grid.RowPostPaint += (sender, e) => ApplyHighQualityTextRendering(e.Graphics);
                grid.EnableHeadersVisualStyles = false; // Required for consistent rendering
            }
            else if (control is Label label)
            {
                // Ensure labels are sized properly to prevent truncation
                label.AutoEllipsis = true;
            }

            // Apply to child controls recursively
            foreach (Control child in control.Controls)
            {
                RegisterForHighQualityTextRendering(child);
            }
        }
    }

    /// <summary>
    /// Extensions to the Theme class for WCAG-compliant contrast
    /// </summary>
    public static class ThemeAccessibilityExtensions
    {
        /// <summary>
        /// Calculates contrast ratio between two colors (WCAG 2.1 formula)
        /// </summary>
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
    }

    /// <summary>
    /// Extensions for DPI awareness
    /// </summary>
    public static class DpiAwarenessManager
    {
        /// <summary>
        /// Enables DPI awareness for the application
        /// </summary>
        public static void EnablePerMonitorDpiAwareness()
        {
            if (Environment.OSVersion.Version.Major >= 10)
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            }
        }

        /// <summary>
        /// Configures a form for proper DPI scaling
        /// </summary>
        public static void ConfigureFormForDpiScaling(Form form)
        {
            if (form == null) return;

            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.AutoScaleDimensions = new SizeF(96F, 96F);

            // Add handler to ensure DPI changes are handled during runtime
            form.DpiChanged += (sender, e) =>
            {
                if (sender is Form dpiForm)
                {
                    // Force immediate redraw of all controls
                    dpiForm.SuspendLayout();
                    foreach (Control control in dpiForm.Controls)
                    {
                        control.Invalidate(true);
                    }
                    dpiForm.ResumeLayout();
                }
            };
        }

        /// <summary>
        /// Converts pixel values to DPI-aware values
        /// </summary>
        public static int ScaleForDpi(int pixels, Control control)
        {
            if (control == null) return pixels;

            using (var graphics = control.CreateGraphics())
            {
                float dpiScale = graphics.DpiX / 96.0f; // 96 DPI is the baseline
                return (int)(pixels * dpiScale);
            }
        }
    }
}
