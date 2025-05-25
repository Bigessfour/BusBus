using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using BusBus.Models;
using BusBus.Services;
using BusBus.Tests.Helpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace BusBus.Tests.Unit
{
    [TestFixture]
    public class RouteServiceTests
    {
        private Mock<IRouteService> _mockRouteService;
        private Fixture _fixture;
        
        [SetUp]
        public void SetUp()
        {
            _mockRouteService = MockHelperEnhanced.CreateRouteServiceMock();
            _fixture = new Fixture();
        }
        
        [Test]
        public async Task GetRoutesAsync_ShouldReturnAllRoutes()
        {
            // Arrange
            var expectedRoutes = MockHelperEnhanced.CreateTestRoutes(5);
            _mockRouteService.Setup(m => m.GetRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRoutes);
                
            // Act
            var result = await _mockRouteService.Object.GetRoutesAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            result.Should().BeEquivalentTo(expectedRoutes);
            _mockRouteService.Verify(m => m.GetRoutesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public async Task GetRouteByIdAsync_WithExistingId_ShouldReturnRoute()
        {
            // Arrange
            var expectedRoute = MockHelperEnhanced.CreateTestRoutes(1).First();
            _mockRouteService.Setup(m => m.GetRouteByIdAsync(expectedRoute.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRoute);
                
            // Act
            var result = await _mockRouteService.Object.GetRouteByIdAsync(expectedRoute.Id);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedRoute);
            _mockRouteService.Verify(m => m.GetRouteByIdAsync(expectedRoute.Id, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public async Task GetRouteByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            _mockRouteService.Setup(m => m.GetRouteByIdAsync(nonExistingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Route?)null);
                
            // Act
            var result = await _mockRouteService.Object.GetRouteByIdAsync(nonExistingId);
            
            // Assert
            result.Should().BeNull();
            _mockRouteService.Verify(m => m.GetRouteByIdAsync(nonExistingId, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public async Task AddRouteAsync_ShouldAddAndReturnRoute()
        {
            // Arrange
            var newRoute = _fixture.Build<Route>()
                .With(r => r.Id, Guid.NewGuid())
                .With(r => r.Name, "New Test Route")
                .Without(r => r.Driver)
                .Without(r => r.Vehicle)
                .Create();
                
            _mockRouteService.Setup(m => m.CreateRouteAsync(It.IsAny<Route>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Route r, CancellationToken _) => r);
            // Act
            var result = await _mockRouteService.Object.CreateRouteAsync(newRoute);
            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(newRoute);
            _mockRouteService.Verify(m => m.CreateRouteAsync(It.IsAny<Route>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public void GetRoutesAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            
            _mockRouteService.Setup(m => m.GetRoutesAsync(It.Is<CancellationToken>(ct => ct.IsCancellationRequested)))
                .Throws<OperationCanceledException>();
                
            // Act & Assert
            Func<Task> act = async () => await _mockRouteService.Object.GetRoutesAsync(cancellationTokenSource.Token);
            act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
