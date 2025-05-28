using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusBus.Models;
using BusBus.Services;
using FluentAssertions;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.Services
{
    [TestClass]
    [TestCategory(TestCategories.Service)]
    [TestCategory(TestCategories.Unit)]
    public class StatisticsServiceTests
    {
        private Mock<IRouteService> _mockRouteService;
        private StatisticsService _statisticsService;
        private List<Route> _testRoutes;

        [TestInitialize]
        public void SetUp()
        {
            _mockRouteService = new Mock<IRouteService>();
            _statisticsService = new StatisticsService(_mockRouteService.Object);

            // Create test data - these routes will be used by all tests
            SetupTestRoutes();

            // Setup mock behavior
            _mockRouteService.Setup(x => x.GetRoutesAsync(default))
                .ReturnsAsync(_testRoutes);
        }

        private void SetupTestRoutes()
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);
            var schoolYearStart = new DateTime(today.Month >= 8 ? today.Year : today.Year - 1, 8, 1);

            _testRoutes = new List<Route>
            {
                // Today's route
                new Route
                {
                    Id = Guid.NewGuid(),
                    Name = "Today Route",
                    RouteDate = today,
                    DriverId = Guid.NewGuid(),
                    VehicleId = Guid.NewGuid(),
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
                    RouteID = 3001,
                    RouteName = "TodayRoute",
                    RouteCode = "RTTODAY",
                    IsActive = true,
                    StopsJson = "[]",
                    ScheduleJson = "{}"
                },

                // Yesterday's route
                new Route
                {
                    Id = Guid.NewGuid(),
                    Name = "Yesterday Route",
                    RouteDate = yesterday,
                    DriverId = Guid.NewGuid(),
                    VehicleId = Guid.NewGuid(),
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
                    RouteID = 3002,
                    RouteName = "YesterdayRoute",
                    RouteCode = "RTYEST",
                    IsActive = true,
                    StopsJson = "[]",
                    ScheduleJson = "{}"
                },

                // Last month route
                new Route
                {
                    Id = Guid.NewGuid(),
                    Name = "Last Month Route",
                    RouteDate = lastMonth,
                    DriverId = Guid.NewGuid(),
                    VehicleId = Guid.NewGuid(),
                    AMStartingMileage = 800,
                    AMEndingMileage = 830,
                    AMRiders = 15,
                    PMStartMileage = 830,
                    PMEndingMileage = 860,
                    PMRiders = 20,
                    RowVersion = new byte[] { 1, 0, 0, 0 },
                    CreatedBy = "UnitTest",
                    CreatedDate = lastMonth,
                    ModifiedDate = lastMonth,
                    RouteID = 3003,
                    RouteName = "LastMonthRoute",
                    RouteCode = "RTLMONTH",
                    IsActive = true,
                    StopsJson = "[]",
                    ScheduleJson = "{}"
                },

                // Last month route with same driver as today
                new Route
                {
                    Id = Guid.NewGuid(),
                    Name = "Last Month Route 2",
                    RouteDate = lastMonth.AddDays(5),
                    DriverId = null, // Set to null for test isolation
                    VehicleId = Guid.NewGuid(),
                    AMStartingMileage = 700,
                    AMEndingMileage = 730,
                    AMRiders = 18,
                    PMStartMileage = 730,
                    PMEndingMileage = 760,
                    PMRiders = 22,
                    RowVersion = new byte[] { 1, 0, 0, 0 },
                    CreatedBy = "UnitTest",
                    CreatedDate = lastMonth.AddDays(5),
                    ModifiedDate = lastMonth.AddDays(5),
                    RouteID = 3004,
                    RouteName = "LastMonthRoute2",
                    RouteCode = "RTLMONTH2",
                    IsActive = true,
                    StopsJson = "[]",
                    ScheduleJson = "{}"
                }
            };
        }

        [TestMethod]
        // Description: Test school year statistics calculation
        public async Task GetSchoolYearStatisticsAsync_ShouldCalculateCorrectValues()
        {
            // Act
            var stats = await _statisticsService.GetSchoolYearStatisticsAsync();

            // Assert
            stats.Should().NotBeNull();
            stats.TotalMilesDriven.Should().BeGreaterThan(0);
            stats.TotalStudentsHauled.Should().BeGreaterThan(0);
            stats.TotalRoutes.Should().BeGreaterThan(0);
            stats.ActiveDrivers.Should().BeGreaterThan(0);
            stats.ActiveVehicles.Should().BeGreaterThan(0);

            // The actual values depend on which routes fall within the school year
            // So we're mostly verifying that the calculation occurs and returns reasonable values
        }

        [TestMethod]
        // Description: Test date range statistics calculation
        public async Task GetDateRangeStatisticsAsync_ShouldCalculateCorrectValues()
        {
            // Arrange
            var today = DateTime.Today;
            var startDate = today.AddDays(-30);
            var endDate = today;

            // Act
            var stats = await _statisticsService.GetDateRangeStatisticsAsync(startDate, endDate);

            // Assert
            stats.Should().NotBeNull();
            stats.StartDate.Should().Be(startDate);
            stats.EndDate.Should().Be(endDate);
            stats.TotalRoutes.Should().BeGreaterThanOrEqualTo(2); // Today + Yesterday routes

            // Verify calculations for routes in the range
            var routesInRange = _testRoutes.FindAll(r => r.RouteDate >= startDate && r.RouteDate <= endDate);
            var expectedMiles = routesInRange.Sum(r =>
                (r.AMEndingMileage - r.AMStartingMileage) +
                (r.PMEndingMileage - r.PMStartMileage));
            var expectedStudents = routesInRange.Sum(r => r.AMRiders + r.PMRiders);

            stats.TotalMilesDriven.Should().Be(expectedMiles);
            stats.TotalStudentsHauled.Should().Be(expectedStudents);
        }

        [TestMethod]
        // Description: Test dashboard statistics calculation
        public async Task GetDashboardStatisticsAsync_ShouldCalculateCorrectValues()
        {
            // Act
            var stats = await _statisticsService.GetDashboardStatisticsAsync();

            // Assert
            stats.Should().NotBeNull();
            stats.TotalMilesYesterday.Should().Be(60); // Yesterday route: (930-900) + (960-930) = 60
            stats.TotalStudentsYesterday.Should().Be(45); // Yesterday route: 20 + 25 = 45

            // The other values are hard to assert precisely as they depend on the current date
            stats.LastUpdated.Date.Should().Be(DateTime.Today);
            stats.TotalMilesThisMonth.Should().BeGreaterThan(0);
            stats.TotalStudentsThisMonth.Should().BeGreaterThan(0);
        }
    }
}
