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
                Name = "John Doe",
                EmployeeId = "EMP12345",
                LicenseNumber = "DL9876543"
            };

            // Act & Assert
            Assert.Equal(id, driver.Id);
            Assert.Equal("John Doe", driver.Name);
            Assert.Equal("EMP12345", driver.EmployeeId);
            Assert.Equal("DL9876543", driver.LicenseNumber);
        }

        [Fact]
        public void Driver_ToString_Returns_Name()
        {
            // Arrange
            var driver = new Driver
            {
                Name = "Jane Smith"
            };

            // Act
            string result = driver.ToString();

            // Assert
            Assert.Equal("Jane Smith", result);
        }
    }
}