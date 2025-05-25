using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using BusBus.Models;
using BusBus.Services;

namespace BusBus.Tests
{
    [TestFixture]
    [Category(TestCategories.Service)]
    public class ServiceTests : TestBase
    {
        private IRouteService _routeService = null!;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _routeService = ServiceProvider.GetRequiredService<IRouteService>();
            
            // Seed the RouteService with sample data
            await _routeService.SeedSampleDataAsync();
        }

        [Test]
        public void RouteService_ShouldBeRegistered()
        {
            // Assert
            Assert.That(_routeService, Is.Not.Null, "RouteService should be registered in DI container");
            Assert.That(_routeService, Is.InstanceOf<IRouteService>());
        }

        [Test]
        public async Task RouteService_GetRoutesAsync_ShouldReturnData()
        {
            // Act
            var routes = await _routeService.GetRoutesAsync();
            
            // Assert
            Assert.That(routes, Is.Not.Null);
            Assert.That(routes.Count, Is.GreaterThan(0), "Should have seeded routes");
            
            var firstRoute = routes.First();
            Assert.That(firstRoute.Name, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task RouteService_CreateRouteAsync_ShouldAddNewRoute()
        {
            // Arrange
            var initialRoutes = await _routeService.GetRoutesAsync();
            var initialCount = initialRoutes.Count;
            
            var newRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Created Route",
                RouteDate = DateTime.Today.AddDays(1),
                ScheduledTime = DateTime.Today.AddDays(1).AddHours(9),
                StartLocation = "Test Start",
                EndLocation = "Test End",
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                AMRiders = 20,
                PMRiders = 25
            };
            
            // Act
            var createdRoute = await _routeService.CreateRouteAsync(newRoute);
            
            // Assert
            Assert.That(createdRoute, Is.Not.Null);
            Assert.That(createdRoute.Id, Is.Not.EqualTo(Guid.Empty));
            
            var allRoutes = await _routeService.GetRoutesAsync();
            Assert.That(allRoutes.Count, Is.EqualTo(initialCount + 1));
            
            var foundRoute = allRoutes.FirstOrDefault(r => r.Name == "Test Created Route");
            Assert.That(foundRoute, Is.Not.Null);
        }

        [Test]
        public async Task RouteService_GetRouteByIdAsync_ShouldReturnCorrectRoute()
        {
            // Arrange
            var routes = await _routeService.GetRoutesAsync();
            var existingRoute = routes.First();
            
            // Act
            var foundRoute = await _routeService.GetRouteByIdAsync(existingRoute.Id);
            
            // Assert
            Assert.That(foundRoute, Is.Not.Null);
            Assert.That(foundRoute.Id, Is.EqualTo(existingRoute.Id));
            Assert.That(foundRoute.Name, Is.EqualTo(existingRoute.Name));
        }

        [Test]
        public async Task RouteService_UpdateRouteAsync_ShouldModifyRoute()
        {
            // Arrange
            var routes = await _routeService.GetRoutesAsync();
            var routeToUpdate = routes.First();
            var originalName = routeToUpdate.Name;
            
            routeToUpdate.Name = "Updated Route Name";
            
            // Act
            var updatedRoute = await _routeService.UpdateRouteAsync(routeToUpdate);
            
            // Assert
            Assert.That(updatedRoute, Is.Not.Null);
            Assert.That(updatedRoute.Name, Is.EqualTo("Updated Route Name"));
            
            // Verify the change persisted
            var retrievedRoute = await _routeService.GetRouteByIdAsync(routeToUpdate.Id);
            Assert.That(retrievedRoute?.Name, Is.EqualTo("Updated Route Name"));
        }

        [Test]
        public async Task RouteService_DeleteRouteAsync_ShouldRemoveRoute()
        {
            // Arrange
            var routes = await _routeService.GetRoutesAsync();
            var routeToDelete = routes.First();
            var initialCount = routes.Count;
            
            // Act
            await _routeService.DeleteRouteAsync(routeToDelete.Id);
            
            // Assert
            var remainingRoutes = await _routeService.GetRoutesAsync();
            Assert.That(remainingRoutes.Count, Is.EqualTo(initialCount - 1));
            
            var deletedRoute = await _routeService.GetRouteByIdAsync(routeToDelete.Id);
            Assert.That(deletedRoute, Is.Null);
        }

        [Test]
        public async Task RouteService_GetDriversAsync_ShouldReturnDrivers()
        {
            // Act
            var drivers = await _routeService.GetDriversAsync();
            
            // Assert
            Assert.That(drivers, Is.Not.Null);
            Assert.That(drivers.Count, Is.GreaterThan(0));
            
            var firstDriver = drivers.First();
            Assert.That(firstDriver.Name, Is.Not.Null.And.Not.Empty);
            Assert.That(firstDriver.LicenseNumber, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task RouteService_GetVehiclesAsync_ShouldReturnVehicles()
        {
            // Act
            var vehicles = await _routeService.GetVehiclesAsync();
            
            // Assert
            Assert.That(vehicles, Is.Not.Null);
            Assert.That(vehicles.Count, Is.GreaterThan(0));
            
            var firstVehicle = vehicles.First();
            Assert.That(firstVehicle.Number, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task RouteService_GetRoutesAsync_WithPagination_ShouldReturnPagedResults()
        {
            // Act
            var firstPage = await _routeService.GetRoutesAsync(1, 1);
            var totalCount = await _routeService.GetRoutesCountAsync();
            
            // Assert
            Assert.That(firstPage.Count, Is.LessThanOrEqualTo(1));
            Assert.That(totalCount, Is.GreaterThan(0));
            
            if (totalCount > 1)
            {
                var secondPage = await _routeService.GetRoutesAsync(2, 1);
                Assert.That(secondPage.Count, Is.LessThanOrEqualTo(1));
                
                if (firstPage.Count > 0 && secondPage.Count > 0)
                {
                    Assert.That(firstPage.First().Id, Is.Not.EqualTo(secondPage.First().Id));
                }
            }
        }
    }
}
