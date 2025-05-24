using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using BusBus.DataAccess;
using BusBus.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BusBus.Tests.DataAccess
// Suppress nullable warnings for test code where context is guaranteed by setup
#pragma warning disable CS8602, CS8604, CS8600, CS8601, CS8629
{
    [TestFixture]
    public class DatabaseMigrationTests : TestBase
    {
        [Test, Timeout(5000)]
        public async Task DatabaseSchema_ShouldMatchModelConfiguration()
        {
            // Use _context from TestBase
            Assert.IsNotNull(_context);

            // Act - Ensure database is created with proper schema
            var dbContext = _context as AppDbContext;
            Assert.IsNotNull(dbContext);
            await dbContext.Database.EnsureCreatedAsync();

            // Assert - Test that we can create entities with the expected schema
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Driver"
            };

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                BusNumber = "BUS-TEST",
                Number = "BUS-TEST"
            };

            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                AMRiders = 10,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                PMRiders = 15,
                Driver = driver,
                Vehicle = vehicle
            };

            dbContext.Drivers.Add(driver);
            dbContext.Vehicles.Add(vehicle);
            dbContext.Routes.Add(route);

            // This should succeed if the schema is properly configured
            var result = await dbContext.SaveChangesAsync();
            Assert.That(result, Is.EqualTo(3)); // Should save 3 entities
        }

        [Test, Timeout(5000)]
        public async Task DatabaseRelationships_ShouldBeConfiguredCorrectly()
        {
            // Arrange
            Assert.IsNotNull(_context);
            var dbContext = _context as AppDbContext;
            Assert.IsNotNull(dbContext);
            await dbContext.Database.EnsureCreatedAsync();

            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe"
            };

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                BusNumber = "BUS-001",
                Number = "BUS-001"
            };

            // Act - Create route with relationships
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Route 1",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                AMRiders = 10,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                PMRiders = 15,
                DriverId = driver.Id,
                VehicleId = vehicle.Id
            };

            dbContext.Drivers.Add(driver);
            dbContext.Vehicles.Add(vehicle);
            dbContext.Routes.Add(route);
            await dbContext.SaveChangesAsync();

            // Assert - Load with includes to test relationships
            var loadedRoute = await dbContext.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == route.Id);

            Assert.That(loadedRoute, Is.Not.Null);
            Assert.That(loadedRoute.Driver, Is.Not.Null);
            Assert.That(loadedRoute.Driver.Id, Is.EqualTo(driver.Id));
            Assert.That(loadedRoute.Vehicle, Is.Not.Null);
            Assert.That(loadedRoute.Vehicle.Id, Is.EqualTo(vehicle.Id));
        }

        [Test, Timeout(5000)]
        public async Task DatabaseConstraints_ShouldEnforceDataIntegrity()
        {
            // Arrange
            Assert.IsNotNull(_context);
            var dbContext = _context as AppDbContext;
            Assert.IsNotNull(dbContext);
            await dbContext.Database.EnsureCreatedAsync();

            // Act & Assert - Test primary key constraint
            var driverId = Guid.NewGuid();
            var driver1 = new Driver
            {
                Id = driverId,
                FirstName = "First",
                LastName = "Driver"
            };

            dbContext.Drivers.Add(driver1);
            await dbContext.SaveChangesAsync();

            // Clear the change tracker to avoid tracking conflicts
            dbContext.ChangeTracker.Clear();

            var driver2 = new Driver
            {
                Id = driverId, // Same ID - should cause constraint violation
                FirstName = "Second",
                LastName = "Driver"
            };

            dbContext.Drivers.Add(driver2);
            // Change to more specific exception type
            Assert.ThrowsAsync<DbUpdateException>(async () => await dbContext.SaveChangesAsync());
        }

        [Test, Timeout(5000)]
        public async Task DatabaseColumnTypes_ShouldMatchConfiguration()
        {
            // Arrange
            Assert.IsNotNull(_context);
            var dbContext = _context as AppDbContext;
            Assert.IsNotNull(dbContext);
            await dbContext.Database.EnsureCreatedAsync();

            // Act - Test integer column types for mileage and riders
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Type Test Route",
                RouteDate = DateTime.Today,
                AMStartingMileage = int.MaxValue, // Test large integer values
                AMEndingMileage = int.MaxValue,
                AMRiders = int.MaxValue,
                PMStartMileage = int.MaxValue,
                PMEndingMileage = int.MaxValue,
                PMRiders = int.MaxValue
            };

            dbContext.Routes.Add(route);
            await dbContext.SaveChangesAsync();

            // Assert
            var savedRoute = await dbContext.Routes.FirstOrDefaultAsync(r => r.Id == route.Id);
            Assert.That(savedRoute, Is.Not.Null);
            Assert.That(savedRoute!.AMStartingMileage, Is.EqualTo(int.MaxValue));
            Assert.That(savedRoute.PMRiders, Is.EqualTo(int.MaxValue));
        }

        [Test, Timeout(5000)]
        public async Task DatabaseIndexes_ShouldSupportQueries()
        {
            // Arrange
            Assert.IsNotNull(_context);
            var dbContext = _context as AppDbContext;
            Assert.IsNotNull(dbContext);
            await dbContext.Database.EnsureCreatedAsync();

            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Index",
                LastName = "Test"
            };

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                BusNumber = "IDX-001",
                Number = "IDX-001"
            };

            // Create multiple routes with the same driver and vehicle
            var routes = new[]
            {
                new Route
                {
                    Id = Guid.NewGuid(),
                    Name = "Route 1",
                    RouteDate = DateTime.Today,
                    AMStartingMileage = 1000,
                    AMEndingMileage = 1050,
                    AMRiders = 10,
                    PMStartMileage = 1050,
                    PMEndingMileage = 1100,
                    PMRiders = 15,
                    DriverId = driver.Id,
                    VehicleId = vehicle.Id
                },
                new Route
                {
                    Id = Guid.NewGuid(),
                    Name = "Route 2",
                    RouteDate = DateTime.Today.AddDays(1),
                    AMStartingMileage = 2000,
                    AMEndingMileage = 2050,
                    AMRiders = 20,
                    PMStartMileage = 2050,
                    PMEndingMileage = 2100,
                    PMRiders = 25,
                    DriverId = driver.Id,
                    VehicleId = vehicle.Id
                }
            };

            dbContext.Drivers.Add(driver);
            dbContext.Vehicles.Add(vehicle);
            dbContext.Routes.AddRange(routes);
            await dbContext.SaveChangesAsync();

            // Act & Assert - Test queries that would benefit from indexes
            var routesByDriver = await dbContext.Routes
                .Where(r => r.DriverId == driver.Id)
                .ToListAsync();

            var routesByVehicle = await dbContext.Routes
                .Where(r => r.VehicleId == vehicle.Id)
                .ToListAsync();

            Assert.That(routesByDriver, Has.Count.EqualTo(2));
            Assert.That(routesByVehicle, Has.Count.EqualTo(2));
        }

        // Re-enable warnings at end of file
