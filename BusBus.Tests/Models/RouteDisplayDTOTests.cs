using BusBus.Models;
using System;
using Xunit;

namespace BusBus.Tests.Models
{
    public class RouteDisplayDTOTests
    {
        [Fact]
        public void FromRoute_MapsPropertiesCorrectly()
        {
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 100,
                AMEndingMileage = 200,
                AMRiders = 10,
                PMStartMileage = 200,
                PMEndingMileage = 300,
                PMRiders = 15,
                Driver = new Driver { FirstName = "Alice", LastName = "Smith" },
                Vehicle = new Vehicle { BusNumber = "Bus 1" }
            };
            var dto = RouteDisplayDTO.FromRoute(route);
            Assert.Equal(route.Id, dto.Id);
            Assert.Equal("Test Route", dto.Name);
            Assert.Equal("Alice Smith", dto.DriverName);
            Assert.Equal("Bus 1", dto.VehicleName);
        }

        [Fact]
        public void FromRoute_NullDriverOrVehicle_UsesUnassigned()
        {
            var route = new Route { Name = "R", Driver = null, Vehicle = null };
            var dto = RouteDisplayDTO.FromRoute(route);
            Assert.Equal("Unassigned", dto.DriverName);
            Assert.Equal("Unassigned", dto.VehicleName);
        }
    }
}
