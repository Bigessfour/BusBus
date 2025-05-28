// <auto-added>
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BusBus.Models;

namespace BusBus.UI.Common
{    /// <summary>
     /// Utility class for managing custom fields configuration
     /// </summary>
    public static class CustomFieldsManager
    {
        private static Dictionary<string, Dictionary<string, CustomField>>? _customFields;
        private static string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomFields.json");

        /// <summary>
        /// Gets or sets the configuration file path (mainly for testing)
        /// </summary>
        public static string ConfigPath
        {
            get => _configPath;
            set => _configPath = value;
        }

        /// <summary>
        /// Loads custom fields configuration from JSON file
        /// </summary>
        public static Dictionary<string, Dictionary<string, CustomField>> LoadCustomFields()
        {
            if (_customFields != null)
                return _customFields;

            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    _customFields = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, CustomField>>>(json)
                                  ?? new Dictionary<string, Dictionary<string, CustomField>>();
                }
                else
                {
                    _customFields = new Dictionary<string, Dictionary<string, CustomField>>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading custom fields: {ex.Message}");
                _customFields = new Dictionary<string, Dictionary<string, CustomField>>();
            }

            return _customFields;
        }

        /// <summary>
        /// Gets custom fields for a specific entity type
        /// </summary>
        public static Dictionary<string, CustomField> GetCustomFields(string entityType)
        {
            var allFields = LoadCustomFields();
            return allFields.TryGetValue(entityType, out var fields)
                ? fields
                : new Dictionary<string, CustomField>();
        }

        /// <summary>
        /// Reloads the custom fields configuration (useful for runtime updates)
        /// </summary>
        public static void ReloadCustomFields()
        {
            _customFields = null;
            LoadCustomFields();
        }
    }
}
