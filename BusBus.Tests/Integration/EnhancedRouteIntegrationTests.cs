using System;
using System.Linq;
using System.Threading.Tasks;
using BusBus.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using BusBus.Services;

namespace BusBus.Tests.Integration
{
    [TestFixture]
    [Category(TestCategories.Integration)]
    public class EnhancedRouteIntegrationTests : TestBase
    {
        private IRouteService _routeService = null!;

        [SetUp]
        public async Task SetUpAsync()
        {
            await base.SetUp();
            // Initialize route service
            _routeService = ServiceProvider.GetRequiredService<IRouteService>();
        }

        [Test]
        public async Task CanConnectToDatabase()
        {
            Assert.That(DbContext, Is.Not.Null);
            var canConnect = await DbContext.Database.CanConnectAsync();
            Assert.That(canConnect, Is.True);
        }

        [Test]
        public void DatabaseProviderIsInMemory()
        {
            Assert.That(DbContext, Is.Not.Null);
            var providerName = DbContext.Database.ProviderName;
            Assert.That(providerName, Is.EqualTo("Microsoft.EntityFrameworkCore.InMemory"));
        }
    }
}

