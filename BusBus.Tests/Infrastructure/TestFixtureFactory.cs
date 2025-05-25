using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using BusBus.DataAccess;
using BusBus.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusBus.Tests.Infrastructure
{
    /// <summary>
    /// Factory for creating test fixtures and test data
    /// </summary>
    public class TestFixtureFactory
    {
        private readonly Fixture _fixture;
        
        public TestFixtureFactory()
        {
            _fixture = new Fixture();
            ConfigureFixture();
        }        private void ConfigureFixture()
        {
            // Remove throwing recursion behavior and add omit on recursion behavior
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
              // Configure Route generation
            _fixture.Customize<Route>(composer => composer
                .With(r => r.Id, () => Guid.NewGuid())
                .With(r => r.Name, () => $"Test Route {Random.Shared.Next(1, 1000)}")
                .With(r => r.RouteDate, () => DateTime.Today)
                .With(r => r.ScheduledTime, () => DateTime.Now.AddHours(1))
                .With(r => r.StartLocation, () => "Test Start Location")
                .With(r => r.EndLocation, () => "Test End Location")
                .With(r => r.AMStartingMileage, () => Random.Shared.Next(100, 1000))
                .With(r => r.AMEndingMileage, () => Random.Shared.Next(1001, 2000))
                .With(r => r.PMStartMileage, () => Random.Shared.Next(2001, 3000))
                .With(r => r.PMEndingMileage, () => Random.Shared.Next(3001, 4000))
                .With(r => r.AMRiders, () => Random.Shared.Next(1, 50))
                .With(r => r.PMRiders, () => Random.Shared.Next(1, 50))
                .Without(r => r.Driver)
                .Without(r => r.Vehicle));            // Configure Driver generation
            _fixture.Customize<Driver>(composer => composer
                .With(d => d.Id, () => Guid.NewGuid())
                .With(d => d.FirstName, () => $"Driver{Random.Shared.Next(1, 1000)}")
                .With(d => d.LastName, () => $"LastName{Random.Shared.Next(1, 1000)}")
                .With(d => d.LicenseNumber, () => $"LIC{Random.Shared.Next(100, 999):000}")
                .With(d => d.PhoneNumber, () => $"555-{Random.Shared.Next(100, 999):000}-{Random.Shared.Next(1000, 9999):0000}")
                .With(d => d.Email, () => $"driver{Random.Shared.Next(1, 1000)}@test.com"));// Configure Vehicle generation
            _fixture.Customize<Vehicle>(composer => composer
                .With(v => v.Id, () => Guid.NewGuid())
                .With(v => v.Number, () => $"{Random.Shared.Next(100, 999):000}")
                .With(v => v.Name, () => $"Bus {Random.Shared.Next(100, 999):000}")
                .With(v => v.Capacity, () => Random.Shared.Next(20, 80))
                .With(v => v.IsActive, () => true));
        }
        
        /// <summary>
        /// Creates a new instance of type T with random test data
        /// </summary>
        public T Create<T>()
        {
            return _fixture.Create<T>();
        }
        
        /// <summary>
        /// Creates multiple instances of type T with random test data
        /// </summary>
        public List<T> CreateMany<T>(int count = 3)
        {
            return _fixture.CreateMany<T>(count).ToList();
        }
        
        /// <summary>
        /// Creates a test Route with the specified properties or random values for unspecified properties
        /// </summary>
        public Route CreateRoute(
            Guid? id = null,
            string? name = null,
            DateTime? routeDate = null,
            DateTime? scheduledTime = null,
            string? startLocation = null,
            string? endLocation = null,
            int? amStartingMileage = null,
            int? amEndingMileage = null,
            int? pmStartMileage = null,
            int? pmEndingMileage = null,
            int? amRiders = null,
            int? pmRiders = null,
            Guid? driverId = null,
            Guid? vehicleId = null)        {
            // Create Route manually to avoid circular reference issues
            var route = new Route
            {
                Id = id ?? Guid.NewGuid(),
                Name = name ?? $"Test Route {Random.Shared.Next(1, 1000)}",
                RouteDate = routeDate ?? DateTime.Today,
                ScheduledTime = scheduledTime ?? DateTime.Today.AddHours(8),
                StartLocation = startLocation ?? "Test Start Location",
                EndLocation = endLocation ?? "Test End Location",
                AMStartingMileage = amStartingMileage ?? Random.Shared.Next(10000, 50000),
                AMEndingMileage = amEndingMileage ?? Random.Shared.Next(50001, 60000),
                PMStartMileage = pmStartMileage ?? Random.Shared.Next(60001, 70000),
                PMEndingMileage = pmEndingMileage ?? Random.Shared.Next(70001, 80000),
                AMRiders = amRiders ?? Random.Shared.Next(15, 35),
                PMRiders = pmRiders ?? Random.Shared.Next(15, 35),
                DriverId = driverId,
                VehicleId = vehicleId
                // Note: Driver and Vehicle navigation properties are left null to avoid circular references
            };
            
            // Apply overrides
            if (id.HasValue) route.Id = id.Value;
            if (name != null) route.Name = name;
            if (routeDate.HasValue) route.RouteDate = routeDate.Value;
            if (scheduledTime.HasValue) route.ScheduledTime = scheduledTime.Value;
            if (startLocation != null) route.StartLocation = startLocation;
            if (endLocation != null) route.EndLocation = endLocation;
            if (amStartingMileage.HasValue) route.AMStartingMileage = amStartingMileage.Value;
            if (amEndingMileage.HasValue) route.AMEndingMileage = amEndingMileage.Value;
            if (pmStartMileage.HasValue) route.PMStartMileage = pmStartMileage.Value;
            if (pmEndingMileage.HasValue) route.PMEndingMileage = pmEndingMileage.Value;
            if (amRiders.HasValue) route.AMRiders = amRiders.Value;
            if (pmRiders.HasValue) route.PMRiders = pmRiders.Value;
            if (driverId.HasValue) route.DriverId = driverId.Value;
            if (vehicleId.HasValue) route.VehicleId = vehicleId.Value;
            
            return route;
        }
        
        /// <summary>
        /// Creates a test Driver with the specified properties or random values for unspecified properties
        /// </summary>
        public Driver CreateDriver(
            Guid? id = null,
            string? firstName = null,
            string? lastName = null,
            string? phoneNumber = null,
            string? email = null)
        {
            var driver = _fixture.Create<Driver>();
            
            if (id.HasValue) driver.Id = id.Value;
            if (firstName != null) driver.FirstName = firstName;
            if (lastName != null) driver.LastName = lastName;
            // Removed: if (employeeNumber != null) driver.EmployeeNumber = employeeNumber;
            if (phoneNumber != null) driver.PhoneNumber = phoneNumber;
            if (email != null) driver.Email = email;
            // Removed: if (hireDate.HasValue) driver.HireDate = hireDate.Value;
            
            return driver;
        }
        
        /// <summary>
        /// Creates a ServiceProvider with test services
        /// </summary>
        public ServiceProvider CreateServiceProvider(string connectionString)
        {
            var services = new ServiceCollection();
            
            // Add database context
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
            
            // Add other services
            // TODO: Add your services here
            
            return services.BuildServiceProvider();
        }
    }
}
