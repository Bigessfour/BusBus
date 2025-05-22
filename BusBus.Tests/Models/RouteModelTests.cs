using BusBus.Models;
using System;
using Xunit;
using BusBus.Tests.Common;

namespace BusBus.Tests.Models
{
    public class RouteModelTests
    {
        [Fact]
        public void Route_Properties_Should_Be_Set_Correctly()
        {
            // Arrange
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Downtown Express",
                RouteDate = new DateTime(2025, 5, 21),
                AMStartingMileage = 10000,
                AMEndingMileage = 10050,
                AMRiders = 25,
                PMStartMileage = 10050,
                PMEndingMileage = 10100,
                PMRiders = 30
            };

            // Act & Assert
            Assert.Equal("Downtown Express", route.Name);
            Assert.Equal(new DateTime(2025, 5, 21), route.RouteDate);
            Assert.Equal(10000, route.AMStartingMileage);
            Assert.Equal(10050, route.AMEndingMileage);
            Assert.Equal(25, route.AMRiders);
            Assert.Equal(10050, route.PMStartMileage);
            Assert.Equal(10100, route.PMEndingMileage);
            Assert.Equal(30, route.PMRiders);
        }
    }
}