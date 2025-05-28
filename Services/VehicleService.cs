#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusBus.Models;
using BusBus.UI.Common;

namespace BusBus.Services
{
    /// <summary>
    /// Service for managing vehicles in the BusBus system
    /// </summary>
    public class VehicleService : IVehicleService
    {
        public Task<Vehicle> CreateAsync(Vehicle entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Vehicle> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Vehicle>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetCountAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Vehicle> UpdateAsync(Vehicle entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public (bool IsValid, string ErrorMessage) ValidateEntity(Vehicle entity) => throw new NotImplementedException();
        public void Dispose()
        {
            // No managed resources to dispose in this implementation
            GC.SuppressFinalize(this);
        }
    }
}
