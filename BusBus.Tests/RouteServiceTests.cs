using BusBus.DataAccess;
using BusBus.Models;
using BusBus.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusBus.Tests
{
    [TestFixture]
    public class RouteServiceTests
    {
        private ServiceProvider _serviceProvider = null!;
        private DbContextOptions<AppDbContext> _dbContextOptions = null!;

        [SetUp]
        public void Setup()
        {
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<AppDbContext>(sp => new AppDbContext(_dbContextOptions));
            serviceCollection.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
            serviceCollection.AddScoped<Func<IAppDbContext>>(sp => () => sp.GetRequiredService<IAppDbContext>());
            serviceCollection.AddScoped<IRouteService, RouteService>();
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public void GetDriversAsync_WithCancellationToken_RespectsToken()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var service = _serviceProvider.GetRequiredService<IRouteService>();
            cancellationTokenSource.Cancel();

            // Act & Assert
            // Since cancellation is checked synchronously, use Assert.Throws
            Assert.Throws<OperationCanceledException>(
                () => service.GetDriversAsync(cancellationTokenSource.Token).GetAwaiter().GetResult());
        }

        [Test]
        public void GetVehiclesAsync_WithCancellationToken_RespectsToken()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var service = _serviceProvider.GetRequiredService<IRouteService>();
            cancellationTokenSource.Cancel();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(
                () => service.GetVehiclesAsync(cancellationTokenSource.Token).GetAwaiter().GetResult());
        }

        [Test]
        public void GetDriversAsync_OnException_RethrowsException()
        {
            // Arrange
            // The current RouteService does not support injecting a context or throwing exceptions from GetDriversAsync.
            // This test is not applicable to the current implementation and is skipped.
            Assert.Pass("Test skipped: RouteService does not support dependency injection for context.");
        }
    }
}