using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;

namespace BusBus.Config
{
    /// <summary>
    /// Configuration class for SQL database information
    /// </summary>
    public class SqlInformation
    {
        public DatabaseConfiguration DatabaseConfiguration { get; set; } = new();
        public DatabaseSchema DatabaseSchema { get; set; } = new();
        public List<string> MigrationsHistory { get; set; } = new();
        public SeedData SeedData { get; set; } = new();
        public AdvancedFeatures AdvancedFeatures { get; set; } = new();
    }

    public class DatabaseConfiguration
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Authentication { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
    }

    public class DatabaseSchema
    {
        public DatabaseTables Tables { get; set; } = new();
    }

    public class DatabaseTables
    {
        public TableInfo Routes { get; set; } = new();
        public TableInfo Drivers { get; set; } = new();
        public TableInfo Vehicles { get; set; } = new();
        public TableInfo CustomFields { get; set; } = new();
    }

    public class TableInfo
    {
        public string PrimaryKey { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public List<string> Relationships { get; set; } = new();
    }

    public class SeedData
    {
        public List<SeedDriver> Drivers { get; set; } = new();
        public List<SeedVehicle> Vehicles { get; set; } = new();
    }

    public class SeedDriver
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
    }

    public class SeedVehicle
    {
        public string Id { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AdvancedFeatures
    {
        public string JsonStorage { get; set; } = string.Empty;
        public string ComputedProperties { get; set; } = string.Empty;
        public string ConcurrencyControl { get; set; } = string.Empty;
        public string AuditFields { get; set; } = string.Empty;
    }

    /// <summary>
    /// Extension methods for loading SQL information from configuration
    /// </summary>
    public static class SqlInformationExtensions
    {
        public static SqlInformation GetSqlInformation(this IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var sqlInfo = new SqlInformation();
            configuration.GetSection("SqlInformation").Bind(sqlInfo);
            return sqlInfo;
        }
    }
}
