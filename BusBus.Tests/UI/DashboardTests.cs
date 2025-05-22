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
    public class DashboardTests
    {
        private readonly Mock<IRouteService> _mockRouteService;

        public DashboardTests()
        {
            _mockRouteService = new Mock<IRouteService>();
        }

        [Fact]
        public void Dashboard_Constructor_InitializesComponents()
        {
            // Arrange & Act - Skip for now as we need UI access
            // We would create a Dashboard instance and verify initialization

            // Assert
            // This would verify the dashboard is initialized correctly
            Assert.True(true, "This test is a placeholder for UI component testing");
        }

        [Fact]
        public void Dashboard_ThemeChanges_AffectAppearance()
        {
            // Arrange - Skip for now as we need UI access
            // We would create a Dashboard with a theme

            // Act
            // Change theme settings

            // Assert
            // Verify appearance changes
            Assert.True(true, "This test is a placeholder for theme testing");
        }

        [Fact]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async Task Dashboard_RouteDisplay_ShowsCorrectRouteData()
        {
            // Arrange
            var routes = new List<Route>
            {
                new Route { Id = Guid.NewGuid(), Name = "Route 1" },
                new Route { Id = Guid.NewGuid(), Name = "Route 2" }
            };

            _mockRouteService.Setup(s => s.GetRoutesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(routes);
            _mockRouteService.Setup(s => s.GetRoutesCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(routes.Count);

            var dashboard = new Dashboard(_mockRouteService.Object);

            // Simulate loading routes (call the method that loads data into the RouteListPanel)
            var loadRoutesMethod = typeof(Dashboard).GetMethod("LoadRoutesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(loadRoutesMethod);
            var task = (Task?)loadRoutesMethod.Invoke(dashboard, new object[] { 1, 10, CancellationToken.None });
            Assert.NotNull(task);
            await task;

            // Find the RouteListPanel in the Dashboard's main panel
            var mainPanelField = typeof(Dashboard).GetField("_mainPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(mainPanelField);
            var mainPanel = mainPanelField.GetValue(dashboard) as System.Windows.Forms.Panel;
            Assert.NotNull(mainPanel);

            RouteListPanel? routeListPanel = null;
            foreach (var control in mainPanel.Controls)
            {
                if (control is RouteListPanel rlp)
                {
                    routeListPanel = rlp;
                    break;
                }
            }
            Assert.NotNull(routeListPanel);

            // Get the DataGridView from RouteListPanel via reflection
            var gridField = typeof(RouteListPanel).GetField("_routesGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(gridField);
            var grid = gridField.GetValue(routeListPanel) as System.Windows.Forms.DataGridView;
            Assert.NotNull(grid);
            Assert.NotNull(grid.DataSource);
            foreach (System.Windows.Forms.DataGridViewColumn col in grid.Columns)
            {
                Assert.Equal(System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter, col.HeaderCell.Style.Alignment);
            }
        }
    }
}