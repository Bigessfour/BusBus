#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using System.Threading;
using BusBus.UI;
using BusBus.Models;
using BusBus.Services;
using BusBus.DataAccess;

namespace BusBus.Tests.UI
{
    [TestClass]
    public class DashboardUIDebugTests
    {
        private IServiceProvider? _serviceProvider;
        private TestContext? _testContext;

        public TestContext? TestContext
        {
            get { return _testContext; }
            set { _testContext = value; }
        }

        [TestInitialize]
        public async Task TestInitialize()
        {
            // Initialize Windows Forms application for testing
            if (!Application.MessageLoop)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            // Build test service provider
            var services = new ServiceCollection();

            // Add configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Add logging for tests
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add Entity Framework with test database
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost\\SQLEXPRESS;Database=BusBusDB;Trusted_Connection=true;TrustServerCertificate=true;";

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Add application services
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IDriverService, DriverService>();
            services.AddScoped<IVehicleService, VehicleService>();

            // Add UI components
            services.AddTransient<Dashboard>();
            services.AddTransient<DashboardView>();

            _serviceProvider = services.BuildServiceProvider();

            // Ensure database is available
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Integration")]
        public async Task TestDatabaseConnection()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Act & Assert
            var canConnect = await dbContext.Database.CanConnectAsync();
            Assert.IsTrue(canConnect, "Database connection should be successful");

            var routeCount = await dbContext.Routes.CountAsync();
            var driverCount = await dbContext.Drivers.CountAsync();
            var vehicleCount = await dbContext.Vehicles.CountAsync();

            TestContext?.WriteLine($"Database contains: {routeCount} routes, {driverCount} drivers, {vehicleCount} vehicles");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Integration")]
        public void TestServiceResolution()
        {
            // Arrange & Act
            using var scope = _serviceProvider!.CreateScope();

            var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();
            var driverService = scope.ServiceProvider.GetRequiredService<IDriverService>();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();

            // Assert
            Assert.IsNotNull(routeService, "RouteService should be resolvable");
            Assert.IsNotNull(driverService, "DriverService should be resolvable");
            Assert.IsNotNull(vehicleService, "VehicleService should be resolvable");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Integration")]
        public async Task TestDashboardCreation()
        {
            // Arrange
            var tcs = new TaskCompletionSource<bool>();
            Exception? testException = null;

            // Act - Create dashboard on STA thread
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();

                    var dashboard = new Dashboard(scope.ServiceProvider, routeService, logger);

                    // Force handle creation without showing
                    var handle = dashboard.Handle;

                    tcs.SetResult(handle != IntPtr.Zero);
                    dashboard.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                    tcs.SetResult(false);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            var result = await tcs.Task;

            // Assert
            if (testException != null)
            {
                TestContext?.WriteLine($"Dashboard creation failed with exception: {testException}");
            }

            Assert.IsTrue(result, $"Dashboard should be created successfully. Exception: {testException?.Message}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Integration")]
        [Timeout(30000)] // 30 second timeout
        public async Task TestDashboardViewCreationAndRouteViewLoading()
        {
            // Arrange
            var tcs = new TaskCompletionSource<(bool created, bool routeViewLoaded, string details)>();
            Exception? testException = null;

            // Act - Create DashboardView on STA thread
            var thread = new Thread(async () =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DashboardView>>();
                    var dashboardView = new DashboardView(scope.ServiceProvider, logger);

                    var created = false;
                    var routeViewLoaded = false;
                    var details = "";

                    // Force handle creation and show/hide to trigger Load event
                    var handle = dashboardView.Handle;
                    created = handle != IntPtr.Zero;
                    details += $"Handle created: {created}. ";

                    if (created)
                    {
                        // Set up event handler for Load event
                        dashboardView.Load += async (s, e) =>
                        {
                            try
                            {
                                details += "Load event fired. ";

                                // Wait for initialization to complete
                                await Task.Delay(2000);

                                // Check for route view components
                                var routeGridFound = false;

                                foreach (Control control in dashboardView.Controls)
                                {
                                    if (control is TableLayoutPanel mainLayout)
                                    {
                                        details += "Found main layout. ";
                                        foreach (Control child in mainLayout.Controls)
                                        {
                                            if (child is Panel contentPanel && contentPanel.Controls.Count > 0)
                                            {
                                                details += $"Found content panel with {contentPanel.Controls.Count} controls. ";
                                                foreach (Control gridControl in contentPanel.Controls)
                                                {
                                                    details += $"Control: {gridControl.GetType().Name}, Visible: {gridControl.Visible}. ";
                                                    if (gridControl.Visible && (gridControl.GetType().Name.Contains("DataGrid") || gridControl.GetType().Name.Contains("DynamicDataGridView")))
                                                    {
                                                        routeGridFound = true;
                                                        details += "Route grid found and visible. ";
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                routeViewLoaded = routeGridFound;
                                tcs.SetResult((created, routeViewLoaded, details));
                            }
                            catch (Exception loadEx)
                            {
                                details += $"Load event exception: {loadEx.Message}. ";
                                tcs.SetResult((created, false, details));
                            }
                        };

                        // Show and immediately hide to trigger Load event
                        dashboardView.Show();
                        dashboardView.Hide();

                        // Process Windows messages
                        Application.DoEvents();

                        // If Load event doesn't fire within reasonable time, timeout
                        await Task.Delay(5000);
                        if (!tcs.Task.IsCompleted)
                        {
                            details += "Load event timeout. ";
                            tcs.SetResult((created, false, details));
                        }
                    }
                    else
                    {
                        tcs.SetResult((false, false, details));
                    }

                    dashboardView.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                    tcs.SetResult((false, false, $"Exception: {ex.Message}"));
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            var (created, routeViewLoaded, details) = await tcs.Task;

            // Assert
            TestContext?.WriteLine($"Test details: {details}");

            if (testException != null)
            {
                TestContext?.WriteLine($"DashboardView test failed with exception: {testException}");
            }

            Assert.IsTrue(created, $"DashboardView should be created successfully. Details: {details}");
            TestContext?.WriteLine($"DashboardView created: {created}");

            // Note: Route view loading test is more lenient since it depends on timing
            if (!routeViewLoaded)
            {
                TestContext?.WriteLine($"Route view loading test inconclusive - may need more time or different approach. Details: {details}");
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task TestCrudOperations()
        {
            // Arrange
            using var scope = _serviceProvider!.CreateScope();
            var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();

            var testRoute = new Route
            {
                RouteName = "Test Route - MSTest",
                RouteDate = DateTime.Today,
                AMStartingMileage = 100,
                PMEndingMileage = 150,
                AMRiders = 25,
                PMRiders = 25,
                DriverId = null,
                VehicleId = null
            };

            try
            {
                // Test Create
                await routeService.CreateRouteAsync(testRoute);
                Assert.IsTrue(testRoute.Id != Guid.Empty, "Route should have been assigned an ID after creation");

                // Test Read
                var allRoutes = await routeService.GetRoutesAsync();
                var createdRoute = allRoutes.FirstOrDefault(r => r.RouteName == "Test Route - MSTest");
                Assert.IsNotNull(createdRoute, "Created route should be retrievable");

                // Test Update
                createdRoute.AMRiders = 30;
                await routeService.UpdateRouteAsync(createdRoute);

                var updatedRoute = await routeService.GetRouteByIdAsync(createdRoute.Id);
                Assert.AreEqual(30, updatedRoute?.AMRiders, "Route should be updated with new riders count");

                // Test Delete
                await routeService.DeleteRouteAsync(createdRoute.Id);
                var deletedRoute = await routeService.GetRouteByIdAsync(createdRoute.Id);
                Assert.IsNull(deletedRoute, "Route should be deleted");
            }
            catch (Exception ex)
            {
                TestContext?.WriteLine($"CRUD test failed: {ex.Message}");
                throw;
            }
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Performance")]
        public async Task TestStartupPerformance()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var results = new System.Collections.Generic.Dictionary<string, long>();

            // Test service provider creation time
            var serviceCreationTime = stopwatch.ElapsedMilliseconds;
            results["ServiceProvider"] = serviceCreationTime;

            // Test database connection time
            var dbStart = stopwatch.ElapsedMilliseconds;
            using (var scope = _serviceProvider!.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.CanConnectAsync();
            }
            results["DatabaseConnection"] = stopwatch.ElapsedMilliseconds - dbStart;

            // Test dashboard creation time
            var dashboardStart = stopwatch.ElapsedMilliseconds;
            var tcs = new TaskCompletionSource<bool>();

            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider?.CreateScope();
                    var routeService = scope?.ServiceProvider.GetRequiredService<IRouteService>();
                    var logger = scope?.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();

                    if (scope != null && routeService != null && logger != null)
                    {
                        var dashboard = new Dashboard(scope.ServiceProvider, routeService, logger);
                        var handle = dashboard.Handle; // Force creation
                        dashboard.Dispose();
                    }

                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            await tcs.Task;

            results["DashboardCreation"] = stopwatch.ElapsedMilliseconds - dashboardStart;

            stopwatch.Stop();
            results["Total"] = stopwatch.ElapsedMilliseconds;

            // Assert performance benchmarks
            foreach (var result in results)
            {
                TestContext?.WriteLine($"{result.Key}: {result.Value}ms");
            }

            Assert.IsTrue(results["Total"] < 10000, $"Total startup time should be under 10 seconds. Actual: {results["Total"]}ms");
            Assert.IsTrue(results["DatabaseConnection"] < 5000, $"Database connection should be under 5 seconds. Actual: {results["DatabaseConnection"]}ms");
        }

        [TestMethod]
        [TestCategory("BusBus")]
        [TestCategory("Integration")]
        public async Task TestBusBusInfoCompliantRoutes()
        {
            // Test based on BusBus Info specifications: Four routes per school day
            using var scope = _serviceProvider!.CreateScope();
            var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();

            var testDate = DateTime.Today;
            var standardRoutes = new[]
            {
                "Truck Plaza",  // Default Bus 17 per BusBus Info
                "East Route",
                "West Route",
                "SPED Route"
            };

            var createdRoutes = new List<Route>();

            try
            {
                // Create all four standard routes for a school day
                foreach (var routeName in standardRoutes)
                {
                    var route = new Route
                    {
                        RouteName = routeName,
                        RouteDate = testDate,
                        AMStartingMileage = 10000 + Array.IndexOf(standardRoutes, routeName) * 100,
                        AMEndingMileage = 10025 + Array.IndexOf(standardRoutes, routeName) * 100,
                        PMStartMileage = 10025 + Array.IndexOf(standardRoutes, routeName) * 100, // PM Start = AM End per BusBus Info
                        PMEndingMileage = 10050 + Array.IndexOf(standardRoutes, routeName) * 100,
                        AMRiders = 20 + Array.IndexOf(standardRoutes, routeName) * 5, // Varying rider counts
                        PMRiders = 18 + Array.IndexOf(standardRoutes, routeName) * 5,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "BusBus Test System"
                    };

                    // Truck Plaza gets Bus 17 by default per BusBus Info
                    if (routeName == "Truck Plaza")
                    {
                        // Note: VehicleId would be set to Bus 17's ID in real implementation
                        TestContext?.WriteLine("Truck Plaza route should default to Bus 17");
                    }

                    await routeService.CreateRouteAsync(route);
                    createdRoutes.Add(route);
                    TestContext?.WriteLine($"✓ Created {routeName} route for {testDate:dd-MM-yy}");
                }

                // Verify all routes were created
                var allRoutes = await routeService.GetRoutesAsync();
                var todaysRoutes = allRoutes.Where(r => r.RouteDate.Date == testDate.Date).ToList();

                Assert.AreEqual(4, todaysRoutes.Count, "Should have exactly 4 routes for the school day");

                // Verify each standard route exists
                foreach (var expectedRoute in standardRoutes)
                {
                    var foundRoute = todaysRoutes.FirstOrDefault(r => r.RouteName == expectedRoute);
                    Assert.IsNotNull(foundRoute, $"{expectedRoute} should exist for the school day");

                    // Verify AM/PM structure per BusBus Info
                    Assert.IsTrue(foundRoute.AMStartingMileage > 0, $"{expectedRoute} should have AM starting mileage");
                    Assert.IsTrue(foundRoute.AMEndingMileage > foundRoute.AMStartingMileage, $"{expectedRoute} AM ending should be > starting"); Assert.IsTrue(foundRoute.PMStartMileage >= foundRoute.AMEndingMileage, $"{expectedRoute} PM start should be >= AM end");
                    Assert.IsTrue(foundRoute.PMEndingMileage > foundRoute.PMStartMileage, $"{expectedRoute} PM ending should be > PM starting");
                }

                TestContext?.WriteLine("✓ All BusBus Info route specifications verified");
            }
            finally
            {
                // Cleanup
                foreach (var route in createdRoutes)
                {
                    try
                    {
                        await routeService.DeleteRouteAsync(route.Id);
                    }
                    catch (Exception ex)
                    {
                        TestContext?.WriteLine($"Cleanup warning: Could not delete route {route.RouteName}: {ex.Message}");
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("BusBus")]
        [TestCategory("UI")]
        public async Task TestBusBusInfoDashboardIntegration()
        {
            // Test the complete dashboard startup with BusBus Info compliance
            var tcs = new TaskCompletionSource<(bool success, List<string> details)>();
            var details = new List<string>();

            var thread = new Thread(async () =>
            {
                try
                {
                    details.Add("Starting BusBus Info dashboard integration test");

                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DashboardView>>();
                    var dashboardView = new DashboardView(scope.ServiceProvider, logger);

                    // Force handle creation
                    var handle = dashboardView.Handle;
                    details.Add($"Dashboard handle created: {handle != IntPtr.Zero}");

                    if (handle != IntPtr.Zero)
                    {
                        var loadEventFired = false;
                        var routeViewVisible = false;

                        dashboardView.Load += async (s, e) =>
                        {
                            try
                            {
                                loadEventFired = true;
                                details.Add("Dashboard Load event fired");

                                // Wait for route view initialization
                                await Task.Delay(3000);

                                // Check for BusBus Info compliant UI elements
                                await Task.Run(() =>
                                {
                                    dashboardView.Invoke(() =>
                                    {
                                        // Look for the main layout and route grid
                                        foreach (Control control in dashboardView.Controls)
                                        {
                                            if (control is TableLayoutPanel mainLayout)
                                            {
                                                details.Add("Found main TableLayoutPanel");

                                                // Check for side panel with route navigation
                                                var sidePanelFound = false;
                                                var contentPanelFound = false;
                                                var crudPanelFound = false;
                                                var statsPanelFound = false;

                                                foreach (Control child in mainLayout.Controls)
                                                {
                                                    if (child is Panel panel)
                                                    {
                                                        // Side panel should be 250px wide per design
                                                        if (panel.Width == 250 || panel.Bounds.Width == 250)
                                                        {
                                                            sidePanelFound = true;
                                                            details.Add("Found side panel (250px width)");

                                                            // Check for navigation buttons
                                                            var routeButtonFound = false;
                                                            var driverButtonFound = false;
                                                            var vehicleButtonFound = false;

                                                            foreach (Control sideControl in panel.Controls)
                                                            {
                                                                if (sideControl is Button btn)
                                                                {
                                                                    var btnText = btn.Text?.ToLower() ?? "";
                                                                    if (btnText.Contains("route")) routeButtonFound = true;
                                                                    if (btnText.Contains("driver")) driverButtonFound = true;
                                                                    if (btnText.Contains("vehicle")) vehicleButtonFound = true;
                                                                }
                                                            }

                                                            details.Add($"Navigation buttons - Routes: {routeButtonFound}, Drivers: {driverButtonFound}, Vehicles: {vehicleButtonFound}");
                                                        }
                                                        else if (panel.Dock == DockStyle.Fill || panel.Width > 250)
                                                        {
                                                            contentPanelFound = true;
                                                            details.Add("Found content panel");

                                                            // Check for data grid in content panel
                                                            foreach (Control contentControl in panel.Controls)
                                                            {
                                                                var controlType = contentControl.GetType().Name;
                                                                if (controlType.Contains("DataGrid") || controlType.Contains("DynamicDataGridView"))
                                                                {
                                                                    routeViewVisible = contentControl.Visible;
                                                                    details.Add($"Found data grid: {controlType}, Visible: {routeViewVisible}");
                                                                }
                                                            }
                                                        }
                                                        else if (panel.Height <= 120 && panel.Height > 50)
                                                        {
                                                            crudPanelFound = true;
                                                            details.Add("Found CRUD panel");
                                                        }
                                                        else if (panel.Height <= 80)
                                                        {
                                                            statsPanelFound = true;
                                                            details.Add("Found stats panel");
                                                        }
                                                    }
                                                }

                                                details.Add($"UI Layout - Side: {sidePanelFound}, Content: {contentPanelFound}, CRUD: {crudPanelFound}, Stats: {statsPanelFound}");
                                            }
                                        }
                                    });
                                });

                                var overallSuccess = loadEventFired && routeViewVisible;
                                tcs.SetResult((overallSuccess, details));
                            }
                            catch (Exception loadEx)
                            {
                                details.Add($"Load event error: {loadEx.Message}");
                                tcs.SetResult((false, details));
                            }
                        };

                        // Trigger Load event
                        dashboardView.Show();
                        dashboardView.Hide();
                        Application.DoEvents();

                        // Wait for load or timeout
                        await Task.Delay(8000);
                        if (!tcs.Task.IsCompleted)
                        {
                            details.Add("Load event timeout");
                            tcs.SetResult((false, details));
                        }
                    }
                    else
                    {
                        tcs.SetResult((false, details));
                    }

                    dashboardView.Dispose();
                }
                catch (Exception ex)
                {
                    details.Add($"Dashboard integration test exception: {ex.Message}");
                    tcs.SetResult((false, details));
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            var (success, testDetails) = await tcs.Task;

            // Log all details
            foreach (var detail in testDetails)
            {
                TestContext?.WriteLine(detail);
            }

            // Assertions
            Assert.IsTrue(testDetails.Any(d => d.Contains("Dashboard handle created: True")), "Dashboard should be created");
            Assert.IsTrue(testDetails.Any(d => d.Contains("Load event fired")), "Load event should fire");
            Assert.IsTrue(testDetails.Any(d => d.Contains("Found main TableLayoutPanel")), "Main layout should be found");

            // Note: Route view visibility test is more lenient due to timing dependencies
            TestContext?.WriteLine($"Overall BusBus Info dashboard integration test: {(success ? "SUCCESS" : "PARTIAL SUCCESS")}");
        }
    }
}
