// <auto-added>
#nullable enable
using BusBus.DataAccess;
using BusBus.Models;
using BusBus.UI.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BusBus.Services
{
    public class DriverService : IDriverService
    {
        public async Task<List<Driver>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await GetAllDriversAsync(cancellationToken);
        }
        private readonly IServiceProvider _serviceProvider;
        public DriverService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<Driver> CreateAsync(Driver entity, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Drivers.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var driver = await dbContext.Drivers.FindAsync(new object[] { id }, cancellationToken);
            if (driver != null)
            {
                dbContext.Drivers.Remove(driver);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Drivers.CountAsync(cancellationToken);
        }

        public async Task<List<Driver>> GetAllDriversAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Drivers
                .AsNoTracking()
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Driver>> GetDriversAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Drivers
                .AsNoTracking()
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetDriversCountAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Drivers.CountAsync(cancellationToken);
        }

        public async Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Drivers.FindAsync(new object[] { id }, cancellationToken);
        }
        public async Task<List<Driver>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Drivers
                .AsNoTracking()  // Use AsNoTracking to avoid entity tracking issues
                .OrderBy(d => d.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<Driver> UpdateAsync(Driver entity, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Drivers.Update(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public (bool IsValid, string ErrorMessage) ValidateEntity(Driver entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (string.IsNullOrWhiteSpace(entity.FirstName) || string.IsNullOrWhiteSpace(entity.LastName))
                return (false, "First and last name are required.");
            if (string.IsNullOrWhiteSpace(entity.LicenseNumber))
                return (false, "License number is required.");
            return (true, string.Empty);
        }
    }
}
