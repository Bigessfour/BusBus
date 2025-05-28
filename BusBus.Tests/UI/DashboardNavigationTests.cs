#pragma warning disable CA1416 // Platform compatibility (WinForms)
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Services;
using BusBus.UI;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BusBus.Tests.UI
{
    /// <summary>
    /// Tests focused on navigation behavior and control hierarchy management.
    /// Specifically targets the multiple competing forms issue.
    /// </summary>
    [TestClass]
    [TestCategory(TestCategories.UI)]
    public class DashboardNavigationTests : TestBase
    {
        private Mock<IRouteService> _mockRouteService;
        private Mock<ILogger<Dashboard>> _mockLogger;
        private ServiceProvider _serviceProvider;
        private Dashboard _dashboard;
        private Panel _contentPanel;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();

            // Setup mocks
            _mockRouteService = new Mock<IRouteService>();
            _mockLogger = new Mock<ILogger<Dashboard>>();

            // Setup service provider with all required services
            var services = new ServiceCollection();
            services.AddSingleton(_mockRouteService.Object);
            services.AddSingleton(_mockLogger.Object);
            services.AddSingleton<IDriverService>(provider => Mock.Of<IDriverService>());
            services.AddSingleton<IVehicleService>(provider => Mock.Of<IVehicleService>());
            services.AddSingleton<ILogger<DriverListView>>(provider => Mock.Of<ILogger<DriverListView>>());
            services.AddSingleton<ILogger<DashboardView>>(provider => Mock.Of<ILogger<DashboardView>>());

            _serviceProvider = services.BuildServiceProvider();

            // Initialize Windows Forms
            if (Application.OpenForms.Count == 0)
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            // Create dashboard and get content panel reference
            _dashboard = new Dashboard(_serviceProvider, _mockRouteService.Object, _mockLogger.Object);

            // Use reflection to get private _contentPanel field
            var contentPanelField = typeof(Dashboard).GetField("_contentPanel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _contentPanel = contentPanelField?.GetValue(_dashboard) as Panel;
        }

        [TestCleanup]
        public void CleanUp()
        {
            _dashboard?.Dispose();
            _serviceProvider?.Dispose();
        }

        [TestMethod]
        public async Task NavigateToAsync_SingleNavigation_ContentPanelHasOneControl()
        {
            // Act
            await _dashboard.NavigateToAsync("dashboard");

            // Assert
            _contentPanel.Should().NotBeNull("Content panel should exist");
            _contentPanel.Controls.Count.Should().Be(1, "Content panel should have exactly one control");
            _contentPanel.Controls[0].Should().NotBeNull("The control should not be null");
            _contentPanel.Controls[0].IsDisposed.Should().BeFalse("The control should not be disposed");
        }

        [TestMethod]
        public async Task NavigateToAsync_MultipleNavigations_AlwaysOneControl()
        {
            // Arrange
            string[] navigationSequence = { "dashboard", "routes", "drivers", "vehicles" };

            // Act & Assert
            foreach (var viewName in navigationSequence)
            {
                await _dashboard.NavigateToAsync(viewName);

                _contentPanel.Controls.Count.Should().Be(1,
                    $"Content panel should have exactly one control after navigating to '{viewName}'");

                var currentControl = _contentPanel.Controls[0];
                currentControl.Should().NotBeNull($"Control should not be null for view '{viewName}'");
                currentControl.IsDisposed.Should().BeFalse($"Control should not be disposed for view '{viewName}'");
                currentControl.Dock.Should().Be(DockStyle.Fill, $"Control should fill content panel for view '{viewName}'");
            }
        }

        [TestMethod]
        public async Task NavigateToAsync_BackAndForth_NoControlLeaks()
        {
            // Arrange - simulate rapid navigation back and forth
            string[] rapidNavigation = { "dashboard", "routes", "dashboard", "drivers", "routes", "settings", "dashboard" };

            // Act
            foreach (var viewName in rapidNavigation)
            {
                await _dashboard.NavigateToAsync(viewName);
            }

            // Assert
            _contentPanel.Controls.Count.Should().Be(1, "Should always have exactly one control despite rapid navigation");

            // Verify final state
            var finalControl = _contentPanel.Controls[0];
            finalControl.Should().NotBeNull("Final control should not be null");
            finalControl.IsDisposed.Should().BeFalse("Final control should not be disposed");
        }

        [TestMethod]
        public async Task NavigateToAsync_NullParameter_DoesNotThrow()
        {
            // Act & Assert
            Func<Task> navigateAction = async () => await _dashboard.NavigateToAsync("dashboard", null);
            await navigateAction.Should().NotThrowAsync("Navigation with null parameter should not throw");

            _contentPanel.Controls.Count.Should().Be(1, "Should have one control after navigation");
        }

        [TestMethod]
        public async Task NavigateToAsync_InvalidViewName_HandlesGracefully()
        {
            // Act
            await _dashboard.NavigateToAsync("invalid-view-name");

            // Assert
            // Should handle gracefully without adding invalid controls
            _contentPanel.Controls.Count.Should().Be(0, "Should not add controls for invalid view names");
        }

        [TestMethod]
        public async Task NavigateToAsync_ViewCacheGrowth_StabilizesAtExpectedSize()
        {
            // Arrange
            string[] allViews = { "dashboard", "routes", "drivers", "vehicles", "reports", "settings" };

            // Act - navigate to all views to populate cache
            foreach (var viewName in allViews)
            {
                await _dashboard.NavigateToAsync(viewName);
            }

            // Use reflection to check cache size
            var viewCacheField = typeof(Dashboard).GetField("_viewCache",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var viewCache = viewCacheField?.GetValue(_dashboard) as System.Collections.IDictionary;

            // Assert
            viewCache.Should().NotBeNull("View cache should exist");
            viewCache.Count.Should().Be(allViews.Length, "Cache should contain all created views");
            _contentPanel.Controls.Count.Should().Be(1, "Content panel should still have only one control");
        }

        [TestMethod]
        public async Task NavigateToAsync_RepeatedSameView_ReusesCachedView()
        {
            // Arrange
            await _dashboard.NavigateToAsync("dashboard");
            var firstControl = _contentPanel.Controls[0];

            // Act
            await _dashboard.NavigateToAsync("routes");
            await _dashboard.NavigateToAsync("dashboard");
            var secondControl = _contentPanel.Controls[0];

            // Assert
            firstControl.Should().BeSameAs(secondControl, "Should reuse cached view control");
            _contentPanel.Controls.Count.Should().Be(1, "Should still have only one control");
        }

        [TestMethod]
        public async Task LogControlHierarchy_Method_ExecutesWithoutException()
        {
            // Arrange
            await _dashboard.NavigateToAsync("dashboard");

            // Act & Assert
            var logMethod = typeof(Dashboard).GetMethod("LogControlHierarchy",
                BindingFlags.NonPublic | BindingFlags.Instance);

            logMethod.Should().NotBeNull("LogControlHierarchy method should exist");

            Action logAction = () => logMethod.Invoke(_dashboard, null);
            logAction.Should().NotThrow("LogControlHierarchy should not throw exceptions");
        }

        [TestMethod]
        public async Task NavigateToAsync_ConcurrentCalls_ThreadSafe()
        {
            // Arrange
            var tasks = new[]
            {
                _dashboard.NavigateToAsync("dashboard"),
                _dashboard.NavigateToAsync("routes"),
                _dashboard.NavigateToAsync("drivers")
            };

            // Act
            await Task.WhenAll(tasks);

            // Assert
            _contentPanel.Controls.Count.Should().Be(1, "Should have exactly one control after concurrent navigation");
            _contentPanel.Controls[0].Should().NotBeNull("Final control should not be null");
            _contentPanel.Controls[0].IsDisposed.Should().BeFalse("Final control should not be disposed");
        }

        [TestMethod]
        public async Task NavigateToAsync_AfterDispose_HandlesGracefully()
        {
            // Arrange
            await _dashboard.NavigateToAsync("dashboard");
            _dashboard.Dispose();

            // Act & Assert
            Func<Task> navigateAction = async () => await _dashboard.NavigateToAsync("routes");

            // Should either handle gracefully or throw a specific expected exception
            // The exact behavior depends on implementation, but it shouldn't crash the test runner
            try
            {
                await navigateAction();
                // If it succeeds, that's fine
                Assert.IsTrue(true, "Navigation after dispose handled gracefully");
            }
            catch (ObjectDisposedException)
            {
                // This is expected and acceptable
                Assert.IsTrue(true, "ObjectDisposedException is expected after dispose");
            }
            catch (InvalidOperationException)
            {
                // This might also be expected
                Assert.IsTrue(true, "InvalidOperationException is acceptable after dispose");
            }
        }
    }
}
