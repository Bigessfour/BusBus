using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using BusBus.DataAccess;
using BusBus.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BusBus.Tests.DataAccess
// Suppress nullable warnings for test code where context is guaranteed by setup
#pragma warning disable CS8602, CS8604, CS8600, CS8601, CS8629
{
    [TestFixture]
    public class AppDbContextTests : TestBase
    {
        // Remove shadowing field, use _context from TestBase

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            Assert.IsNotNull(_context, "_context should be initialized in TestBase.SetUp");
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [Test]
        public void AppDbContext_ShouldCreateAllDbSets()
        {
            // Assert
            Assert.IsNotNull(_context);
            Assert.That(_context.Routes, Is.Not.Null, "Routes DbSet should be created");
            Assert.That(_context.Drivers, Is.Not.Null, "Drivers DbSet should be created");
            Assert.That(_context.Vehicles, Is.Not.Null, "Vehicles DbSet should be created");
        }

        [Test]
        public async Task AppDbContext_ShouldSaveAndRetrieveDriver()
        {
            // Arrange
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            Assert.IsNotNull(_context);
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            // Assert
            var savedDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driver.Id);
            Assert.IsNotNull(savedDriver);
            Assert.That(savedDriver!.FirstName, Is.EqualTo("John"));
            Assert.That(savedDriver.LastName, Is.EqualTo("Doe"));
        }

        [Test]
        public async Task AppDbContext_ShouldSaveAndRetrieveVehicle()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                BusNumber = "BUS-001",
                Number = "BUS-001"
            };

            // Act
            Assert.IsNotNull(_context);
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            // Assert
            var savedVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);
            Assert.IsNotNull(savedVehicle);
            Assert.That(savedVehicle!.BusNumber, Is.EqualTo("BUS-001"));
        }

        [Test]
        public async Task AppDbContext_ShouldSaveAndRetrieveRoute()
        {
            // Arrange
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Route 1",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                AMRiders = 25,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                PMRiders = 30
            };

            // Act
            Assert.IsNotNull(_context);
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            // Assert
            var savedRoute = await _context.Routes.FirstOrDefaultAsync(r => r.Id == route.Id);
            Assert.IsNotNull(savedRoute);
            Assert.That(savedRoute!.Name, Is.EqualTo("Route 1"));
            Assert.That(savedRoute.AMStartingMileage, Is.EqualTo(1000));
            Assert.That(savedRoute.PMRiders, Is.EqualTo(30));
        }

        [Test]
        public async Task AppDbContext_ShouldHandleRouteWithDriverAndVehicle()
        {
            // Arrange
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith"
            };

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                BusNumber = "BUS-002",
                Number = "BUS-002"
            };

            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Route 2",
                RouteDate = DateTime.Today,
                AMStartingMileage = 2000,
                AMEndingMileage = 2050,
                AMRiders = 20,
                PMStartMileage = 2050,
                PMEndingMileage = 2100,
                PMRiders = 25,
                Driver = driver,
                Vehicle = vehicle
            };

            // Act
            Assert.IsNotNull(_context);
            _context.Drivers.Add(driver);
            _context.Vehicles.Add(vehicle);
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            // Assert
            var savedRoute = await _context.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == route.Id);

            Assert.IsNotNull(savedRoute);
            Assert.IsNotNull(savedRoute!.Driver);
            Assert.That(savedRoute.Driver!.FirstName, Is.EqualTo("Jane"));
            Assert.IsNotNull(savedRoute.Vehicle);
            Assert.That(savedRoute.Vehicle!.BusNumber, Is.EqualTo("BUS-002"));
        }

        [Test]
        public async Task AppDbContext_ShouldHandleDeleteBehaviorSetNull()
        {
            // Arrange
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Driver"
            };

            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                AMRiders = 10,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                PMRiders = 15,
                Driver = driver
            };

            Assert.IsNotNull(_context);
            _context.Drivers.Add(driver);
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            // Act - Delete the driver
            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();

            // Assert - Route should still exist but with null driver
            var savedRoute = await _context.Routes.FirstOrDefaultAsync(r => r.Id == route.Id);
            Assert.IsNotNull(savedRoute);
            Assert.That(savedRoute!.DriverId, Is.Null);
        }

        [Test]
        public void AppDbContext_ShouldEnforceRequiredFields()
        {
            // Arrange - Driver with missing required fields
            var invalidDriver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "", // Required field is empty
                LastName = null! // Required field is null
            };

            // Act & Assert
            Assert.IsNotNull(_context);
            _context.Drivers.Add(invalidDriver);
            // SaveChanges is not available on IAppDbContext, so cast to AppDbContext
            var dbContext = _context as AppDbContext;
            Assert.IsNotNull(dbContext);
            var ex = Assert.Throws<DbUpdateException>(
                () => dbContext!.SaveChanges()
            );
            // The exact exception depends on the database provider, but it should fail
            Assert.That(ex, Is.Not.Null);
        }

        [Test]
        public async Task AppDbContext_ShouldTrackChanges()
        {
            // Arrange
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Original",
                LastName = "Name"
            };

            Assert.IsNotNull(_context);
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            // Act
            driver.FirstName = "Updated";
            await _context.SaveChangesAsync();

            // Assert
            var updatedDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driver.Id);
            Assert.IsNotNull(updatedDriver);
            Assert.That(updatedDriver!.FirstName, Is.EqualTo("Updated"));
        }

        [Test]
        public void AppDbContext_ShouldImplementIAppDbContext()
        {
            // Assert
            Assert.IsNotNull(_context);
            Assert.That(_context, Is.InstanceOf<IAppDbContext>());

            // Test interface methods
            var dbInterface = _context as IAppDbContext;
            Assert.IsNotNull(dbInterface);
            Assert.That(dbInterface!.Routes, Is.Not.Null);
            Assert.That(dbInterface.Drivers, Is.Not.Null);
            Assert.That(dbInterface.Vehicles, Is.Not.Null);
        }

        [Test]
        public async Task AppDbContext_IAppDbContext_SaveChangesAsync_ShouldWork()
        {
            // Arrange
            Assert.IsNotNull(_context);
            var dbInterface = _context as IAppDbContext;
            Assert.IsNotNull(dbInterface);
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Interface",
                LastName = "Test"
            };

            // Act
            dbInterface!.Drivers.Add(driver);
            var result = await dbInterface.SaveChangesAsync();

            // Assert
            Assert.That(result, Is.EqualTo(1)); // One record saved
            var savedDriver = await dbInterface.Drivers.FirstOrDefaultAsync(d => d.Id == driver.Id);
            Assert.IsNotNull(savedDriver);
            // Re-enable warnings at end of file
#pragma warning restore CS8602, CS8604, CS8600, CS8601, CS8629
        }
    }
}
