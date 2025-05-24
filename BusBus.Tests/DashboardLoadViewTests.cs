using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using BusBus.Services;
using BusBus.UI;

namespace BusBus.Tests
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class DashboardLoadViewTests : TestBase
    {
        private Dashboard? _dashboard;
        private Mock<IRouteService>? _mockRouteService;

        [SetUp]
        public async Task SetUpLoadView()
        {
            await base.SetUp();
            _mockRouteService = new Mock<IRouteService>();
            _dashboard = new Dashboard(_mockRouteService.Object);
        }

        [Test, Timeout(5000)]
        public async Task LoadView_WithValidViewName_SetsCurrentView()
        {
            // Simulate loading the "Routes" view using the actual Dashboard API
            // (Dashboard uses LoadViewAsync internally)
            var loadViewAsync = typeof(Dashboard).GetMethod("LoadViewAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(loadViewAsync, "LoadViewAsync method not found on Dashboard");
            var task = (Task?)loadViewAsync!.Invoke(_dashboard, new object[] { "Routes" });
            Assert.IsNotNull(task, "LoadViewAsync did not return a Task");
            await task!;

            var currentViewField = typeof(Dashboard).GetField("_currentView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var currentView = currentViewField?.GetValue(_dashboard) as string;
            Assert.AreEqual("Routes", currentView);
        }

        [TearDown]
        public void TearDownLoadView()
        {
            _dashboard?.Dispose();
        }

    }
}

