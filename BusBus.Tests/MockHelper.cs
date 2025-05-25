using System;
using System.Collections.Generic;
using Moq;
using BusBus.Services;
using BusBus.Models;

namespace BusBus.Tests
{
    /// <summary>
    /// Provides helper methods for creating mocks for testing
    /// </summary>
    public static class MockHelper
    {
        /// <summary>
        /// Creates a mock of IRouteService with basic setup
        /// </summary>
        public static Mock<IRouteService> CreateRouteServiceMock()
        {
            var mock = new Mock<IRouteService>();
            
            // Setup basic methods with empty results
            mock.Setup(m => m.GetRoutesAsync(It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new List<Route>());
                
            mock.Setup(m => m.GetDriversAsync(It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new List<Driver>());
                
            mock.Setup(m => m.GetVehiclesAsync(It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new List<Vehicle>());
            
            return mock;
        }
        
        /// <summary>
        /// Creates test routes for use in tests
        /// </summary>
        public static List<Route> CreateTestRoutes(int count = 3)
        {
            var routes = new List<Route>();
            
            for (int i = 1; i <= count; i++)
            {
                routes.Add(new Route
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Route {i}",
                    RouteDate = DateTime.Today,
                    ScheduledTime = DateTime.Today.AddHours(8 + i),
                    StartLocation = $"Start Location {i}",
                    EndLocation = $"End Location {i}",
                    AMStartingMileage = 1000 * i,
                    AMEndingMileage = 1050 * i,
                    PMStartMileage = 1050 * i,
                    PMEndingMileage = 1100 * i,
                    AMRiders = 20 + i,
                    PMRiders = 25 + i
                });
            }
            
            return routes;
        }
        
        /// <summary>
        /// Creates test drivers for use in tests
        /// </summary>
        public static List<Driver> CreateTestDrivers(int count = 2)
        {
            var drivers = new List<Driver>();
            
            for (int i = 1; i <= count; i++)
            {
                drivers.Add(new Driver
                {
                    Id = Guid.NewGuid(),
                    FirstName = $"FirstName{i}",
                    LastName = $"LastName{i}",
                    Name = $"FirstName{i} LastName{i}",
                    LicenseNumber = $"LIC00{i}"
                });
            }
            
            return drivers;
        }
        
        /// <summary>
        /// Creates test vehicles for use in tests
        /// </summary>
        public static List<Vehicle> CreateTestVehicles(int count = 2)
        {
            var vehicles = new List<Vehicle>();
            
            for (int i = 1; i <= count; i++)
            {
                vehicles.Add(new Vehicle
                {
                    Id = Guid.NewGuid(),
                    BusNumber = $"BUS00{i}",
                    Name = $"Bus 00{i}",
                    Number = $"00{i}",
                    Capacity = 40 + (i * 5),
                    IsActive = true
                });
            }
            
            return vehicles;
        }
    }
}
