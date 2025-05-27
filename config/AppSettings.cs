#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System;
using System.IO;
using System.Text.Json;

namespace BusBus.Configuration
{
    public class AppSettings
    {
        public ConnectionStrings ConnectionStrings { get; set; }
        public DatabaseSettings DatabaseSettings { get; set; }

        private static AppSettings _instance;
        private static readonly object _lock = new object();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = LoadSettings();
                        }
                    }
                }
                return _instance;
            }
        }

        private static AppSettings LoadSettings()
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            if (!File.Exists(settingsPath))
            {
                throw new FileNotFoundException($"Settings file not found: {settingsPath}");
            }

            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
        }
    }

    public class ConnectionStrings
    {
        public string DefaultConnection { get; set; }
    }

    public class DatabaseSettings
    {
        public int CommandTimeout { get; set; } = 30;
        public bool EnableRetryOnFailure { get; set; } = true;
        public int MaxRetryCount { get; set; } = 3;
        public int MaxRetryDelay { get; set; } = 30;
        public int ConnectionTimeout { get; set; } = 15;
    }
}
