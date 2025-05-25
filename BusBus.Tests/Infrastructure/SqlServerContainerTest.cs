using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BusBus.DataAccess;
using NUnit.Framework;

namespace BusBus.Tests.Infrastructure
{
    /// <summary>
    /// Base class for integration tests that require a SQL Server database using Testcontainers
    /// </summary>
    [TestFixture]
    public abstract class SqlServerContainerTest : IAsyncDisposable
    {
        private readonly IContainer _sqlServerContainer;
        protected string ConnectionString { get; private set; } = null!;
        protected ServiceProvider ServiceProvider { get; private set; } = null!;

        protected SqlServerContainerTest()
        {
            // Create a new SQL Server container
            _sqlServerContainer = new ContainerBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("MSSQL_SA_PASSWORD", "StrongPassword!123")
                .WithPortBinding(1433, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
                .Build();
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Start the SQL Server container
            await _sqlServerContainer.StartAsync();

            // Build the connection string
            var port = _sqlServerContainer.GetMappedPublicPort(1433);
            ConnectionString = $"Server=localhost,{port};Database=BusBus_Test;User Id=sa;Password=StrongPassword!123;TrustServerCertificate=True;";

            // Setup services
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // Ensure database is created
            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_sqlServerContainer != null)
            {
                await _sqlServerContainer.StopAsync();
                await _sqlServerContainer.DisposeAsync();
            }
            
            if (ServiceProvider != null)
            {
                await ServiceProvider.DisposeAsync();
            }
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(ConnectionString));
        }
        
        protected AppDbContext GetDbContext()
        {
            var scope = ServiceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        public async ValueTask DisposeAsync()
        {
            if (_sqlServerContainer != null)
            {
                await _sqlServerContainer.DisposeAsync();
            }
            
            if (ServiceProvider != null)
            {
                await ServiceProvider.DisposeAsync();
            }
        }
    }
}
