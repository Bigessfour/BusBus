using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using BusBus.Models;
using BusBus.Services;

namespace BusBus.Tests
{    [TestFixture]
    public class BasicTests : TestBase
    {
        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
        }        [Test]
        public async Task TestBase_ShouldSeedTestData()
        {
            // Arrange & Act - TestBase setup should have seeded data
            var dbContext = GetDbContext();
            
            Console.WriteLine("Checking seeded data...");
            
            // Assert
            var drivers = await dbContext.Drivers.ToListAsync();
            var vehicles = await dbContext.Vehicles.ToListAsync();
            var routes = await dbContext.Routes.ToListAsync();
            
            Console.WriteLine($"Found: {drivers.Count} drivers, {vehicles.Count} vehicles, {routes.Count} routes");
            
            Assert.That(drivers.Count, Is.GreaterThan(0), "Should have at least one driver");
            Assert.That(vehicles.Count, Is.GreaterThan(0), "Should have at least one vehicle");
            Assert.That(routes.Count, Is.GreaterThan(0), "Should have at least one route");
            
            // Check that we have both John Doe and other test drivers
            var johnDoe = drivers.FirstOrDefault(d => d.Name == "John Doe");
            var anyValidDriver = drivers.FirstOrDefault(d => !string.IsNullOrEmpty(d.Name) && !string.IsNullOrEmpty(d.LicenseNumber));
            
            Assert.That(anyValidDriver, Is.Not.Null, "Should have at least one valid driver with name and license");
            Assert.That(anyValidDriver.Name, Is.Not.Null.And.Not.Empty);
            Assert.That(anyValidDriver.LicenseNumber, Is.Not.Null.And.Not.Empty);
            
            // Check that routes have valid relationships
            var validRoute = routes.FirstOrDefault();
            Assert.That(validRoute, Is.Not.Null, "Should have at least one route");
            Assert.That(validRoute.DriverId, Is.Not.EqualTo(Guid.Empty), "Route should have a valid driver ID");
            Assert.That(validRoute.VehicleId, Is.Not.EqualTo(Guid.Empty), "Route should have a valid vehicle ID");
        }

        [Test]
        public async Task Database_ShouldCreateAndQuery()
        {
            // Arrange
            var dbContext = GetDbContext();
            var newDriverId = Guid.NewGuid();
            
            var newDriver = new Driver
            {
                Id = newDriverId,
                FirstName = "Jane",
                LastName = "Smith",
                Name = "Jane Smith",
                LicenseNumber = "LIC002"
            };
            
            // Act
            dbContext.Drivers.Add(newDriver);
            await dbContext.SaveChangesAsync();
            
            var retrievedDriver = await dbContext.Drivers
                .FirstOrDefaultAsync(d => d.Id == newDriverId);
            
            // Assert
            Assert.That(retrievedDriver, Is.Not.Null);
            Assert.That(retrievedDriver.Name, Is.EqualTo("Jane Smith"));
            Assert.That(retrievedDriver.LicenseNumber, Is.EqualTo("LIC002"));
        }

        [Test]
        public void RouteService_ShouldBeRegistered()
        {
            // Arrange & Act
            var routeService = ServiceProvider.GetService(typeof(IRouteService));
            
            // Assert
            Assert.That(routeService, Is.Not.Null, "RouteService should be registered in DI container");
            Assert.That(routeService, Is.InstanceOf<IRouteService>());
        }

        [Test]
        public async Task Routes_ShouldHaveValidRelationships()
        {
            // Arrange
            var dbContext = GetDbContext();
            
            // Act
            var routes = await dbContext.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .ToListAsync();
            
            // Assert
            Assert.That(routes.Count, Is.GreaterThan(0));
            
            var route = routes.First();
            Assert.That(route.Driver, Is.Not.Null, "Route should have a driver");
            Assert.That(route.Vehicle, Is.Not.Null, "Route should have a vehicle");
            Assert.That(route.DriverId, Is.EqualTo(route.Driver.Id));
            Assert.That(route.VehicleId, Is.EqualTo(route.Vehicle.Id));
        }
    }
}
