using System;
using System.Configuration;
using System.Drawing;
using System.IO;

namespace BusBus.UI
{
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
