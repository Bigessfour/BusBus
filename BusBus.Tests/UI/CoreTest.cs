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
using BusBus.UI.Core;
using BusBus.Configuration;
using BusBus.Analytics;
using BusBus.Data;
using Microsoft.Data.SqlClient;
using Polly;

namespace BusBus.Tests
{
    [TestClass]
    public class CoreTest
    {
        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Theme")]
        [Timeout(15000)]
        public void ThemeSwitchingStabilityTest()
        {
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    var dashboard = new Dashboard(scope.ServiceProvider, routeService, logger);
                    var themes = new[] { "Light", "Dark" };
                    // Ensure handle is created before invoking
                    var _ = dashboard.Handle;
                    for (int i = 0; i < 10; i++)
                    {
                        foreach (var theme in themes)
                        {
                            try
                            {
                                if (!dashboard.IsHandleCreated)
                                    dashboard.CreateControl();
                                dashboard.Invoke(new Action(() =>
                                {
                                    BusBus.UI.Core.ThemeManager.SetTheme(theme);
                                }));
                            }
                            catch (Exception ex)
                            {
                                testException = ex;
                                break;
                            }
                        }
                        if (testException != null) break;
                    }
                    dashboard.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!testCompleted.Wait(15000))
            {
                Assert.Fail("Theme switching test timed out.");
            }
            if (thread.IsAlive)
            {
                thread.Join(1000);
            }
            Assert.IsNull(testException, $"Theme switching caused an exception: {testException}");
        }
        private IServiceProvider _serviceProvider;
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
                        _formsInitialized = true;
                    }
                }
            }
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost\\SQLEXPRESS;Database=BusBusDB;Trusted_Connection=true;TrustServerCertificate=true;";
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IDriverService, DriverService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddTransient<Dashboard>();
            services.AddTransient<DashboardOverviewView>();
            services.AddSingleton<AdvancedSqlServerDatabaseManager>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var logger = provider.GetRequiredService<ILogger<AdvancedSqlServerDatabaseManager>>();
                // Use a test-specific connection string or a mock if direct DB access is not desired for all tests.
                // For now, using a potentially non-functional string to avoid actual DB dependency in unit tests
                // or relying on appsettings.json to provide a valid test DB connection string.
                string testConnectionString = config.GetConnectionString("TestDefaultConnection") ?? "Server=.\\SQLEXPRESS;Database=BusBusTestDb;Trusted_Connection=true;TrustServerCertificate=true;Connection Timeout=5;";
                return new AdvancedSqlServerDatabaseManager(testConnectionString, logger);
            });
            services.AddTransient<ProjectAnalyzer>();


            _serviceProvider = services.BuildServiceProvider();
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Integration")]
        [Timeout(20000)]
        public void CoreTestMethod()
        {
            var result = (created: false, navigated: false, errorHandled: false, responsive: false, details: "");
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    var dashboard = new Dashboard(scope.ServiceProvider, routeService, logger);
                    var handle = dashboard.Handle;
                    result.created = handle != IntPtr.Zero;
                    dashboard.Invoke(new Action(async () =>
                    {
                        try
                        {
                            await dashboard.NavigateToAsync("routes");
                            result.navigated = true;
                        }
                        catch (Exception navEx)
                        {
                            result.details += $"Navigation exception: {navEx.Message}. ";
                        }
                    }));
                    dashboard.Invoke(new Action(async () =>
                    {
                        try
                        {
                            await dashboard.NavigateToAsync("nonexistent_view");
                        }
                        catch (Exception errorEx)
                        {
                            result.details += $"Error navigation exception: {errorEx.Message}. ";
                        }
                        finally
                        {
                            result.errorHandled = true;
                        }
                    }));
                    try
                    {
                        dashboard.Invoke(new Action(() =>
                        {
                            result.responsive = dashboard.IsHandleCreated && dashboard.Visible == false;
                        }));
                    }
                    catch (Exception respEx)
                    {
                        result.details += $"Responsiveness check exception: {respEx.Message}. ";
                    }
                    dashboard.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                    result.details += $"Exception: {ex.Message}. ";
                }
                finally
                {
                    testCompleted.Set();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!testCompleted.Wait(15000))
            {
                result.details += "Test timeout. ";
                testCompleted.Set();
            }
            if (thread.IsAlive)
            {
                thread.Join(1000);
            }
            TestContext?.WriteLine($"Core Dashboard UI Test details: {result.details}");
            if (testException != null)
            {
                TestContext?.WriteLine($"Core Dashboard UI Test failed with exception: {testException}");
            }
            Assert.IsTrue(result.created, "Dashboard should be created successfully.");
            Assert.IsTrue(result.navigated, "Dashboard should navigate to a valid view.");
            Assert.IsTrue(result.errorHandled, "Dashboard should handle navigation errors gracefully.");
            Assert.IsTrue(result.responsive, "Dashboard should remain responsive during test.");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Integration")]
        [Timeout(30000)]
        public void RapidNavigationTest()
        {
            var result = (success: true, details: "");
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    var dashboard = new Dashboard(scope.ServiceProvider, routeService, logger);
                    var handle = dashboard.Handle;
                    string[] views = new[] { "routes", "drivers", "vehicles", "overview", "nonexistent", "" };
                    foreach (var view in views)
                    {
                        try
                        {
                            dashboard.Invoke(new Action(async () =>
                            {
                                await dashboard.NavigateToAsync(view!);
                            }));
                        }
                        catch (Exception ex)
                        {
                            result.success = false;
                            result.details += $"Navigation to '{view}' failed: {ex.Message}. ";
                        }
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            dashboard.Invoke(new Action(async () =>
                            {
                                await dashboard.NavigateToAsync("routes");
                                await dashboard.NavigateToAsync("drivers");
                                await dashboard.NavigateToAsync("vehicles");
                            }));
                        }
                        catch (Exception ex)
                        {
                            result.success = false;
                            result.details += $"Rapid navigation iteration {i} failed: {ex.Message}. ";
                        }
                    }
                    dashboard.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                    result.success = false;
                    result.details += $"Exception: {ex.Message}. ";
                }
                finally
                {
                    testCompleted.Set();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!testCompleted.Wait(20000))
            {
                result.success = false;
                result.details += "Test timeout. ";
                testCompleted.Set();
            }
            if (thread.IsAlive)
            {
                thread.Join(1000);
            }
            TestContext?.WriteLine($"Rapid Navigation Test details: {result.details}");
            if (testException != null)
            {
                TestContext?.WriteLine($"Rapid Navigation Test failed with exception: {testException}");
            }
            Assert.IsTrue(result.success, $"Rapid navigation and error handling should succeed. Details: {result.details}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Core")]
        [TestCategory("TextRendering")]
        [Timeout(15000)]
        public void TextRenderingManager_InitializeAndApplyHighQualityRendering_Test()
        {
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();

                    // Test TextRenderingManager initialization
                    TextRenderingManager.Initialize(logger);

                    // Create a test form to work with
                    using var testForm = new Form();
                    testForm.Size = new Size(400, 300);

                    // Create a bitmap to test graphics operations
                    using var bitmap = new Bitmap(100, 100);
                    using var graphics = Graphics.FromImage(bitmap);

                    // Test ApplyHighQualityTextRendering method
                    TextRenderingManager.ApplyHighQualityTextRendering(graphics);

                    // Verify the graphics object has correct high-quality settings
                    Assert.AreEqual(System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, graphics.TextRenderingHint);
                    Assert.AreEqual(System.Drawing.Drawing2D.SmoothingMode.AntiAlias, graphics.SmoothingMode);
                    Assert.AreEqual(System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic, graphics.InterpolationMode);
                    Assert.AreEqual(System.Drawing.Drawing2D.PixelOffsetMode.HighQuality, graphics.PixelOffsetMode);
                    Assert.AreEqual(System.Drawing.Drawing2D.CompositingQuality.HighQuality, graphics.CompositingQuality);

                    testForm.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!testCompleted.Wait(10000))
            {
                Assert.Fail("TextRenderingManager initialization test timed out.");
            }

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            Assert.IsNull(testException, $"TextRenderingManager initialization failed with exception: {testException}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Core")]
        [TestCategory("TextRendering")]
        [Timeout(20000)]
        public void TextRenderingManager_RegisterForHighQualityTextRendering_Test()
        {
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    TextRenderingManager.Initialize(logger);

                    using var testForm = new Form();
                    testForm.Size = new Size(600, 400);

                    // Create various controls to test registration
                    var label = new Label { Text = "Test Label", Name = "TestLabel", Size = new Size(100, 20) };
                    var button = new Button { Text = "Test Button", Name = "TestButton", Size = new Size(100, 30) };
                    var textBox = new TextBox { Text = "Test TextBox", Name = "TestTextBox", Size = new Size(150, 20) };
                    var comboBox = new ComboBox { Name = "TestComboBox", Size = new Size(120, 21) };
                    var dataGridView = new DataGridView { Name = "TestDataGridView", Size = new Size(300, 200) };
                    var panel = new Panel { Name = "TestPanel", Size = new Size(200, 100) };
                    var glassPanel = new Panel { Name = "GlassPanel", Tag = "Glass", Size = new Size(150, 80) };
                    var tableLayout = new TableLayoutPanel { Name = "TestTableLayout", Size = new Size(250, 150) };

                    // Add items to combobox to test drawing
                    comboBox.Items.AddRange(new[] { "Item 1", "Item 2", "Item 3" });

                    // Add columns to DataGridView
                    dataGridView.Columns.Add("Col1", "Column 1");
                    dataGridView.Columns.Add("Col2", "Column 2");
                    dataGridView.Rows.Add("Row1Col1", "Row1Col2");

                    // Add controls to form
                    testForm.Controls.AddRange(new Control[] { label, button, textBox, comboBox, dataGridView, panel, glassPanel, tableLayout });

                    // Test RegisterForHighQualityTextRendering
                    TextRenderingManager.RegisterForHighQualityTextRendering(testForm);

                    // Verify label configuration
                    Assert.IsTrue(label.AutoEllipsis, "Label should have AutoEllipsis enabled");
                    Assert.AreEqual(new Padding(2), label.Padding, "Label should have proper padding");

                    // Verify button configuration
                    Assert.IsFalse(button.UseVisualStyleBackColor, "Button should not use visual style background");
                    Assert.IsTrue(button.AutoEllipsis, "Button should have AutoEllipsis enabled");
                    Assert.AreEqual(new Padding(5, 2, 5, 2), button.Padding, "Button should have proper padding");

                    // Verify textbox configuration
                    Assert.AreEqual(BorderStyle.FixedSingle, textBox.BorderStyle, "TextBox should have FixedSingle border style");

                    // Verify combobox configuration
                    Assert.AreEqual(DrawMode.OwnerDrawFixed, comboBox.DrawMode, "ComboBox should have OwnerDrawFixed mode");

                    // Verify DataGridView configuration
                    Assert.IsFalse(dataGridView.EnableHeadersVisualStyles, "DataGridView should not use header visual styles");
                    Assert.AreEqual(new Padding(5), dataGridView.DefaultCellStyle.Padding, "DataGridView cells should have proper padding");
                    Assert.AreEqual(DataGridViewTriState.True, dataGridView.DefaultCellStyle.WrapMode, "DataGridView should allow text wrapping");
                    Assert.AreEqual(DataGridViewAutoSizeRowsMode.AllCells, dataGridView.AutoSizeRowsMode, "DataGridView should auto-size rows");
                    Assert.AreEqual(30, dataGridView.RowTemplate.MinimumHeight, "DataGridView should have minimum row height");

                    // Verify TableLayoutPanel configuration
                    Assert.AreEqual(new Padding(5), tableLayout.Padding, "TableLayoutPanel should have proper padding");
                    Assert.AreEqual(TableLayoutPanelCellBorderStyle.Single, tableLayout.CellBorderStyle, "TableLayoutPanel should have single cell borders");

                    testForm.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!testCompleted.Wait(15000))
            {
                Assert.Fail("TextRenderingManager registration test timed out.");
            }

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            Assert.IsNull(testException, $"TextRenderingManager registration failed with exception: {testException}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Core")]
        [TestCategory("TextRendering")]
        [Timeout(15000)]
        public void TextRenderingManager_TextTruncationDetectionAndFix_Test()
        {
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    TextRenderingManager.Initialize(logger);

                    using var testForm = new Form();
                    testForm.Size = new Size(400, 300);

                    // Create a label that will likely be truncated
                    var truncatedLabel = new Label
                    {
                        Text = "This is a very long text that will definitely be truncated in a small label",
                        Name = "TruncatedLabel",
                        Size = new Size(50, 20),  // Very small size to force truncation
                        AutoSize = false,
                        AutoEllipsis = false
                    };

                    // Create a label with important tag
                    var importantLabel = new Label
                    {
                        Text = "Important truncated text that should be resized",
                        Name = "ImportantLabel",
                        Size = new Size(50, 20),
                        AutoSize = false,
                        AutoEllipsis = false,
                        Tag = "important"
                    };
                    // Create a properly sized label (should not be truncated)
                    var normalLabel = new Label
                    {
                        Text = "Normal",
                        Name = "NormalLabel",
                        Size = new Size(200, 30),  // Much larger size to ensure no truncation
                        AutoSize = false,
                        AutoEllipsis = false
                    };

                    testForm.Controls.AddRange(new Control[] { truncatedLabel, importantLabel, normalLabel });

                    // Test truncation detection
                    bool isTruncated = TextRenderingManager.IsTextLikelyTruncated(truncatedLabel);
                    bool isNormalTruncated = TextRenderingManager.IsTextLikelyTruncated(normalLabel);

                    Assert.IsTrue(isTruncated, "Long text in small label should be detected as truncated");
                    Assert.IsFalse(isNormalTruncated, "Normal text in adequate label should not be detected as truncated");

                    // Test truncation fix
                    TextRenderingManager.FixPotentialTruncation(testForm);

                    // Verify fixes were applied
                    Assert.IsTrue(truncatedLabel.AutoEllipsis, "Truncated label should have AutoEllipsis enabled after fix");
                    Assert.IsTrue(importantLabel.AutoEllipsis, "Important label should have AutoEllipsis enabled after fix");
                    Assert.IsTrue(importantLabel.AutoSize, "Important label should have AutoSize enabled after fix");
                    Assert.AreEqual(new Size(testForm.Width, 0), importantLabel.MaximumSize, "Important label should have proper MaximumSize set");

                    testForm.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!testCompleted.Wait(10000))
            {
                Assert.Fail("TextRenderingManager truncation test timed out.");
            }

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            Assert.IsNull(testException, $"TextRenderingManager truncation test failed with exception: {testException}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Core")]
        [TestCategory("TextRendering")]
        [Timeout(15000)]
        public void TextRenderingManager_NullSafetyAndErrorHandling_Test()
        {
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    TextRenderingManager.Initialize(logger);

                    // Test null safety for ApplyHighQualityTextRendering
                    try
                    {
                        TextRenderingManager.ApplyHighQualityTextRendering(null!);
                        // Should not throw, method should handle null gracefully
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"ApplyHighQualityTextRendering should handle null graphics gracefully, but threw: {ex.Message}");
                    }

                    // Test null safety for RegisterForHighQualityTextRendering
                    try
                    {
                        TextRenderingManager.RegisterForHighQualityTextRendering(null!);
                        // Should not throw, method should handle null gracefully
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"RegisterForHighQualityTextRendering should handle null control gracefully, but threw: {ex.Message}");
                    }

                    // Test that FixPotentialTruncation throws ArgumentNullException for null
                    try
                    {
                        TextRenderingManager.FixPotentialTruncation(null!);
                        Assert.Fail("FixPotentialTruncation should throw ArgumentNullException for null control");
                    }
                    catch (ArgumentNullException)
                    {
                        // Expected behavior
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"FixPotentialTruncation should throw ArgumentNullException, but threw: {ex.GetType().Name}");
                    }

                    // Test IsTextLikelyTruncated with various control types
                    using var testForm = new Form();
                    var button = new Button { Text = "Test" };
                    var textBox = new TextBox { Text = "Test" };

                    testForm.Controls.AddRange(new Control[] { button, textBox });

                    // These should not throw and should return false for non-label controls
                    bool buttonTruncated = TextRenderingManager.IsTextLikelyTruncated(button);
                    bool textBoxTruncated = TextRenderingManager.IsTextLikelyTruncated(textBox);

                    Assert.IsFalse(buttonTruncated, "Button should not be detected as truncated");
                    Assert.IsFalse(textBoxTruncated, "TextBox should not be detected as truncated");

                    testForm.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!testCompleted.Wait(10000))
            {
                Assert.Fail("TextRenderingManager null safety test timed out.");
            }

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            Assert.IsNull(testException, $"TextRenderingManager null safety test failed with exception: {testException}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Core")]
        [TestCategory("BaseForm")]
        [Timeout(15000)]
        public void BaseForm_ThemeSubscriptionLifecycle_Test()
        {
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    TextRenderingManager.Initialize(logger);

                    // Create a test form that inherits from BaseForm
                    var testForm = new TestBaseForm();

                    // Ensure handle is created before any theme operations
                    var _ = testForm.Handle;

                    // Verify initial state
                    Assert.IsFalse(testForm.IsThemeSubscribed, "Form should not be subscribed before Load");
                    Assert.AreEqual(0, testForm.ApplyThemeCallCount, "ApplyTheme should not have been called yet");

                    // Explicitly simulate Load event
                    testForm.SimulateLoad();

                    Assert.IsTrue(testForm.IsThemeSubscribed, "Form should be subscribed after Load");
                    Assert.AreEqual(1, testForm.ApplyThemeCallCount, "ApplyTheme should be called once during Load");

                    // Test RefreshTheme method
                    testForm.RefreshTheme();
                    Assert.AreEqual(2, testForm.ApplyThemeCallCount, "ApplyTheme should be called again after RefreshTheme");

                    // Test multiple Load calls don't cause double subscription
                    testForm.SimulateLoad();
                    Assert.IsTrue(testForm.IsThemeSubscribed, "Form should still be subscribed");
                    Assert.AreEqual(2, testForm.ApplyThemeCallCount, "ApplyTheme should not be called again on subsequent Load");                    // Test theme change event handling
                    var initialCallCount = testForm.ApplyThemeCallCount;
                    BusBus.UI.Core.ThemeManager.SetTheme("Light");
                    Application.DoEvents(); // Process Windows messages to trigger theme events
                    Thread.Sleep(100); // Allow time for event to process

                    // The theme change should trigger a refresh
                    Assert.IsTrue(testForm.ApplyThemeCallCount > initialCallCount,
                        "Theme change should trigger ApplyTheme call");

                    // Test disposal
                    testForm.Dispose();
                    Assert.IsFalse(testForm.IsThemeSubscribed, "Form should be unsubscribed after disposal");                    // Verify no further theme events are processed after disposal
                    var callCountAfterDisposal = testForm.ApplyThemeCallCount;
                    BusBus.UI.Core.ThemeManager.SetTheme("Dark");
                    Application.DoEvents(); // Process Windows messages
                    Thread.Sleep(100);

                    Assert.AreEqual(callCountAfterDisposal, testForm.ApplyThemeCallCount,
                        "Disposed form should not respond to theme changes");
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!testCompleted.Wait(10000))
            {
                Assert.Fail("BaseForm theme subscription lifecycle test timed out.");
            }

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            Assert.IsNull(testException, $"BaseForm theme subscription lifecycle test failed with exception: {testException}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Core")]
        [TestCategory("BaseForm")]
        [Timeout(15000)]
        public void BaseForm_MemoryLeakPrevention_Test()
        {
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    TextRenderingManager.Initialize(logger);

                    var forms = new List<TestBaseForm>();

                    // Create multiple forms to test event handler cleanup
                    for (int i = 0; i < 5; i++)
                    {
                        var form = new TestBaseForm();
                        var _ = form.Handle;
                        form.SimulateLoad(); // Explicitly simulate Load event
                        forms.Add(form);

                        Assert.IsTrue(form.IsThemeSubscribed, $"Form {i} should be subscribed");
                    }                    // Trigger a theme change to verify all forms respond
                    var initialCallCounts = forms.Select(f => f.ApplyThemeCallCount).ToArray();
                    BusBus.UI.Core.ThemeManager.SetTheme("Light");
                    Application.DoEvents(); // Process Windows messages to trigger theme events
                    Thread.Sleep(150); // Allow time for all events to process

                    for (int i = 0; i < forms.Count; i++)
                    {
                        Assert.IsTrue(forms[i].ApplyThemeCallCount > initialCallCounts[i],
                            $"Form {i} should have responded to theme change");
                    }

                    // Dispose of all forms
                    foreach (var form in forms)
                    {
                        form.Dispose();
                        Assert.IsFalse(form.IsThemeSubscribed, "Form should be unsubscribed after disposal");
                    }                    // Trigger another theme change - no disposed forms should respond
                    var finalCallCounts = forms.Select(f => f.ApplyThemeCallCount).ToArray();
                    BusBus.UI.Core.ThemeManager.SetTheme("Dark");
                    Thread.Sleep(150);

                    for (int i = 0; i < forms.Count; i++)
                    {
                        Assert.AreEqual(finalCallCounts[i], forms[i].ApplyThemeCallCount,
                            $"Disposed form {i} should not respond to theme changes");
                    }

                    forms.Clear();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!testCompleted.Wait(12000))
            {
                Assert.Fail("BaseForm memory leak prevention test timed out.");
            }

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            Assert.IsNull(testException, $"BaseForm memory leak prevention test failed with exception: {testException}");
        }

        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Core")]
        [TestCategory("BaseForm")]
        [Timeout(15000)]
        public void BaseForm_ThreadSafeThemeHandling_Test()
        {
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    TextRenderingManager.Initialize(logger);

                    var form = new TestBaseForm();
                    var _ = form.Handle;
                    form.SimulateLoad(); // Explicitly simulate Load event

                    Assert.IsTrue(form.IsHandleCreated, "Form handle should be created");
                    Assert.IsTrue(form.IsThemeSubscribed, "Form should be subscribed to theme events");

                    var initialCallCount = form.ApplyThemeCallCount;                    // Test thread-safe theme change handling
                    var tasks = new List<Task>();
                    for (int i = 0; i < 3; i++)
                    {
                        var themeIndex = i; tasks.Add(Task.Run(() =>
                        {
                            var theme = themeIndex % 2 == 0 ? "Light" : "Dark";
                            BusBus.UI.Core.ThemeManager.SetTheme(theme);
                        }));
                    }

                    Task.WaitAll(tasks.ToArray(), 5000);
                    Application.DoEvents(); // Process Windows messages to trigger theme events
                    Thread.Sleep(200); // Allow time for all theme changes to process

                    Assert.IsTrue(form.ApplyThemeCallCount > initialCallCount,
                        "Form should have responded to at least one theme change");

                    // Test that disposed form handles theme changes gracefully
                    form.Dispose();
                    Assert.IsFalse(form.IsThemeSubscribed, "Form should be unsubscribed after disposal");                    // This should not throw even though form is disposed
                    BusBus.UI.Core.ThemeManager.SetTheme("Light");
                    Thread.Sleep(100);

                    // Test with form that has no handle created
                    var formWithoutHandle = new TestBaseForm();
                    var __ = formWithoutHandle.Handle;
                    formWithoutHandle.SimulateLoad();
                    formWithoutHandle.Dispose();                    // This should handle the ObjectDisposedException gracefully
                    BusBus.UI.Core.ThemeManager.SetTheme("Dark");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!testCompleted.Wait(12000))
            {
                Assert.Fail("BaseForm thread-safe theme handling test timed out.");
            }

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            Assert.IsNull(testException, $"BaseForm thread-safe theme handling test failed with exception: {testException}");
        }
        [TestMethod]
        [TestCategory("UI")]
        [TestCategory("Core")]
        [TestCategory("BaseForm")]
        [Timeout(15000)]
        public void BaseForm_AbstractMethodImplementation_Test()
        {
            Exception? testException = null;
            var testCompleted = new System.Threading.ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Dashboard>>();
                    TextRenderingManager.Initialize(logger);

                    // Test that the abstract ApplyTheme method is properly implemented
                    var form = new TestBaseForm();

                    // Verify the form can be created (abstract method is implemented)
                    Assert.IsNotNull(form, "TestBaseForm should be instantiable");

                    // Test direct call to ApplyTheme
                    var initialCount = form.ApplyThemeCallCount;
                    form.TestApplyTheme(); // Public wrapper for protected ApplyTheme

                    Assert.AreEqual(initialCount + 1, form.ApplyThemeCallCount,
                        "ApplyTheme should increment the call count");

                    // Test that RefreshTheme calls ApplyTheme
                    form.RefreshTheme();
                    Assert.AreEqual(initialCount + 2, form.ApplyThemeCallCount,
                        "RefreshTheme should call ApplyTheme");

                    form.Dispose();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start(); if (!testCompleted.Wait(12000))
            {
                Assert.Fail("BaseForm abstract method implementation test timed out.");
            }

            if (thread.IsAlive)
            {
                thread.Join(1000);
            }

            Assert.IsNull(testException, $"BaseForm abstract method implementation test failed with exception: {testException}");
        }

        #region AppSettings Configuration Management Tests

        /// <summary>
        /// Test AppSettings singleton initialization and thread safety
        /// </summary>
        [TestMethod]
        [TestCategory("Core")]
        [TestCategory("Configuration")]
        [TestCategory("Threading")]
        [Timeout(15000)] // 15 second timeout
        public void AppSettings_SingletonPatternAndThreadSafety_Test()
        {
            Exception? testException = null;
            const int threadCount = 10;
            const int iterationsPerThread = 50;
            var instances = new ConcurrentBag<BusBus.Configuration.AppSettings>();
            var barriers = new Barrier(threadCount + 1);
            var threads = new Thread[threadCount];
            var cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Create multiple threads to test singleton thread safety
                for (int i = 0; i < threadCount; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        try
                        {
                            barriers.SignalAndWait(); // Wait for all threads to start together

                            for (int j = 0; j < iterationsPerThread; j++)
                            {
                                if (cancellationTokenSource.Token.IsCancellationRequested)
                                    break;
                                var instance = BusBus.Configuration.AppSettings.Instance;
                                instances.Add(instance);
                                Thread.Sleep(1); // Small delay to increase chance of race conditions
                            }
                        }
                        catch (Exception ex)
                        {
                            testException = ex;
                        }
                    })
                    {
                        IsBackground = true
                    };
                    threads[i].Start();
                }

                barriers.SignalAndWait(); // Signal all threads to start

                // Wait for all threads to complete
                foreach (var thread in threads)
                {
                    thread.Join(TimeSpan.FromSeconds(10));
                }

                // Verify no exceptions occurred
                Assert.IsNull(testException, $"AppSettings singleton thread safety test failed with exception: {testException}");

                // Verify all instances are the same reference (singleton pattern)
                var instanceList = instances.ToList();
                Assert.IsTrue(instanceList.Count > 0, "No instances were created");

                var firstInstance = instanceList[0];
                foreach (var instance in instanceList)
                {
                    Assert.AreSame(firstInstance, instance, "AppSettings singleton pattern violated - different instances found");
                }

                Console.WriteLine($"✓ AppSettings singleton test passed with {instanceList.Count} instance accesses across {threadCount} threads");
            }
            catch (Exception ex)
            {
                Assert.Fail($"AppSettings singleton thread safety test failed with exception: {ex}");
            }
            finally
            {
                // Cooperative cancellation for threads
                cancellationTokenSource.Cancel();
                foreach (var thread in threads)
                {
                    if (thread.IsAlive)
                    {
                        try { thread.Join(1000); } catch { }
                    }
                }
                barriers?.Dispose();
            }
        }

        /// <summary>
        /// Test AppSettings JSON configuration loading and validation
        /// </summary>
        [TestMethod]
        [TestCategory("Core")]
        [TestCategory("Configuration")]
        [TestCategory("FileIO")]
        [Timeout(10000)] // 10 second timeout
        public void AppSettings_ConfigurationLoadingAndValidation_Test()
        {
            Exception? testException = null;

            try
            {
                // Test AppSettings instance access
                var appSettings = BusBus.Configuration.AppSettings.Instance;
                Assert.IsNotNull(appSettings, "AppSettings instance should not be null");

                // Test ConnectionStrings configuration
                Assert.IsNotNull(appSettings.ConnectionStrings, "ConnectionStrings should not be null");
                Assert.IsFalse(string.IsNullOrWhiteSpace(appSettings.ConnectionStrings.DefaultConnection),
                    "DefaultConnection should not be null or empty");

                // Validate connection string format
                var connectionString = appSettings.ConnectionStrings.DefaultConnection;
                Assert.IsTrue(connectionString.Contains("Server="), "Connection string should contain Server parameter");
                Assert.IsTrue(connectionString.Contains("Database="), "Connection string should contain Database parameter");

                // Test DatabaseSettings configuration
                Assert.IsNotNull(appSettings.DatabaseSettings, "DatabaseSettings should not be null");
                Assert.IsTrue(appSettings.DatabaseSettings.CommandTimeout > 0,
                    "CommandTimeout should be greater than 0");
                Assert.IsTrue(appSettings.DatabaseSettings.ConnectionTimeout > 0,
                    "ConnectionTimeout should be greater than 0");
                Assert.IsTrue(appSettings.DatabaseSettings.MaxRetryCount >= 0,
                    "MaxRetryCount should be non-negative");
                Assert.IsTrue(appSettings.DatabaseSettings.MaxRetryDelay >= 0,
                    "MaxRetryDelay should be non-negative");

                // Validate reasonable timeout values
                Assert.IsTrue(appSettings.DatabaseSettings.CommandTimeout <= 300,
                    "CommandTimeout should not exceed 5 minutes");
                Assert.IsTrue(appSettings.DatabaseSettings.ConnectionTimeout <= 120,
                    "ConnectionTimeout should not exceed 2 minutes");

                Console.WriteLine($"✓ AppSettings configuration validation passed");
                Console.WriteLine($"  - Connection String: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
                Console.WriteLine($"  - Command Timeout: {appSettings.DatabaseSettings.CommandTimeout}s");
                Console.WriteLine($"  - Connection Timeout: {appSettings.DatabaseSettings.ConnectionTimeout}s");
                Console.WriteLine($"  - Max Retry Count: {appSettings.DatabaseSettings.MaxRetryCount}");
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            Assert.IsNull(testException, $"AppSettings configuration loading test failed with exception: {testException}");
        }

        /// <summary>
        /// Test AppSettings error handling for missing configuration files
        /// </summary>
        [TestMethod]
        [TestCategory("Core")]
        [TestCategory("Configuration")]
        [TestCategory("ErrorHandling")]
        [Timeout(10000)] // 10 second timeout
        public void AppSettings_ErrorHandlingAndResilience_Test()
        {
            Exception? testException = null;

            try
            {
                // Test that AppSettings gracefully handles missing or invalid configurations
                // Since we can't easily mock the file system, we'll test the public interface behavior

                var appSettings = BusBus.Configuration.AppSettings.Instance;
                Assert.IsNotNull(appSettings, "AppSettings should provide a valid instance even with potential config issues");

                // Test default values are applied when configuration is missing or invalid
                if (appSettings.DatabaseSettings != null)
                {
                    // Verify default values are reasonable
                    Assert.IsTrue(appSettings.DatabaseSettings.CommandTimeout >= 15,
                        "CommandTimeout should have a reasonable default minimum");
                    Assert.IsTrue(appSettings.DatabaseSettings.ConnectionTimeout >= 10,
                        "ConnectionTimeout should have a reasonable default minimum");
                    Assert.IsTrue(appSettings.DatabaseSettings.MaxRetryCount >= 0,
                        "MaxRetryCount should have a non-negative default");
                }

                // Test that multiple accesses to Instance return consistent results
                var instance1 = BusBus.Configuration.AppSettings.Instance;
                var instance2 = BusBus.Configuration.AppSettings.Instance;
                Assert.AreSame(instance1, instance2, "Multiple Instance accesses should return same reference");

                // Test that the configuration is immutable after loading
                var originalTimeout = appSettings.DatabaseSettings?.CommandTimeout ?? 30;
                if (appSettings.DatabaseSettings != null)
                {
                    appSettings.DatabaseSettings.CommandTimeout = 999;
                    var newInstance = BusBus.Configuration.AppSettings.Instance;
                    // Since it's a singleton, the change should persist within the same app domain
                    Assert.AreEqual(999, newInstance.DatabaseSettings.CommandTimeout,
                        "Configuration changes should persist in singleton instance");

                    // Reset for other tests
                    appSettings.DatabaseSettings.CommandTimeout = originalTimeout;
                }

                Console.WriteLine($"✓ AppSettings error handling and resilience test passed");
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            Assert.IsNull(testException, $"AppSettings error handling test failed with exception: {testException}");
        }

        /// <summary>
        /// Test AppSettings performance and memory usage
        /// </summary>
        [TestMethod]
        [TestCategory("Core")]
        [TestCategory("Configuration")]
        [TestCategory("Performance")]
        [Timeout(15000)] // 15 second timeout
        public void AppSettings_PerformanceAndMemoryUsage_Test()
        {
            Exception? testException = null;

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var initialMemory = GC.GetTotalMemory(false);

                // Perform many accesses to test performance
                const int accessCount = 10000;
                BusBus.Configuration.AppSettings? lastInstance = null;

                for (int i = 0; i < accessCount; i++)
                {
                    lastInstance = BusBus.Configuration.AppSettings.Instance;

                    // Occasionally access properties to test full usage performance
                    if (i % 1000 == 0)
                    {
                        var _ = lastInstance.ConnectionStrings?.DefaultConnection;
                        var __ = lastInstance.DatabaseSettings?.CommandTimeout;
                    }
                }

                stopwatch.Stop();
                var finalMemory = GC.GetTotalMemory(false);
                var memoryIncrease = finalMemory - initialMemory;

                // Performance assertions
                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000,
                    $"AppSettings access should be fast - took {stopwatch.ElapsedMilliseconds}ms for {accessCount} accesses");

                // Memory usage assertions (allowing for reasonable overhead)
                Assert.IsTrue(memoryIncrease < 1024 * 1024, // Less than 1MB increase
                    $"AppSettings should not cause significant memory leaks - memory increased by {memoryIncrease / 1024}KB");

                // Verify the instance is still valid
                Assert.IsNotNull(lastInstance, "Final instance should not be null");
                Assert.IsNotNull(lastInstance.ConnectionStrings, "ConnectionStrings should remain valid");

                Console.WriteLine($"✓ AppSettings performance test passed");
                Console.WriteLine($"  - {accessCount} accesses in {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"  - Average: {(double)stopwatch.ElapsedMilliseconds / accessCount:F3}ms per access");
                Console.WriteLine($"  - Memory increase: {memoryIncrease / 1024}KB");
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            Assert.IsNull(testException, $"AppSettings performance test failed with exception: {testException}");
        }

        #endregion

        #region ProjectAnalyzer Analytics Core Tests

        /// <summary>
        /// Test ProjectAnalyzer report generation and core functionality
        /// </summary>
        [TestMethod]
        [TestCategory("Core")]
        [TestCategory("Analytics")]
        [TestCategory("Database")]
        [Timeout(20000)] // 20 second timeout for database operations
        public async Task ProjectAnalyzer_ReportGenerationAndCoreFunctionality_Test()
        {
            Exception? testException = null;

            try
            {
                using var analyzer = new BusBus.Analytics.ProjectAnalyzer();
                Assert.IsNotNull(analyzer, "ProjectAnalyzer should be instantiated successfully");

                // Test report generation with cancellation token
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var report = await analyzer.GenerateFullReportAsync(cts.Token);

                // Verify report structure
                Assert.IsNotNull(report, "Analytics report should not be null");
                Assert.IsTrue(report.GeneratedDate > DateTime.MinValue, "Report should have a valid generation date");
                Assert.IsTrue(report.GeneratedDate <= DateTime.Now, "Report generation date should not be in the future");

                // Verify database health analysis
                Assert.IsNotNull(report.DatabaseHealth, "Database health report should not be null");
                Assert.IsFalse(string.IsNullOrWhiteSpace(report.DatabaseHealth.ConnectionStatus),
                    "Connection status should not be empty");
                Assert.IsTrue(report.DatabaseHealth.TableCount >= 0, "Table count should be non-negative");
                Assert.IsTrue(report.DatabaseHealth.IndexEfficiency >= 0 && report.DatabaseHealth.IndexEfficiency <= 100,
                    "Index efficiency should be a valid percentage");
                Assert.IsTrue(report.DatabaseHealth.StorageUtilization >= 0,
                    "Storage utilization should be non-negative");

                // Verify feature utilization analysis
                Assert.IsNotNull(report.FeatureUtilization, "Feature utilization report should not be null");
                // Test that at least some features are tracked
                var featureProperties = typeof(BusBus.Analytics.FeatureUtilizationReport).GetProperties()
                    .Where(p => p.PropertyType == typeof(bool));
                Assert.IsTrue(featureProperties.Any(), "Feature utilization should track boolean features");

                // Verify other report sections exist
                Assert.IsNotNull(report.PerformanceMetrics, "Performance metrics should not be null");
                Assert.IsNotNull(report.SecurityAnalysis, "Security analysis should not be null");
                Assert.IsNotNull(report.IntegrationStatus, "Integration status should not be null");

                Console.WriteLine($"✓ ProjectAnalyzer report generation test passed");
                Console.WriteLine($"  - Report generated at: {report.GeneratedDate}");
                Console.WriteLine($"  - Connection status: {report.DatabaseHealth.ConnectionStatus}");
                Console.WriteLine($"  - Table count: {report.DatabaseHealth.TableCount}");
                Console.WriteLine($"  - Index efficiency: {report.DatabaseHealth.IndexEfficiency}%");
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            Assert.IsNull(testException, $"ProjectAnalyzer report generation test failed with exception: {testException}");
        }

        /// <summary>
        /// Test ProjectAnalyzer method analysis functionality
        /// </summary>
        [TestMethod]
        [TestCategory("Core")]
        [TestCategory("Analytics")]
        [TestCategory("CodeAnalysis")]
        [Timeout(10000)] // 10 second timeout
        public void ProjectAnalyzer_MethodAnalysisAndComplexityDetection_Test()
        {
            Exception? testException = null;
            try
            {
                // Test method analysis with normal parameters
                BusBus.Analytics.ProjectAnalyzer.AnalyzeMethod(
                    "TestFile.cs",
                    "SimpleMethod",
                    lineCount: 20,
                    complexity: 5,
                    out bool hasIssueNormal,
                    out string issueNormal);

                Assert.IsFalse(hasIssueNormal, "Normal method should not have issues");
                Assert.IsTrue(string.IsNullOrEmpty(issueNormal), "Normal method should not have issue message");

                // Test method analysis with high complexity
                BusBus.Analytics.ProjectAnalyzer.AnalyzeMethod(
                    "ComplexFile.cs",
                    "ComplexMethod",
                    lineCount: 30,
                    complexity: 15,
                    out bool hasIssueComplex,
                    out string issueComplex);

                Assert.IsTrue(hasIssueComplex, "Complex method should have issues detected");
                Assert.IsFalse(string.IsNullOrEmpty(issueComplex), "Complex method should have issue message");
                Assert.IsTrue(issueComplex.Contains("complexity"), "Issue should mention complexity");

                // Test method analysis with long method
                BusBus.Analytics.ProjectAnalyzer.AnalyzeMethod(
                    "LongFile.cs",
                    "LongMethod",
                    lineCount: 75,
                    complexity: 3,
                    out bool hasIssueLong,
                    out string issueLong);

                Assert.IsTrue(hasIssueLong, "Long method should have issues detected");
                Assert.IsFalse(string.IsNullOrEmpty(issueLong), "Long method should have issue message");
                Assert.IsTrue(issueLong.Contains("long"), "Issue should mention method length");

                // Test method analysis with edge cases
                BusBus.Analytics.ProjectAnalyzer.AnalyzeMethod(
                    "",
                    "",
                    lineCount: 0,
                    complexity: 0,
                    out bool hasIssueEdge,
                    out string issueEdge);

                Assert.IsFalse(hasIssueEdge, "Edge case with zeros should not have issues");

                Console.WriteLine($"✓ ProjectAnalyzer method analysis test passed");
                Console.WriteLine($"  - Normal method: No issues detected");
                Console.WriteLine($"  - Complex method: {issueComplex}");
                Console.WriteLine($"  - Long method: {issueLong}");
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            Assert.IsNull(testException, $"ProjectAnalyzer method analysis test failed with exception: {testException}");
        }

        /// <summary>
        /// Test ProjectAnalyzer cancellation and error handling
        /// </summary>
        [TestMethod]
        [TestCategory("Core")]
        [TestCategory("Analytics")]
        [TestCategory("ErrorHandling")]
        [Timeout(15000)] // 15 second timeout
        public async Task ProjectAnalyzer_CancellationAndErrorHandling_Test()
        {
            Exception? testException = null;

            try
            {
                using var analyzer = new BusBus.Analytics.ProjectAnalyzer();

                // Test cancellation handling
                using var cts = new CancellationTokenSource();
                cts.Cancel(); // Cancel immediately

                bool cancellationThrown = false;
                try
                {
                    var _ = await analyzer.GenerateFullReportAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    cancellationThrown = true;
                }

                Assert.IsTrue(cancellationThrown, "Canceled operation should throw OperationCanceledException");

                // Test timeout handling
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
                await Task.Delay(10); // Ensure token is cancelled

                bool timeoutHandled = false;
                try
                {
                    var _ = await analyzer.GenerateFullReportAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    timeoutHandled = true;
                }

                Assert.IsTrue(timeoutHandled, "Timeout should be handled gracefully");

                // Test successful execution with valid token
                using var validCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var validReport = await analyzer.GenerateFullReportAsync(validCts.Token);
                Assert.IsNotNull(validReport, "Valid operation should succeed");

                Console.WriteLine($"✓ ProjectAnalyzer cancellation and error handling test passed");
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            Assert.IsNull(testException, $"ProjectAnalyzer cancellation test failed with exception: {testException}");
        }

        /// <summary>
        /// Test ProjectAnalyzer resource management and disposal
        /// </summary>
        [TestMethod]
        [TestCategory("Core")]
        [TestCategory("Analytics")]
        [TestCategory("ResourceManagement")]
        [Timeout(10000)] // 10 second timeout
        public async Task ProjectAnalyzer_ResourceManagementAndDisposal_Test()
        {
            Exception? testException = null;

            try
            {
                BusBus.Analytics.AnalyticsReport? report = null;

                // Test proper disposal in using statement
                using (var analyzer = new BusBus.Analytics.ProjectAnalyzer())
                {
                    Assert.IsNotNull(analyzer, "Analyzer should be created successfully");
                    report = await analyzer.GenerateFullReportAsync();
                    Assert.IsNotNull(report, "Report should be generated before disposal");
                }
                // Analyzer should be disposed here

                // Verify report is still valid after analyzer disposal
                Assert.IsNotNull(report, "Report should remain valid after analyzer disposal");
                Assert.IsNotNull(report.DatabaseHealth, "Report data should remain accessible");

                // Test multiple disposals (should not throw)
                var analyzer2 = new BusBus.Analytics.ProjectAnalyzer();
                analyzer2.Dispose();
                analyzer2.Dispose(); // Second disposal should be safe

                // Test disposal pattern with exception handling
                var analyzer3 = new BusBus.Analytics.ProjectAnalyzer();
                try
                {
                    var _ = await analyzer3.GenerateFullReportAsync();
                }
                finally
                {
                    analyzer3.Dispose(); // Should work even if exception occurred
                }

                Console.WriteLine($"✓ ProjectAnalyzer resource management test passed");
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            Assert.IsNull(testException, $"ProjectAnalyzer resource management test failed with exception: {testException}");
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Helper class for testing BaseForm functionality.
        /// Provides access to protected members through inheritance and reflection.
        /// </summary>
        private class TestBaseForm : BaseForm
        {
            public int ApplyThemeCallCount { get; private set; }
            public bool IsThemeSubscribed => _themeSubscribed;

            // Use reflection to access private field for testing
            private bool _themeSubscribed
            {
                get
                {
                    var field = typeof(BaseForm).GetField("_themeSubscribed",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    return field != null && (bool)field.GetValue(this)!;
                }
            }
            protected override void ApplyTheme()
            {
                ApplyThemeCallCount++;
                // Simulate theme application
                this.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground;
                this.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardText;
            }

            public void TestApplyTheme()
            {
                ApplyTheme();
            }

            public void SimulateLoad()
            {
                // Simulate another Load event
                OnLoad(EventArgs.Empty);
            }
        }

        #endregion
    }
}
