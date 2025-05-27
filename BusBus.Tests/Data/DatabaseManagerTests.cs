using System;
using System.Threading.Tasks;
using BusBus.Data;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace BusBus.Tests.Data
{
    [TestFixture]
    [Category(TestCategories.Database)]
    [Category(TestCategories.Integration)]
    public class DatabaseManagerTests : TestBase
    {
        private DatabaseManager _databaseManager;
        private ILogger<DatabaseManager> _logger;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _logger = ServiceProvider.GetRequiredService<ILogger<DatabaseManager>>();
            var configuration = ServiceProvider.GetRequiredService<IConfiguration>();

            // Create a database manager with in-memory settings
            _databaseManager = new DatabaseManager(configuration, _logger);
        }

        [Test]
        [Description("Test getting connection string from configuration")]
        public void GetConnectionString_WithValidConfig_ShouldReturnConnectionString()
        {
            // Act
            var connectionString = _databaseManager.GetConnectionString();

            // Assert
            connectionString.Should().NotBeNullOrEmpty();
            // In-memory connection string typically contains "InMemory"
            connectionString.Should().Contain("memory", "In-memory database should be used for tests");
        }

        [Test]
        [Description("Test checking database existence")]
        public async Task CheckDatabaseExistsAsync_WithInMemoryDb_ShouldReturnTrue()
        {
            // Act
            var exists = await _databaseManager.CheckDatabaseExistsAsync();

            // Assert
            exists.Should().BeTrue("In-memory database should always exist");
        }

        [Test]
        [Description("Test database initialization")]
        public async Task InitializeDatabaseAsync_ShouldSucceed()
        {
            // Act
            var result = await _databaseManager.InitializeDatabaseAsync();

            // Assert
            result.Should().BeTrue("Database initialization should succeed");
        }

        [Test]
        [Description("Test migrations application")]
        public async Task ApplyMigrationsAsync_ShouldSucceed()
        {
            // Act
            var result = await _databaseManager.ApplyMigrationsAsync();

            // Assert
            result.Should().BeTrue("Applying migrations should succeed for in-memory database");
        }

        [Test]
        [Description("Test getting database status")]
        public async Task GetDatabaseStatusAsync_ShouldReturnStatus()
        {
            // Act
            var status = await _databaseManager.GetDatabaseStatusAsync();

            // Assert
            status.Should().NotBeNull();
            status.IsConnected.Should().BeTrue();
            status.DatabaseExists.Should().BeTrue();
            status.HasTables.Should().BeTrue();
            status.ConnectionString.Should().NotBeNull();
            status.ServerVersion.Should().NotBeNull();
        }

        [Test]
        [Description("Test backup and restore operations")]
        public async Task BackupAndRestoreDatabase_ShouldSimulateSuccessfully()
        {
            // In-memory database can't actually be backed up/restored,
            // but we can test that the methods don't throw exceptions

            // Act & Assert - Backup
            Func<Task> backupAction = async () =>
                await _databaseManager.BackupDatabaseAsync("test_backup.bak");

            await backupAction.Should().NotThrowAsync();

            // Act & Assert - Restore
            Func<Task> restoreAction = async () =>
                await _databaseManager.RestoreDatabaseAsync("test_backup.bak");

            await restoreAction.Should().NotThrowAsync();
        }
    }
}
