#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System;
using System.Collections.Generic;

namespace BusBus.Models
{
    public class MigrationStatus
    {
        public List<string> PendingMigrations { get; set; } = new List<string>();
        public List<string> AppliedMigrations { get; set; } = new List<string>();
        public DateTime? LastMigrationDate { get; set; }
    }

    public class DatabaseSizeInfo
    {
        public string DatabaseName { get; set; } = string.Empty;
        public decimal DataFileSize { get; set; }
        public decimal LogFileSize { get; set; }
        public decimal TotalSize { get; set; }
    }

    public class TableStatistics
    {
        public string TableName { get; set; } = string.Empty;
        public long RowCount { get; set; }
        public long DataSize { get; set; }
    }

    public class ConnectionStringComponents
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public bool IntegratedSecurity { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    public class DatabaseIndex
    {
        public string IndexName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string IndexType { get; set; } = string.Empty;
    }

    public class DatabaseHealth
    {
        public bool IsHealthy { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }
}
