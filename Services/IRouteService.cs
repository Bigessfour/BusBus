#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusBus.Models;
using BusBus.Common;

namespace BusBus.Services
{
    /// <summary>
    /// Service interface for managing routes in the BusBus system
    /// </summary>
    public interface IRouteService
    {
        /// <summary>
        /// Seeds the database with sample data if needed
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SeedSampleDataAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Creates a new route
        /// </summary>
        /// <param name="route">The route to create</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task<Route> CreateRouteAsync(Route route, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets all routes
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A list of routes</returns>
        Task<List<Route>> GetRoutesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of routes
        /// </summary>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A paginated list of routes</returns>
        Task<List<Route>> GetRoutesAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total count of routes
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The total number of routes</returns>
        Task<int> GetRoutesCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a route by its ID
        /// </summary>
        /// <param name="id">The ID of the route to get</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The route if found, or null</returns>
        Task<Route?> GetRouteByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing route
        /// </summary>
        /// <param name="route">The route to update</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task<Route> UpdateRouteAsync(Route route, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a route by its ID
        /// </summary>
        /// <param name="id">The ID of the route to delete</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task DeleteRouteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all drivers
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A list of drivers</returns>
        Task<List<Driver>> GetDriversAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all vehicles
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A list of vehicles</returns>
        Task<List<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets routes for a specific date
        /// </summary>
        /// <param name="date">The date to filter routes by</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A list of routes on the specified date</returns>
        Task<List<Route>> GetRoutesByDateAsync(DateTime routeDate, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets a paginated list of routes (v2)
        /// </summary>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A paginated result of routes</returns>
        Task<PagedResult<Route>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all routes (v2)
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A list of all routes</returns>
        Task<List<Route>> GetAllAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a route (v2)
        /// </summary>
        /// <param name="id">The ID of the route to delete</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new route (v2)
        /// </summary>
        /// <param name="route">The route to create</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task<Route> CreateAsync(Route route, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing route (v2)
        /// </summary>
        /// <param name="route">The route to update</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task<Route> UpdateAsync(Route route, CancellationToken cancellationToken);
    }
}
