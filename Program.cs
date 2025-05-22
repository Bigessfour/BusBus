using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BusBus.DataAccess;
using BusBus.Services;
using BusBus.UI;
using System;
using System.Windows.Forms;

namespace BusBus
{
    public static class Program
    {
        private static readonly int[] SqlRetryErrorNumbers = new[] { 1205, 10054 };
        [STAThread]
        public static async Task Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Build configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Check if configuration was loaded successfully
            if (string.IsNullOrEmpty(configuration.GetConnectionString("DefaultConnection")))
            {
                _ = MessageBox.Show("Warning: Could not find appsettings.json or the connection string is missing.\n" +
                                "Please ensure appsettings.json exists in the application directory.",
                                "Configuration Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
            }

            // Set up dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services, configuration);

            using (var serviceProvider = services.BuildServiceProvider())
            {
                // Seed the database with sample data if needed
                var routeService = serviceProvider.GetRequiredService<IRouteService>();
                try
                {
                    await routeService.SeedSampleDataAsync();
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Failed to seed database: {ex.Message}", "Seeding Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // Resolve and run the main form
                var dashboard = serviceProvider.GetRequiredService<Dashboard>();
                Application.Run(dashboard);
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {

            // Add DbContext with SQL Server and retry on transient failures
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(60),
                        errorNumbersToAdd: SqlRetryErrorNumbers
                    )
                )
            );

            // Register services
            services.AddScoped<IRouteService, RouteService>();

            // Register main form
            services.AddScoped<Dashboard>();
        }
    }
}