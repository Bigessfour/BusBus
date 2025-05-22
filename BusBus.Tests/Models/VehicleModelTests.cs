using BusBus.Models;
using System;
using Xunit;
using BusBus.Tests.Common;

namespace BusBus.Tests.Models
{
    public class VehicleModelTests
    {
        [Fact]
        public void Vehicle_Properties_Should_Be_Set_Correctly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var vehicle = new Vehicle
            {
                Id = id,
                BusNumber = "Bus 101"
            };

            // Act & Assert
            Assert.Equal(id, vehicle.Id);
            Assert.Equal("Bus 101", vehicle.BusNumber);
        }

        [Fact]
        public void Vehicle_ToString_Returns_Name()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                BusNumber = "Shuttle 202"
            };

            // Act
            string result = vehicle.ToString();

            // Assert
            Assert.Equal("Shuttle 202", result);
        }
    }
}