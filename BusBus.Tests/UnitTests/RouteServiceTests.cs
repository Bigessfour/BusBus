using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;
using BusBus.Tests.Common;
using BusBus.Services;
using BusBus.Models;
using System.Linq;
using System.Threading;

namespace BusBus.Tests.UnitTests
{
    [TestFixture]
    public class RouteServiceTests : TestBase
    {
        private Mock<IRouteService> _mockRouteService;
        
        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _mockRouteService = MockHelper.CreateRouteServiceMock();
        }
        
        [Test]
        public async Task GetRoutesAsync_ReturnsRoutes()
        {
            // Arrange
            var testRoutes = MockHelper.CreateTestRoutes(3);
            _mockRouteService.Setup(m => m.GetRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(testRoutes);
                
            // Act
            var result = await _mockRouteService.Object.GetRoutesAsync();
            
            // Assert
            Assert.That(result, Is.Not.Null, "Should return routes");
            Assert.That(result.Count, Is.EqualTo(3), "Should return 3 routes");
            
            // Verify the method was called exactly once
            _mockRouteService.Verify(m => m.GetRoutesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public async Task GetDriversAsync_ReturnsDrivers()
        {
            // Arrange
            var testDrivers = MockHelper.CreateTestDrivers(2);
            _mockRouteService.Setup(m => m.GetDriversAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(testDrivers);
                
            // Act
            var result = await _mockRouteService.Object.GetDriversAsync();
            
            // Assert
            Assert.That(result, Is.Not.Null, "Should return drivers");
            Assert.That(result.Count, Is.EqualTo(2), "Should return 2 drivers");
            
            // Verify the method was called exactly once
            _mockRouteService.Verify(m => m.GetDriversAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public void GetRoutesAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            
            _mockRouteService.Setup(m => m.GetRoutesAsync(It.IsAny<CancellationToken>()))
                .Throws<OperationCanceledException>();
                
            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(async () => 
                await _mockRouteService.Object.GetRoutesAsync(cancellationTokenSource.Token));
        }
    }
}
