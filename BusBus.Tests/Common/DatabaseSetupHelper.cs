using Microsoft.EntityFrameworkCore;
using BusBus.DataAccess;
using System;
using System.Threading.Tasks;

namespace BusBus.Tests.Common
{
    public static class DatabaseSetupHelper
    {
        public static void EnsureDatabaseCreated(AppDbContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            try
            {
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create test database: {ex.Message}", ex);
            }
        }

        public static void CleanDatabase(AppDbContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            try
            {
                // Clear all tables
                context.Routes.RemoveRange(context.Routes);
                context.Drivers.RemoveRange(context.Drivers);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to clean test database: {ex.Message}", ex);
            }
        }
        
        public static async Task<bool> TestDatabaseConnection(AppDbContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            try
            {
                return await context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection test failed: {ex.Message}");
                return false;
            }
        }
        
        public static string GetDatabaseName(AppDbContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.Database.GetDbConnection().Database;
        }
    }
}
