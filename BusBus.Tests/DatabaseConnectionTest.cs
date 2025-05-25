using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BusBus.Tests.Common;

namespace BusBus.Tests.IntegrationTests
{
    [TestFixture]
    public class DatabaseConnectionTest : TestBase
    {
        [Test]
        public async Task CanConnectToDatabase()
        {
            // Arrange
            var context = GetDbContext();
            
            // Act & Assert - For InMemory database, just verify we can access it
            Assert.That(context, Is.Not.Null);
            var canConnect = await context.Database.CanConnectAsync();
            Assert.That(canConnect, Is.True);
            
            // Verify we can query the database
            var drivers = await context.Drivers.ToListAsync();
            Assert.That(drivers, Is.Not.Null);
        }

        [Test]
        public void DatabaseHasCorrectTables()
        {
            var context = GetDbContext();
            
            // This will throw if tables don't exist
            var routeCount = context.Routes.Count();
            var driverCount = context.Drivers.Count();
            
            Assert.That(routeCount, Is.GreaterThanOrEqualTo(0), "Routes table should exist");
            Assert.That(driverCount, Is.GreaterThanOrEqualTo(0), "Drivers table should exist");
        }
        
        [Test]
        public void DatabaseProviderIsInMemory()
        {
            // Arrange
            var context = GetDbContext();
            
            // Act
            var providerName = context.Database.ProviderName;
            
            // Assert - Verify we're using InMemory database
            Assert.That(providerName, Is.EqualTo("Microsoft.EntityFrameworkCore.InMemory"),
                "Should be using InMemory database provider for tests");
        }
    }
}
