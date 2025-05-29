using BusBus.Models;
using BusBus.UI.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusBus.Services
{
    /// <summary>
    /// Service interface for managing drivers in the BusBus system
    /// </summary>
    public interface IDriverService : ICrudService<Driver, Guid>
    {
        /// <summary>
        /// Gets all drivers
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A list of drivers</returns>
        Task<List<Driver>> GetAllDriversAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of drivers
        /// </summary>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A paginated list of drivers</returns>
        Task<List<Driver>> GetDriversAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total count of drivers
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The total number of drivers</returns>
        Task<int> GetDriversCountAsync(CancellationToken cancellationToken = default);
    }
}
