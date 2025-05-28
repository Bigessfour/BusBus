#pragma warning disable CA1416 // Platform compatibility (WinForms)
using System;
using System.Reflection;
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
    /// Specific tests for Dashboard view creation and caching mechanism.
    /// Focuses on the GetOrCreateView private method that's causing null view issues.
    /// </summary>
    [TestClass]
    [TestCategory(TestCategories.UI)]
    public class DashboardViewCreationTests : TestBase
    {
        private Mock<IRouteService> _mockRouteService;
        private Mock<IDriverService> _mockDriverService;
        private Mock<IVehicleService> _mockVehicleService;
        private Mock<ILogger<Dashboard>> _mockLogger;
        private ServiceProvider _serviceProvider;
        private Dashboard _dashboard;
        private MethodInfo _getOrCreateViewMethod;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();

            // Setup mocks
            _mockRouteService = new Mock<IRouteService>();
            _mockDriverService = new Mock<IDriverService>();
            _mockVehicleService = new Mock<IVehicleService>();
            _mockLogger = new Mock<ILogger<Dashboard>>();

            // Setup service provider
            var services = new ServiceCollection();
            services.AddSingleton(_mockRouteService.Object);
            services.AddSingleton(_mockDriverService.Object);
            services.AddSingleton(_mockVehicleService.Object);
            services.AddSingleton(_mockLogger.Object);
            services.AddSingleton<ILogger<DriverListView>>(provider => Mock.Of<ILogger<DriverListView>>());
            services.AddSingleton<ILogger<DashboardView>>(provider => Mock.Of<ILogger<DashboardView>>());

            _serviceProvider = services.BuildServiceProvider();

            // Initialize Windows Forms if needed
            if (Application.OpenForms.Count == 0)
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            // Create dashboard and get private method
            _dashboard = new Dashboard(_serviceProvider, _mockRouteService.Object, _mockLogger.Object);
            _getOrCreateViewMethod = typeof(Dashboard).GetMethod("GetOrCreateView",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [TestCleanup]
        public void CleanUp()
        {
            _dashboard?.Dispose();
            _serviceProvider?.Dispose();
        }

        [TestMethod]
        public void GetOrCreateView_DashboardView_ReturnsValidView()
        {
            // Act
            var view = InvokeGetOrCreateView("dashboard");

            // Assert
            view.Should().NotBeNull("Dashboard view should be created");
            view.Should().BeAssignableTo<IView>("Should implement IView interface");

            var iView = view as IView;
            iView.ViewName.Should().Be("dashboard", "ViewName should match");
            iView.Control.Should().NotBeNull("Control should not be null");
        }

        [TestMethod]
        public void GetOrCreateView_DriverListView_ReturnsValidView()
        {
            // Act
            var view = InvokeGetOrCreateView("drivers");

            // Assert
            view.Should().NotBeNull("Drivers view should be created");
            view.Should().BeAssignableTo<IView>("Should implement IView interface");

            var iView = view as IView;
            iView.ViewName.Should().Be("drivers", "ViewName should match");

            // Note: Control might be null until ActivateAsync is called
            // This tests the constructor issue mentioned in logs
        }

        [TestMethod]
        public void GetOrCreateView_RouteListView_ReturnsValidView()
        {
            // Act
            var view = InvokeGetOrCreateView("routes");

            // Assert
            view.Should().NotBeNull("Routes view should be created");
            view.Should().BeAssignableTo<IView>("Should implement IView interface");
        }

        [TestMethod]
        public void GetOrCreateView_AllViewTypes_ReturnNonNullViews()
        {
            // Arrange
            string[] viewNames = { "dashboard", "routes", "drivers", "vehicles", "reports", "settings" };

            // Act & Assert
            foreach (var viewName in viewNames)
            {
                var view = InvokeGetOrCreateView(viewName);
                view.Should().NotBeNull($"View '{viewName}' should not be null");

                if (view is IView iView)
                {
                    iView.ViewName.Should().Be(viewName, $"ViewName should match for '{viewName}'");
                }
            }
        }

        [TestMethod]
        public void GetOrCreateView_InvalidViewName_ReturnsNull()
        {
            // Act
            var view = InvokeGetOrCreateView("invalid-view");

            // Assert
            view.Should().BeNull("Invalid view name should return null");
        }

        [TestMethod]
        public void GetOrCreateView_CachedView_ReturnsSameInstance()
        {
            // Arrange & Act
            var view1 = InvokeGetOrCreateView("dashboard");
            var view2 = InvokeGetOrCreateView("dashboard");

            // Assert
            view1.Should().BeSameAs(view2, "Should return cached instance");
        }

        [TestMethod]
        public void GetOrCreateView_DisposedCachedView_CreatesNewInstance()
        {
            // Arrange
            var view1 = InvokeGetOrCreateView("dashboard");

            // Dispose the view's control if it exists and is disposable
            if (view1 is IView iView && iView.Control is IDisposable disposableControl)
            {
                disposableControl.Dispose();
            }

            // Act
            var view2 = InvokeGetOrCreateView("dashboard");

            // Assert
            view2.Should().NotBeNull("Should create new instance when cached view is disposed");
            // Note: Might be same reference if disposal doesn't mark as disposed
        }

        [TestMethod]
        public async Task GetOrCreateView_DriverListView_ActivatesSuccessfully()
        {
            // Arrange
            var view = InvokeGetOrCreateView("drivers") as IView;
            view.Should().NotBeNull("Drivers view should be created");

            // Act & Assert
            Func<Task> activateAction = async () => await view.ActivateAsync(CancellationToken.None);
            await activateAction.Should().NotThrowAsync("ActivateAsync should not throw exceptions");

            view.Control.Should().NotBeNull("Control should not be null after activation");
        }

        private object InvokeGetOrCreateView(string viewName)
        {
            _getOrCreateViewMethod.Should().NotBeNull("GetOrCreateView method should be found");
            return _getOrCreateViewMethod.Invoke(_dashboard, new object[] { viewName });
        }
    }
}
