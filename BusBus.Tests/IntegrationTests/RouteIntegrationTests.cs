using BusBus.DataAccess;
using BusBus.Models;
using BusBus.Services;
using BusBus.Tests.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BusBus.Tests.IntegrationTests
{
    public class RouteIntegrationTests : TestBase
    {
        private readonly RouteService _routeService;

        public RouteIntegrationTests()
        {
            _routeService = new RouteService(DbContext);
        }

        [Fact]
        public async Task EndToEnd_CreateAndRetrieveRoute()
        {
            // Arrange
            var newRoute = new Route
            {
                Name = "Integration Test Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                AMRiders = 15,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                PMRiders = 20
            };

            // Act - Create route
            var createdRoute = await _routeService.CreateRouteAsync(newRoute);

            // Act - Retrieve all routes
            var allRoutes = await _routeService.GetRoutesAsync();
            // Act - Retrieve specific route
            var retrievedRoute = await _routeService.GetRouteByIdAsync(createdRoute.Id);

            // Assert
            Assert.NotNull(createdRoute);
            Assert.Contains(allRoutes, r => r.Id == createdRoute.Id);
            Assert.NotNull(retrievedRoute);
            Assert.Equal(newRoute.Name, retrievedRoute.Name);
            Assert.Equal(newRoute.RouteDate, retrievedRoute.RouteDate);
        }

        [Fact]
        public async Task EndToEnd_UpdateRoute()
        {
            // Arrange
            var newRoute = new Route
            {
                Name = "Route To Update",
                RouteDate = DateTime.Today
            };
            var createdRoute = await _routeService.CreateRouteAsync(newRoute);

            // Act - Update route
            createdRoute.Name = "Updated Route Name";
            await _routeService.UpdateRouteAsync(createdRoute);
            // Act - Retrieve updated route
            var updatedRoute = await _routeService.GetRouteByIdAsync(createdRoute.Id);

            // Assert
            Assert.NotNull(updatedRoute);
            Assert.Equal("Updated Route Name", updatedRoute.Name);
        }

        [Fact]
        public async Task EndToEnd_DeleteRoute()
        {
            // Arrange
            var newRoute = new Route
            {
                Name = "Route To Delete",
                RouteDate = DateTime.Today
            };
            var createdRoute = await _routeService.CreateRouteAsync(newRoute);

            // Act - Delete route
            await _routeService.DeleteRouteAsync(createdRoute.Id);

            // Act - Try to retrieve deleted route
            var deletedRoute = await _routeService.GetRouteByIdAsync(createdRoute.Id);

            // Assert
            Assert.Null(deletedRoute);
        }
    }
}