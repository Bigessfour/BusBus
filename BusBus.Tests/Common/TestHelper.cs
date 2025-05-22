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
                new Route { Id = Guid.NewGuid(), RouteDate = DateTime.Today },
                new Route { Id = Guid.NewGuid(), RouteDate = DateTime.Today }
            };

            context.Routes.AddRange(routes);
            context.SaveChanges();
            return routes;
        }
        public static List<Driver> SeedDrivers(AppDbContext context)
        {
            var drivers = new List<Driver>
            {
                new Driver { Id = Guid.NewGuid(), FirstName = "John", LastName = "Smith" },
                new Driver { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" }
            };

            context.Drivers.AddRange(drivers);
            context.SaveChanges();
            return drivers;
        }
        public static List<Vehicle> SeedVehicles(AppDbContext context)
        {
            var vehicles = new List<Vehicle>
            {
                new Vehicle { Id = Guid.NewGuid(), BusNumber = "Bus 101" },
                new Vehicle { Id = Guid.NewGuid(), BusNumber = "Bus 102" }
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