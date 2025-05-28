using System;
using System.Linq;
using System.Threading.Tasks;
using BusBus.Models;
using BusBus.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.Services
{
    [TestClass]
    [TestCategory(TestCategories.Service)]
    [TestCategory(TestCategories.Unit)]
    public class RouteServiceTests : TestBase
    {
        private IRouteService _routeService;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();
            _routeService = ServiceProvider.GetRequiredService<IRouteService>();
        }

        [TestMethod]
        // Description: Test pagination functionality for routes
        public async Task GetRoutesAsync_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange - Create multiple test routes
            var routeDate = DateTime.Today.AddDays(1);
            for (int i = 1; i <= 15; i++)
            {
                await _routeService.CreateRouteAsync(new Route
                {
                    Id = Guid.NewGuid(),
                    Name = $"Pagination Test Route {i}",
                    RouteDate = routeDate,
                    AMStartingMileage = 1000 + (i * 100),
                    AMEndingMileage = 1030 + (i * 100),
                    AMRiders = 20 + i,
                    PMStartMileage = 1030 + (i * 100),
                    PMEndingMileage = 1060 + (i * 100),
                    PMRiders = 25 + i,
                    RowVersion = new byte[] { 1, 0, 0, 0 },
                    CreatedBy = "UnitTest",
                    CreatedDate = DateTime.Today,
                    ModifiedDate = DateTime.Today,
                    RouteID = 1000 + i,
                    RouteName = $"PaginationTestRoute{i}",
                    RouteCode = $"RTPAG{i:D2}",
                    IsActive = true,
                    StopsJson = "[]",
                    ScheduleJson = "{}"
                });
            }

            // Act - Get total count
            var totalCount = await _routeService.GetRoutesCountAsync();

            // Assert - Total count
            totalCount.Should().BeGreaterThanOrEqualTo(15);

            // Act - Get first page (5 items)
            var firstPage = await _routeService.GetRoutesAsync(1, 5);

            // Assert - First page
            firstPage.Should().NotBeNull();
            firstPage.Count.Should().Be(5);

            // Act - Get second page
            var secondPage = await _routeService.GetRoutesAsync(2, 5);

            // Assert - Second page
            secondPage.Should().NotBeNull();
            secondPage.Count.Should().Be(5);
            secondPage.Should().NotContain(r => firstPage.Any(fp => fp.Id == r.Id));

            // Act - Get third page
            var thirdPage = await _routeService.GetRoutesAsync(3, 5);

            // Assert - Third page
            thirdPage.Should().NotBeNull();
            thirdPage.Count.Should().BeGreaterThanOrEqualTo(5);
            thirdPage.Should().NotContain(r => firstPage.Any(fp => fp.Id == r.Id) || secondPage.Any(sp => sp.Id == r.Id));
        }

        [TestMethod]
        // Description: Test route filtering by date
        public async Task GetRoutesByDateAsync_ShouldReturnCorrectRoutes()
        {
            // Arrange
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var tomorrow = today.AddDays(1);

            // Create routes with different dates
            var todayRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Today Test Route",
                RouteDate = today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1030,
                AMRiders = 25,
                PMStartMileage = 1030,
                PMEndingMileage = 1060,
                PMRiders = 30,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = today,
                ModifiedDate = today,
                RouteID = 2001,
                RouteName = "TodayTestRoute",
                RouteCode = "RTTODAY",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };

            var yesterdayRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Yesterday Test Route",
                RouteDate = yesterday,
                AMStartingMileage = 900,
                AMEndingMileage = 930,
                AMRiders = 20,
                PMStartMileage = 930,
                PMEndingMileage = 960,
                PMRiders = 25,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = yesterday,
                ModifiedDate = yesterday,
                RouteID = 2002,
                RouteName = "YesterdayTestRoute",
                RouteCode = "RTYEST",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };

            var tomorrowRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Tomorrow Test Route",
                RouteDate = tomorrow,
                AMStartingMileage = 1100,
                AMEndingMileage = 1130,
                AMRiders = 30,
                PMStartMileage = 1130,
                PMEndingMileage = 1160,
                PMRiders = 35,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = tomorrow,
                ModifiedDate = tomorrow,
                RouteID = 2003,
                RouteName = "TomorrowTestRoute",
                RouteCode = "RTTOM",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };

            await _routeService.CreateRouteAsync(todayRoute);
            await _routeService.CreateRouteAsync(yesterdayRoute);
            await _routeService.CreateRouteAsync(tomorrowRoute);

            // Act - Get routes for today
            var todayRoutes = await _routeService.GetRoutesByDateAsync(today);

            // Assert - Today routes
            todayRoutes.Should().Contain(r => r.Id == todayRoute.Id);
            todayRoutes.Should().NotContain(r => r.Id == yesterdayRoute.Id);
            todayRoutes.Should().NotContain(r => r.Id == tomorrowRoute.Id);

            // Act - Get routes for yesterday
            var yesterdayRoutes = await _routeService.GetRoutesByDateAsync(yesterday);

            // Assert - Yesterday routes
            yesterdayRoutes.Should().Contain(r => r.Id == yesterdayRoute.Id);
            yesterdayRoutes.Should().NotContain(r => r.Id == todayRoute.Id);
            yesterdayRoutes.Should().NotContain(r => r.Id == tomorrowRoute.Id);

            // Act - Get routes for tomorrow
            var tomorrowRoutes = await _routeService.GetRoutesByDateAsync(tomorrow);

            // Assert - Tomorrow routes
            tomorrowRoutes.Should().Contain(r => r.Id == tomorrowRoute.Id);
            tomorrowRoutes.Should().NotContain(r => r.Id == todayRoute.Id);
            tomorrowRoutes.Should().NotContain(r => r.Id == yesterdayRoute.Id);
        }

        [TestMethod]
        // Description: Test sample data seeding
        public async Task SeedSampleDataAsync_ShouldCreateSampleData()
        {
            // Arrange - Clear existing routes
            var existingRoutes = await _routeService.GetRoutesAsync();
            foreach (var route in existingRoutes)
            {
                await _routeService.DeleteRouteAsync(route.Id);
            }

            // Act
            await _routeService.SeedSampleDataAsync();

            // Assert
            var routes = await _routeService.GetRoutesAsync();
            var drivers = await _routeService.GetDriversAsync();
            var vehicles = await _routeService.GetVehiclesAsync();

            routes.Should().NotBeEmpty("Sample routes should be created");
            drivers.Should().NotBeEmpty("Sample drivers should be created");
            vehicles.Should().NotBeEmpty("Sample vehicles should be created");

            // Check that routes have drivers and vehicles assigned
            routes.Should().Contain(r => r.DriverId.HasValue);
            routes.Should().Contain(r => r.VehicleId.HasValue);
        }
    }
}
