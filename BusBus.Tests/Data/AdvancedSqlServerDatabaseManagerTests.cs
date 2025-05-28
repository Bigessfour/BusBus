using System;
using System.Threading.Tasks;
using BusBus.Data;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.Data
{
    [TestClass]
    [TestCategory(TestCategories.Database)]
    [TestCategory(TestCategories.Integration)]
    public class AdvancedSqlServerDatabaseManagerTests : TestBase
    {
        private AdvancedSqlServerDatabaseManager _databaseManager;
        private ILogger<AdvancedSqlServerDatabaseManager> _logger;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();
            _logger = ServiceProvider.GetRequiredService<ILogger<AdvancedSqlServerDatabaseManager>>();
            var configuration = ServiceProvider.GetRequiredService<IConfiguration>();

            // Create a database manager with in-memory settings
            _databaseManager = new AdvancedSqlServerDatabaseManager(configuration, _logger);
        }        [TestMethod]
        // Description: Test database migration status check
        public async Task CheckMigrationStatus_ShouldReturnMigrationInfo()
        {
            // Act
            var status = await AdvancedSqlServerDatabaseManager.CheckMigrationStatusAsync();

            // Assert
            status.Should().NotBeNull();
            status.PendingMigrations.Should().NotBeNull();
            status.AppliedMigrations.Should().NotBeNull();
        }        [TestMethod]
        // Description: Test getting database size and growth information
        public async Task GetDatabaseSizeInfo_ShouldReturnSizeInformation()
        {
            // Act
            var sizeInfo = await AdvancedSqlServerDatabaseManager.GetDatabaseSizeInfoAsync();

            // Assert
            sizeInfo.Should().NotBeNull();
            sizeInfo.DatabaseName.Should().NotBeNullOrEmpty();
            sizeInfo.DataFileSize.Should().BeGreaterOrEqualTo(0);
            sizeInfo.LogFileSize.Should().BeGreaterOrEqualTo(0);
        }        [TestMethod]
        // Description: Test getting table statistics
        public async Task GetTableStatistics_ShouldReturnTableInfo()
        {
            // Act
            var tableStats = await AdvancedSqlServerDatabaseManager.GetTableStatisticsAsync();

            // Assert
            tableStats.Should().NotBeNull();
            tableStats.Should().ContainKey("Routes");
            tableStats.Should().ContainKey("Drivers");
            tableStats.Should().ContainKey("Vehicles");
        }        [TestMethod]
        // Description: Test database connection string parsing
        public void ParseConnectionString_ShouldExtractDatabaseComponents()
        {
            // Arrange
            string testConnString = "Server=localhost\\SQLEXPRESS;Database=BusBusDb;Trusted_Connection=True;";

            // Act
            var components = AdvancedSqlServerDatabaseManager.ParseConnectionString(testConnString);

            // Assert
            components.Should().NotBeNull();
            components.Server.Should().Be("localhost\\SQLEXPRESS");
            components.Database.Should().Be("BusBusDb");
            components.IntegratedSecurity.Should().BeTrue();
        }

        [TestMethod]
        // Description: Test getting database indexes
        public async Task GetDatabaseIndexes_ShouldReturnIndexInformation()
        {
            // Act
            var indexes = await AdvancedSqlServerDatabaseManager.GetDatabaseIndexesAsync();

            // Assert
            indexes.Should().NotBeNull();
            // At minimum, we should have primary key indexes
            indexes.Should().Contain(i => i.IndexType.Contains("PRIMARY"));
        }

        [TestMethod]
        // Description: Test checking database health
        public async Task CheckDatabaseHealth_ShouldReturnHealthStatus()
        {
            // Act
            var health = await AdvancedSqlServerDatabaseManager.CheckDatabaseHealthAsync();

            // Assert
            health.Should().NotBeNull();
            health.IsHealthy.Should().BeTrue(); // In-memory DB should be healthy
            health.Metrics.Should().NotBeNull();
            health.Recommendations.Should().NotBeNull();
        }
    }
}
