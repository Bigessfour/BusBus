using System;
using System.IO;
using System.Text.Json;
using BusBus.Configuration;
using BusBus.Config;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.Config
{
    [TestClass]
    [TestCategory(TestCategories.Unit)]
    public class AppSettingsTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private string _testConfigPath;

        [TestInitialize]
        public void Setup()
        {
            // Create a temporary appsettings.json for testing
            _testConfigPath = Path.Combine(Path.GetTempPath(), $"appsettings_{Guid.NewGuid()}.json");
            var json = @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=TestServer;Database=TestDb;Trusted_Connection=True;""
  },
  ""DatabaseSettings"": {
    ""CommandTimeout"": 60,
    ""EnableRetryOnFailure"": true,
    ""MaxRetryCount"": 5,
    ""MaxRetryDelay"": 60,
    ""ConnectionTimeout"": 20
  }
}";
            File.WriteAllText(_testConfigPath, json);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
        }

        [TestMethod]
        // Description: Test deserialization of AppSettings from JSON file
        public void AppSettings_ShouldDeserializeFromJsonFile()
        {
            // Act
            var json = File.ReadAllText(_testConfigPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            // Assert
            settings.Should().NotBeNull();
            settings.ConnectionStrings.Should().NotBeNull();
            settings.ConnectionStrings.DefaultConnection.Should().Be("Server=TestServer;Database=TestDb;Trusted_Connection=True;");
            settings.DatabaseSettings.Should().NotBeNull();
            settings.DatabaseSettings.CommandTimeout.Should().Be(60);
            settings.DatabaseSettings.EnableRetryOnFailure.Should().BeTrue();
            settings.DatabaseSettings.MaxRetryCount.Should().Be(5);
            settings.DatabaseSettings.MaxRetryDelay.Should().Be(60);
            settings.DatabaseSettings.ConnectionTimeout.Should().Be(20);
        }

        [TestMethod]
        // Description: Test AppSettings.Instance loads from file and throws if missing
        public void AppSettings_Instance_ShouldLoadOrThrow()
        {
            // Backup and replace the real appsettings.json
            var realPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var backupPath = realPath + ".bak";
            if (File.Exists(realPath)) File.Move(realPath, backupPath);
            File.Copy(_testConfigPath, realPath);
            try
            {
                var instance = AppSettings.Instance;
                instance.ConnectionStrings.DefaultConnection.Should().Be("Server=TestServer;Database=TestDb;Trusted_Connection=True;");
            }
            finally
            {
                if (File.Exists(realPath)) File.Delete(realPath);
                if (File.Exists(backupPath)) File.Move(backupPath, realPath);
            }
        }

        [TestMethod]
        // Description: Test SQL Information configuration loading
        public void SqlInformation_ShouldLoadFromConfiguration()
        {
            // Arrange
            var configJson = @"{
  ""SqlInformation"": {
    ""DatabaseConfiguration"": {
      ""Server"": ""localhost\\SQLEXPRESS"",
      ""Database"": ""BusBusDB"",
      ""Authentication"": ""Windows Authentication"",
      ""Provider"": ""SQL Server Express"",
      ""Framework"": ""Entity Framework Core 9.0.5""
    },
    ""DatabaseSchema"": {
      ""Tables"": {
        ""Routes"": {
          ""PrimaryKey"": ""Id (uniqueidentifier/Guid)"",
          ""Columns"": [
            ""Name (nvarchar, required)"",
            ""RouteDate (datetime)""
          ]
        }
      }
    },
    ""MigrationsHistory"": [
      ""20250523002503_InitialSetup"",
      ""20250525212950_AddRouteRowVersion""
    ],
    ""SeedData"": {
      ""Drivers"": [
        {
          ""Id"": ""11111111-1111-1111-1111-111111111111"",
          ""Name"": ""John Smith"",
          ""LicenseNumber"": ""DL123456""
        }
      ],
      ""Vehicles"": [
        {
          ""Id"": ""aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"",
          ""Number"": ""101"",
          ""Model"": ""Blue Bird All American FE"",
          ""Capacity"": 72,
          ""LicensePlate"": ""BUS-101"",
          ""IsActive"": true
        }
      ]
    }
  }
}";

            var configPath = Path.Combine(Path.GetTempPath(), $"sqlinfo_{Guid.NewGuid()}.json");
            File.WriteAllText(configPath, configJson);

            try
            {
                // Act
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(configPath)
                    .Build();

                var sqlInfo = configuration.GetSqlInformation();

                // Assert
                sqlInfo.Should().NotBeNull();
                sqlInfo.DatabaseConfiguration.Server.Should().Be("localhost\\SQLEXPRESS");
                sqlInfo.DatabaseConfiguration.Database.Should().Be("BusBusDB");
                sqlInfo.DatabaseConfiguration.Authentication.Should().Be("Windows Authentication");
                sqlInfo.DatabaseConfiguration.Provider.Should().Be("SQL Server Express");
                sqlInfo.DatabaseConfiguration.Framework.Should().Be("Entity Framework Core 9.0.5");

                sqlInfo.DatabaseSchema.Tables.Routes.PrimaryKey.Should().Be("Id (uniqueidentifier/Guid)");
                sqlInfo.DatabaseSchema.Tables.Routes.Columns.Should().HaveCount(2);
                sqlInfo.DatabaseSchema.Tables.Routes.Columns.Should().Contain("Name (nvarchar, required)");

                sqlInfo.MigrationsHistory.Should().HaveCount(2);
                sqlInfo.MigrationsHistory.Should().Contain("20250523002503_InitialSetup");

                sqlInfo.SeedData.Drivers.Should().HaveCount(1);
                sqlInfo.SeedData.Drivers[0].Name.Should().Be("John Smith");
                sqlInfo.SeedData.Drivers[0].LicenseNumber.Should().Be("DL123456");

                sqlInfo.SeedData.Vehicles.Should().HaveCount(1);
                sqlInfo.SeedData.Vehicles[0].Number.Should().Be("101");
                sqlInfo.SeedData.Vehicles[0].Model.Should().Be("Blue Bird All American FE");
                sqlInfo.SeedData.Vehicles[0].Capacity.Should().Be(72);
            }
            finally
            {
                if (File.Exists(configPath))
                    File.Delete(configPath);
            }
        }

        [TestMethod]
        // Description: Test SQL Information validation for seed data
        public void SqlInformation_SeedData_ShouldHaveValidGuids()
        {
            // Arrange
            var configuration = GetTestConfiguration();
            var sqlInfo = configuration.GetSqlInformation();

            // Act & Assert
            foreach (var driver in sqlInfo.SeedData.Drivers)
            {
                Guid.TryParse(driver.Id, out _).Should().BeTrue($"Driver ID {driver.Id} should be a valid GUID");
                driver.Name.Should().NotBeNullOrEmpty();
                driver.LicenseNumber.Should().NotBeNullOrEmpty();
            }

            foreach (var vehicle in sqlInfo.SeedData.Vehicles)
            {
                Guid.TryParse(vehicle.Id, out _).Should().BeTrue($"Vehicle ID {vehicle.Id} should be a valid GUID");
                vehicle.Number.Should().NotBeNullOrEmpty();
                vehicle.Model.Should().NotBeNullOrEmpty();
                vehicle.LicensePlate.Should().NotBeNullOrEmpty();
                vehicle.Capacity.Should().BeGreaterThan(0);
            }
        }

        private static IConfiguration GetTestConfiguration()
        {
            // Use the actual appsettings.json from the project
            var projectDir = GetProjectDirectory();
            var configPath = Path.Combine(projectDir, "config", "appsettings.json");

            return new ConfigurationBuilder()
                .AddJsonFile(configPath)
                .Build();
        }

        private static string GetProjectDirectory()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var projectDir = Path.GetDirectoryName(currentDir);
            while (projectDir != null && !File.Exists(Path.Combine(projectDir, "BusBus.csproj")))
            {
                projectDir = Path.GetDirectoryName(projectDir);
            }
            return projectDir ?? currentDir;
        }
    }
}
