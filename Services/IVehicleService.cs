using BusBus.Models;
using BusBus.UI.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusBus.Services
{
    /// <summary>
    /// Service interface for managing vehicles in the BusBus system
    /// </summary>
    public interface IVehicleService : ICrudService<Vehicle, Guid>
    {
        /// <summary>
        /// Gets all vehicles (alias for GetAllVehiclesAsync)
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A list of vehicles</returns>
        Task<List<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets all vehicles
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A list of vehicles</returns>
        Task<List<Vehicle>> GetAllVehiclesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of vehicles
        /// </summary>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A paginated list of vehicles</returns>
        Task<List<Vehicle>> GetVehiclesAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total count of vehicles
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The total number of vehicles</returns>
        Task<int> GetVehiclesCountAsync(CancellationToken cancellationToken = default);
    }
}
