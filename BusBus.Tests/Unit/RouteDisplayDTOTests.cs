using System;
using NUnit.Framework;
using BusBus.Models;
using BusBus.Tests;

namespace BusBus.Tests.Unit
{
    [TestFixture]
    [Category(TestCategories.Unit)]
    public class RouteDisplayDTOTests
    {
        #region FromRoute Method Tests

        [Test]
        public void FromRoute_WithValidRoute_ShouldMapAllProperties()
        {
            // Arrange
            var driver = new Driver 
            { 
                Id = Guid.NewGuid(), 
                FirstName = "John", 
                LastName = "Doe" 
            };
            var vehicle = new Vehicle 
            { 
                Id = Guid.NewGuid(), 
                BusNumber = "BUS001",
                Number = "BUS001"  // Required property
            };
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Downtown Route",
                RouteDate = new DateTime(2023, 12, 15),
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                AMRiders = 25,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                PMRiders = 30,
                DriverId = driver.Id,
                Driver = driver,
                VehicleId = vehicle.Id,
                Vehicle = vehicle
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.Id, Is.EqualTo(route.Id));
            Assert.That(dto.Name, Is.EqualTo(route.Name));
            Assert.That(dto.RouteDate, Is.EqualTo(route.RouteDate));
            Assert.That(dto.AMStartingMileage, Is.EqualTo(route.AMStartingMileage));
            Assert.That(dto.AMEndingMileage, Is.EqualTo(route.AMEndingMileage));
            Assert.That(dto.AMRiders, Is.EqualTo(route.AMRiders));
            Assert.That(dto.PMStartMileage, Is.EqualTo(route.PMStartMileage));
            Assert.That(dto.PMEndingMileage, Is.EqualTo(route.PMEndingMileage));
            Assert.That(dto.PMRiders, Is.EqualTo(route.PMRiders));
            Assert.That(dto.DriverId, Is.EqualTo(route.DriverId));
            Assert.That(dto.VehicleId, Is.EqualTo(route.VehicleId));
            Assert.That(dto.DriverName, Is.EqualTo("John Doe"));
            Assert.That(dto.VehicleName, Is.EqualTo("BUS001"));
        }

