using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BusBus.DataAccess;

namespace BusBus.Tests
{
    /// <summary>
    /// Utility class for testing database connectivity and operations
    /// </summary>
    public static class DatabaseTestRunner
    {
        public static async Task RunDatabaseTests()
        {
            Console.WriteLine("====================================");
            Console.WriteLine("BusBus Database Connectivity Tests");
            Console.WriteLine("====================================");

            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                Console.WriteLine($"✓ Configuration loaded");
                Console.WriteLine($"Connection string: {connectionString}");

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(connectionString)
                    .EnableSensitiveDataLogging() // For debugging
                    .Options;

                using var context = new AppDbContext(options);

                Console.WriteLine("\n1. Testing database connectivity...");
                var canConnect = await context.Database.CanConnectAsync();
                if (canConnect)
                {
                    Console.WriteLine("✓ Database connection successful!");
                }
                else
                {
                    Console.WriteLine("✗ Database connection failed!");
                    return;
                }

                Console.WriteLine("\n2. Checking database existence...");
                var dbExists = await context.Database.EnsureCreatedAsync();
                if (dbExists)
                {
                    Console.WriteLine("✓ Database created!");
                }
                else
                {
                    Console.WriteLine("✓ Database already exists!");
                }

                Console.WriteLine("\n3. Checking pending migrations...");
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    Console.WriteLine($"Found {pendingMigrations.Count()} pending migrations:");
                    foreach (var migration in pendingMigrations)
                    {
                        Console.WriteLine($"  - {migration}");
                    }

                    Console.WriteLine("Applying migrations...");
                    await context.Database.MigrateAsync();
                    Console.WriteLine("✓ Migrations applied successfully!");
                }
                else
                {
                    Console.WriteLine("✓ No pending migrations");
                }

                Console.WriteLine("\n4. Testing basic entity operations...");

                // Test Routes table
                var routeCount = await context.Routes.CountAsync();
                Console.WriteLine($"✓ Routes table accessible - count: {routeCount}");

                // Test Vehicles table
                var vehicleCount = await context.Vehicles.CountAsync();
                Console.WriteLine($"✓ Vehicles table accessible - count: {vehicleCount}");
                // Test Drivers table
                var driverCount = await context.Drivers.CountAsync();
                Console.WriteLine($"✓ Drivers table accessible - count: {driverCount}");

                Console.WriteLine("\n5. Testing a simple insert and rollback...");
                using var transaction = await context.Database.BeginTransactionAsync();

                var testRoute = new Models.Route
                {
                    Name = "Test Route",
                    StartLocation = "Test Start",
                    EndLocation = "Test End",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                context.Routes.Add(testRoute);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ Test route inserted successfully");

                await transaction.RollbackAsync();
                Console.WriteLine("✓ Transaction rolled back successfully");

                Console.WriteLine("\n====================================");
                Console.WriteLine("✓ All database tests passed!");
                Console.WriteLine("Database setup is working correctly with LocalDB");
                Console.WriteLine("====================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Database test failed:");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}
