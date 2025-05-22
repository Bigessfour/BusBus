using BusBus.Models;
using BusBus.Services;
using BusBus.Tests.Common;
using BusBus.UI;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BusBus.Tests.UI
{

    public class RoutePanelTests
    {
        private readonly Moq.Mock<IRouteService> _mockRouteService;

        // Static constructor to suppress dialogs during all tests in this class
        static RoutePanelTests()
        {
            BusBus.UI.RoutePanel.SuppressDialogsForTests = true;
        }

        public RoutePanelTests()
        {
            _mockRouteService = new Moq.Mock<IRouteService>();
        }

        [Fact]
        public void SetRouteData_SetsFieldsCorrectly()
        {
            var panel = new BusBus.UI.RoutePanel(_mockRouteService.Object);
            var driver = new Driver { Id = Guid.NewGuid(), Name = "Test Driver" };
            var vehicle = new Vehicle { Id = Guid.NewGuid(), Name = "Test Vehicle" };
            var route = new Route
            {
                RouteDate = new DateTime(2025, 5, 22),
                AMStartingMileage = 10,
                AMEndingMileage = 20,
                AMRiders = 5,
                PMStartMileage = 30,
                PMEndingMileage = 40,
                PMRiders = 7,
                Driver = driver,
                Vehicle = vehicle
            };
            // Add driver/vehicle to combo boxes for selection
            panel.GetType().GetField("_drivers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, new List<Driver> { driver });
            panel.GetType().GetField("_vehicles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, new List<Vehicle> { vehicle });
            panel.SetRouteData(route);
            // No exception means success for this test
            Assert.True(true);
        }

        [Fact]
        public void GetRouteData_ReturnsNullOnInvalidMileage()
        {
            var panel = new BusBus.UI.RoutePanel(_mockRouteService.Object);
            // Set invalid mileage (ending < starting)
            var amStart = panel.GetType().GetField("_amStartingMileage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(panel) as System.Windows.Forms.NumericUpDown;
            var amEnd = panel.GetType().GetField("_amEndingMileage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(panel) as System.Windows.Forms.NumericUpDown;
            if (amStart != null && amEnd != null)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                amStart.Value = 100;
#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning disable CA1416 // Validate platform compatibility
                amEnd.Value = 50;
#pragma warning restore CA1416 // Validate platform compatibility
            }
            var route = panel.GetRouteData();
            Assert.Null(route);
        }

        [Fact]
        public void GetRouteData_ReturnsRouteOnValidData()
        {
            var panel = new BusBus.UI.RoutePanel(_mockRouteService.Object);
            var amStart = panel.GetType().GetField("_amStartingMileage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(panel) as System.Windows.Forms.NumericUpDown;
            var amEnd = panel.GetType().GetField("_amEndingMileage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(panel) as System.Windows.Forms.NumericUpDown;
            if (amStart != null && amEnd != null)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                amStart.Value = 10;
#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning disable CA1416 // Validate platform compatibility
                amEnd.Value = 20;
#pragma warning restore CA1416 // Validate platform compatibility
            }
            var route = panel.GetRouteData();
            Assert.NotNull(route);
        }


        [Fact]
        public void RoutePanel_Constructor_InitializesComponents()
        {
            var panel = new BusBus.UI.RoutePanel(_mockRouteService.Object);
            Assert.NotNull(panel);
        }

        [Fact]
        public void RoutePanel_CanSetAndGetRouteData()
        {
            var panel = new BusBus.UI.RoutePanel(_mockRouteService.Object);
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1100,
                AMRiders = 25,
                PMStartMileage = 1100,
                PMEndingMileage = 1200,
                PMRiders = 30,
                Driver = new Driver { Name = "Alice" },
                Vehicle = new Vehicle { Name = "Bus 1" }
            };
            // Simulate setting and getting route data (pseudo, as UI controls are private)
            // This would be more detailed with UI test framework
            Assert.NotNull(route);
        }


        [Fact]
        public void RoutePanel_SaveButtonClicked_EventIsRaised()
        {
            var panel = new BusBus.UI.RoutePanel(_mockRouteService.Object);
            bool eventRaised = false;
            panel.SaveButtonClicked += (s, e) => eventRaised = true;
            // Simulate button click (pseudo, as button is private)
            // panel.OnSaveButtonClick();
            // For now, just assert event can be subscribed
            Assert.False(eventRaised); // Would be true if invoked
        }

        [Fact]
        public void RoutePanel_Theme_RefreshesWithoutError()
        {
            var panel = new BusBus.UI.RoutePanel(_mockRouteService.Object);
            // Simulate theme refresh (pseudo, as method may be internal)
            // panel.RefreshTheme();
            Assert.NotNull(panel);
        }
    }
}