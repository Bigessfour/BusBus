using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using BusBus.DataAccess;
using System;
using System.Threading.Tasks;

namespace BusBus.Tests
{
    public static class DatabaseSetupHelper
    {
        public static void EnsureDatabaseCreated(AppDbContext context)
        {
            if (context?.Database == null)
                throw new ArgumentNullException(nameof(context), "Database context cannot be null");
                
            try
            {
                context.Database.EnsureCreated();
            }
            catch (SqlException ex) when (ex.Number == 18456) // Login failed
            {
                throw new Exception($"Database login failed. Consider using Windows Authentication: {ex.Message}", ex);
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
                // Clear all tables in proper order to avoid FK constraints
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
            try
            {
                if (context == null) throw new ArgumentNullException(nameof(context));
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

        public static string GetTestConnectionString()
        {
            // Use SqlConnectionStringBuilder for proper formatting
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = "(localdb)\\MSSQLLocalDB",
                InitialCatalog = "BusBusTestDb",
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                ConnectTimeout = 30
            };
            
            return builder.ConnectionString;
        }
    }
}
    
