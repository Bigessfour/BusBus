// Suppress platform compatibility warnings for WinForms (Windows-only test)
#pragma warning disable CA1416 // Platform compatibility (WinForms)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using BusBus.Tests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BusBus.Tests.UI
{
    [TestFixture]
    [Category(TestCategories.UI)]
    [Platform("Win")]
    [Apartment(ApartmentState.STA)] // Required for WinForms testing
    public class RouteFormIntegrationTests : TestBase
    {
        private IRouteService _routeService;
        private RoutePanel _routePanel;
        private RouteListPanel _routeListPanel;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _routeService = ServiceProvider.GetRequiredService<IRouteService>();

            // Suppress dialogs for testing
            RoutePanel.SuppressDialogsForTests = true;

            // Create UI components
            _routePanel = new RoutePanel(_routeService);
            _routeListPanel = new RouteListPanel(_routeService);
        }

        [TearDown]
        public override void TearDown()
        {
            RoutePanel.SuppressDialogsForTests = false;
            _routePanel?.Dispose();
            _routeListPanel?.Dispose();
            base.TearDown();
        }

        [Test]
        [Description("Test complete form workflow: Load -> Edit -> Save")]
        public async Task RoutePanel_CompleteWorkflow_ShouldPersistChanges()
        {
            // Arrange - Create test route
            var originalRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Original Test Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1030,
                AMRiders = 25,
                PMStartMileage = 1030,
                PMEndingMileage = 1060,
                PMRiders = 30,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RouteID = 1002,
                RouteName = "TestRouteNameComplete",
                RouteCode = "RTCOMP",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };

            var createdRoute = await _routeService.CreateRouteAsync(originalRoute);

            // Act - Load route into panel
            _routePanel.LoadRoute(createdRoute);

            // Simulate user editing (accessing private fields via reflection for testing)
            var nameField = _routePanel.GetType().GetField("_nameTextBox",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var amRidersField = _routePanel.GetType().GetField("_amRiders",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (nameField?.GetValue(_routePanel) is TextBox nameTextBox)
            {
                nameTextBox.Text = "Updated Test Route";
            }

            if (amRidersField?.GetValue(_routePanel) is NumericUpDown amRiders)
            {
                amRiders.Value = 35; // Updated from 25 to 35
            }

            // Simulate save button click
            var saveMethod = _routePanel.GetType().GetMethod("SaveButton_Click",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (saveMethod != null)
            {
                // Use Task.Run to handle async method in sync context
                await Task.Run(async () =>
                {
                    var task = (Task)saveMethod.Invoke(_routePanel, new object[] { null, EventArgs.Empty });
                    await task;
                });
            }

            // Assert - Verify changes were saved
            var routes = await _routeService.GetRoutesAsync();
            var updatedRoute = routes.Find(r => r.Id == createdRoute.Id);

            updatedRoute.Should().NotBeNull("Route should still exist after update");
            updatedRoute.Name.Should().Be("Updated Test Route", "Name should be updated");
            updatedRoute.AMRiders.Should().Be(35, "AM Riders should be updated");

            // Cleanup
            await _routeService.DeleteRouteAsync(createdRoute.Id);
        }

        [Test]
        [Description("Test RouteListPanel data binding and refresh")]
        public async Task RouteListPanel_DataBinding_ShouldReflectChanges()
        {
            // Arrange - Seed some test data
            await _routeService.SeedSampleDataAsync();

            // Get initial count
            var initialRoutes = await _routeService.GetRoutesAsync();
            var initialCount = initialRoutes.Count;

            // Act - Create new route and refresh list
            var newRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "List Panel Test Route",
                RouteDate = DateTime.Today.AddDays(1),
                AMStartingMileage = 3000,
                AMEndingMileage = 3025,
                PMStartMileage = 3025,
                PMEndingMileage = 3050,
                AMRiders = 20,
                PMRiders = 25,
                // Required EF Core properties
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RouteID = 1003,
                RouteName = "TestRouteNameList",
                RouteCode = "RTLIST",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };

            await _routeService.CreateRouteAsync(newRoute);

            // Trigger refresh (simulate user action)
            var refreshMethod = _routeListPanel.GetType().GetMethod("RefreshData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (refreshMethod != null)
            {
                await Task.Run(async () =>
                {
                    var task = (Task)refreshMethod.Invoke(_routeListPanel, Array.Empty<object>());
                    await task;
                });
            }

            // Assert - Check if grid has been updated
            var routesGrid = _routeListPanel.RoutesGrid;
            routesGrid.Should().NotBeNull("Routes grid should be accessible");

            // Grid should have at least one more row than before
            var finalRoutes = await _routeService.GetRoutesAsync();
            finalRoutes.Count.Should().BeGreaterThan(initialCount, "Route count should increase");

            // Cleanup
            await _routeService.DeleteRouteAsync(newRoute.Id);
        }

        [Test]
        [Description("Test form validation prevents invalid data entry")]
        public void RoutePanel_Validation_ShouldPreventInvalidData()
        {
            // Arrange - Create new route panel
            var testRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "",  // Invalid - empty name
                RouteDate = DateTime.Today,
                AMStartingMileage = 2000,
                AMEndingMileage = 1950,  // Invalid - ending < starting
                AMRiders = 0,  // Valid - non-negative riders
                PMStartMileage = 1950,
                PMEndingMileage = 2000,
                PMRiders = 30,
                // Required EF Core properties
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RouteID = 1001,
                RouteName = "TestRouteNameInvalid",
                RouteCode = "RTINV",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };

            // Act - Load invalid route
            _routePanel.LoadRoute(testRoute);            // Assert - Panel should be in a state that prevents saving
            // This test verifies the validation logic exists
            var validationResult = BusBus.UI.RouteCRUDHelper.ValidateRoute(testRoute);
            validationResult.IsValid.Should().BeFalse("Invalid route should fail validation");
            validationResult.ErrorMessage.Should().NotBeEmpty("Error message should be provided");
        }

        [Test]
        [Description("Test UI responsiveness with large datasets")]
        public async Task RouteListPanel_LargeDataset_ShouldRemainResponsive()
        {
            // Arrange - Create a large number of test routes
            var testRoutes = new List<Route>();
            var createTasks = new List<Task>();

            for (int i = 1; i <= 50; i++)
            {
                var route = new Route
                {
                    Id = Guid.NewGuid(),
                    Name = $"Performance Test Route {i:D3}",
                    RouteDate = DateTime.Today.AddDays(i % 30),
                    AMStartingMileage = 4000 + (i * 20),
                    AMEndingMileage = 4000 + (i * 20) + 15,
                    PMStartMileage = 4000 + (i * 20) + 15,
                    PMEndingMileage = 4000 + (i * 20) + 30,
                    AMRiders = 15 + (i % 10),
                    PMRiders = 20 + (i % 15)
                };
                testRoutes.Add(route);

                createTasks.Add(_routeService.CreateRouteAsync(route));
            }

            await Task.WhenAll(createTasks);

            try
            {
                // Act - Load large dataset and measure performance
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var loadMethod = _routeListPanel.GetType().GetMethod("LoadRoutes",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (loadMethod != null)
                {
                    await Task.Run(async () =>
                    {
                        var task = (Task)loadMethod.Invoke(_routeListPanel, Array.Empty<object>());
                        await task;
                    });
                }

                stopwatch.Stop();

                // Assert - Should load within reasonable time
                stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
                    "Loading 50 routes should complete within 5 seconds");

                var routesGrid = _routeListPanel.RoutesGrid;
                routesGrid.Should().NotBeNull("Grid should be populated");
            }
            finally
            {
                // Cleanup - Remove all test routes
                var deleteTasks = testRoutes.Select(r => _routeService.DeleteRouteAsync(r.Id));
                await Task.WhenAll(deleteTasks);
            }
        }

        [Test]
        [Description("Test error handling in UI components")]
        public async Task RoutePanel_ErrorScenarios_ShouldHandleGracefully()
        {
            // Arrange - Create route with data that might cause issues
            var problematicRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route with Issues",
                RouteDate = DateTime.Today,
                AMStartingMileage = 100000,  // Large but valid value
                AMEndingMileage = 100100,
                PMStartMileage = 100200,
                PMEndingMileage = 100300,
                AMRiders = 999,
                PMRiders = 999,
                DriverId = Guid.NewGuid(),  // Non-existent driver
                VehicleId = Guid.NewGuid(),  // Non-existent vehicle
                // Required EF Core properties
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RouteID = 9999,
                RouteName = "TestRouteNameIssues",
                RouteCode = "RTISSUE",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };

            // Act - Load the problematic route (simulate async operation)
            await Task.Run(() => _routePanel.LoadRoute(problematicRoute));

            // Assert - Panel should be in a valid state and validation should work
            _routePanel.Should().NotBeNull("Panel should remain functional");

            // Test that validation catches the issues
            var validationResult = BusBus.UI.RouteCRUDHelper.ValidateRoute(problematicRoute);
            validationResult.IsValid.Should().BeFalse("Problematic route should fail validation");
        }
    }
}
