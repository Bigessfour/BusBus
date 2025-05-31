#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
#pragma warning disable CS0169 // The field is never used
#pragma warning disable CA1416 // Platform compatibility (Windows-only)
#pragma warning disable CS1998 // Async method lacks 'await' operators
#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.DependencyInjection;
using BusBus.UI.Core;

// Moved to UI/Core/ThemeManager.cs on 2025-05-31 as part of UI/Core consolidation for shared theming.

public enum ThemeType
{
    Light,
    Dark
}
/// <summary>
/// Manages application themes and provides centralized theme switching functionality
/// </summary>
public static class ThemeManager
{
    /// <summary>
    /// Applies glassmorphic text color to all controls within a glassmorphic panel/card.
    /// Ensures consistent, high-contrast text across all forms (Netguru, NN/g, DesignStudioUIUX).
    /// </summary>
    public static void EnforceGlassmorphicTextColor(Control control)
    {
        if (control == null) return;
        // If this is a glassmorphic panel (by Tag or Name convention)
        if ((control.Tag?.ToString()?.Contains("Glass", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (control.Name?.Contains("Glass", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (control.Tag?.ToString()?.Contains("ModernCard", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            CurrentTheme.ApplyGlassmorphicTextColor(control);
        }
        // Recursively apply to children
        foreach (Control child in control.Controls)
        {
            EnforceGlassmorphicTextColor(child);
        }
    }

    private static readonly Dictionary<string, Func<Theme>> _themeRegistry = new()
    {
        ["Light"] = () => new LightTheme(),
        ["Dark"] = () => new DarkTheme()
    };
    private static Theme _currentTheme = new DarkTheme();
    private static ThemeType currentTheme = ThemeType.Light;

    /// <summary>
    /// Event fired when the theme changes
    /// </summary>
    public static event EventHandler<EventArgs> ThemeChanged;

    /// <summary>
    /// Gets or sets the current active theme
    /// </summary>
    public static Theme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme?.Dispose();
                _currentTheme = value ?? throw new ArgumentNullException(nameof(value));
                OnThemeChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets the current theme type
    /// </summary>
    public static ThemeType CurrentThemeType => currentTheme;

    /// <summary>
    /// Gets the primary color for the current theme
    /// </summary>
    public static Color PrimaryColor => currentTheme == ThemeType.Light
        ? Color.FromArgb(0, 122, 204)
        : Color.FromArgb(30, 30, 30);

    /// <summary>
    /// Gets the background color for the current theme
    /// </summary>
    public static Color BackgroundColor => currentTheme == ThemeType.Light
        ? Color.White
        : Color.FromArgb(45, 45, 48);

    /// <summary>
    /// Gets the text color for the current theme
    /// </summary>
    public static Color TextColor => currentTheme == ThemeType.Light
        ? Color.Black
        : Color.White;

    /// <summary>
    /// Gets the secondary background color for the current theme
    /// </summary>
    public static Color SecondaryBackgroundColor => currentTheme == ThemeType.Light
        ? Color.FromArgb(240, 240, 240)
        : Color.FromArgb(60, 60, 60);

    /// <summary>
    /// Raises the ThemeChanged event
    /// </summary>
    private static void OnThemeChanged(EventArgs e)
    {
        ThemeChanged?.Invoke(null, e);
    }

    /// <summary>
    /// Switches to a theme by name
    /// </summary>
    /// <param name="themeName">Name of the theme to switch to</param>
    public static void SwitchTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            throw new ArgumentException("Theme name cannot be null or empty", nameof(themeName));

        if (_themeRegistry.TryGetValue(themeName, out var themeFactory))
        {
            CurrentTheme = themeFactory();
            currentTheme = themeName == "Light" ? ThemeType.Light : ThemeType.Dark;
        }
        else
        {
            throw new ArgumentException($"Theme '{themeName}' is not registered", nameof(themeName));
        }
    }

    /// <summary>
    /// Sets the theme by name (alias for SwitchTheme)
    /// </summary>
    /// <param name="themeName">Name of the theme to set</param>
    public static void SetTheme(string themeName)
    {
        SwitchTheme(themeName);
    }

    /// <summary>
    /// Registers a new theme with the theme manager
    /// </summary>
    /// <param name="name">Name of the theme</param>
    /// <param name="themeFactory">Factory function to create the theme</param>
    public static void RegisterTheme(string name, Func<Theme> themeFactory)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(themeFactory);

        _themeRegistry[name] = themeFactory;
    }        /// <summary>
             /// Gets all available theme names
             /// </summary>
             /// <returns>Collection of theme names</returns>
    public static IEnumerable<string> AvailableThemes => _themeRegistry.Keys;

    /// <summary>
    /// Applies the current theme to a form and all its child controls
    /// </summary>
    /// <param name="form">The form to apply the theme to</param>
    public static void RefreshTheme(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);
        ApplyThemeToControl(form);
    }

    /// <summary>
    /// Applies the current theme to a specific control
    /// </summary>
    /// <param name="control">The control to apply the theme to</param>
    public static void RefreshControl(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        ApplyThemeToControl(control);
    }

    /// <summary>
    /// Recursively applies the current theme to a control and all its children
    /// </summary>
    /// <param name="control">The control to apply the theme to</param>
    public static void ApplyThemeToControl(Control control)
    {
        if (control == null) return;

        try
        {
            // Apply theme based on control type and tags
            switch (control)
            {
                case Form form:
                    form.BackColor = CurrentTheme.MainBackground;
                    break;

                case Panel panel when panel.Tag?.ToString() == "HeadlinePanel":
                    panel.BackColor = CurrentTheme.HeadlineBackground;
                    break;

                case Panel panel when panel.Tag?.ToString() == "SidePanel":
                    panel.BackColor = CurrentTheme.SidePanelBackground;
                    break;
                case Panel panel when panel.Tag?.ToString()?.StartsWith("Elevation", StringComparison.Ordinal) == true:
                    if (int.TryParse(panel.Tag.ToString()!.Replace("Elevation", "", StringComparison.Ordinal), out int elevation))
                    {
                        panel.BackColor = CurrentTheme.GetElevatedBackground(elevation);
                    }
                    else
                    {
                        panel.BackColor = CurrentTheme.CardBackground;
                    }
                    break;

                case Panel panel:
                    panel.BackColor = CurrentTheme.CardBackground;
                    break;

                case Button button:
                    button.BackColor = CurrentTheme.ButtonBackground;
                    button.ForeColor = CurrentTheme.ButtonText;
                    button.Font = CurrentTheme.ButtonFont;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 0;
                    break;

                case Label label:
                    label.ForeColor = CurrentTheme.CardText;
                    label.Font = CurrentTheme.CardFont;
                    break;

                case TextBox textBox:
                    textBox.BackColor = CurrentTheme.TextBoxBackground;
                    textBox.ForeColor = CurrentTheme.CardText;
                    textBox.Font = CurrentTheme.TextBoxFont;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ComboBox comboBox:
                    comboBox.BackColor = CurrentTheme.TextBoxBackground;
                    comboBox.ForeColor = CurrentTheme.CardText;
                    comboBox.Font = CurrentTheme.TextBoxFont;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    break;

                case NumericUpDown numericUpDown:
                    numericUpDown.BackColor = CurrentTheme.TextBoxBackground;
                    numericUpDown.ForeColor = CurrentTheme.CardText;
                    numericUpDown.Font = CurrentTheme.TextBoxFont;
                    break;

                case DataGridView grid:
                    grid.BackgroundColor = CurrentTheme.GridBackground;
                    grid.ForeColor = CurrentTheme.CardText;
                    grid.BorderStyle = BorderStyle.None;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = CurrentTheme.HeadlineBackground;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = CurrentTheme.HeadlineText;
                    grid.DefaultCellStyle.BackColor = CurrentTheme.CardBackground;
                    grid.DefaultCellStyle.ForeColor = CurrentTheme.CardText;
                    grid.EnableHeadersVisualStyles = false;
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
    }

    /// <summary>
    /// Applies a theme to a control and all its child controls
    /// </summary>
    /// <param name="control">The control to apply the theme to</param>
    /// <param name="theme">The theme to apply</param>
    public static void ApplyTheme(Control control, Theme theme)
    {
        if (control == null || theme == null) return;

        control.BackColor = theme.MainBackground;
        control.ForeColor = theme.CardText;

        foreach (Control child in control.Controls)
        {
            ApplyTheme(child, theme);
        }
    }

    /// <summary>
    /// Determines if a control has been styled according to the current theme
    /// </summary>
    /// <param name="control">The control to check</param>
    /// <returns>True if the control matches the current theme styling</returns>
    private static bool IsControlStyled(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        return control switch
        {
            Form form => form.BackColor == CurrentTheme.MainBackground,
            Panel panel when panel.Tag?.ToString() == "HeadlinePanel" =>
                panel.BackColor == CurrentTheme.HeadlineBackground,
            Panel panel when panel.Tag?.ToString() == "SidePanel" =>
                panel.BackColor == CurrentTheme.SidePanelBackground,
            Panel panel => panel.BackColor == CurrentTheme.CardBackground,
            Button button => button.BackColor == CurrentTheme.ButtonBackground,
            TextBox textBox => textBox.BackColor == CurrentTheme.TextBoxBackground &&
                               textBox.ForeColor == CurrentTheme.CardText,
            ComboBox comboBox => comboBox.BackColor == CurrentTheme.TextBoxBackground &&
                                 comboBox.ForeColor == CurrentTheme.CardText,
            _ => false
        };
    }

    /// <summary>
    /// Applies modern dark UI enhancements to a form and all its controls
    /// </summary>
    /// <param name="form">The form to enhance</param>
    public static void ApplyModernTheme(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);

        // Apply base theme first
        RefreshTheme(form);

        // Apply modern enhancements
        ApplyModernEnhancements(form);
    }

    /// <summary>
    /// Recursively applies modern UI enhancements to controls
    /// </summary>
    /// <param name="control">The control to enhance</param>
    private static void ApplyModernEnhancements(Control control)
    {
        if (control == null) return;

        try
        {
            switch (control)
            {
                case Panel panel when panel.Tag?.ToString() == "ModernCard":
                    CurrentTheme.StyleModernCard(panel);
                    // Enforce glassmorphic text color for all children
                    EnforceGlassmorphicTextColor(panel);
                    break;

                case Panel panel when panel.Tag?.ToString() == "GlassPanel":
                    CurrentTheme.StyleGlassPanel(panel);
                    EnforceGlassmorphicTextColor(panel);
                    break;

                case Button button when button.Tag?.ToString() == "EnhancedButton":
                    CurrentTheme.StyleEnhancedButton(button);
                    break;
            }

            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyModernEnhancements(child);
            }
        }
        catch (ObjectDisposedException)
        {
            // Control was disposed while applying enhancements
        }
    }

    /// <summary>
    /// Creates a themed action button with modern styling
    /// </summary>
    /// <param name="text">Button text</param>
    /// <param name="description">Button description</param>
    /// <param name="action">Action identifier</param>
    /// <returns>A styled action button panel</returns>
    public static Panel CreateThemedActionButton(string text, string description, string action)
    {
        var panel = new Panel
        {
            BackColor = CurrentTheme.ButtonBackground,
            Margin = CurrentTheme.EnhancedButtonMargin,
            Padding = CurrentTheme.EnhancedButtonPadding,
            MinimumSize = CurrentTheme.EnhancedButtonMinSize,
            Cursor = Cursors.Hand,
            Tag = "ModernCard" // Mark for theme enhancement
        };

        // Apply modern card styling
        CurrentTheme.StyleModernCard(panel);

        var textLabel = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = CurrentTheme.ButtonText,
            Location = new Point(12, 8),
            AutoSize = true,
            MaximumSize = new Size(160, 20),
            BackColor = Color.Transparent
        };

        var descLabel = new Label
        {
            Text = description,
            Font = new Font("Segoe UI", 9F),
            ForeColor = CurrentTheme.SecondaryText,
            Location = new Point(12, 30),
            MaximumSize = new Size(160, 20),
            AutoEllipsis = true,
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var arrowLabel = new Label
        {
            Text = "→",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = CurrentTheme.ButtonText,
            Location = new Point(175, 20),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        panel.Controls.AddRange(new Control[] { textLabel, descLabel, arrowLabel });
        // Add hover effects
        panel.MouseEnter += (s, e) =>
        {
            panel.BackColor = CurrentTheme.ButtonHoverBackground;
            textLabel.ForeColor = Color.White;
            arrowLabel.ForeColor = Color.White;
        };

        panel.MouseLeave += (s, e) =>
        {
            panel.BackColor = CurrentTheme.ButtonBackground;
            textLabel.ForeColor = CurrentTheme.ButtonText;
            arrowLabel.ForeColor = CurrentTheme.ButtonText;
        };

        return panel;
    }

    /// <summary>
    /// Applies high-quality text rendering settings to all controls in a form
    /// </summary>
    /// <param name="form">The form to enhance</param>
    public static void ApplyHighQualityTextRendering(Form form)
    {
        if (form == null) return;

        // Apply to all controls
        TextRenderingManager.RegisterForHighQualityTextRendering(form);

        // Set application-wide settings
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        form.AutoScaleMode = AutoScaleMode.Dpi;

        // Add form load handler to ensure settings are applied on resize
        form.Load += (s, e) =>
        {
            if (s is Form loadedForm)
            {
                loadedForm.AutoScaleDimensions = new SizeF(96F, 96F);
                loadedForm.AutoScaleMode = AutoScaleMode.Dpi;

                // Force immediate redraw of all controls
                foreach (Control control in loadedForm.Controls)
                {
                    control.Invalidate(true);
                }
            }
        };
    }        /// <summary>
             /// Registers default and accessible themes
             /// </summary>
             /// <param name="serviceProvider">Service provider for dependency injection</param>
    public static void RegisterAccessibleThemes(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Get logger if available
        var logger = serviceProvider.GetService(typeof(ILogger)) as ILogger;

        // Register accessible themes
        RegisterTheme("LightAccessible", () => new AccessibleLightTheme(logger));
        RegisterTheme("DarkAccessible", () => new AccessibleDarkTheme(logger));
    }

    /// <summary>
    /// Switches to accessible theme mode
    /// </summary>
    public static void EnableAccessibleTheme(bool isDarkTheme = false)
    {
        SwitchTheme(isDarkTheme ? "DarkAccessible" : "LightAccessible");
    }

    /// <summary>
    /// Switches to standard theme mode
    /// </summary>
    public static void EnableStandardTheme(bool isDarkTheme = false)
    {
        SwitchTheme(isDarkTheme ? "Dark" : "Light");
    }

    /// <summary>
    /// Sets high contrast mode on the current theme if it supports it
    /// </summary>
    public static void SetHighContrastMode(bool enabled)
    {
        if (CurrentTheme is IAccessibleTheme accessibleTheme)
        {
            accessibleTheme.SetHighContrastMode(enabled);
            // Refresh all open forms
            foreach (Form form in Application.OpenForms)
            {
                RefreshTheme(form);
            }
        }
        else
        {
            // Switch to an accessible theme if needed
            bool isDarkTheme = currentTheme == ThemeType.Dark;
            EnableAccessibleTheme(isDarkTheme);
            SetHighContrastMode(enabled);
        }
    }

    internal static void SetTheme(Theme theme)
    {
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));

        // Set the current theme
        CurrentTheme = theme;

        // Update the currentTheme type if possible
        if (theme is LightTheme)
            currentTheme = ThemeType.Light;
        else if (theme is DarkTheme)
            currentTheme = ThemeType.Dark;
        // else leave as is or extend for more themes
    }
}
// File removed. See UI/Core/ThemeManager.cs for the canonical implementation.
