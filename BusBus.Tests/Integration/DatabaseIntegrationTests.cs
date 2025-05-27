using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusBus.Models;
using BusBus.Services;
using BusBus.Tests;
using BusBus.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BusBus.Tests.Integration
{
    [TestFixture]
    [Category(TestCategories.Integration)]
    public class DatabaseIntegrationTests : TestBase
    {
        private IRouteService _routeService;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _routeService = ServiceProvider.GetRequiredService<IRouteService>();
        }

        [Test]
        [Description("End-to-end test: Create -> Read -> Update -> Delete route with real database")]
        public async Task RouteLifecycle_CompleteWorkflow_ShouldWorkEndToEnd()
        {
            // CREATE - Test route creation
            var newRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Integration Test Route",
                RouteDate = DateTime.Today.AddDays(1),
                StartLocation = "Integration Test Start",
                EndLocation = "Integration Test End",
                ScheduledTime = DateTime.Today.AddDays(1).AddHours(8),
                AMStartingMileage = 5000,
                AMEndingMileage = 5025,
                AMRiders = 30,
                PMStartMileage = 5025,
                PMEndingMileage = 5050,
                PMRiders = 35,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RouteID = 4001,
                RouteName = "IntegrationTestRoute",
                RouteCode = "RTINTEG",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };

            var createdRoute = await _routeService.CreateRouteAsync(newRoute);
            createdRoute.Should().NotBeNull();
            createdRoute.Id.Should().Be(newRoute.Id);
            createdRoute.Name.Should().Be("Integration Test Route");

            // READ - Test route retrieval
            var routes = await _routeService.GetRoutesAsync();
            routes.Should().Contain(r => r.Id == newRoute.Id);

            var specificRoute = routes.First(r => r.Id == newRoute.Id);
            specificRoute.AMStartingMileage.Should().Be(5000);
            specificRoute.PMRiders.Should().Be(35);

            // UPDATE - Test route modification
            specificRoute.Name = "Updated Integration Route";
            specificRoute.AMRiders = 40;

            var updatedRoute = await _routeService.UpdateRouteAsync(specificRoute);
            updatedRoute.Name.Should().Be("Updated Integration Route");
            updatedRoute.AMRiders.Should().Be(40);

            // Verify update persisted
            var routesAfterUpdate = await _routeService.GetRoutesAsync();
            var verifyRoute = routesAfterUpdate.First(r => r.Id == newRoute.Id);
            verifyRoute.Name.Should().Be("Updated Integration Route");
            verifyRoute.AMRiders.Should().Be(40);

            // DELETE - Test route deletion
            await _routeService.DeleteRouteAsync(newRoute.Id);

            var routesAfterDelete = await _routeService.GetRoutesAsync();
            routesAfterDelete.Should().NotContain(r => r.Id == newRoute.Id);
        }

        [Test]
        [Description("Test database seeding functionality")]
        public async Task SeedSampleData_ShouldCreateValidTestData()
        {
            // Act
            await _routeService.SeedSampleDataAsync();

            // Assert
            var routes = await _routeService.GetRoutesAsync();
            var drivers = await _routeService.GetDriversAsync();
            var vehicles = await _routeService.GetVehiclesAsync();

            routes.Should().NotBeEmpty("Sample routes should be created");
            drivers.Should().NotBeEmpty("Sample drivers should be created");
            vehicles.Should().NotBeEmpty("Sample vehicles should be created");

            // Verify relationships
            routes.Should().OnlyContain(r => r.DriverId.HasValue, "Routes should have drivers assigned");
            routes.Should().OnlyContain(r => r.VehicleId.HasValue, "Routes should have vehicles assigned");
        }

        [Test]
        [Description("Test pagination functionality with real data")]
        public async Task GetRoutesWithPagination_ShouldReturnCorrectPages()
        {
            // Arrange - Create multiple test routes
            var testRoutes = new List<Route>();
            for (int i = 1; i <= 15; i++)
            {
                var route = new Route
                {
                    Id = Guid.NewGuid(),
                    Name = $"Pagination Test Route {i}",
                    RouteDate = DateTime.Today.AddDays(i),
                    AMStartingMileage = 1000 + (i * 10),
                    AMEndingMileage = 1000 + (i * 10) + 5,
                    PMStartMileage = 1000 + (i * 10) + 5,
                    PMEndingMileage = 1000 + (i * 10) + 10,
                    AMRiders = 20 + i,
                    PMRiders = 25 + i
                };
                testRoutes.Add(route);
                await _routeService.CreateRouteAsync(route);
            }

            try
            {
                // Act & Assert - Test different page sizes
                var page1 = await _routeService.GetRoutesAsync(1, 5);
                var page2 = await _routeService.GetRoutesAsync(2, 5);
                var totalCount = await _routeService.GetRoutesCountAsync();

                page1.Should().HaveCount(5, "First page should have 5 items");
                page2.Should().HaveCount(5, "Second page should have 5 items");
                totalCount.Should().BeGreaterOrEqualTo(15, "Total count should include our test routes");

                // Verify no overlap between pages
                var page1Ids = page1.Select(r => r.Id).ToList();
                var page2Ids = page2.Select(r => r.Id).ToList();
                page1Ids.Should().NotIntersectWith(page2Ids, "Pages should not have overlapping routes");
            }
            finally
            {
                // Cleanup
                foreach (var route in testRoutes)
                {
                    await _routeService.DeleteRouteAsync(route.Id);
                }
            }
        }

        [Test]
        [Description("Test concurrent access to database")]
        public async Task ConcurrentRouteOperations_ShouldHandleMultipleUsers()
        {
            // Arrange
            var tasks = new List<Task<Route>>();

            // Act - Create multiple routes concurrently
            for (int i = 1; i <= 5; i++)
            {
                var routeNumber = i;
                var task = Task.Run(async () =>
                {
                    var route = new Route
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Concurrent Route {routeNumber}",
                        RouteDate = DateTime.Today.AddDays(routeNumber),
                        AMStartingMileage = 2000 + (routeNumber * 100),
                        AMEndingMileage = 2000 + (routeNumber * 100) + 50,
                        PMStartMileage = 2000 + (routeNumber * 100) + 50,
                        PMEndingMileage = 2000 + (routeNumber * 100) + 100,
                        AMRiders = 30,
                        PMRiders = 35
                    };
                    return await _routeService.CreateRouteAsync(route);
                });
                tasks.Add(task);
            }

            // Wait for all tasks to complete
            var createdRoutes = await Task.WhenAll(tasks);

            try
            {
                // Assert
                createdRoutes.Should().HaveCount(5, "All concurrent operations should succeed");
                createdRoutes.Should().OnlyHaveUniqueItems(r => r.Id, "All routes should have unique IDs");

                foreach (var route in createdRoutes)
                {
                    route.Should().NotBeNull("Each route should be created successfully");
                    route.Name.Should().StartWith("Concurrent Route", "Route names should be preserved");
                }
            }
            finally
            {
                // Cleanup
                foreach (var route in createdRoutes)
                {
                    await _routeService.DeleteRouteAsync(route.Id);
                }
            }
        }
    }
}
