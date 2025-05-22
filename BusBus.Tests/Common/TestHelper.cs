using BusBus.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using BusBus.Models;
using System.Collections.Generic;

namespace BusBus.Tests.Common
{
    public static class TestHelper
    {
        public static AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"InMemoryTestDb_{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }
        public static List<Route> SeedRoutes(AppDbContext context)
        {
            var routes = new List<Route>
            {
                new Route { Id = Guid.NewGuid(), Name = "Downtown Express", RouteDate = DateTime.Today },
                new Route { Id = Guid.NewGuid(), Name = "Airport Shuttle", RouteDate = DateTime.Today }
            };

            context.Routes.AddRange(routes);
            context.SaveChanges();
            return routes;
        }
        public static List<Driver> SeedDrivers(AppDbContext context)
        {
            var drivers = new List<Driver>
            {
                new Driver { Id = Guid.NewGuid(), Name = "John Smith", LicenseNumber = "DL12345", EmployeeId = "EMP001" },
                new Driver { Id = Guid.NewGuid(), Name = "Jane Doe", LicenseNumber = "DL67890", EmployeeId = "EMP002" }
            };

            context.Drivers.AddRange(drivers);
            context.SaveChanges();
            return drivers;
        }
        public static List<Vehicle> SeedVehicles(AppDbContext context)
        {
            var vehicles = new List<Vehicle>
            {
                new Vehicle { Id = Guid.NewGuid(), Name = "Bus 101", RegistrationNumber = "BUS101", Capacity = 45 },
                new Vehicle { Id = Guid.NewGuid(), Name = "Bus 102", RegistrationNumber = "BUS102", Capacity = 50 }
            };

            context.Vehicles.AddRange(vehicles);
            context.SaveChanges();
            return vehicles;
        }

        public static IConfiguration MockConfiguration()
        {
            var configData = new Dictionary<string, string?>
            {
                {"ConnectionStrings:DefaultConnection", "TestConnectionString"},
                {"AppSettings:Theme", "Dark"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            return configuration;
        }
    }
}