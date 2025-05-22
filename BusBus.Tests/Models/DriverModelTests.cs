using BusBus.Models;
using System;
using Xunit;
using BusBus.Tests.Common;

namespace BusBus.Tests.Models
{
    public class DriverModelTests
    {
        [Fact]
        public void Driver_Properties_Should_Be_Set_Correctly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var driver = new Driver
            {
                Id = id,
                FirstName = "John",
                LastName = "Doe"
            };

            // Act & Assert
            Assert.Equal(id, driver.Id);
            Assert.Equal("John", driver.FirstName);
            Assert.Equal("Doe", driver.LastName);
        }

        [Fact]
        public void Driver_ToString_Returns_Name()
        {
            // Arrange
            var driver = new Driver
            {
                FirstName = "Jane",
                LastName = "Smith"
            };

            // Act
            string result = driver.ToString();

            // Assert
            Assert.Equal("Jane Smith", result);
        }
    }
}