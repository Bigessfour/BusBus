using BusBus.Models;
using System;
using System.Collections.Generic;

namespace BusBus.Tests.Infrastructure
{
    /// <summary>
    /// Centralized factory for creating properly initialized test data
    /// </summary>
    public static class TestDataFactory
    {
        // Known test GUID patterns for consistent testing
        public static readonly Guid TestDriverId1 = new Guid("11111111-1111-1111-1111-111111111111");
        public static readonly Guid TestDriverId2 = new Guid("22222222-2222-2222-2222-222222222222");
        public static readonly Guid TestVehicleId1 = new Guid("33333333-3333-3333-3333-333333333333");
        public static readonly Guid TestVehicleId2 = new Guid("44444444-4444-4444-4444-444444444444");
        public static readonly Guid TestRouteId1 = new Guid("55555555-5555-5555-5555-555555555555");
        public static readonly Guid TestRouteId2 = new Guid("66666666-6666-6666-6666-666666666666");

        public static Route CreateValidRoute(string routeName = "Test Route", int routeId = 1)
        {
            var route = new Route
            {
                Id = TestRouteId1,
                RouteID = routeId,
                Name = routeName,
                RouteName = routeName,
                RouteCode = $"RT{routeId:D4}",
                CreatedBy = "TestUser",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RouteDate = DateTime.Today,
                DriverId = TestDriverId1,
                VehicleId = TestVehicleId1,
                StartLocation = "Start Location",
                EndLocation = "End Location",
                // ScheduledTime removed for schedule scrub
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                AMRiders = 25,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                PMRiders = 30,
                Distance = 50,
                RowVersion = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }
            };

            // Set JSON properties with valid data
            route.Stops = new List<BusStop>
            {
                new BusStop
                {
                    StopID = 1,
                    Name = "Main Street Stop",
                    Address = "123 Main St",
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    // ScheduledArrival = TimeSpan.FromHours(8),
                    // ScheduledDeparture = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(2)),
                    IsAccessible = true,
                    Amenities = new List<string> { "Bench", "Shelter" }
                }
            };

            // route.Schedule = new RouteSchedule { ... } // Schedule scrubbed

            return route;
        }
        public static Driver CreateValidDriver(string firstName = "John", string lastName = "Doe")
        {
            ArgumentNullException.ThrowIfNull(firstName);
            ArgumentNullException.ThrowIfNull(lastName);

            return new Driver
            {
                Id = TestDriverId1,
                FirstName = firstName,
                LastName = lastName,
                LicenseNumber = "DL123456789",
                HireDate = DateTime.Today.AddYears(-2),
                Status = "Active",
                PhoneNumber = "555-0123",
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@testdriver.com",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "TestUser",
                DriverID = 1,
                DriverName = $"{firstName} {lastName}",
                ContactInfo = "555-0123",
                RowVersion = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }
            };
        }
        public static Vehicle CreateValidVehicle(string vehicleNumber = "BUS001", int capacity = 65)
        {
            return new Vehicle
            {
                Id = TestVehicleId1,
                VehicleId = 1,
                Number = vehicleNumber,
                Name = $"Test Vehicle {vehicleNumber}",
                MakeModel = "Blue Bird Vision",
                Model = "Vision",
                Year = 2020,
                Capacity = capacity,
                LicensePlate = "ABC123",
                IsActive = true,
                Mileage = 50000,
                FuelType = "Diesel",
                CreatedDate = DateTime.UtcNow,
                Status = "Available",
                VehicleCode = $"VC{vehicleNumber}",
                RowVersion = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }
            };
        }

        public static Route CreateInvalidRoute()
        {
            return new Route
            {
                Id = Guid.NewGuid(),
                // Missing required fields intentionally for testing validation
                Name = "", // Invalid - too short
                RouteName = null, // Invalid - required
                CreatedBy = null, // Invalid - required
                RouteCode = null, // Invalid - required
                StopsJson = null // Invalid - required
                // ScheduleJson removed for schedule scrub
            };
        }

        public static List<Route> CreateMultipleRoutes(int count = 3)
        {
            var routes = new List<Route>();
            for (int i = 1; i <= count; i++)
            {
                var route = CreateValidRoute($"Test Route {i}", i);
                route.Id = new Guid($"{i:D8}-{i:D4}-{i:D4}-{i:D4}-{i:D12}");
                routes.Add(route);
            }
            return routes;
        }

        /// <summary>
        /// Creates a route with minimal valid data for basic tests
        /// </summary>
        public static Route CreateMinimalValidRoute()
        {
            var route = new Route
            {
                Id = TestRouteId2,
                RouteID = 999,
                Name = "Minimal Route",
                RouteName = "Minimal Route",
                RouteCode = "RT0999",
                CreatedBy = "TestUser",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RowVersion = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }
            };

            // Set minimal JSON data
            route.StopsJson = "[]";
            // route.ScheduleJson = "{}"; // Schedule scrubbed

            return route;
        }

        /// <summary>
        /// Checks if a GUID follows known test patterns
        /// </summary>
        public static bool IsKnownTestGuid(Guid guid)
        {
            var guidString = guid.ToString();
            return guidString.StartsWith("11111111") ||
                   guidString.StartsWith("22222222") ||
                   guidString.StartsWith("33333333") ||
                   guidString.StartsWith("44444444") ||
                   guidString.StartsWith("55555555") ||
                   guidString.StartsWith("66666666") ||
                   guidString.Contains("test", StringComparison.OrdinalIgnoreCase);
        }
    }
}