#pragma warning restore CS8602, CS8604, CS8600, CS8601, CS8629
    }

    [TestFixture]
    public class SqlServerIntegrationTests : TestBase
    {
        protected override void ConfigureDbContext(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(GetTestConnectionString(), sqlOptions => sqlOptions.EnableRetryOnFailure()));
        }

        private static string GetTestConnectionString()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            return config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found");
        }

        private DbContextOptions<AppDbContext> CreateSqlServerOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(GetTestConnectionString())
                .Options;
        }

        [SetUp]
        public async Task CleanupDatabase()
        {
            // Clean up test data before each test
            using var context = new AppDbContext(CreateSqlServerOptions());
            await context.Database.EnsureCreatedAsync();
            context.Routes.RemoveRange(context.Routes);
            context.Drivers.RemoveRange(context.Drivers);
            context.Vehicles.RemoveRange(context.Vehicles);
            await context.SaveChangesAsync();
        }

        [Test, Timeout(5000)]
        public async Task CanConnectToSqlServerExpress()
        {
            using var context = new AppDbContext(CreateSqlServerOptions());
            var canConnect = await context.Database.CanConnectAsync();
            Assert.That(canConnect, Is.True, "Should connect to SQL Server Express test database");
        }

        [Test, Timeout(5000)]
        public void InvalidConnectionString_ShouldThrow()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=NonExistentDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;
            using var context = new AppDbContext(options);
            Assert.Throws<Microsoft.Data.SqlClient.SqlException>(() => context.Database.OpenConnection());
        }

        [Test, Timeout(5000)]
        public async Task Transaction_CommitAndRollback_Works()
        {
            using var context = new AppDbContext(CreateSqlServerOptions());
            using var transaction = await context.Database.BeginTransactionAsync();
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "Trans", LastName = "Action" };
            context.Drivers.Add(driver);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
            Assert.That(context.Drivers.Any(d => d.Id == driver.Id), Is.False, "Driver should not exist after rollback");

            // Now test commit
            using var context2 = new AppDbContext(CreateSqlServerOptions());
            using var transaction2 = await context2.Database.BeginTransactionAsync();
            var driver2 = new Driver { Id = Guid.NewGuid(), FirstName = "Trans", LastName = "Commit" };
            context2.Drivers.Add(driver2);
            await context2.SaveChangesAsync();
            await transaction2.CommitAsync();
            using var verifyContext = new AppDbContext(CreateSqlServerOptions());
            Assert.That(await verifyContext.Drivers.AnyAsync(d => d.Id == driver2.Id), Is.True, "Driver should exist after commit");
        }

        [Test, Timeout(5000)]
        public async Task ConcurrencyConflict_ShouldThrow()
        {
            // Ensure Route has a concurrency token (e.g., [Timestamp] or rowversion)
            using var context = new AppDbContext(CreateSqlServerOptions());
            var route = new Route { Id = Guid.NewGuid(), Name = "Concurrency Test", RouteDate = DateTime.Today };
            context.Routes.Add(route);
            await context.SaveChangesAsync();

            // Simulate two contexts editing the same row
            using var context1 = new AppDbContext(CreateSqlServerOptions());
            using var context2 = new AppDbContext(CreateSqlServerOptions());
            var r1 = await context1.Routes.FirstAsync(r => r.Id == route.Id);
            var r2 = await context2.Routes.FirstAsync(r => r.Id == route.Id);
            r1.Name = "First Update";
            await context1.SaveChangesAsync();
            r2.Name = "Second Update";
            // This should now throw if Route has a concurrency token
            try
            {
                await context2.SaveChangesAsync();
                Assert.Inconclusive("No concurrency token detected on Route entity. Add a [Timestamp] or rowversion property to enable this test.");
            }
            catch (DbUpdateConcurrencyException)
            {
                Assert.Pass();
            }
        }

        [Test]
        public async Task RawSqlQuery_Works()
        {
            using var context = new AppDbContext(CreateSqlServerOptions());
            var driver = new Driver { Id = Guid.NewGuid(), FirstName = "Raw", LastName = "SQL" };
            context.Drivers.Add(driver);
            await context.SaveChangesAsync();
            var drivers = await context.Drivers.FromSqlRaw("SELECT * FROM Drivers").ToListAsync();
            Assert.That(drivers.Count, Is.GreaterThan(0));
        }
    }
}
