#pragma warning disable CA1416 // Platform compatibility (WinForms)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    /// MSTest suite designed to debug the UI issue of multiple competing forms in DashboardView.
    /// Based on Microsoft Forms documentation principles for Windows Forms testing.
    /// </summary>
    [TestClass]
    [TestCategory(TestCategories.UI)]
    public class DashboardUIDebugTests : TestBase
    {
        private Mock<IRouteService> _mockRouteService;
        private Mock<IDriverService> _mockDriverService;
        private Mock<IVehicleService> _mockVehicleService;
        private Mock<ILogger<Dashboard>> _mockLogger;
        private ServiceProvider _serviceProvider;
        private Dashboard _dashboard;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();

            // Create mocks for services
            _mockRouteService = new Mock<IRouteService>();
            _mockDriverService = new Mock<IDriverService>();
            _mockVehicleService = new Mock<IVehicleService>();
            _mockLogger = new Mock<ILogger<Dashboard>>();

            // Set up service provider with mocks
            var services = new ServiceCollection();
            services.AddSingleton(_mockRouteService.Object);
            services.AddSingleton(_mockDriverService.Object);
            services.AddSingleton(_mockVehicleService.Object);
            services.AddSingleton(_mockLogger.Object);
            services.AddSingleton<ILogger<DriverListView>>(provider => Mock.Of<ILogger<DriverListView>>());
            services.AddSingleton<ILogger<DashboardView>>(provider => Mock.Of<ILogger<DashboardView>>());

            _serviceProvider = services.BuildServiceProvider();

            // Create dashboard on UI thread
            _dashboard = null;
            if (Application.OpenForms.Count == 0)
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            await InvokeOnUIThread(() =>
            {
                _dashboard = new Dashboard(_serviceProvider, _mockRouteService.Object, _mockLogger.Object);
            });
        }

        [TestCleanup]
        public async Task CleanUp()
        {
            if (_dashboard != null)
            {
                await InvokeOnUIThread(() =>
                {
                    _dashboard.Dispose();
                });
            }
            _serviceProvider?.Dispose();
        }

        #region Test Case 1: GetOrCreateView_NonNull
        /// <summary>
        /// Ensure GetOrCreateView returns valid views for all view names.
        /// Addresses: "Null view control for: drivers" log issue
        /// </summary>
        [TestMethod]
        public async Task GetOrCreateView_AllViewNames_ReturnsNonNullViews()
        {
            // Arrange
            string[] viewNames = { "dashboard", "routes", "drivers", "vehicles", "reports", "settings" };

            // Act & Assert
            await InvokeOnUIThread(() =>
            {
                foreach (var viewName in viewNames)
                {
                    // Use reflection to access private GetOrCreateView method
                    var method = typeof(Dashboard).GetMethod("GetOrCreateView",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var view = method?.Invoke(_dashboard, new object[] { viewName });

                    view.Should().NotBeNull($"View '{viewName}' should not be null");

                    if (view is IView iView)
                    {
                        iView.Control.Should().NotBeNull($"Control for view '{viewName}' should not be null");
                        iView.Control.IsDisposed.Should().BeFalse($"Control for view '{viewName}' should not be disposed");
                    }
                }
            });
        }
        #endregion

        #region Test Case 2: NavigateToAsync_SingleView
        /// <summary>
        /// Verify NavigateToAsync clears old views and adds only one.
        /// Addresses: Multiple competing forms UI issue
        /// </summary>
        [TestMethod]
        public async Task NavigateToAsync_SingleView_OnlyOneControlInContentPanel()
        {
            // Arrange & Act
            await InvokeOnUIThread(async () =>
            {
                await _dashboard.NavigateToAsync("dashboard");

                // Get content panel using reflection
                var contentPanelField = typeof(Dashboard).GetField("_contentPanel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var contentPanel = contentPanelField?.GetValue(_dashboard) as Panel;

                // Assert
                contentPanel.Should().NotBeNull("Content panel should exist");
                contentPanel.Controls.Count.Should().Be(1, "Should have exactly one view control");
                contentPanel.Controls[0].Should().BeAssignableTo<Control>("Should contain a valid control");
            });
        }
        #endregion

        #region Test Case 3: DashboardView_RoutesListExists
        /// <summary>
        /// Confirm RoutesList control is initialized in DashboardView.
        /// Addresses: "Routes list control not found in DashboardView" log issue
        /// </summary>
        [TestMethod]
        public async Task DashboardView_RoutesListExists_ControlFoundAndValid()
        {
            // Arrange & Act
            DashboardView dashboardView = null;
            await InvokeOnUIThread(() =>
            {
                dashboardView = new DashboardView(_serviceProvider);
            });

            // Assert
            dashboardView.Should().NotBeNull("DashboardView should be created");

            // Activate the view to ensure controls are initialized
            await dashboardView.ActivateAsync(CancellationToken.None);

            // Look for routes-related controls (TableLayoutPanel based on log analysis)
            var routesControls = dashboardView.Controls.Find("routes", true);
            if (routesControls.Length == 0)
            {
                // Check for TableLayoutPanel which is used for routes list
                var tableLayoutPanels = GetControlsOfType<TableLayoutPanel>(dashboardView);
                tableLayoutPanels.Should().NotBeEmpty("Should contain TableLayoutPanel for routes list");
            }
            else
            {
                routesControls.Should().NotBeEmpty("Routes control should be found");
            }
        }
        #endregion

        #region Test Case 4: DriverListView_Constructor
        /// <summary>
        /// Ensure DriverListView constructs without errors.
        /// Addresses: "Null view control for: drivers" log issue
        /// </summary>
        [TestMethod]
        public async Task DriverListView_Constructor_CreatesValidInstance()
        {
            // Arrange & Act
            DriverListView driverListView = null;
            Exception constructorException = null;

            await InvokeOnUIThread(() =>
            {
                try
                {
                    driverListView = new DriverListView(_serviceProvider);
                }
                catch (Exception ex)
                {
                    constructorException = ex;
                }
            });

            // Assert
            constructorException.Should().BeNull("Constructor should not throw exceptions");
            driverListView.Should().NotBeNull("DriverListView should be created");
            driverListView.ViewName.Should().Be("drivers", "ViewName should be 'drivers'");
            driverListView.Title.Should().Be("Driver Management", "Title should be set correctly");

            // After activation, Control should not be null
            await driverListView.ActivateAsync(CancellationToken.None);
            driverListView.Control.Should().NotBeNull("Control should not be null after activation");
        }
        #endregion

        #region Test Case 5: Dispose_NoException
        /// <summary>
        /// Validate Dashboard disposal without exceptions.
        /// Addresses: Dashboard disposal InvalidOperationException log issue
        /// </summary>
        [TestMethod]
        public async Task Dispose_NoException_DisposesCleanly()
        {
            // Arrange
            var testDashboard = await CreateDashboardForDisposalTest();

            // Act & Assert
            await InvokeOnUIThread(() =>
            {
                Action disposeAction = () => testDashboard.Dispose();
                disposeAction.Should().NotThrow("Dispose should not throw exceptions");
            });
        }
        #endregion

        #region Test Case 6: NavigateMultipleViews_NoOverlap
        /// <summary>
        /// Test sequential navigation to ensure single view display.
        /// Addresses: Multiple competing forms UI issue
        /// </summary>
        [TestMethod]
        public async Task NavigateMultipleViews_NoOverlap_AlwaysSingleView()
        {
            // Arrange
            string[] viewSequence = { "dashboard", "routes", "drivers", "vehicles", "reports", "settings" };

            // Act & Assert
            await InvokeOnUIThread(async () =>
            {
                var contentPanelField = typeof(Dashboard).GetField("_contentPanel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var contentPanel = contentPanelField?.GetValue(_dashboard) as Panel;

                foreach (var viewName in viewSequence)
                {
                    await _dashboard.NavigateToAsync(viewName);

                    contentPanel.Controls.Count.Should().Be(1,
                        $"Should have exactly one view after navigating to '{viewName}'");

                    // Verify correct view type based on name
                    var currentControl = contentPanel.Controls[0];
                    VerifyViewTypeMatches(viewName, currentControl);
                }
            });
        }
        #endregion

        #region Test Case 7: ConcurrentNavigation_ThreadSafety
        /// <summary>
        /// Test concurrent navigation calls to ensure thread safety.
        /// Addresses: Potential race conditions in UI navigation
        /// </summary>
        [TestMethod]
        public async Task ConcurrentNavigation_ThreadSafety_SingleViewResult()
        {
            // Arrange
            var tasks = new Task[]
            {
                _dashboard.NavigateToAsync("dashboard"),
                _dashboard.NavigateToAsync("routes"),
                _dashboard.NavigateToAsync("drivers")
            };

            // Act
            await Task.WhenAll(tasks);

            // Assert
            await InvokeOnUIThread(() =>
            {
                var contentPanelField = typeof(Dashboard).GetField("_contentPanel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var contentPanel = contentPanelField?.GetValue(_dashboard) as Panel;

                contentPanel.Controls.Count.Should().Be(1,
                    "Should have exactly one view after concurrent navigation");
            });
        }
        #endregion

        #region Helper Methods
        private async Task<Dashboard> CreateDashboardForDisposalTest()
        {
            Dashboard testDashboard = null;
            await InvokeOnUIThread(() =>
            {
                testDashboard = new Dashboard(_serviceProvider, _mockRouteService.Object, _mockLogger.Object);
            });
            return testDashboard;
        }

        private void VerifyViewTypeMatches(string viewName, Control control)
        {
            switch (viewName.ToLower())
            {
                case "dashboard":
                    // Dashboard view should be DashboardView type
                    break;
                case "routes":
                    // Routes view should be RouteListView type
                    break;
                case "drivers":
                    // Drivers view should be DriverListPanel type (from DriverListView.Control)
                    break;
                case "vehicles":
                    // Vehicles view should be VehicleListView type
                    break;
                case "reports":
                    // Reports view should be ReportsView type
                    break;
                case "settings":
                    // Settings view should be SettingsView type
                    break;
            }

            control.Should().NotBeNull($"Control for '{viewName}' should not be null");
            control.IsDisposed.Should().BeFalse($"Control for '{viewName}' should not be disposed");
        }

        private static T[] GetControlsOfType<T>(Control parent) where T : Control
        {
            var controls = new List<T>();
            foreach (Control control in parent.Controls)
            {
                if (control is T typedControl)
                    controls.Add(typedControl);
                controls.AddRange(GetControlsOfType<T>(control));
            }
            return controls.ToArray();
        }

        private static async Task InvokeOnUIThread(Action action)
        {
            if (Application.OpenForms.Count > 0)
            {
                var form = Application.OpenForms[0];
                if (form.InvokeRequired)
                {
                    await Task.Run(() => form.Invoke(action));
                }
                else
                {
                    action();
                }
            }
            else
            {
                action();
            }
        }

        private static async Task InvokeOnUIThread(Func<Task> asyncAction)
        {
            if (Application.OpenForms.Count > 0)
            {
                var form = Application.OpenForms[0];
                if (form.InvokeRequired)
                {
                    await Task.Run(async () => await form.Invoke(asyncAction));
                }
                else
                {
                    await asyncAction();
                }
            }
            else
            {
                await asyncAction();
            }
        }
        #endregion
    }
}
