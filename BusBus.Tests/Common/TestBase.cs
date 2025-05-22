using BusBus.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace BusBus.Tests.Common
{
    public abstract class TestBase : IDisposable
    {
        protected AppDbContext DbContext { get; private set; }
        protected IConfiguration Configuration { get; private set; }

        protected TestBase()
        {
            // Initialize the in-memory database
            DbContext = TestHelper.CreateInMemoryDbContext();

            // Initialize mock configuration
            Configuration = TestHelper.MockConfiguration();
        }

        public void Dispose()
        {
            DbContext?.Dispose();
        }
    }
}