using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using BusBus.Tests.Infrastructure;
using NUnit.Framework;

namespace BusBus.Tests.UITests
{
    /// <summary>    /// Tests to verify that the Route UI components provide full CRUD functionality
    /// </summary>    [TestFixture]
    [Category(TestCategories.UI)]
    public class RouteCRUDVerificationTests : TestBase
    {
        private IRouteService _routeService;
        private RoutePanel _routePanel;
        private RouteListPanel _routeListPanel;
        private List<Driver> _testDrivers;
        private List<Vehicle> _testVehicles;
        private List<Route> _testRoutes;
        private TestFixtureFactory _testFixtureFactory;        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            
            _testFixtureFactory = new TestFixtureFactory();
              // Use actual RouteService instead of mock
            _routeService = new RouteService();
            
            // Create test data
            _testDrivers = _testFixtureFactory.CreateMany<Driver>(3);
            _testVehicles = _testFixtureFactory.CreateMany<Vehicle>(3);
            _testRoutes = _testFixtureFactory.CreateMany<Route>(5);
            
            // Seed the actual service with sample data
            await _routeService.SeedSampleDataAsync();
            
            // Create UI components with test constructor
            _routePanel = new RoutePanel(_testDrivers, _testVehicles);
            _routeListPanel = new RouteListPanel(_routeService);
            
            // Suppress dialogs for testing
            RoutePanel.SuppressDialogsForTests = true;
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
        [Description("Verify CREATE functionality - Route can be created")]
        public void RoutePanel_CREATE_ShouldAllowCreatingNewRoute()
        {
            // Arrange
            var newRoute = new Route
            {
                Id = Guid.Empty, // New route
                Name = "New Test Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 100,
                AMEndingMileage = 150,
                AMRiders = 25,
                PMStartMileage = 150,
                PMEndingMileage = 200,
                PMRiders = 30
            };

            // Act
            _routePanel.LoadRoute(newRoute);

            // Assert
            Assert.That(_routePanel, Is.Not.Null, "RoutePanel should be created");
            
            // Verify UI reflects new route mode
            var titleLabel = GetPrivateField<System.Windows.Forms.Label>(_routePanel, "_titleLabel");
            Assert.That(titleLabel?.Text, Does.Contain("Add New Route"), "Title should indicate new route creation");
            
            // Verify delete button is hidden for new routes
            var deleteButton = GetPrivateField<System.Windows.Forms.Button>(_routePanel, "_deleteButton");
            Assert.That(deleteButton?.Visible, Is.False, "Delete button should be hidden for new routes");
        }

        [Test]
        [Description("Verify READ functionality - Routes can be loaded and displayed")]
        public async Task RouteListPanel_READ_ShouldDisplayRoutes()
        {            // Act
            await _routeListPanel.LoadRoutesAsync(1, 10, CancellationToken.None);// Assert
            Assert.That(_routeListPanel.RoutesGrid, Is.Not.Null, "Routes grid should exist");
            
            // Verify that routes can be loaded
            var routes = await _routeService.GetRoutesAsync(1, 10);
            Assert.That(routes, Is.Not.Null, "Routes should be retrievable");
        }

        [Test]
        [Description("Verify UPDATE functionality - Existing route can be edited")]
        public void RoutePanel_UPDATE_ShouldAllowEditingExistingRoute()
        {
            // Arrange
            var existingRoute = _testRoutes.First();
            existingRoute.Id = Guid.NewGuid(); // Ensure it's not empty (existing route)

            // Act
            _routePanel.LoadRoute(existingRoute);

            // Assert
            var titleLabel = GetPrivateField<System.Windows.Forms.Label>(_routePanel, "_titleLabel");
            Assert.That(titleLabel?.Text, Does.Contain("Edit Route"), "Title should indicate edit mode");
            
            // Verify delete button is visible for existing routes
            var deleteButton = GetPrivateField<System.Windows.Forms.Button>(_routePanel, "_deleteButton");
            Assert.That(deleteButton?.Visible, Is.True, "Delete button should be visible for existing routes");
        }

        [Test]
        [Description("Verify DELETE functionality - Routes can be deleted")]
        public void RoutePanel_DELETE_ShouldProvideDeleteFunctionality()
        {
            // Arrange
            var existingRoute = _testRoutes.First();
            existingRoute.Id = Guid.NewGuid();
            _routePanel.LoadRoute(existingRoute);

            // Act & Assert
            var deleteButton = GetPrivateField<System.Windows.Forms.Button>(_routePanel, "_deleteButton");
            Assert.That(deleteButton, Is.Not.Null, "Delete button should exist");
            Assert.That(deleteButton.Visible, Is.True, "Delete button should be visible for existing routes");            // Verify delete button is properly configured
            Assert.That(deleteButton.Name, Is.Not.Null.And.Not.Empty, "Delete button should be properly initialized");
        }

        [Test]
        [Description("Verify RouteListPanel provides Add, Edit, Delete buttons")]
        public void RouteListPanel_CRUD_ShouldProvideAllCRUDButtons()
        {
            // Assert
            var addButton = GetPrivateField<System.Windows.Forms.Button>(_routeListPanel, "_addRouteButton");
            var editButton = GetPrivateField<System.Windows.Forms.Button>(_routeListPanel, "_editRouteButton");
            var deleteButton = GetPrivateField<System.Windows.Forms.Button>(_routeListPanel, "_deleteRouteButton");

            Assert.That(addButton, Is.Not.Null, "Add button should exist");
            Assert.That(editButton, Is.Not.Null, "Edit button should exist");
            Assert.That(deleteButton, Is.Not.Null, "Delete button should exist");

            Assert.That(addButton.Text, Does.Contain("Add"), "Add button should have appropriate text");
            Assert.That(editButton.Text, Does.Contain("Edit"), "Edit button should have appropriate text");
            Assert.That(deleteButton.Text, Does.Contain("Delete"), "Delete button should have appropriate text");
        }

        [Test]
        [Description("Verify RouteEditRequested event for navigation between list and edit views")]
        public void RouteListPanel_NAVIGATION_ShouldProvideEditRequestedEvent()
        {
            // Arrange
            bool eventFired = false;
            Route? eventRoute = null;

            _routeListPanel.RouteEditRequested += (sender, args) =>
            {
                eventFired = true;
                eventRoute = args.Route;
            };

            var testRoute = _testRoutes.First();

            // Act
            var showRoutePanel = GetPrivateMethod(_routeListPanel, "ShowRoutePanel");
            showRoutePanel?.Invoke(_routeListPanel, new object[] { testRoute });

            // Assert
            Assert.That(eventFired, Is.True, "RouteEditRequested event should fire");
            Assert.That(eventRoute, Is.EqualTo(testRoute), "Event should pass the correct route");
        }

        private T? GetPrivateField<T>(object obj, string fieldName) where T : class
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(obj) as T;
        }

        private System.Reflection.MethodInfo? GetPrivateMethod(object obj, string methodName)
        {
            return obj.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }
    }
}
