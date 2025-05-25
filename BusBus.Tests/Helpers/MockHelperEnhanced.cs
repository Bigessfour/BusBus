using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using BusBus.Services;
using BusBus.Models;
using BusBus.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BusBus.Tests.Helpers
{
    /// <summary>
    /// Enhanced mock helper with AutoFixture integration for generating test data
    /// </summary>
    public static class MockHelperEnhanced
    {
        private static readonly Fixture _fixture;

        static MockHelperEnhanced()
        {
            // Configure AutoFixture with AutoMoq
            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());
        }

        /// <summary>
        /// Creates a mock of IRouteService with more advanced setup options
        /// </summary>
        public static Mock<IRouteService> CreateRouteServiceMock(MockBehavior behavior = MockBehavior.Loose)
        {
            var mock = new Mock<IRouteService>(behavior);
            
            // Setup basic methods with generated test data
            var routes = CreateTestRoutes(3);
            var drivers = CreateTestDrivers(2);
            var vehicles = CreateTestVehicles(2);
            

            mock.Setup(m => m.GetRoutesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(routes));

            mock.Setup(m => m.GetDriversAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(drivers));

            mock.Setup(m => m.GetVehiclesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(vehicles));

            // Setup CRUD operations

            mock.Setup(m => m.GetRouteByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) => Task.FromResult<Route?>(routes.FirstOrDefault(r => r.Id == id)));

            mock.Setup(m => m.CreateRouteAsync(It.IsAny<Route>(), It.IsAny<CancellationToken>()))
                .Returns((Route route, CancellationToken _) =>
                {
                    routes.Add(route);
                    return Task.FromResult(route);
                });

            mock.Setup(m => m.UpdateRouteAsync(It.IsAny<Route>(), It.IsAny<CancellationToken>()))
                .Returns((Route route, CancellationToken _) =>
                {
                    var existing = routes.FirstOrDefault(r => r.Id == route.Id);
                    if (existing != null)
                    {
                        routes.Remove(existing);
                        routes.Add(route);
                        return Task.FromResult(route);
                    }
                    // If not found, add the route and return it (never return null)
                    routes.Add(route);
                    return Task.FromResult(route);
                });

            mock.Setup(m => m.DeleteRouteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) =>
                {
                    var existing = routes.FirstOrDefault(r => r.Id == id);
                    if (existing != null)
                    {
                        routes.Remove(existing);
                    }
                    return Task.CompletedTask;
                });
            
            return mock;
        }
        
        /// <summary>
        /// Creates a mock DbContext for testing repositories and services
        /// </summary>
        public static Mock<AppDbContext> CreateMockDbContext()
        {
            var routes = CreateTestRoutes(3).AsQueryable();
            var drivers = CreateTestDrivers(2).AsQueryable();
            var vehicles = CreateTestVehicles(2).AsQueryable();
            
            var mockRouteSet = CreateMockDbSet(routes);
            var mockDriverSet = CreateMockDbSet(drivers);
            var mockVehicleSet = CreateMockDbSet(vehicles);
            
            var mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockContext.Setup(c => c.Routes).Returns(mockRouteSet.Object);
            mockContext.Setup(c => c.Drivers).Returns(mockDriverSet.Object);
            mockContext.Setup(c => c.Vehicles).Returns(mockVehicleSet.Object);
            
            return mockContext;
        }
        
        /// <summary>
        /// Creates a mock DbSet for the specified entity type
        /// </summary>
        private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            
            return mockSet;
        }
        
        /// <summary>
        /// Creates test routes using AutoFixture
        /// </summary>
        public static List<Route> CreateTestRoutes(int count = 3)
        {
            return _fixture.Build<Route>()
                .With(r => r.DriverId, Guid.NewGuid())
                .With(r => r.VehicleId, Guid.NewGuid())
                .With(r => r.Name, (string name) => $"Test Route {Guid.NewGuid().ToString().Substring(0, 8)}")
                .With(r => r.RouteDate, DateTime.Today)
                .With(r => r.ScheduledTime, DateTime.Today.AddHours(8))
                .With(r => r.StartLocation, "Start Test Location")
                .With(r => r.EndLocation, "End Test Location")
                .Without(r => r.Driver)
                .Without(r => r.Vehicle)
                .CreateMany(count)
                .ToList();
        }
        
        /// <summary>
        /// Creates test drivers using AutoFixture
        /// </summary>
        public static List<Driver> CreateTestDrivers(int count = 2)
        {
            return _fixture.Build<Driver>()
                .With(d => d.FirstName, (string name) => $"FirstName{Guid.NewGuid().ToString().Substring(0, 5)}")
                .With(d => d.LastName, (string name) => $"LastName{Guid.NewGuid().ToString().Substring(0, 5)}")
                .With(d => d.LicenseNumber, (string lic) => $"LIC-{Guid.NewGuid().ToString().Substring(0, 8)}")
                .CreateMany(count)
                .ToList();
        }
        
        /// <summary>
        /// Creates test vehicles using AutoFixture
        /// </summary>
        public static List<Vehicle> CreateTestVehicles(int count = 2)
        {
            return _fixture.Build<Vehicle>()
                .With(v => v.BusNumber, (string num) => $"BUS-{Guid.NewGuid().ToString().Substring(0, 5)}")
                .With(v => v.Name, (string name) => $"Test Bus {Guid.NewGuid().ToString().Substring(0, 5)}")
                .With(v => v.Number, (string num) => $"{new Random().Next(100, 999)}")
                .With(v => v.Capacity, 45)
                .With(v => v.IsActive, true)
                .CreateMany(count)
                .ToList();
        }
        
        /// <summary>
        /// Creates a single entity with customized properties
        /// </summary>
        public static T Create<T>() where T : class
        {
            return _fixture.Create<T>();
        }
        
        /// <summary>
        /// Creates many entities with customized properties
        /// </summary>
        public static List<T> CreateMany<T>(int count) where T : class
        {
            return _fixture.CreateMany<T>(count).ToList();
        }
    }
}
