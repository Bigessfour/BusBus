// <auto-added>
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusBus.Services;
using BusBus.Models;
using BusBus.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BusBus.Common;

namespace BusBus.Services
{
    public class RouteService : IRouteService
    {
        private readonly IServiceProvider _serviceProvider;

        public RouteService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Helper method to handle ObjectDisposedException when service provider is disposed
        /// </summary>
        private bool IsServiceProviderDisposed()
        {
            try
            {
                // Try to create a scope to test if service provider is still available
                using var testScope = _serviceProvider.CreateScope();
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        public async Task SeedSampleDataAsync(CancellationToken cancellationToken = default)
        {
            // Check if data already exists
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (await dbContext.Routes.AnyAsync(cancellationToken))
                return;

            var driver1 = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Name = "John Doe",
                LicenseNumber = "DL001"
            };

            var driver2 = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                Name = "Jane Smith",
                LicenseNumber = "DL002"
            };

            var vehicle1 = new Vehicle
            {
                Id = Guid.NewGuid(),
                Number = "BUS001",
                Capacity = 50
            };

            var vehicle2 = new Vehicle
            {
                Id = Guid.NewGuid(),
                Number = "BUS002",
                Capacity = 40
            }; var route1 = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Route 1",
                RouteName = "Downtown to Airport",
                RouteCode = "RT0001",
                RouteDate = DateTime.Today,
                StartLocation = "Downtown",
                EndLocation = "Airport",
                ScheduledTime = DateTime.Today.AddHours(9),
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                AMRiders = 25,
                PMRiders = 30,
                DriverId = driver1.Id,
                VehicleId = vehicle1.Id,
                CreatedBy = "System",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RowVersion = new byte[8],
                StopsJson = "[]",
                ScheduleJson = "{\"EstimatedTripTime\":\"00:30:00\"}"
            };

            var route2 = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Route 2",
                RouteName = "Mall to University",
                RouteCode = "RT0002",
                RouteDate = DateTime.Today,
                StartLocation = "Mall",
                EndLocation = "University",
                ScheduledTime = DateTime.Today.AddHours(14),
                AMStartingMileage = 1100,
                AMEndingMileage = 1150,
                PMStartMileage = 1150,
                PMEndingMileage = 1200,
                AMRiders = 20,
                PMRiders = 25,
                DriverId = driver2.Id,
                VehicleId = vehicle2.Id,
                CreatedBy = "System",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RowVersion = new byte[8],
                StopsJson = "[]",
                ScheduleJson = "{\"EstimatedTripTime\":\"00:45:00\"}"
            };

            dbContext.Drivers.AddRange(driver1, driver2);
            dbContext.Vehicles.AddRange(vehicle1, vehicle2);
            dbContext.Routes.AddRange(route1, route2);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        public async Task<Route> CreateRouteAsync(Route route, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(route);

            if (route.Id == Guid.Empty)
                route.Id = Guid.NewGuid();

            // Ensure required fields are set
            if (string.IsNullOrEmpty(route.CreatedBy))
                route.CreatedBy = "System";

            if (string.IsNullOrEmpty(route.RouteName))
                route.RouteName = route.Name;

            if (string.IsNullOrEmpty(route.RouteCode))
                route.RouteCode = $"RT{route.RouteID:D4}";

            if (string.IsNullOrEmpty(route.StopsJson))
                route.StopsJson = "[]";

            if (string.IsNullOrEmpty(route.ScheduleJson))
                route.ScheduleJson = "{\"EstimatedTripTime\":\"00:30:00\"}";

            if (route.RowVersion == null || route.RowVersion.Length == 0)
                route.RowVersion = new byte[8];

            if (route.CreatedDate == default)
                route.CreatedDate = DateTime.UtcNow;

            if (route.ModifiedDate == default)
                route.ModifiedDate = DateTime.UtcNow;

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Routes.Add(route);
            await dbContext.SaveChangesAsync(cancellationToken);
            return route;
        }

        public async Task<List<Route>> GetRoutesAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<Route>> GetRoutesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .OrderByDescending(r => r.RouteDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetRoutesCountAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Routes.CountAsync(cancellationToken);
        }

        public async Task<Route?> GetRouteByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<Route> UpdateRouteAsync(Route route, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Routes.Update(route);
            await dbContext.SaveChangesAsync(cancellationToken);
            return route;
        }

        public async Task DeleteRouteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var route = await dbContext.Routes.FindAsync(new object[] { id }, cancellationToken);
            if (route != null)
            {
                dbContext.Routes.Remove(route);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        public async Task<List<Driver>> GetDriversAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (IsServiceProviderDisposed())
                    return new List<Driver>();

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await dbContext.Drivers.ToListAsync(cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                return new List<Driver>();
            }
        }

        public async Task<List<BusBus.Models.Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (IsServiceProviderDisposed())
                    return new List<BusBus.Models.Vehicle>();

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await dbContext.Vehicles.ToListAsync(cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                return new List<BusBus.Models.Vehicle>();
            }
        }

        public async Task<List<Route>> GetRoutesByDateAsync(DateTime routeDate, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .Where(r => r.RouteDate.Date == routeDate.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Route>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = dbContext.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(r => r.RouteDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Route>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<List<Route>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await GetRoutesAsync();
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            await DeleteRouteAsync(id);
        }

        public async Task<Route> CreateAsync(Route route, CancellationToken cancellationToken)
        {
            return await CreateRouteAsync(route);
        }

        public async Task<Route> UpdateAsync(Route route, CancellationToken cancellationToken)
        {
            return await UpdateRouteAsync(route);
        }
    }
}
