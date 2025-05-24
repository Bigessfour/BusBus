using System;
using System.Configuration;
using System.Drawing;
using System.IO;

namespace BusBus.UI
{
    /// <summary>
    /// Constants for UI styling and layout
    /// </summary>
    public static class UiConstants
    {
        // Default values
        private const int DEFAULT_FORM_WIDTH = 800;
        private const int DEFAULT_FORM_HEIGHT = 600;
        private const int DEFAULT_MIN_PANEL_WIDTH = 60;
        private const int DEFAULT_MAX_PANEL_WIDTH = 200;
        private const int DEFAULT_SPLITTER_DISTANCE = 80;
        private const int DEFAULT_CARD_PANEL_WIDTH = 500;
        private const int DEFAULT_CARD_PANEL_HEIGHT = 220;

        // Public properties with fallback to default values
        public static int FormWidth => GetIntSetting("UI.FormWidth", DEFAULT_FORM_WIDTH);
        public static int FormHeight => GetIntSetting("UI.FormHeight", DEFAULT_FORM_HEIGHT);
        public static int MinPanelWidth => GetIntSetting("UI.MinPanelWidth", DEFAULT_MIN_PANEL_WIDTH);
        public static int MaxPanelWidth => GetIntSetting("UI.MaxPanelWidth", DEFAULT_MAX_PANEL_WIDTH);
        public static int DefaultSplitterDistance => GetIntSetting("UI.DefaultSplitterDistance", DEFAULT_SPLITTER_DISTANCE);
        public static int CardPanelWidth => GetIntSetting("UI.CardPanelWidth", DEFAULT_CARD_PANEL_WIDTH);
        public static int CardPanelHeight => GetIntSetting("UI.CardPanelHeight", DEFAULT_CARD_PANEL_HEIGHT);

        // Spacing and Sizing
        public const int DefaultPadding = 10;
        public const int SmallPadding = 5;
        public const int LargePadding = 20;
        public const int DefaultButtonHeight = 35;
        public const int DefaultControlHeight = 25;
        public const int HeaderHeight = 50;
        public const int FooterHeight = 30;
        public const int SidebarWidth = 200;

        // Font Sizes
        public const float TitleFontSize = 14F;
        public const float HeaderFontSize = 12F;
        public const float DefaultFontSize = 10F;
        public const float SmallFontSize = 8F;

        // Grid Settings
        public const int DefaultPageSize = 10;
        public const int GridRowHeight = 25;
        public const int GridHeaderHeight = 30;

        // Control Names (for testing)
        public const string AddButtonName = "AddButton";
        public const string EditButtonName = "EditButton";
        public const string DeleteButtonName = "DeleteButton";
        public const string SaveButtonName = "SaveButton";
        public const string CancelButtonName = "CancelButton";
        public const string RoutesGridName = "RoutesGrid";

        // Default Colors (fallback when theme is not available)
        public static readonly Color DefaultBackgroundColor = Color.White;
        public static readonly Color DefaultTextColor = Color.Black;
        public static readonly Color DefaultButtonColor = Color.LightGray;
        public static readonly Color DefaultBorderColor = Color.DarkGray;

        // Animation and Timing
        public const int DefaultAnimationDuration = 250; // milliseconds
        public const int LoadingTimeout = 5000; // milliseconds
        public const int DatabaseTimeout = 30000; // milliseconds

        // Helper method to read settings from config or use defaults
        private static int GetIntSetting(string key, int defaultValue)
        {
            try
            {
                string? value = ConfigurationManager.AppSettings[key];
                if (int.TryParse(value, out int result))
                {
                    return result;
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                Console.WriteLine($"Error reading configuration: {ex.Message}");
            }

            return defaultValue;
        }

        // Save settings (can be used by a configuration UI)
        public static void SaveSettings(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings.Remove(key);
                config.AppSettings.Settings.Add(key, value);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (System.Configuration.ConfigurationErrorsException ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }
    }
}
