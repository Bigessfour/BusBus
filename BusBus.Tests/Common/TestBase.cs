using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using BusBus.DataAccess;
using BusBus.Models;
using BusBus.Services;
using System.Transactions;

namespace BusBus.Tests.Common
{
    public abstract class TestBase : IDisposable
    {
        protected ServiceProvider ServiceProvider { get; private set; } = null!;
        protected IConfiguration Configuration { get; private set; } = null!;
        protected TransactionScope TransactionScope { get; private set; }
        
        [SetUp]
        public virtual async Task SetUp()
        {
            // Initialize services and DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            
            // Ensure database exists and is clean for each test
            var context = GetDbContext();
            DatabaseSetupHelper.EnsureDatabaseCreated(context);
            DatabaseSetupHelper.CleanDatabase(context);

            // Create transaction scope for test isolation
            TransactionScope = new TransactionScope(TransactionScopeOption.Required, 
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, 
                TransactionScopeAsyncFlowOption.Enabled);
                
            // Setup test data
            await SeedTestDataAsync(context);
        }

        [TearDown]
        public virtual void TearDown()
        {
            ServiceProvider?.Dispose();
            TransactionScope.Dispose();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            services.AddSingleton(Configuration);

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        protected AppDbContext GetDbContext()
        {
            return ServiceProvider.GetRequiredService<AppDbContext>();
        }

        protected virtual void SeedTestData()
        {
            // This method is deprecated - use SeedTestDataAsync instead
            throw new NotImplementedException("Use SeedTestDataAsync instead of SeedTestData");
        }        public void Dispose()
        {
            try
            {
                TransactionScope?.Dispose();
                ServiceProvider?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during dispose: {ex.Message}");
            }
        }

        protected virtual async Task SeedTestDataAsync(AppDbContext context)
        {
            // Add parameter validation
            if (context == null)
                throw new ArgumentNullException(nameof(context));
                
            // Seed test data if needed
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

            context.Drivers.Add(driver1);
            context.Drivers.Add(driver2);
            context.Vehicles.Add(vehicle1);
            context.Vehicles.Add(vehicle2);
            context.Routes.Add(route1);
            await context.SaveChangesAsync();
        }
    }
}
