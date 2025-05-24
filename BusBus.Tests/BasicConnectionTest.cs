using NUnit.Framework;
using BusBus.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace BusBus.Tests
{
    [TestFixture]
    public class BasicConnectionTest
    {
        [Test, Timeout(5000)]
        public void CanCreateDbContext_WithTestConfiguration()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                using var context = new AppDbContext(optionsBuilder.Options);
                Assert.IsNotNull(context);
            });
        }
    }
}
