using System;

#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return

namespace BusBus.Models
{
    public class DatabaseStatus
    {
        public bool IsConnected { get; set; }
        public bool DatabaseExists { get; set; }
        public bool HasTables { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string ServerVersion { get; set; } = string.Empty;
        public DateTime CheckTime { get; set; } = DateTime.UtcNow;
    }
}
