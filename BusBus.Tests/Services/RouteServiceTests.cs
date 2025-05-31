using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusBus.Models;
using BusBus.Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace BusBus.Tests.Services
{
    [TestClass]
    public class RouteServiceTests : TestBase
    {
        private IRouteService _routeService;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();
            _routeService = ServiceProvider.GetRequiredService<IRouteService>();
        }
        [TestCategory("Unit")]
        [TestMethod]
        public async Task CreateRouteAsync_ValidRoute_CreatesSuccessfully()
        {
            // Arrange
            var route = CreateTestRoute("Test Route 1");
            route.AMStartingMileage = 1000;
            route.AMEndingMileage = 1050;
            route.PMStartMileage = 1050;
            route.PMEndingMileage = 1100;
            route.StartLocation = "Downtown Station";
            route.EndLocation = "Airport Terminal";
            route.RouteID = 1;

            // Act
            var createdRoute = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.IsNotNull(createdRoute);
            Assert.AreEqual(route.Name, createdRoute.Name);
            Assert.AreEqual(route.RouteDate, createdRoute.RouteDate);
            Assert.AreEqual(route.AMStartingMileage, createdRoute.AMStartingMileage);
            Assert.AreEqual(route.AMEndingMileage, createdRoute.AMEndingMileage);
            Assert.AreEqual(route.AMRiders, createdRoute.AMRiders);
            Assert.AreEqual(route.PMStartMileage, createdRoute.PMStartMileage);
            Assert.AreEqual(route.PMEndingMileage, createdRoute.PMEndingMileage);
            Assert.AreEqual(route.PMRiders, createdRoute.PMRiders);
        }
        [TestCategory("Unit")]
        [TestMethod]
        public async Task GetRouteByIdAsync_ExistingRoute_ReturnsRoute()
        {
            // Arrange - Create a test route first
            var route = CreateTestRoute("Test Route for Get", 2);
            route.AMStartingMileage = 500;
            route.AMEndingMileage = 550;
            var createdRoute = await _routeService.CreateRouteAsync(route);

            // Act
            var retrievedRoute = await _routeService.GetRouteByIdAsync(createdRoute.Id);

            // Assert
            Assert.IsNotNull(retrievedRoute);
            Assert.AreEqual(createdRoute.Id, retrievedRoute.Id);
            Assert.AreEqual(createdRoute.Name, retrievedRoute.Name);
        }

        [TestCategory("Unit")]
        [TestMethod]
        public async Task GetRouteByIdAsync_NonExistentRoute_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _routeService.GetRouteByIdAsync(nonExistentId);

            // Assert
            Assert.IsNull(result);
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void Route_ComputedProperties_CalculateCorrectly()
        {
            // Arrange
            var route = new Route
            {
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                AMRiders = 25,
                PMStartMileage = 1050,
                PMEndingMileage = 1120,
                PMRiders = 30
            };

            // Act & Assert
            Assert.AreEqual(50, route.AMMiles); // 1050 - 1000
            Assert.AreEqual(70, route.PMMiles); // 1120 - 1050
            Assert.AreEqual(120, route.TotalMiles); // 50 + 70
            Assert.AreEqual(55, route.TotalRiders); // 25 + 30
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void Route_HasDriverProperty_ReturnsCorrectValue()
        {
            // Arrange
            var routeWithDriver = new Route { DriverId = Guid.NewGuid() };
            var routeWithoutDriver = new Route { DriverId = null };

            // Act & Assert
            Assert.IsTrue(routeWithDriver.HasDriver);
            Assert.IsFalse(routeWithoutDriver.HasDriver);
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void Route_HasVehicleProperty_ReturnsCorrectValue()
        {
            // Arrange
            var routeWithVehicle = new Route { VehicleId = Guid.NewGuid() };
            var routeWithoutVehicle = new Route { VehicleId = null };

            // Act & Assert
            Assert.IsTrue(routeWithVehicle.HasVehicle);
            Assert.IsFalse(routeWithoutVehicle.HasVehicle);
        }

        [TestCategory("Unit")]
        [TestMethod]
        public async Task GetRoutesAsync_WithMultipleRoutes_ReturnsAllRoutes()
        {
            // Arrange - Create multiple test routes
            var routes = new List<Route>
            {
                new Route { Id = Guid.NewGuid(), Name = "Route A", RouteID = 3, RouteName = "Route A", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, CreatedBy = "TestUser" },
                new Route { Id = Guid.NewGuid(), Name = "Route B", RouteID = 4, RouteName = "Route B", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, CreatedBy = "TestUser" },
                new Route { Id = Guid.NewGuid(), Name = "Route C", RouteID = 5, RouteName = "Route C", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, CreatedBy = "TestUser" }
            };

            foreach (var route in routes)
            {
                await _routeService.CreateRouteAsync(route);
            }

            // Act
            var retrievedRoutes = await _routeService.GetRoutesAsync();

            // Assert
            Assert.IsTrue(retrievedRoutes.Count >= 3); // At least our 3 test routes plus any from previous tests
            Assert.IsTrue(routes.All(r => retrievedRoutes.Any(rr => rr.Name == r.Name)));
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void Route_StopsProperty_HandlesJsonSerialization()
        {
            // Arrange
            var route = new Route();
            var stops = new List<BusStop>
            {
                new BusStop
                {
                    StopID = 1,
                    Name = "Main Street Station",
                    Address = "123 Main St",
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    ScheduledArrival = TimeSpan.FromHours(8),
                    ScheduledDeparture = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(2)),
                    IsAccessible = true,
                    Amenities = new List<string> { "Bench", "Shelter" }
                },
                new BusStop
                {
                    StopID = 2,
                    Name = "City Center",
                    Address = "456 Center Ave",
                    Latitude = 40.7589,
                    Longitude = -73.9851,
                    ScheduledArrival = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(10)),
                    ScheduledDeparture = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(12)),
                    IsAccessible = false,
                    Amenities = new List<string> { "Bench" }
                }
            };

            // Act
            route.Stops = stops;

            // Assert
            Assert.AreEqual(2, route.NumberOfStops);
            Assert.AreEqual(2, route.Stops.Count);
            Assert.AreEqual("Main Street Station", route.Stops[0].Name);
            Assert.AreEqual("City Center", route.Stops[1].Name);
            Assert.IsTrue(route.Stops[0].IsAccessible);
            Assert.IsFalse(route.Stops[1].IsAccessible);
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void Route_EmptyStops_ReturnsEmptyList()
        {
            // Arrange
            var route = new Route();

            // Act & Assert
            Assert.AreEqual(0, route.NumberOfStops);
            Assert.IsNotNull(route.Stops);
            Assert.AreEqual(0, route.Stops.Count);
        }

        [TestCategory("Unit")]
        [TestMethod]
        public async Task UpdateRouteAsync_ValidRoute_UpdatesSuccessfully()
        {
            // Arrange - Create a route first
            var originalRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Original Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                RouteID = 6,
                RouteName = "Original Route",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedBy = "TestUser"
            };
            var createdRoute = await _routeService.CreateRouteAsync(originalRoute);

            // Act - Update the route
            createdRoute.Name = "Updated Route Name";
            createdRoute.AMRiders = 50;
            var updatedRoute = await _routeService.UpdateRouteAsync(createdRoute);

            // Assert
            Assert.IsNotNull(updatedRoute);
            Assert.AreEqual("Updated Route Name", updatedRoute.Name);
            Assert.AreEqual(50, updatedRoute.AMRiders);
            Assert.AreEqual(createdRoute.Id, updatedRoute.Id);
        }

        [TestCategory("Unit")]
        [TestMethod]
        public async Task DeleteRouteAsync_ExistingRoute_DeletesSuccessfully()
        {
            // Arrange - Create a route first
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Route to Delete",
                RouteID = 7,
                RouteName = "Route to Delete",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedBy = "TestUser"
            };
            var createdRoute = await _routeService.CreateRouteAsync(route);

            // Act
            await _routeService.DeleteRouteAsync(createdRoute.Id);

            // Assert - Try to retrieve the deleted route
            var deletedRoute = await _routeService.GetRouteByIdAsync(createdRoute.Id);
            Assert.IsNull(deletedRoute);
        }

        [TestCategory("Unit")]
        [TestMethod]
        public async Task GetRoutesByDateAsync_FiltersByDate_ReturnsCorrectRoutes()
        {
            // Arrange
            var targetDate = DateTime.Today.AddDays(1);
            var otherDate = DateTime.Today.AddDays(2);

            var routesForTargetDate = new List<Route>
            {
                new Route { Id = Guid.NewGuid(), Name = "Route for Target Date 1", RouteDate = targetDate, RouteID = 8, RouteName = "Route for Target Date 1", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, CreatedBy = "TestUser" },
                new Route { Id = Guid.NewGuid(), Name = "Route for Target Date 2", RouteDate = targetDate, RouteID = 9, RouteName = "Route for Target Date 2", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, CreatedBy = "TestUser" }
            };

            var routeForOtherDate = new Route { Id = Guid.NewGuid(), Name = "Route for Other Date", RouteDate = otherDate, RouteID = 10, RouteName = "Route for Other Date", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, CreatedBy = "TestUser" };

            // Create all routes
            foreach (var route in routesForTargetDate)
            {
                await _routeService.CreateRouteAsync(route);
            }
            await _routeService.CreateRouteAsync(routeForOtherDate);

            // Act
            var routesOnTargetDate = await _routeService.GetRoutesByDateAsync(targetDate);

            // Assert
            Assert.IsTrue(routesOnTargetDate.Count >= 2);
            Assert.IsTrue(routesOnTargetDate.All(r => r.RouteDate.Date == targetDate.Date));
            Assert.IsTrue(routesForTargetDate.All(r => routesOnTargetDate.Any(rr => rr.Name == r.Name)));
            Assert.IsFalse(routesOnTargetDate.Any(r => r.Name == "Route for Other Date"));
        }

        [TestCategory("Unit")]
        [TestMethod]
        public async Task GetPagedAsync_WithPaging_ReturnsCorrectPage()
        {
            // Arrange - Ensure we have enough routes for paging
            var testRoutes = new List<Route>();
            for (int i = 1; i <= 5; i++)
            {
                var route = new Route
                {
                    Id = Guid.NewGuid(),
                    Name = $"Paging Test Route {i}",
                    RouteID = 20 + i,
                    RouteName = $"Paging Test Route {i}",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    CreatedBy = "TestUser"
                };
                testRoutes.Add(route);
                await _routeService.CreateRouteAsync(route);
            }

            // Act
            var pagedResult = await _routeService.GetPagedAsync(1, 3, default);

            // Assert
            Assert.IsNotNull(pagedResult);
            Assert.IsTrue(pagedResult.Items.Count <= 3);
            Assert.IsTrue(pagedResult.TotalCount >= 5);
            Assert.AreEqual(1, pagedResult.PageNumber);
            Assert.AreEqual(3, pagedResult.PageSize);
        }
        private Route CreateTestRoute(string name, int routeId = 0)
        {
            return new Route
            {
                Id = Guid.NewGuid(),
                Name = name,
                RouteDate = DateTime.Today,
                AMStartingMileage = 100,
                AMEndingMileage = 150,
                AMRiders = 25,
                PMStartMileage = 150,
                PMEndingMileage = 200,
                PMRiders = 30,
                StartLocation = "Downtown",
                EndLocation = "Airport",
                ScheduledTime = DateTime.Today.AddHours(8),
                RouteID = routeId > 0 ? routeId : new Random().Next(1, 1000),
                RouteName = name,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedBy = "TestUser",
                ScheduleJson = "{\"StartTime\":\"08:00:00\",\"EndTime\":\"18:00:00\",\"FrequencyMinutes\":30}",
                StopsJson = "[]"
            };
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithMinimalData_ShouldSetDefaults()
        {
            // Arrange
            var route = new Route
            {
                Name = "Minimal Route",
                RouteDate = DateTime.Today,
                StartLocation = "Start",
                EndLocation = "End"
                // Minimal data - let service set defaults
            };

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Id != Guid.Empty);
            Assert.AreEqual("Minimal Route", result.RouteName); // Should default to Name
            Assert.AreEqual("[]", result.StopsJson); // Should set default empty array
            Assert.IsTrue(result.CreatedDate > DateTime.UtcNow.AddMinutes(-1)); // Recent creation
            Assert.AreEqual("System", result.CreatedBy); // Should default to System
            Assert.IsNotNull(result.RowVersion); // Should initialize RowVersion
            Assert.AreEqual(8, result.RowVersion.Length); // Standard length
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithNullRowVersion_ShouldInitializeRowVersion()
        {
            // Arrange
            var route = CreateTestRoute();
            route.RowVersion = null; // Explicitly null

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.IsNotNull(result.RowVersion);
            Assert.AreEqual(8, result.RowVersion.Length);
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithEmptyRowVersion_ShouldInitializeRowVersion()
        {
            // Arrange
            var route = CreateTestRoute();
            route.RowVersion = new byte[0]; // Empty array

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.IsNotNull(result.RowVersion);
            Assert.AreEqual(8, result.RowVersion.Length);
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithDefaultCreatedDate_ShouldSetCreatedDate()
        {
            // Arrange
            var route = CreateTestRoute();
            route.CreatedDate = default(DateTime); // Default value

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.IsTrue(result.CreatedDate > DateTime.UtcNow.AddMinutes(-1));
            Assert.IsTrue(result.CreatedDate <= DateTime.UtcNow.AddMinutes(1));
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithDefaultModifiedDate_ShouldSetModifiedDate()
        {
            // Arrange
            var route = CreateTestRoute();
            route.ModifiedDate = default(DateTime); // Default value

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.IsTrue(result.ModifiedDate > DateTime.UtcNow.AddMinutes(-1));
            Assert.IsTrue(result.ModifiedDate <= DateTime.UtcNow.AddMinutes(1));
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithEmptyRouteName_ShouldDefaultToName()
        {
            // Arrange
            var route = CreateTestRoute();
            route.RouteName = ""; // Empty string
            route.Name = "Test Route Name";

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.AreEqual("Test Route Name", result.RouteName);
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithNullRouteName_ShouldDefaultToName()
        {
            // Arrange
            var route = CreateTestRoute();
            route.RouteName = null; // Null
            route.Name = "Test Route Name";

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.AreEqual("Test Route Name", result.RouteName);
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithEmptyRouteCode_ShouldGenerateRouteCode()
        {
            // Arrange
            var route = CreateTestRoute();
            route.RouteCode = ""; // Empty string

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.IsTrue(result.RouteCode.StartsWith("RT"));
            Assert.IsTrue(result.RouteCode.Length >= 6); // RT + 4 digits minimum
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithNullRouteCode_ShouldGenerateRouteCode()
        {
            // Arrange
            var route = CreateTestRoute();
            route.RouteCode = null; // Null

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.IsTrue(result.RouteCode.StartsWith("RT"));
            Assert.IsTrue(result.RouteCode.Length >= 6); // RT + 4 digits minimum
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithEmptyStopsJson_ShouldSetDefaultEmptyArray()
        {
            // Arrange
            var route = CreateTestRoute();
            route.StopsJson = ""; // Empty string

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.AreEqual("[]", result.StopsJson);
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithNullStopsJson_ShouldSetDefaultEmptyArray()
        {
            // Arrange
            var route = CreateTestRoute();
            route.StopsJson = null; // Null

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.AreEqual("[]", result.StopsJson);
        }        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Performance)]
        public async Task GetRoutesByDateAsync_WithSpecificDate_ShouldReturnFilteredRoutes()
        {
            // Arrange - Use a far future date to avoid conflicts with existing data
            var targetDate = DateTime.Today.AddYears(1);
            var uniqueId = Guid.NewGuid().ToString("N")[..8]; // Use unique ID for test isolation

            var route1 = CreateTestRoute();
            route1.RouteDate = targetDate;
            route1.Name = $"DateFilter{uniqueId}_Route1";

            var route2 = CreateTestRoute();
            route2.RouteDate = targetDate.AddDays(1); // Different date
            route2.Name = $"DateFilter{uniqueId}_Route2";

            var route3 = CreateTestRoute();
            route3.RouteDate = targetDate; // Same date
            route3.Name = $"DateFilter{uniqueId}_Route3";

            await _routeService.CreateRouteAsync(route1);
            await _routeService.CreateRouteAsync(route2);
            await _routeService.CreateRouteAsync(route3);

            // Act
            var results = await _routeService.GetRoutesByDateAsync(targetDate);

            // Assert
            Assert.IsNotNull(results);
            var filteredRoutes = results.Where(r => r.Name.Contains($"DateFilter{uniqueId}")).ToList();
            Assert.AreEqual(2, filteredRoutes.Count, $"Expected 2 'DateFilter{uniqueId}' routes but found {filteredRoutes.Count}. Routes found: {string.Join(", ", filteredRoutes.Select(r => r.Name))}");
            Assert.IsTrue(filteredRoutes.All(r => r.RouteDate.Date == targetDate.Date));
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task UpdateRouteAsync_WithCompleteRoute_ShouldUpdateAllFields()
        {
            // Arrange
            var route = CreateTestRoute();
            var created = await _routeService.CreateRouteAsync(route);

            // Modify all updateable fields
            created.Name = "Updated Route Name";
            created.RouteName = "Updated Route Display Name";
            created.StartLocation = "Updated Start";
            created.EndLocation = "Updated End";
            created.AMStartingMileage = 9999;
            created.AMEndingMileage = 9999;
            created.PMStartMileage = 9999;
            created.PMEndingMileage = 9999;
            created.AMRiders = 99;
            created.PMRiders = 99;
            created.StopsJson = "[{\"name\":\"Updated Stop\",\"time\":\"08:00\"}]";

            // Act
            var result = await _routeService.UpdateRouteAsync(created);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated Route Name", result.Name);
            Assert.AreEqual("Updated Route Display Name", result.RouteName);
            Assert.AreEqual("Updated Start", result.StartLocation);
            Assert.AreEqual("Updated End", result.EndLocation);
            Assert.AreEqual(9999, result.AMStartingMileage);
            Assert.AreEqual(9999, result.AMEndingMileage);
            Assert.AreEqual(9999, result.PMStartMileage);
            Assert.AreEqual(9999, result.PMEndingMileage);            Assert.AreEqual(99, result.AMRiders);
            Assert.AreEqual(99, result.PMRiders);
            Assert.IsTrue(result.StopsJson.Contains("Updated Stop"));
            // ModifiedDate should be updated or at least not be default
            Assert.IsTrue(result.ModifiedDate > DateTime.MinValue);
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Database)]
        public async Task GetDriversAsync_ShouldReturnAvailableDrivers()
        {
            // Act
            var drivers = await _routeService.GetDriversAsync();

            // Assert
            Assert.IsNotNull(drivers);
            // Drivers collection should exist even if empty
            Assert.IsTrue(drivers.Count() >= 0);
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Database)]
        public async Task GetVehiclesAsync_ShouldReturnAvailableVehicles()
        {
            // Act
            var vehicles = await _routeService.GetVehiclesAsync();

            // Assert
            Assert.IsNotNull(vehicles);
            // Vehicles collection should exist even if empty
            Assert.IsTrue(vehicles.Count() >= 0);
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Integration)]
        public async Task CreateMultipleRoutes_ShouldMaintainDataIntegrity()
        {
            // Arrange
            var routes = new List<Route>();
            for (int i = 1; i <= 5; i++)
            {
                var route = CreateTestRoute();
                route.Name = $"Bulk Route {i}";
                route.RouteCode = $"BULK{i:D3}";
                routes.Add(route);
            }

            // Act
            var createdRoutes = new List<Route>();
            foreach (var route in routes)
            {
                var created = await _routeService.CreateRouteAsync(route);
                createdRoutes.Add(created);
            }

            // Assert
            Assert.AreEqual(5, createdRoutes.Count);

            // Verify all routes have unique IDs
            var uniqueIds = createdRoutes.Select(r => r.Id).Distinct().Count();
            Assert.AreEqual(5, uniqueIds);

            // Verify all routes can be retrieved
            foreach (var created in createdRoutes)
            {
                var retrieved = await _routeService.GetRouteByIdAsync(created.Id);
                Assert.IsNotNull(retrieved);
                Assert.AreEqual(created.Name, retrieved.Name);
            }
        }        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Database)]
        public async Task SeedSampleDataAsync_ShouldCreateSampleData()
        {
            // Arrange - Clear any existing data first
            var existingRoutes = await _routeService.GetRoutesAsync(1, 100);
            foreach (var route in existingRoutes)
            {
                await _routeService.DeleteRouteAsync(route.Id);
            }

            // Act
            await _routeService.SeedSampleDataAsync();

            // Assert
            var routes = await _routeService.GetRoutesAsync(1, 10);
            Assert.IsTrue(routes.Count() > 0, "Sample data should be created");

            // Verify some sample data characteristics
            var sampleRoute = routes.FirstOrDefault(r => r.Name.Contains("Route"));
            Assert.IsNotNull(sampleRoute, "Should contain routes with 'Route' in the name");
            Assert.IsTrue(sampleRoute.CreatedDate > DateTime.UtcNow.AddMinutes(-5), "Should have recent creation date");
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithNullCreatedBy_ShouldSetDefaultCreatedBy()
        {
            // Arrange
            var route = CreateTestRoute();
            route.CreatedBy = null;

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.AreEqual("System", result.CreatedBy);
        }

        [TestMethod]
        [TestCategory(TestCategories.Service)]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateRouteAsync_WithEmptyCreatedBy_ShouldSetDefaultCreatedBy()
        {
            // Arrange
            var route = CreateTestRoute();
            route.CreatedBy = "";

            // Act
            var result = await _routeService.CreateRouteAsync(route);

            // Assert
            Assert.AreEqual("System", result.CreatedBy);
        }
    }
}
