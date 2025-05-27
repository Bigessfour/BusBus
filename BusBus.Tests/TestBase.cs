#pragma warning disable CA1848 // Use the LoggerMessage delegates
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using BusBus.DataAccess;
using BusBus.Services;
using BusBus.Models;
using System.Collections.Generic;

namespace BusBus.Tests
{
    /// <summary>
    /// Base class for all tests providing common setup, dependency injection, and cleanup
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected ServiceProvider ServiceProvider { get; private set; }
        protected AppDbContext DbContext { get; private set; }
        protected IConfiguration Configuration { get; private set; }
        protected ILogger Logger { get; private set; }


        private bool _disposed;

        /// <summary>
        /// Sets up the test environment with dependency injection, database, and logging
        /// </summary>
        [SetUp]
        public virtual async Task SetUp()
        {
            // Configure test services
            var services = new ServiceCollection();
            ConfigureTestServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // Get required services
            DbContext = ServiceProvider.GetRequiredService<AppDbContext>();
            Logger = ServiceProvider.GetRequiredService<ILogger<TestBase>>();

            // Ensure test database is created and clean
            await EnsureTestDatabaseAsync();

            // Seed test data if needed
            await SeedTestDataAsync();

            Logger.LogInformation("Test setup completed for {TestClass}", GetType().Name);
        }

        /// <summary>
        /// Cleans up resources after each test
        /// </summary>
        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                Logger?.LogInformation("Test teardown starting for {TestClass}", GetType().Name);

                // Clean up test data
                CleanupTestData();

                Logger?.LogInformation("Test teardown completed for {TestClass}", GetType().Name);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error during test teardown for {TestClass}", GetType().Name);
            }
            finally
            {
                // Dispose resources
                Dispose();
            }
        }

        /// <summary>
        /// Configures services for testing with in-memory database and test-specific settings
        /// </summary>
        protected virtual void ConfigureTestServices(IServiceCollection services)
        {
            // Build configuration from test settings
            Configuration = new ConfigurationBuilder()
                .SetBasePath(GetTestConfigurationPath())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddInMemoryCollection(GetTestConfigurationOverrides())
                .Build();

            services.AddSingleton(Configuration);

            // Add logging with test-friendly configuration
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole(options =>
                {
                    options.FormatterName = "simple";
                });
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "HH:mm:ss.fff ";
                    options.UseUtcTimestamp = false;
                });
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add Entity Framework with test database
            ConfigureDatabaseServices(services);

            // Register business services
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IDriverService, DriverService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
        }

        /// <summary>
        /// Configures database services for testing
        /// </summary>
        protected virtual void ConfigureDatabaseServices(IServiceCollection services)
        {
            // Use in-memory database for fast, isolated tests
            var databaseName = $"TestDb_{Guid.NewGuid()}";

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors()
                    .LogTo(message => Logger?.LogDebug("EF Core: {Message}", message))
            );
        }        /// <summary>
                 /// Gets the path to test configuration files
                 /// </summary>
        protected virtual string GetTestConfigurationPath()
        {
            // Look for config files in the main project directory
            var currentDir = Directory.GetCurrentDirectory();
            var projectDir = Path.GetDirectoryName(currentDir);
            while (projectDir != null && !File.Exists(Path.Combine(projectDir, "BusBus.csproj")))
            {
                projectDir = Path.GetDirectoryName(projectDir);
            }

            if (projectDir != null)
            {
                // Check if config files are in the config subdirectory
                var configDir = Path.Combine(projectDir, "config");
                if (Directory.Exists(configDir) && File.Exists(Path.Combine(configDir, "appsettings.json")))
                {
                    return configDir;
                }
            }

            return projectDir ?? currentDir;
        }

        /// <summary>
        /// Gets test-specific configuration overrides
        /// </summary>
        protected virtual Dictionary<string, string> GetTestConfigurationOverrides()
        {
            return new Dictionary<string, string>
            {
                ["Logging:LogLevel:Default"] = "Debug",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Information"
            };
        }

        /// <summary>
        /// Ensures the test database is created and clean
        /// </summary>
        protected virtual async Task EnsureTestDatabaseAsync()
        {
            try
            {
                // Ensure database is created (for in-memory this is always successful)
                await DbContext.Database.EnsureCreatedAsync();

                Logger.LogInformation("Test database ensured for {TestClass}", GetType().Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to ensure test database for {TestClass}", GetType().Name);
                throw;
            }
        }

        /// <summary>
        /// Seeds basic test data. Override in derived classes for specific test data needs
        /// </summary>
        protected virtual async Task SeedTestDataAsync()
        {
            // Default implementation - override in derived classes for specific test data
            await Task.CompletedTask;
        }

        /// <summary>
        /// Cleans up test data after test execution
        /// </summary>
        protected virtual void CleanupTestData()
        {
            try
            {
                if (DbContext != null)
                {
                    // For in-memory database, disposal automatically cleans up
                    // For real database, you might want to delete test records
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error cleaning up test data for {TestClass}", GetType().Name);
            }
        }        /// <summary>
                 /// Creates a test route with default values for testing
                 /// </summary>
        protected static Route CreateTestRoute(string nameSuffix = null)
        {
            var suffix = nameSuffix ?? Guid.NewGuid().ToString("N")[..8];
            return new Route
            {
                Id = Guid.NewGuid(),
                Name = $"Test Route {suffix}",
                RouteDate = DateTime.Today.AddDays(1),
                StartLocation = $"Test Start Location {suffix}",
                EndLocation = $"Test End Location {suffix}",
                ScheduledTime = DateTime.Today.AddDays(1).AddHours(8),
                AMStartingMileage = 1000,
                AMEndingMileage = 1025,
                AMRiders = 25,
                PMStartMileage = 1025,
                PMEndingMileage = 1050,
                PMRiders = 30,
                // Required for EF Core
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RouteID = 1000,
                RouteName = $"TestRouteName{suffix}",
                RouteCode = $"RT{suffix.Substring(0, 4)}",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };
        }        /// <summary>
                 /// Creates a test driver with default values for testing
                 /// </summary>
        protected static Driver CreateTestDriver(string nameSuffix = null)
        {
            var suffix = nameSuffix ?? Guid.NewGuid().ToString("N")[..8];
            return new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = $"Test",
                LastName = $"Driver{suffix}",
                PhoneNumber = $"555-{suffix[..4]}",
                LicenseNumber = $"LIC{suffix[..6]}"
            };
        }        /// <summary>
                 /// Creates a test vehicle with default values for testing
                 /// </summary>
        protected static Vehicle CreateTestVehicle(string nameSuffix = null)
        {
            var suffix = nameSuffix ?? Guid.NewGuid().ToString("N")[..8];
            return new Vehicle
            {
                Id = Guid.NewGuid(),
                Number = $"BUS{suffix[..4]}",
                MakeModel = "Test Make Model",
                Model = "Test Model",
                Year = DateTime.Now.Year - 1,
                IsActive = true,
                Mileage = 50000
            };
        }

        /// <summary>
        /// Disposes of resources used by the test
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method for proper cleanup
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    DbContext?.Dispose();
                    ServiceProvider?.Dispose();
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "Error disposing test resources for {TestClass}", GetType().Name);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Destructor to ensure cleanup if Dispose is not called
        /// </summary>
        ~TestBase()
        {
            Dispose(false);
        }
    }
}
