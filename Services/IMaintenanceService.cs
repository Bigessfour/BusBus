using BusBus.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusBus.Services
{
    /// <summary>
    /// Service interface for managing vehicle maintenance records
    /// </summary>
    public interface IMaintenanceService
    {
        Task<List<Maintenance>> GetMaintenanceHistoryAsync(Guid vehicleId, CancellationToken cancellationToken = default);
        Task<List<Maintenance>> GetUpcomingMaintenanceAsync(int days = 30, CancellationToken cancellationToken = default);
        Task<Maintenance> CompleteMaintenanceAsync(int maintenanceId, CancellationToken cancellationToken = default);
        Task<decimal> GetMaintenanceCostAsync(Guid vehicleId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}
