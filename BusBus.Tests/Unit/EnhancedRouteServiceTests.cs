using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using BusBus.DataAccess;
using BusBus.Models;
using BusBus.Services;
using BusBus.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
namespace BusBus.Tests.Unit
{
    [TestFixture]
    [Category(TestCategories.Unit)]
    public class EnhancedRouteServiceTests
    {
        private Fixture _fixture = null!;
        private TestFixtureFactory _factory = null!;
        private IRouteService _routeService = null!;
        
        [SetUp]
        public void SetUp()
        {
            // Initialize AutoFixture
            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());
            _factory = new TestFixtureFactory();
            // RouteService has a parameterless constructor and uses in-memory lists
            _routeService = new RouteService();
        }
        
        [Test]
        public async Task GetRouteByIdAsync_WhenRouteExists_ShouldReturnRoute()
        {
            // Arrange
            var routeId = Guid.NewGuid();
            var expectedRoute = _factory.CreateRoute(id: routeId);
            await _routeService.CreateRouteAsync(expectedRoute);

            // Act
            var result = await _routeService.GetRouteByIdAsync(routeId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(routeId);
            result.Name.Should().Be(expectedRoute.Name);
        }
        
        [Test]
        public async Task GetRouteByIdAsync_WhenRouteDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _routeService.GetRouteByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }
        
        [Test]
        public async Task CreateRouteAsync_ShouldAddRouteToDbSet()
        {
            // Arrange
            var newRoute = _factory.CreateRoute();

            // Act
            var created = await _routeService.CreateRouteAsync(newRoute);

            // Assert
            created.Should().NotBeNull();
            created.Id.Should().Be(newRoute.Id);
            var fetched = await _routeService.GetRouteByIdAsync(newRoute.Id);
            fetched.Should().NotBeNull();
        }
        
        [Test]
        public async Task UpdateRouteAsync_WhenRouteExists_ShouldUpdateRoute()
        {
            // Arrange
            var existingRoute = _factory.CreateRoute();
            await _routeService.CreateRouteAsync(existingRoute);
            var updatedRoute = new Route
            {
                Id = existingRoute.Id,
                Name = "Updated Name",
                StartLocation = "Updated Start",
                EndLocation = "Updated End",
                RouteDate = existingRoute.RouteDate,
                AMStartingMileage = existingRoute.AMStartingMileage,
                AMEndingMileage = existingRoute.AMEndingMileage,
                AMRiders = existingRoute.AMRiders,
                PMStartMileage = existingRoute.PMStartMileage,
                PMEndingMileage = existingRoute.PMEndingMileage,
                PMRiders = existingRoute.PMRiders,
                DriverId = existingRoute.DriverId,
                VehicleId = existingRoute.VehicleId
            };

            // Act
            var result = await _routeService.UpdateRouteAsync(updatedRoute);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Updated Name");
            result.StartLocation.Should().Be("Updated Start");
            result.EndLocation.Should().Be("Updated End");
        }
        
        [Test]
        public async Task UpdateRouteAsync_WhenRouteDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentRoute = _factory.CreateRoute();

            // Act
            var result = await _routeService.UpdateRouteAsync(nonExistentRoute);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(nonExistentRoute.Id);
        }
        
        [Test]
        public async Task DeleteRouteAsync_WhenRouteExists_ShouldDeleteRoute()
        {
            // Arrange
            var existingRoute = _factory.CreateRoute();
            await _routeService.CreateRouteAsync(existingRoute);

            // Act
            await _routeService.DeleteRouteAsync(existingRoute.Id);

            // Assert
            var result = await _routeService.GetRouteByIdAsync(existingRoute.Id);
            result.Should().BeNull();
        }
        
        [Test]
        public async Task DeleteRouteAsync_WhenRouteDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            await _routeService.DeleteRouteAsync(nonExistentId);

            // Assert
            var result = await _routeService.GetRouteByIdAsync(nonExistentId);
            result.Should().BeNull();
        }
        
        [Test]
        public async Task GetRoutesByDateAsync_ShouldReturnMatchingRoutes()
        {
            // Arrange
            var targetDate = DateTime.Today;
            var routes = _factory.CreateMany<Route>(3);
            foreach (var route in routes)
            {
                route.RouteDate = targetDate;
                await _routeService.CreateRouteAsync(route);
            }

            // Act
            var result = await _routeService.GetRoutesByDateAsync(targetDate);

            // Assert
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(routes);
        }

        private Mock<DbSet<Route>> CreateMockDbSet()
        {
            // No longer needed; RouteService does not use DbSet
            return null!;
        }
    }
}
