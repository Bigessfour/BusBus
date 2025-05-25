using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using BusBus.DataAccess;
using BusBus.Models;

namespace BusBus.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected ServiceProvider ServiceProvider { get; private set; } = null!;
        protected IConfiguration Configuration { get; private set; } = null!;
        protected AppDbContext? DbContext { get; private set; }

        [SetUp]
        public virtual async Task SetUp()
        {
            try
            {
                // Initialize services and DI container
                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();
                
                // Use a new context for each test to avoid tracking issues
                DbContext = ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Ensure database exists but don't clean - let each test manage its own data
                await DbContext.Database.EnsureCreatedAsync();
                
                // Setup test data
                await SeedTestDataAsync(DbContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetUp failed: {ex.Message}");
                throw;
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                // Dispose context first
                DbContext?.Dispose();
                DbContext = null;
                
                // Dispose service provider
                ServiceProvider?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TearDown error: {ex.Message}");
            }
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.test.json", optional: true)
                .Build();

            services.AddSingleton(Configuration);

            // Use InMemory database for testing instead of SQL Server
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                       .EnableSensitiveDataLogging()
                       .EnableDetailedErrors());
            
            // Register the correct RouteService implementation
            services.AddScoped<BusBus.Services.IRouteService, BusBus.RouteService>();
        }

        protected AppDbContext GetDbContext()
        {
            return DbContext ?? throw new InvalidOperationException("DbContext not initialized");
        }

        public void Dispose()
        {
            try
            {
                DbContext?.Dispose();
                ServiceProvider?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during dispose: {ex.Message}");
            }
        }

        protected virtual async Task SeedTestDataAsync(AppDbContext context)
        {
            if (context == null) return;
            
            // Only seed if no data exists to avoid conflicts
            if (await context.Drivers.AnyAsync()) return;
                
            var driver1Id = Guid.NewGuid();
            var vehicle1Id = Guid.NewGuid();

            var driver1 = new Driver
            {
                Id = driver1Id,
                FirstName = "John",
                LastName = "Doe",
                Name = "John Doe",
                LicenseNumber = "LIC001"
            };

            var vehicle1 = new Vehicle
            {
                Id = vehicle1Id,
                Number = "001",
                Name = "Bus 001",
                Capacity = 50,
                IsActive = true
            };

            var route1 = new Route
            {
                Id = Guid.NewGuid(),
                DriverId = driver1Id,
                VehicleId = vehicle1Id,
                Name = "Test Route 1",
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

            context.Drivers.Add(driver1);
            context.Vehicles.Add(vehicle1);
            context.Routes.Add(route1);
            
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Seeding failed: {ex.Message}");
                // Don't rethrow - let tests continue
            }
        }
    }
}



