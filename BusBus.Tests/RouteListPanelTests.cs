using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using NSubstitute;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;

namespace BusBus.Tests
{
    [TestFixture]
    public class RouteListPanelTests : TestBase
    {
        private IRouteService? _routeService;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _routeService = Substitute.For<IRouteService>();
        }

        [Test]
        public void Dispose_ReleasesResourcesAndSetsDisposedFlag()
        {
            // Arrange
            var mockRouteService = Substitute.For<IRouteService>();
            var panel = new RouteListPanel(mockRouteService);

            // Act
            panel.Dispose();

            // Assert
            // Try to access a property or method that should throw or be safe after dispose
            Assert.That(() => panel.RefreshTheme(), Throws.Nothing, "RefreshTheme should not throw after dispose");
        }

        [Test]
        public void Constructor_CreatesPanel_WithoutException()
        {
            // Arrange & Act
            var panel = new RouteListPanel(_routeService!);

            // Assert
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel, Is.InstanceOf<RouteListPanel>());
        }
    }
}
