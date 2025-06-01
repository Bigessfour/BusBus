using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.DataAccess
{
    [TestClass]
    public class AppDbContextTests
    {
        [TestMethod]
        public void CreateDbContext_ReturnsDbContext_WhenConnectionStringExists()
        {
            // Arrange
            var factory = new BusBus.DataAccess.AppDbContextFactory();

            // Act
            var context = factory.CreateDbContext(new string[0]);

            // Assert
            Assert.IsNotNull(context);
            Assert.IsInstanceOfType(context, typeof(BusBus.DataAccess.AppDbContext));
        }

        [TestMethod]
        public void CreateDbContext_Throws_WhenConnectionStringMissing()
        {
            // Arrange
            var factory = new BusBus.DataAccess.AppDbContextFactory();

            // Temporarily rename appsettings.json to simulate missing connection string
            var configPath = "appsettings.json";
            var backupPath = "appsettings.json.bak";
            if (System.IO.File.Exists(configPath))
                System.IO.File.Move(configPath, backupPath);

            try
            {
                // Act & Assert
                Assert.ThrowsException<System.InvalidOperationException>(() => factory.CreateDbContext(new string[0]));
            }
            finally
            {
                // Restore appsettings.json
                if (System.IO.File.Exists(backupPath))
                    System.IO.File.Move(backupPath, configPath);
            }
        }
    }
}
