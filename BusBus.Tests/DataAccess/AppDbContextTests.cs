using BusBus.DataAccess;
using BusBus.Models;
using BusBus.Tests.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BusBus.Tests.DataAccess
{
    public class AppDbContextTests
    {
        [Fact]
        public void CreateDbContext_Success()
        {
            // Arrange & Act
            using var context = TestHelper.CreateInMemoryDbContext();

            // Assert
            Assert.NotNull(context);
        }

        [Fact]
        public async Task Routes_CanBeAddedAndRetrieved()
        {
            // Arrange
            using var context = TestHelper.CreateInMemoryDbContext();
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today
            };

            // Act
            context.Routes.Add(route);
            await context.SaveChangesAsync();

            // Assert
            var retrievedRoute = await context.Routes.FirstOrDefaultAsync(r => r.Id == route.Id);
            Assert.NotNull(retrievedRoute);
            Assert.Equal(route.Name, retrievedRoute.Name);
            Assert.Equal(route.RouteDate, retrievedRoute.RouteDate);
        }

        [Fact]
        public async Task Drivers_CanBeAddedAndRetrieved()
        {
            // Arrange
            using var context = TestHelper.CreateInMemoryDbContext();
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                Name = "John Doe",
                EmployeeId = "EMP12345",
                LicenseNumber = "DL9876543"
            };

            // Act
            context.Drivers.Add(driver);
            await context.SaveChangesAsync();

            // Assert
            var retrievedDriver = await context.Drivers.FirstOrDefaultAsync(d => d.Id == driver.Id);
            Assert.NotNull(retrievedDriver);
            Assert.Equal(driver.Name, retrievedDriver.Name);
            Assert.Equal(driver.EmployeeId, retrievedDriver.EmployeeId);
            Assert.Equal(driver.LicenseNumber, retrievedDriver.LicenseNumber);
        }

        [Fact]
        public async Task Vehicles_CanBeAddedAndRetrieved()
        {
            // Arrange
            using var context = TestHelper.CreateInMemoryDbContext();
            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Name = "Bus 101",
                RegistrationNumber = "REG12345",
                Capacity = 45
            };

            // Act
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            // Assert
            var retrievedVehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);
            Assert.NotNull(retrievedVehicle);
            Assert.Equal(vehicle.Name, retrievedVehicle.Name);
            Assert.Equal(vehicle.RegistrationNumber, retrievedVehicle.RegistrationNumber);
            Assert.Equal(vehicle.Capacity, retrievedVehicle.Capacity);
        }
    }
}