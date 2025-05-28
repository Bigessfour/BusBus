// Suppress platform compatibility warnings for WinForms (Windows-only test)
#pragma warning disable CA1416 // Platform compatibility (WinForms)
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.UI
{
    [TestClass]
    [TestCategory(TestCategories.UI)]
    // Platform attribute removed (MSTest incompatible)
    // Apartment attribute removed (MSTest incompatible) // Required for WinForms testing
    public class DashboardTests : TestBase
    {
        private static readonly object[] EmptyObjectArray = Array.Empty<object>();
        private Mock<IStatisticsService> _mockStatisticsService;
        private Dashboard _dashboard;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();

            // Mock the statistics service
            _mockStatisticsService = new Mock<IStatisticsService>();

            // Set up mock responses
            _mockStatisticsService.Setup(m => m.GetDashboardStatisticsAsync())
                .ReturnsAsync(new DashboardStatistics
                {
                    TotalMilesThisMonth = 1500,
                    TotalStudentsThisMonth = 750,
                    TotalMilesThisWeek = 300,
                    TotalStudentsThisWeek = 150,
                    TotalMilesYesterday = 60,
                    TotalStudentsYesterday = 30,
                    TotalMilesSchoolYear = 10000,
                    TotalStudentsSchoolYear = 5000,
                    LastUpdated = DateTime.Now
                });

            _mockStatisticsService.Setup(m => m.GetSchoolYearStatisticsAsync())
                .ReturnsAsync(new SchoolYearStatistics
                {
                    TotalMilesDriven = 10000,
                    TotalStudentsHauled = 5000,
                    TotalRoutes = 100,
                    ActiveDrivers = 10,
                    ActiveVehicles = 5,
                    SchoolYearStart = new DateTime(DateTime.Now.Year, 8, 1),
                    SchoolYearEnd = new DateTime(DateTime.Now.Year + 1, 6, 30),
                    AverageMilesPerRoute = 100,
                    AverageStudentsPerRoute = 50
                });

            // Provide all required constructor arguments for Dashboard
            var mockRouteService = new Mock<IRouteService>().Object;
            var logger = ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Dashboard>>();
            _dashboard = new Dashboard(ServiceProvider, mockRouteService, logger);
        }

        [TestCleanup]
        public override void TearDown()
        {
            _dashboard?.Dispose();
            base.TearDown();
        }

        [TestMethod]
        // Description: Test dashboard initialization and statistics loading
        public async Task Dashboard_LoadStatistics_ShouldDisplayCorrectData()
        {
            // Act - Load statistics
            var loadMethod = _dashboard.GetType().GetMethod("LoadStatistics",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (loadMethod != null)
            {
                await Task.Run(async () =>
                {
                    var task = (Task)loadMethod.Invoke(_dashboard, EmptyObjectArray);
                    await task;
                });
            }

            // Assert - Verify statistics service was called
            _mockStatisticsService.Verify(m => m.GetDashboardStatisticsAsync(), Times.Once);

            // Check that labels were updated with statistics
            // Access via reflection
            var milesMonthLabel = _dashboard.GetType().GetField("_milesThisMonthLabel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var studentsMonthLabel = _dashboard.GetType().GetField("_studentsThisMonthLabel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (milesMonthLabel?.GetValue(_dashboard) is Label milesLabel)
            {
                milesLabel.Text.Should().Contain("1,500");
            }

            if (studentsMonthLabel?.GetValue(_dashboard) is Label studentsLabel)
            {
                studentsLabel.Text.Should().Contain("750");
            }
        }

        [TestMethod]
        // Description: Test dashboard refresh functionality
        public async Task Dashboard_RefreshButton_ShouldReloadStatistics()
        {
            // Act - Click refresh button
            var refreshButton = _dashboard.GetType().GetField("_refreshButton",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (refreshButton?.GetValue(_dashboard) is Button button)
            {
                // Clear previous calls
                _mockStatisticsService.Invocations.Clear();

                // Simulate button click
                button.PerformClick();

                // Allow time for async operation
                await Task.Delay(100);
            }

            // Assert - Verify statistics service was called again
            _mockStatisticsService.Verify(m => m.GetDashboardStatisticsAsync(), Times.Once);
        }

        [TestMethod]
        // Description: Test dashboard chart data loading
        public async Task Dashboard_LoadChartData_ShouldPopulateChart()
        {
            // Act - Load chart data
            var loadChartMethod = _dashboard.GetType().GetMethod("LoadChartData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (loadChartMethod != null)
            {
                await Task.Run(async () =>
                {
                    var task = (Task)loadChartMethod.Invoke(_dashboard, EmptyObjectArray);
                    await task;
                });
            }

            // Assert - Verify chart was populated
            // Access chart via reflection
            var chartField = _dashboard.GetType().GetField("_statsChart",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (chartField?.GetValue(_dashboard) is DataVisualization.Charting.Chart chart)
            {
                chart.Series.Should().NotBeEmpty();
                chart.Series[0].Points.Should().NotBeEmpty();
            }
        }
    }
}
