using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BusBus.Tests.UI
{
    [TestClass]
    [TestCategory(TestCategories.UI)]
    public class DashboardViewControlTests : TestBase
    {
        private Mock<IRouteService> _mockRouteService;
        private Mock<ILogger<DashboardView>> _mockLogger;
        private ServiceProvider _serviceProvider;
        private List<string> _logMessages;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();
            _logMessages = new List<string>();
            _mockRouteService = new Mock<IRouteService>();
            _mockLogger = new Mock<ILogger<DashboardView>>(); _mockLogger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var logLevel = (LogLevel)invocation.Arguments[0];
                    var eventId = (EventId)invocation.Arguments[1];
                    var state = invocation.Arguments[2];
                    var exception = (Exception)invocation.Arguments[3];
                    var formatter = invocation.Arguments[4];

                    if (formatter != null)
                    {
                        try
                        {
                            var method = formatter.GetType().GetMethod("Invoke");
                            var message = method?.Invoke(formatter, new[] { state, exception });
                            _logMessages.Add($"{logLevel}: {message}");
                        }
                        catch
                        {
                            _logMessages.Add($"{logLevel}: [Log message could not be formatted]");
                        }
                    }
                }));

            var services = new ServiceCollection();
            services.AddSingleton(_mockRouteService.Object);
            services.AddSingleton(_mockLogger.Object); _serviceProvider = services.BuildServiceProvider();

            // Initialize Windows Forms if not already done
            try
            {
                if (Application.OpenForms.Count == 0)
                {
                    Application.SetHighDpiMode(HighDpiMode.SystemAware);
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                }
            }
            catch (InvalidOperationException)
            {
                // SetCompatibleTextRenderingDefault can only be called once per app domain
                // If it's already been called, just continue
            }
        }

        [TestCleanup]
        public void CleanUp()
        {
            _serviceProvider?.Dispose();
        }

        [TestMethod]
        public async Task Constructor_CreatesValidInstance()
        {
            DashboardView view = null;
            Exception ex = null;
            await InvokeOnUIThread(() =>
            {
                try { view = new DashboardView(_serviceProvider); }
                catch (Exception e) { ex = e; }
            });
            ex.Should().BeNull();
            view.Should().NotBeNull().And.BeAssignableTo<Control>();
        }

        [TestMethod]
        public async Task AfterActivation_HasRequiredPanels()
        {
            var view = await CreateAndActivateView();
            view.Controls.Count.Should().BeGreaterThan(0);
            GetControlsOfType<TableLayoutPanel>(view).Should().NotBeEmpty();
        }

        [TestMethod]
        public async Task RoutesPanel_ExistsAndAccessible()
        {
            var view = await CreateAndActivateView();
            var routesPanels = FindControlsByName(view, "routes");
            var todaysRoutesControls = FindControlsByName(view, "today");
            GetControlsOfType<TableLayoutPanel>(view).Should().NotBeEmpty();
            FindControlsWithRoutesTag(view).Should().NotBeEmpty();
        }        [TestMethod]
        public async Task TodaysRoutesPanel_ContainsTableLayoutPanel()
        {
            var view = await CreateAndActivateView();
            var todaysRoutesPanel = GetField<Panel>(view, "_todaysRoutesPanel");
            todaysRoutesPanel.Should().NotBeNull();
            if (todaysRoutesPanel != null)
            {
                // First, let's see the complete structure with detailed logging
                LogCompleteHierarchy(todaysRoutesPanel, "TodaysRoutesPanel");
                
                var tableLayoutPanels = GetControlsOfType<TableLayoutPanel>(todaysRoutesPanel);
                if (tableLayoutPanels.Length == 0)
                {
                    var controlHierarchy = AnalyzeControlHierarchy(todaysRoutesPanel);
                    var hierarchyInfo = string.Join(", ", controlHierarchy.Select(kv => $"{kv.Key}: {kv.Value}"));
                    Assert.Fail($"No TableLayoutPanel found in _todaysRoutesPanel. Found: [{hierarchyInfo}]");
                }

                tableLayoutPanels.Length.Should().BeGreaterThan(0, "Should contain at least one TableLayoutPanel for routes");
                var routesList = tableLayoutPanels[0];
                routesList.Name.Should().Be("routesList", "TableLayoutPanel should be named routesList");
            }
        }

        [TestMethod]
        public async Task RefreshRoutesAsync_DoesNotThrow()
        {
            var view = await CreateAndActivateView(); var refreshMethod = typeof(DashboardView).GetMethod("RefreshRoutesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (refreshMethod != null)
            {
                var task = refreshMethod.Invoke(view, null) as Task;
                Func<Task> act = () => task;
                await act.Should().NotThrowAsync();
            }
            else
            {
                Assert.Inconclusive("RefreshRoutesAsync not found");
            }
        }

        [TestMethod]
        public async Task WithMockedRouteService_LoadsSuccessfully()
        {
            _mockRouteService.Setup(rs => rs.GetRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Route> { new Route { Id = Guid.NewGuid(), Name = "Test Route 1", RouteCode = "TR1" } });
            var view = await CreateAndActivateView();
            view.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ControlHierarchy_IsWellFormed()
        {
            var view = await CreateAndActivateView();
            var hierarchy = AnalyzeControlHierarchy(view);
            hierarchy.Should().NotBeEmpty();
            Action verify = () => VerifyNoCircularReferences(view);
            verify.Should().NotThrow();
        }

        [TestMethod]
        public async Task RoutesList_InitializedCorrectly()
        {
            _mockRouteService.Setup(rs => rs.GetRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Route> { new Route { Id = Guid.NewGuid(), Name = "Test Route", RouteCode = "TR" } });
            var view = await CreateAndActivateView();
            var routesList = FindControlsByName(view, "routesList").FirstOrDefault();
            routesList.Should().NotBeNull("RoutesList should exist").And.BeOfType<TableLayoutPanel>();
            var log = _logMessages.FirstOrDefault(m => m.Contains("Routes list control not found"));
            log.Should().BeNull("No 'Routes list control not found' warning should be logged");
        }

        [TestMethod]
        public async Task EmptyRoutes_DoesNotCrash()
        {
            _mockRouteService.Setup(rs => rs.GetRoutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Route>());
            var view = await CreateAndActivateView();
            var routesList = FindControlsByName(view, "routesList").FirstOrDefault();
            routesList.Should().NotBeNull();
            Func<Task> act = () => RefreshRoutesAsync(view);
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task RapidRefresh_StressTest()
        {
            _mockRouteService.Setup(rs => rs.GetRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Route> { new Route { Id = Guid.NewGuid(), Name = "Test Route", RouteCode = "TR" } });
            var view = await CreateAndActivateView();
            for (int i = 0; i < 50; i++)
            {
                Func<Task> act = () => RefreshRoutesAsync(view);
                await act.Should().NotThrowAsync($"Iteration {i} should not throw");
            }
            view.Controls.Count.Should().BeGreaterThan(0, "Controls should remain after rapid refresh");
        }
        [TestMethod]
        public async Task ConcurrentActivation_ThreadSafety()
        {
            var view = new DashboardView(_serviceProvider);
            var activateMethod = typeof(DashboardView).GetMethod("OnActivateAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tasks = Enumerable.Range(0, 10).Select(_ => activateMethod.Invoke(view, new object[] { CancellationToken.None }) as Task).ToArray();
            Func<Task> act = () => Task.WhenAll(tasks);
            await act.Should().NotThrowAsync();
            view.Controls.Count.Should().BeGreaterThan(0);
            var log = _logMessages.FirstOrDefault(m => m.Contains("initialization", StringComparison.OrdinalIgnoreCase));
            log.Should().NotBeNull("Initialization should be logged");
        }

        [TestMethod]
        public async Task Dispose_AfterActivation_NoException()
        {
            var view = await CreateAndActivateView();
            await InvokeOnUIThread(() =>
            {
                Action dispose = () => view.Dispose();
                dispose.Should().NotThrow();
            });
            view.IsDisposed.Should().BeTrue();
        }

        [TestMethod]
        public async Task InvalidRouteData_HandlesGracefully()
        {
            _mockRouteService.Setup(rs => rs.GetRoutesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Service error"));
            var view = await CreateAndActivateView();
            var log = _logMessages.FirstOrDefault(m => m.Contains("service error", StringComparison.OrdinalIgnoreCase));
            log.Should().NotBeNull("Service error should be logged");
            view.Controls.Count.Should().BeGreaterThan(0, "Controls should remain after error");
        }

        #region Helpers

        private async Task<DashboardView> CreateAndActivateView()
        {
            DashboardView view = null;
            await InvokeOnUIThread(() => view = new DashboardView(_serviceProvider));
            var activateMethod = typeof(DashboardView).GetMethod("OnActivateAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (activateMethod != null)
                await (Task)activateMethod.Invoke(view, new object[] { CancellationToken.None });
            return view;
        }
        private async Task RefreshRoutesAsync(DashboardView view)
        {
            var method = typeof(DashboardView).GetMethod("RefreshRoutesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
                await (Task)method.Invoke(view, null);
        }

        private static T GetField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)field?.GetValue(obj);
        }

        private static T[] GetControlsOfType<T>(Control parent) where T : Control
        {
            var controls = new List<T>();
            GetControlsOfTypeRecursive(parent, controls);
            return controls.ToArray();
        }

        private static void GetControlsOfTypeRecursive<T>(Control parent, List<T> result) where T : Control
        {
            foreach (Control control in parent.Controls)
            {
                if (control is T typedControl)
                    result.Add(typedControl);
                GetControlsOfTypeRecursive(control, result);
            }
        }

        private static Control[] FindControlsByName(Control parent, string nameContains)
        {
            var controls = new List<Control>();
            FindControlsByNameRecursive(parent, nameContains.ToLowerInvariant(), controls);
            return controls.ToArray();
        }

        private static void FindControlsByNameRecursive(Control parent, string nameContains, List<Control> result)
        {
            foreach (Control control in parent.Controls)
            {
                if (!string.IsNullOrEmpty(control.Name) && control.Name.ToLowerInvariant().Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                    result.Add(control);
                FindControlsByNameRecursive(control, nameContains, result);
            }
        }

        private static Control[] FindControlsWithRoutesTag(Control parent)
        {
            var controls = new List<Control>();
            FindControlsWithRoutesTagRecursive(parent, controls);
            return controls.ToArray();
        }

        private static void FindControlsWithRoutesTagRecursive(Control parent, List<Control> result)
        {
            foreach (Control control in parent.Controls)
            {
                var tag = control.Tag?.ToString()?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(tag) && (tag.Contains("route") || tag.Contains("routes")))
                    result.Add(control);
                FindControlsWithRoutesTagRecursive(control, result);
            }
        }

        private static Dictionary<string, int> AnalyzeControlHierarchy(Control parent)
        {
            var hierarchy = new Dictionary<string, int>();
            AnalyzeControlHierarchyRecursive(parent, hierarchy);
            return hierarchy;
        }

        private static void AnalyzeControlHierarchyRecursive(Control parent, Dictionary<string, int> hierarchy)
        {
            var typeName = parent.GetType().Name;
            hierarchy[typeName] = hierarchy.GetValueOrDefault(typeName, 0) + 1;
            foreach (Control child in parent.Controls)
                AnalyzeControlHierarchyRecursive(child, hierarchy);
        }

        private static void VerifyNoCircularReferences(Control parent)
        {
            var visited = new HashSet<Control>();
            VerifyNoCircularReferencesRecursive(parent, visited);
        }

        private static void VerifyNoCircularReferencesRecursive(Control current, HashSet<Control> visited)
        {
            if (visited.Contains(current))
                throw new InvalidOperationException($"Circular reference detected at {current.GetType().Name}");
            visited.Add(current);
            foreach (Control child in current.Controls)
                VerifyNoCircularReferencesRecursive(child, new HashSet<Control>(visited));
        }

        private static void LogCompleteHierarchy(Control parent, string parentName, int level = 0)
        {
            string indent = new string(' ', level * 2);
            Console.WriteLine($"{indent}{parentName}: {parent.GetType().Name} (Name='{parent.Name}', Tag='{parent.Tag}', Controls={parent.Controls.Count})");
            
            for (int i = 0; i < parent.Controls.Count; i++)
            {
                var child = parent.Controls[i];
                LogCompleteHierarchy(child, $"[{i}]", level + 1);
            }
        }

        private static async Task InvokeOnUIThread(Action action) => await Task.Run(action);
        private static async Task InvokeOnUIThread(Func<Task> asyncAction) => await asyncAction();

        #endregion
    }
}
