// Suppress CS8618, CS8600, CS8602: Non-nullable field is uninitialized or possible null reference. Safe for test code with [SetUp] initialization.
#pragma warning disable CS8618, CS8600, CS8602
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using NSubstitute;

namespace BusBus.Tests
{
    [TestFixture]
    public class RoutePanelTests : TestBase
    {
        private IRouteService _mockRouteService;
        private RoutePanel _routePanel;
        private Driver _driver;
        private Vehicle _vehicle;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _mockRouteService = Substitute.For<IRouteService>();
            _driver = new Driver { Id = Guid.NewGuid(), FirstName = "Test", LastName = "Driver", Name = "Test Driver" };
            _vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "123", Name = "Bus 123", Number = "123" };
            _routePanel = new RoutePanel(_mockRouteService);
            // Inject drivers and vehicles for combo box population
            typeof(RoutePanel).GetField("_drivers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_routePanel, new System.Collections.Generic.List<Driver> { _driver });
            typeof(RoutePanel).GetField("_vehicles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_routePanel, new System.Collections.Generic.List<Vehicle> { _vehicle });
        }

        [TearDown]
        public override void TearDown()
        {
            (_routePanel as IDisposable)?.Dispose();
            base.TearDown();
        }

        [Test]
        public async Task SaveRouteAsync_CreatesNewRoute_WhenCurrentRouteIsNull()
        {
            // Arrange
            var route = default(Route);
            _mockRouteService.CreateRouteAsync(Arg.Any<Route>()).Returns(ci =>
            {
                route = ci.Arg<Route>();
                return Task.FromResult(route);
            });
            typeof(RoutePanel).GetField("_currentRoute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_routePanel, null);
            // Set up control values
            SetNumeric(_routePanel, "_amStartingMileage", 10);
            SetNumeric(_routePanel, "_amEndingMileage", 20);
            SetNumeric(_routePanel, "_amRiders", 5);
            SetNumeric(_routePanel, "_pmStartMileage", 30);
            SetNumeric(_routePanel, "_pmEndingMileage", 40);
            SetNumeric(_routePanel, "_pmRiders", 7);
            SetComboBox(_routePanel, "_driverComboBox", 1); // Select first driver
            SetComboBox(_routePanel, "_vehicleComboBox", 1); // Select first vehicle
            // Act
            var saveRouteAsync = typeof(RoutePanel).GetMethod("SaveRouteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = (Task<bool>)saveRouteAsync.Invoke(_routePanel, null);
            var result = await task;
            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(route);
            Assert.AreEqual(_driver.Id, route.DriverId);
            Assert.AreEqual(_vehicle.Id, route.VehicleId);
            Assert.AreEqual(10, route.AMStartingMileage);
            Assert.AreEqual(20, route.AMEndingMileage);
            Assert.AreEqual(5, route.AMRiders);
            Assert.AreEqual(30, route.PMStartMileage);
            Assert.AreEqual(40, route.PMEndingMileage);
            Assert.AreEqual(7, route.PMRiders);
            await _mockRouteService.Received(1).CreateRouteAsync(Arg.Any<Route>());
        }

        [Test]
        public async Task SaveRouteAsync_UpdatesExistingRoute_WhenCurrentRouteIsNotNull()
        {
            // Arrange
            var existingRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Existing Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1,
                AMEndingMileage = 2,
                AMRiders = 3,
                PMStartMileage = 4,
                PMEndingMileage = 5,
                PMRiders = 6,
                DriverId = _driver.Id,
                VehicleId = _vehicle.Id
            };
            typeof(RoutePanel).GetField("_currentRoute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_routePanel, existingRoute);
            _mockRouteService.UpdateRouteAsync(Arg.Any<Route>()).Returns(ci => Task.FromResult(ci.Arg<Route>()));
            // Set up control values
            SetNumeric(_routePanel, "_amStartingMileage", 100);
            SetNumeric(_routePanel, "_amEndingMileage", 200);
            SetNumeric(_routePanel, "_amRiders", 50);
            SetNumeric(_routePanel, "_pmStartMileage", 300);
            SetNumeric(_routePanel, "_pmEndingMileage", 400);
            SetNumeric(_routePanel, "_pmRiders", 70);
            SetComboBox(_routePanel, "_driverComboBox", 1); // Select first driver
            SetComboBox(_routePanel, "_vehicleComboBox", 1); // Select first vehicle
            // Act
            var saveRouteAsync = typeof(RoutePanel).GetMethod("SaveRouteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = (Task<bool>)saveRouteAsync.Invoke(_routePanel, null);
            var result = await task;
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(100, existingRoute.AMStartingMileage);
            Assert.AreEqual(200, existingRoute.AMEndingMileage);
            Assert.AreEqual(50, existingRoute.AMRiders);
            Assert.AreEqual(300, existingRoute.PMStartMileage);
            Assert.AreEqual(400, existingRoute.PMEndingMileage);
            Assert.AreEqual(70, existingRoute.PMRiders);
            await _mockRouteService.Received(1).UpdateRouteAsync(Arg.Any<Route>());
        }

        private void SetNumeric(RoutePanel panel, string fieldName, decimal value)
        {
            var field = typeof(RoutePanel).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var numeric = (NumericUpDown)field.GetValue(panel);
            // Ensure value is within the allowed range
            if (value < numeric.Minimum) value = numeric.Minimum;
            if (value > numeric.Maximum) numeric.Maximum = value;
            numeric.Value = value;
        }

        private void SetComboBox(RoutePanel panel, string fieldName, int index)
        {
            var field = typeof(RoutePanel).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var combo = (ComboBox)field.GetValue(panel);
            combo.Items.Clear();
            combo.Items.Add("Unassigned");
            combo.Items.Add("Test");
            combo.SelectedIndex = index;
        }
    }
}
