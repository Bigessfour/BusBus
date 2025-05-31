using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BusBus.DataAccess;

class DatabaseTestUtility
{
    public static async Task TestDatabaseConnection()
    {
        Console.WriteLine("Testing database connection...");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine($"Connection string: {connectionString}");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            using var context = new AppDbContext(options);

            Console.WriteLine("Testing database connectivity...");
            await context.Database.CanConnectAsync();
            Console.WriteLine("✓ Database connection successful!");

            Console.WriteLine("Checking if database exists...");
            var dbExists = await context.Database.EnsureCreatedAsync();
            if (dbExists)
            {
                Console.WriteLine("✓ Database created!");
            }
            else
            {
                Console.WriteLine("✓ Database already exists!");
            }

            Console.WriteLine("Applying pending migrations...");
            await context.Database.MigrateAsync();
            Console.WriteLine("✓ Migrations applied successfully!");

            Console.WriteLine("Testing basic query...");
            var routeCount = await context.Routes.CountAsync();
            Console.WriteLine($"✓ Route count: {routeCount}");

            Console.WriteLine("Database setup completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
