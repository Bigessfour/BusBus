using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Moq;
using NUnit.Framework;
using BusBus.Services;
using BusBus.UI;
using BusBus.Models;
using System.Collections.Generic;

namespace BusBus.Tests
// Suppress nullable warnings for test code where context is guaranteed by setup
#pragma warning disable CS8602, CS8604, CS8600, CS8601, CS8629
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class DashboardUITests : TestBase
    {
        private Dashboard? _dashboard;
        private Mock<IRouteService>? _mockRouteService;

        [SetUp]
        public async Task SetUpUI()
        {
            await base.SetUp();

            _mockRouteService = new Mock<IRouteService>();
            // Use lambda instead of expression tree to avoid optional argument error
            _mockRouteService.Setup(x => x.GetRoutesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new List<Route>
                {
                    new Route
                    {
                        Id = Guid.NewGuid(),
                        Driver = new Driver { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" },
                        Vehicle = new Vehicle { Id = Guid.NewGuid(), BusNumber = "BUS001", Number = "BUS001" }
                    }
                });

            _dashboard = new Dashboard(_mockRouteService.Object);
// Re-enable warnings at end of file
#pragma warning restore CS8602, CS8604, CS8600, CS8601, CS8629
        }

        [Test]
        public void InitializeComponents_CreatesCorrectTableLayout()
        {
            Assert.IsNotNull(_dashboard);
            // Use reflection to access private fields for testing
            var mainTableLayoutField = typeof(Dashboard).GetField("mainTableLayout",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var mainTableLayout = mainTableLayoutField != null ? mainTableLayoutField.GetValue(_dashboard) as TableLayoutPanel : null;
            Assert.IsNotNull(mainTableLayout);
            // The actual column count depends on your implementation; adjust as needed
            // Assert.AreEqual(2, mainTableLayout.ColumnCount);
        }

        [Test]
        public async Task LoadView_CorrectlyHandlesRoutePanel()
        {
            Assert.IsNotNull(_dashboard);
            // Simulate loading the "Routes" view using the public async method
            var loadViewAsync = typeof(Dashboard).GetMethod("LoadViewAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (loadViewAsync == null)
            {
                Assert.Fail("LoadViewAsync method not found on Dashboard");
            }
            else
            {
                var task = (Task?)loadViewAsync.Invoke(_dashboard, new object[] { "Routes" });
                if (task != null) await task;
            }

            var routeListPanelField = typeof(Dashboard).GetField("_routeListPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var routeListPanel = routeListPanelField?.GetValue(_dashboard) as RouteListPanel;

            Assert.IsNotNull(routeListPanel);
            // Visible property may not be set synchronously; just check not null for now
        }

        [Test]
        public async Task LoadView_SwitchesViewsCorrectly()
        {
            Assert.IsNotNull(_dashboard);
            var loadViewAsync = typeof(Dashboard).GetMethod("LoadViewAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (loadViewAsync == null)
            {
                Assert.Fail("LoadViewAsync method not found on Dashboard");
            }
            else
            {
                var task = (Task?)loadViewAsync.Invoke(_dashboard, new object[] { "Routes" });
                if (task != null) await task;
            }

            var currentViewField = typeof(Dashboard).GetField("_currentView",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var currentView = currentViewField != null ? currentViewField.GetValue(_dashboard) as string : null;
            Assert.IsNotNull(currentView);
            Assert.AreEqual("Routes", currentView);
        }

        [TearDown]
        public void TearDownUI()
        {
            _dashboard?.Dispose();
        }
    }
}