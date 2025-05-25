using System;
using System.Threading.Tasks;
using BusBus.DataAccess;
using BusBus.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BusBus.Tests.Infrastructure
{
    /// <summary>
    /// Base class for database integration tests using a real SQL Server instance in a container
    /// </summary>
    public abstract class DatabaseIntegrationTest : SqlServerContainerTest
    {
        protected AppDbContext DbContext { get; private set; } = null!;

        [SetUp]
        public virtual async Task SetUp()
        {
            // Get a fresh context for each test
            DbContext = GetDbContext();
            
            // Clear existing data
            await ClearDatabase();
            
            // Seed test data if needed
            await SeedTestDataAsync();
        }

        [TearDown]
        public virtual async Task TearDown()
        {
            // Clean up after test
            await ClearDatabase();
            
            // Dispose context if needed
            if (DbContext != null)
            {
                await DbContext.DisposeAsync();
            }
        }

        /// <summary>
        /// Seeds the database with test data for the current test
        /// </summary>
        protected virtual async Task SeedTestDataAsync()
        {
            // By default, no data is seeded
            // Override this in derived classes to seed specific test data
            await Task.CompletedTask;
        }

        /// <summary>
        /// Clears all data from the database
        /// </summary>
        private async Task ClearDatabase()
        {
            DbContext.ChangeTracker.Clear();
            
            if (DbContext.Routes != null)
            {
                DbContext.Routes.RemoveRange(await DbContext.Routes.ToListAsync());
            }
            if (DbContext.Drivers != null)
            {
                DbContext.Drivers.RemoveRange(await DbContext.Drivers.ToListAsync());
            }
            if (DbContext.Vehicles != null)
            {
                DbContext.Vehicles.RemoveRange(await DbContext.Vehicles.ToListAsync());
            }
            // Removed: Buses and Stops cleanup
            await DbContext.SaveChangesAsync();
        }
    }
}
