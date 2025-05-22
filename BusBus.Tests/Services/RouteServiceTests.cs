using BusBus.Models;
using BusBus.Services;
using BusBus.Tests.Common;
using System;
using System.Linq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BusBus.Tests.Services
{
    public class RouteServiceTests : TestBase
    {
        private readonly RouteService _routeService;

        public RouteServiceTests()
        {
            _routeService = new RouteService(DbContext);
        }

        [Fact]
        public async Task GetRoutesAsync_ReturnsAllRoutes()
        {
            // Arrange
            var testRoutes = TestHelper.SeedRoutes(DbContext);

            // Act
            var result = await _routeService.GetRoutesAsync();

            // Assert
            Assert.Equal(testRoutes.Count, result.Count);
        }

        [Fact]
        public async Task GetRouteByIdAsync_ReturnsCorrectRoute()
        {
            // Arrange
            var testRoutes = TestHelper.SeedRoutes(DbContext);
            var targetRoute = testRoutes[0];

            // Act
            var result = await _routeService.GetRouteByIdAsync(targetRoute.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetRoute.Id, result.Id);
            Assert.Equal(targetRoute.Name, result.Name);
        }

        [Fact]
        public async Task CreateRouteAsync_CreatesNewRoute()
        {
            // Arrange
            var newRoute = new Route
            {
                Name = "New Test Route",
                RouteDate = DateTime.Today
            };

            // Act
            var result = await _routeService.CreateRouteAsync(newRoute);

            // Assert
            var savedRoute = await DbContext.Routes.FirstOrDefaultAsync(r => r.Name == "New Test Route");
            Assert.NotNull(savedRoute);
            Assert.Equal(newRoute.Id, savedRoute.Id);
            Assert.Equal(newRoute.Name, savedRoute.Name);
        }
    }
}