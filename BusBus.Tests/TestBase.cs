using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using BusBus.DataAccess;
using BusBus.Models;
using BusBus.Services;

namespace BusBus.Tests
{
    public class TestBase
    {
        protected IAppDbContext? _context;
        protected ServiceProvider? _serviceProvider;

        [SetUp]
        public virtual async Task SetUp()
        {
            var services = new ServiceCollection();
            ConfigureDbContext(services);

            services.AddTransient<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
            services.AddTransient<IRouteService, RouteService>();
            services.AddTransient<Func<IAppDbContext>>(provider => () => provider.GetRequiredService<IAppDbContext>());

            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<IAppDbContext>();

            await SeedSampleDataAsync();
        }

        /// <summary>
        /// Configures the DbContext for tests. Uses SQL Server database from appsettings.json.
        /// </summary>
        protected virtual void ConfigureDbContext(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            // Build configuration to read from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Remove any existing DbContext registrations
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Use SQL Server database from configuration
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
            );
        }

        [TearDown]
        public virtual void TearDown()
        {
            _context?.Dispose();
            _serviceProvider?.Dispose();
        }

        protected async Task SeedSampleDataAsync()
        {
            var driver1Id = Guid.NewGuid();
            var driver2Id = Guid.NewGuid();
            var vehicle1Id = Guid.NewGuid();
            var vehicle2Id = Guid.NewGuid();

            var driver1 = new Driver
            {
                Id = driver1Id,
                FirstName = "John",
                LastName = "Doe",
                Name = "John Doe",
                LicenseNumber = "LIC001"
            };

            var driver2 = new Driver
            {
                Id = driver2Id,
                FirstName = "Jane",
                LastName = "Smith",
                Name = "Jane Smith",
                LicenseNumber = "LIC002"
            };

            var vehicle1 = new Vehicle
            {
                Id = vehicle1Id,
                BusNumber = "BUS001",
                Name = "Bus 001",
                Number = "001",
                Capacity = 50,
                IsActive = true
            };

            var vehicle2 = new Vehicle
            {
                Id = vehicle2Id,
                BusNumber = "BUS002",
                Name = "Bus 002",
                Number = "002",
                Capacity = 45,
                IsActive = true
            };

            var route1 = new Route
            {
                Id = Guid.NewGuid(),
                DriverId = driver1Id,
                VehicleId = vehicle1Id,
                Name = "Route 1",
                RouteDate = DateTime.Today,
                ScheduledTime = DateTime.Today.AddHours(8),
                StartLocation = "Start Location",
                EndLocation = "End Location",
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                AMRiders = 25,
                PMRiders = 30
            };

            _context?.Drivers.Add(driver1);
            _context?.Drivers.Add(driver2);
            _context?.Vehicles.Add(vehicle1);
            _context?.Vehicles.Add(vehicle2);
            _context?.Routes.Add(route1);            if (_context != null)
                await _context.SaveChangesAsync();
        }
    }
}