        [Test]
        public void FromRoute_WithNullDriver_ShouldSetDriverNameToUnassigned()
        {
            // Arrange
            var vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "BUS001", Number = "BUS001" };
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                Driver = null,
                DriverId = null,
                Vehicle = vehicle,
                VehicleId = vehicle.Id
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.DriverName, Is.EqualTo("Unassigned"));
            Assert.That(dto.DriverId, Is.Null);
        }

        [Test]
        public void FromRoute_WithNullVehicle_ShouldSetVehicleNameToUnassigned()
        {
            // Arrange
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith" };
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                Driver = driver,
                DriverId = driver.Id,
                Vehicle = null,
                VehicleId = null
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.VehicleName, Is.EqualTo("Unassigned"));
            Assert.That(dto.VehicleId, Is.Null);
        }

        [Test]
        public void FromRoute_WithDriverHavingEmptyLastName_ShouldTrimCorrectly()
        {
            // Arrange
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "" };
            var vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "BUS002", Number = "BUS002" };
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                Driver = driver,
                DriverId = driver.Id,
                Vehicle = vehicle,
                VehicleId = vehicle.Id
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.DriverName, Is.EqualTo("Jane"));
        }

        [Test]
        public void FromRoute_WithDriverHavingEmptyFirstName_ShouldTrimCorrectly()
        {
            // Arrange
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "", LastName = "Smith" };
            var vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "BUS003", Number = "BUS003" };
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                Driver = driver,
                DriverId = driver.Id,
                Vehicle = vehicle,
                VehicleId = vehicle.Id
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.DriverName, Is.EqualTo("Smith"));
        }

        [Test]
        public void FromRoute_WithDriverHavingWhitespaceNames_ShouldTrimCorrectly()
        {
            // Arrange
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "  John  ", LastName = "  Doe  " };
            var vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "BUS004", Number = "BUS004" };
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                Driver = driver,
                DriverId = driver.Id,
                Vehicle = vehicle,
                VehicleId = vehicle.Id
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.DriverName, Is.EqualTo("John Doe"));
        }        [Test]
        public void FromRoute_WithVehicleHavingEmptyBusNumber_ShouldSetVehicleNameToUnassigned()
        {
            // Arrange
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "Test", LastName = "Driver" };
            var vehicle = new Vehicle { Id = Guid.NewGuid(), Number = "" }; // Empty number
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                Driver = driver,
                DriverId = driver.Id,
                Vehicle = vehicle,
                VehicleId = vehicle.Id
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.VehicleName, Is.EqualTo("Unassigned"));
        }

        [Test]
        public void FromRoute_WithNullRoute_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => RouteDisplayDTO.FromRoute(null!));
        }

        [Test]
        public void FromRoute_WithZeroMileageValues_ShouldMapCorrectly()
        {
            // Arrange
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "Zero", LastName = "Mileage" };
            var vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "BUS006", Number = "BUS006" };
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Zero Mileage Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 0,
                AMEndingMileage = 0,
                AMRiders = 0,
                PMStartMileage = 0,
                PMEndingMileage = 0,
                PMRiders = 0,
                Driver = driver,
                DriverId = driver.Id,
                Vehicle = vehicle,
                VehicleId = vehicle.Id
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.AMStartingMileage, Is.EqualTo(0));
            Assert.That(dto.AMEndingMileage, Is.EqualTo(0));
            Assert.That(dto.AMRiders, Is.EqualTo(0));
            Assert.That(dto.PMStartMileage, Is.EqualTo(0));
            Assert.That(dto.PMEndingMileage, Is.EqualTo(0));
            Assert.That(dto.PMRiders, Is.EqualTo(0));
        }

        [Test]
        public void FromRoute_WithMaximumMileageValues_ShouldMapCorrectly()
        {
            // Arrange
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "Max", LastName = "Mileage" };
            var vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "BUS007", Number = "BUS007" };
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "High Mileage Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 999999,
                AMEndingMileage = 999999,
                AMRiders = 9999,
                PMStartMileage = 999999,
                PMEndingMileage = 999999,
                PMRiders = 9999,
                Driver = driver,
                DriverId = driver.Id,
                Vehicle = vehicle,
                VehicleId = vehicle.Id
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.AMStartingMileage, Is.EqualTo(999999));
            Assert.That(dto.AMEndingMileage, Is.EqualTo(999999));
            Assert.That(dto.AMRiders, Is.EqualTo(9999));
            Assert.That(dto.PMStartMileage, Is.EqualTo(999999));
            Assert.That(dto.PMEndingMileage, Is.EqualTo(999999));
            Assert.That(dto.PMRiders, Is.EqualTo(9999));
        }

        #endregion

        #region ToRoute Method Tests

        [Test]
        public void ToRoute_WithValidDTO_ShouldMapAllProperties()
        {
            // Arrange
            var dto = new RouteDisplayDTO
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = new DateTime(2023, 12, 15),
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                AMRiders = 25,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                PMRiders = 30,
                DriverId = Guid.NewGuid(),
                VehicleId = Guid.NewGuid(),
                DriverName = "John Doe",
                VehicleName = "BUS001"
            };

            // Act
            var route = dto.ToRoute();

            // Assert
            Assert.That(route.Id, Is.EqualTo(dto.Id));
            Assert.That(route.Name, Is.EqualTo(dto.Name));
            Assert.That(route.RouteDate, Is.EqualTo(dto.RouteDate));
            Assert.That(route.AMStartingMileage, Is.EqualTo(dto.AMStartingMileage));
            Assert.That(route.AMEndingMileage, Is.EqualTo(dto.AMEndingMileage));
            Assert.That(route.AMRiders, Is.EqualTo(dto.AMRiders));
            Assert.That(route.PMStartMileage, Is.EqualTo(dto.PMStartMileage));
            Assert.That(route.PMEndingMileage, Is.EqualTo(dto.PMEndingMileage));
            Assert.That(route.PMRiders, Is.EqualTo(dto.PMRiders));
            Assert.That(route.DriverId, Is.EqualTo(dto.DriverId));
            Assert.That(route.VehicleId, Is.EqualTo(dto.VehicleId));
            // Navigation properties should be null when converting from DTO
            Assert.That(route.Driver, Is.Null);
            Assert.That(route.Vehicle, Is.Null);
        }

        [Test]
        public void ToRoute_WithNullIds_ShouldMapCorrectly()
        {
            // Arrange
            var dto = new RouteDisplayDTO
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                DriverId = null,
                VehicleId = null,
                DriverName = "Unassigned",
                VehicleName = "Unassigned"
            };

            // Act
            var route = dto.ToRoute();

            // Assert
            Assert.That(route.DriverId, Is.Null);
            Assert.That(route.VehicleId, Is.Null);
            Assert.That(route.Driver, Is.Null);
            Assert.That(route.Vehicle, Is.Null);
        }

        [Test]
        public void ToRoute_WithEmptyName_ShouldMapCorrectly()
        {
            // Arrange
            var dto = new RouteDisplayDTO
            {
                Id = Guid.NewGuid(),
                Name = "",
                RouteDate = DateTime.Today
            };

            // Act
            var route = dto.ToRoute();

            // Assert
            Assert.That(route.Name, Is.EqualTo(""));
        }

        [Test]
        public void ToRoute_WithZeroValues_ShouldMapCorrectly()
        {
            // Arrange
            var dto = new RouteDisplayDTO
            {
                Id = Guid.NewGuid(),
                Name = "Zero Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 0,
                AMEndingMileage = 0,
                AMRiders = 0,
                PMStartMileage = 0,
                PMEndingMileage = 0,
                PMRiders = 0
            };

            // Act
            var route = dto.ToRoute();

            // Assert
            Assert.That(route.AMStartingMileage, Is.EqualTo(0));
            Assert.That(route.AMEndingMileage, Is.EqualTo(0));
            Assert.That(route.AMRiders, Is.EqualTo(0));
            Assert.That(route.PMStartMileage, Is.EqualTo(0));
            Assert.That(route.PMEndingMileage, Is.EqualTo(0));
            Assert.That(route.PMRiders, Is.EqualTo(0));
        }

        #endregion

        #region Round-trip Conversion Tests

        [Test]
        public void FromRoute_ToRoute_RoundTrip_ShouldPreserveEssentialData()
        {
            // Arrange
            var originalRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Round Trip Route",
                RouteDate = new DateTime(2023, 12, 15),
                AMStartingMileage = 1500,
                AMEndingMileage = 1550,
                AMRiders = 35,
                PMStartMileage = 1550,
                PMEndingMileage = 1600,
                PMRiders = 40,
                DriverId = Guid.NewGuid(),
                VehicleId = Guid.NewGuid()
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(originalRoute);
            var convertedRoute = dto.ToRoute();

            // Assert - Essential data should be preserved
            Assert.That(convertedRoute.Id, Is.EqualTo(originalRoute.Id));
            Assert.That(convertedRoute.Name, Is.EqualTo(originalRoute.Name));
            Assert.That(convertedRoute.RouteDate, Is.EqualTo(originalRoute.RouteDate));
            Assert.That(convertedRoute.AMStartingMileage, Is.EqualTo(originalRoute.AMStartingMileage));
            Assert.That(convertedRoute.AMEndingMileage, Is.EqualTo(originalRoute.AMEndingMileage));
            Assert.That(convertedRoute.AMRiders, Is.EqualTo(originalRoute.AMRiders));
            Assert.That(convertedRoute.PMStartMileage, Is.EqualTo(originalRoute.PMStartMileage));
            Assert.That(convertedRoute.PMEndingMileage, Is.EqualTo(originalRoute.PMEndingMileage));
            Assert.That(convertedRoute.PMRiders, Is.EqualTo(originalRoute.PMRiders));
            Assert.That(convertedRoute.DriverId, Is.EqualTo(originalRoute.DriverId));
            Assert.That(convertedRoute.VehicleId, Is.EqualTo(originalRoute.VehicleId));
        }

        [Test]
        public void FromRoute_ToRoute_RoundTrip_WithNavigationProperties_ShouldPreserveIds()
        {
            // Arrange
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "Round", LastName = "Trip" };
            var vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "BUS-RT", Number = "BUS-RT" };
            var originalRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Navigation Test Route",
                RouteDate = DateTime.Today,
                DriverId = driver.Id,
                Driver = driver,
                VehicleId = vehicle.Id,
                Vehicle = vehicle
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(originalRoute);
            var convertedRoute = dto.ToRoute();

            // Assert - IDs should be preserved, but navigation properties will be null
            Assert.That(convertedRoute.DriverId, Is.EqualTo(originalRoute.DriverId));
            Assert.That(convertedRoute.VehicleId, Is.EqualTo(originalRoute.VehicleId));
            Assert.That(convertedRoute.Driver, Is.Null);
            Assert.That(convertedRoute.Vehicle, Is.Null);

            // DTO should have the display names
            Assert.That(dto.DriverName, Is.EqualTo("Round Trip"));
            Assert.That(dto.VehicleName, Is.EqualTo("BUS-RT"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void FromRoute_WithRealisticBusRouteData_ShouldMapCorrectly()
        {
            // Arrange - Realistic bus route scenario
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "Michael", LastName = "Johnson" };
            var vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "BUS-042", Number = "BUS-042" };
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Elementary School Route 15",
                RouteDate = new DateTime(2023, 11, 15),
                AMStartingMileage = 45623,
                AMEndingMileage = 45655,
                AMRiders = 28,
                PMStartMileage = 45655,
                PMEndingMileage = 45687,
                PMRiders = 28,
                DriverId = driver.Id,
                Driver = driver,
                VehicleId = vehicle.Id,
                Vehicle = vehicle
            };

            // Act
            var dto = RouteDisplayDTO.FromRoute(route);

            // Assert
            Assert.That(dto.Id, Is.EqualTo(route.Id));
            Assert.That(dto.Name, Is.EqualTo("Elementary School Route 15"));
            Assert.That(dto.RouteDate, Is.EqualTo(new DateTime(2023, 11, 15)));
            Assert.That(dto.AMStartingMileage, Is.EqualTo(45623));
            Assert.That(dto.AMEndingMileage, Is.EqualTo(45655));
            Assert.That(dto.AMRiders, Is.EqualTo(28));
            Assert.That(dto.PMStartMileage, Is.EqualTo(45655));
            Assert.That(dto.PMEndingMileage, Is.EqualTo(45687));
            Assert.That(dto.PMRiders, Is.EqualTo(28));
            Assert.That(dto.AMEndingMileage - dto.AMStartingMileage, Is.EqualTo(32)); // AM distance
            Assert.That(dto.PMEndingMileage - dto.PMStartMileage, Is.EqualTo(32));    // PM distance
            Assert.That(dto.DriverName, Is.EqualTo("Michael Johnson"));
            Assert.That(dto.VehicleName, Is.EqualTo("BUS-042"));
        }

        #endregion
    }
}
