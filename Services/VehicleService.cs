#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusBus.DataAccess;
using BusBus.Models;
using BusBus.UI.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusBus.Services
{
    /// <summary>
    /// Service for managing vehicles in the BusBus system
    /// </summary>
    public class VehicleService : IVehicleService
    {
        public async Task<List<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await GetAllVehiclesAsync(cancellationToken);
        }
        private readonly IServiceProvider _serviceProvider;

        public VehicleService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<Vehicle> CreateAsync(Vehicle entity, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Vehicles.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Vehicles.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<List<Vehicle>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>(); return await dbContext.Vehicles
                .AsNoTracking()
                .OrderBy(v => v.Number)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Vehicles.CountAsync(cancellationToken);
        }

        public async Task<List<Vehicle>> GetAllVehiclesAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>(); return await dbContext.Vehicles
                .AsNoTracking()
                .OrderBy(v => v.Number)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Vehicle>> GetVehiclesAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>(); return await dbContext.Vehicles
                .AsNoTracking()
                .OrderBy(v => v.Number)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetVehiclesCountAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Vehicles.CountAsync(cancellationToken);
        }

        public async Task<Vehicle> UpdateAsync(Vehicle entity, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Vehicles.Update(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var vehicle = await dbContext.Vehicles.FindAsync(new object[] { id }, cancellationToken);
            if (vehicle != null)
            {
                dbContext.Vehicles.Remove(vehicle);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public (bool IsValid, string ErrorMessage) ValidateEntity(Vehicle entity)
        {
            ArgumentNullException.ThrowIfNull(entity); if (string.IsNullOrWhiteSpace(entity.Number))
                return (false, "Vehicle number is required.");
            if (entity.Capacity <= 0)
                return (false, "Capacity must be greater than 0.");
            return (true, string.Empty);
        }

        public void Dispose()
        {
            // No managed resources to dispose in this implementation
            GC.SuppressFinalize(this);
        }
    }
}
