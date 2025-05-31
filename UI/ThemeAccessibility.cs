using System;
using System.Drawing;
using System.Windows.Forms;

namespace BusBus.UI.Core
{
    /// <summary>
    /// Interface for themes that support accessibility features
    /// </summary>
    public interface IAccessibleTheme
    {
        /// <summary>
        /// Enables or disables high contrast mode for improved accessibility
        /// </summary>
        void SetHighContrastMode(bool enabled);

        /// <summary>
        /// Checks if a color pair meets WCAG AA contrast requirements (4.5:1 for normal text)
        /// </summary>
        bool EnsureAccessibleContrast(Color foreground, Color background);

        /// <summary>
        /// Gets the current accessibility mode state
        /// </summary>
        bool IsHighContrastModeEnabled { get; }
    }

    /// <summary>
    /// Extension methods for Theme to support accessibility features
    /// </summary>
    public static class ThemeAccessibilityExtensions
    {
        /// <summary>
        /// Calculates the contrast ratio between two colors according to WCAG 2.0
        /// </summary>
        public static double CalculateContrastRatio(this Theme theme, Color foreground, Color background)
        {
            // Calculate relative luminance for both colors
            double foreL = GetRelativeLuminance(foreground);
            double backL = GetRelativeLuminance(background);

            // Calculate contrast ratio
            if (backL > foreL)
            {
                return (backL + 0.05) / (foreL + 0.05);
            }
            else
            {
                return (foreL + 0.05) / (backL + 0.05);
            }
        }

        /// <summary>
        /// Get the relative luminance of a color (WCAG formula)
        /// </summary>
        private static double GetRelativeLuminance(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        /// <summary>
        /// Ensures that the contrast between foreground and background meets WCAG AA standards (4.5:1)
        /// If not, adjusts the foreground color to meet the standard
        /// </summary>
        public static Color EnsureAccessibleTextColor(this Theme theme, Color foreground, Color background)
        {
            double contrast = theme.CalculateContrastRatio(foreground, background);

            // If contrast is already good, return original foreground
            if (contrast >= 4.5)
            {
                return foreground;
            }

            // Try to adjust foreground to meet contrast
            Color adjustedForeground = foreground;
            int step = 5;
            int maxIterations = 50;

            // Determine if we need to lighten or darken
            bool shouldLighten = GetRelativeLuminance(background) < 0.5;

            for (int i = 0; i < maxIterations; i++)
            {
                if (shouldLighten)
                {
                    // Lighten the foreground
                    adjustedForeground = Color.FromArgb(
                        Math.Min(255, adjustedForeground.R + step),
                        Math.Min(255, adjustedForeground.G + step),
                        Math.Min(255, adjustedForeground.B + step)
                    );
                }
                else
                {
                    // Darken the foreground
                    adjustedForeground = Color.FromArgb(
                        Math.Max(0, adjustedForeground.R - step),
                        Math.Max(0, adjustedForeground.G - step),
                        Math.Max(0, adjustedForeground.B - step)
                    );
                }

                contrast = theme.CalculateContrastRatio(adjustedForeground, background);
                if (contrast >= 4.5)
                {
                    break;
                }
            }

            return adjustedForeground;
        }

        /// <summary>
        /// Adjusts the opacity of a color for glassmorphic effects while maintaining readability
        /// </summary>
        public static Color AdjustGlassOpacity(this Theme theme, Color baseColor, Color backgroundColor, double opacity)
        {
            // For accessibility, limit minimum opacity
            opacity = Math.Max(opacity, 0.35);

            // Blend baseColor with backgroundColor using opacity
            int r = (int)(baseColor.R * opacity + backgroundColor.R * (1 - opacity));
            int g = (int)(baseColor.G * opacity + backgroundColor.G * (1 - opacity));
            int b = (int)(baseColor.B * opacity + backgroundColor.B * (1 - opacity));

            return Color.FromArgb(baseColor.A, r, g, b);
        }

        /// <summary>
        /// Gets a button hover text color that maintains good contrast with the hover background
        /// </summary>
        public static Color GetButtonHoverText(this Theme theme)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            return theme.EnsureAccessibleTextColor(
                theme.ButtonText,
                theme.ButtonHoverBackground);
        }

        /// <summary>
        /// Gets a glassmorphic secondary text color that maintains good contrast
        /// </summary>
        public static Color GetGlassmorphicSecondaryTextColor(this Theme theme)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            Color baseColor = theme.SecondaryText;
            Color background = theme.CardBackground;
            return theme.EnsureAccessibleTextColor(baseColor, background);
        }
    }
}
// File removed. See UI/Core/ThemeAccessibility.cs for the canonical implementation.
