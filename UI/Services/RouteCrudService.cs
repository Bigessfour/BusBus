using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Common;

namespace BusBus.UI.Services
{
    /// <summary>
    /// Route-specific CRUD service implementation that adapts IRouteService to ICrudService
    /// </summary>
    public class RouteCrudService : ICrudService<Route, Guid>
    {
        private readonly IRouteService _routeService;

        public RouteCrudService(IRouteService routeService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
        }

        public async Task<Route> CreateAsync(Route entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            // Ensure new route has a valid ID
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            // Set a default name if not provided
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                entity.Name = $"Route {DateTime.Now:yyyyMMdd-HHmmss}";
            }

            return await _routeService.CreateRouteAsync(entity, cancellationToken);
        }

        public async Task<Route?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _routeService.GetRouteByIdAsync(id, cancellationToken);
        }

        public async Task<List<Route>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _routeService.GetRoutesAsync(page, pageSize, cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await _routeService.GetRoutesCountAsync(cancellationToken);
        }

        public async Task<Route> UpdateAsync(Route entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (entity.Id == Guid.Empty)
            {
                throw new ArgumentException("Cannot update a route with an empty ID", nameof(entity));
            }

            return await _routeService.UpdateRouteAsync(entity, cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _routeService.DeleteRouteAsync(id, cancellationToken);
        }

        public (bool IsValid, string ErrorMessage) ValidateEntity(Route entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            // Basic validation rules for routes
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                return (false, "Route name is required.");
            }

            if (entity.Name.Length > 100)
            {
                return (false, "Route name cannot exceed 100 characters.");
            }

            if (entity.RouteDate == default)
            {
                return (false, "Route date is required.");
            }

            if (entity.RouteDate > DateTime.Today.AddYears(1))
            {
                return (false, "Route date cannot be more than one year in the future.");
            }

            // Validate mileage values
            if (entity.AMStartingMileage < 0 || entity.AMEndingMileage < 0 ||
                entity.PMStartMileage < 0 || entity.PMEndingMileage < 0)
            {
                return (false, "Mileage values cannot be negative.");
            }

            if (entity.AMEndingMileage < entity.AMStartingMileage)
            {
                return (false, "AM ending mileage cannot be less than AM starting mileage.");
            }

            if (entity.PMEndingMileage < entity.PMStartMileage)
            {
                return (false, "PM ending mileage cannot be less than PM starting mileage.");
            }

            // Validate rider counts
            if (entity.AMRiders < 0 || entity.PMRiders < 0)
            {
                return (false, "Rider counts cannot be negative.");
            }

            if (entity.AMRiders > 100 || entity.PMRiders > 100)
            {
                return (false, "Rider counts seem unusually high (over 100). Please verify.");
            }

            return (true, string.Empty);
        }
    }
}
