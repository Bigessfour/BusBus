using BusBus.DataAccess;
using BusBus.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusBus.Services
{
    public class RouteService : IRouteService
    {
        private readonly AppDbContext _context;

        public RouteService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context), "AppDbContext cannot be null.");
        }

        // Existing methods (unchanged)
        public async Task<Route> CreateRouteAsync(Route route, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(route);
            if (string.IsNullOrWhiteSpace(route.Name))
                throw new ArgumentException("Route name cannot be empty.", nameof(route));

            if (route.Driver != null)
            {
                var driverExists = await _context.Drivers.AnyAsync(d => d.Id == route.Driver.Id, cancellationToken);
                if (!driverExists)
                    throw new ArgumentException($"Driver with ID {route.Driver.Id} does not exist.", nameof(route));
            }

            if (route.Vehicle != null)
            {
                var vehicleExists = await _context.Vehicles.AnyAsync(v => v.Id == route.Vehicle.Id, cancellationToken);
                if (!vehicleExists)
                    throw new ArgumentException($"Vehicle with ID {route.Vehicle.Id} does not exist.", nameof(route));
            }

            _context.Routes.Add(route);
            await _context.SaveChangesAsync(cancellationToken);
            return route;
        }

        public async Task<List<Route>> GetRoutesAsync(CancellationToken cancellationToken = default)
        {
            return await GetRoutesAsync(1, int.MaxValue, cancellationToken);
        }

        public async Task<List<Route>> GetRoutesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than 0");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than 0");

            return await _context.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetRoutesCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Routes.CountAsync(cancellationToken);
        }

        public async Task<Route?> GetRouteByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<Route> UpdateRouteAsync(Route route, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(route);

            var existingRoute = await _context.Routes.FindAsync(new object[] { route.Id }, cancellationToken);
            if (existingRoute == null)
                throw new ArgumentException($"Route with ID {route.Id} does not exist.", nameof(route));

            if (route.Driver != null)
            {
                var driverExists = await _context.Drivers.AnyAsync(d => d.Id == route.Driver.Id, cancellationToken);
                if (!driverExists)
                    throw new ArgumentException($"Driver with ID {route.Driver.Id} does not exist.", nameof(route));
            }

            if (route.Vehicle != null)
            {
                var vehicleExists = await _context.Vehicles.AnyAsync(v => v.Id == route.Vehicle.Id, cancellationToken);
                if (!vehicleExists)
                    throw new ArgumentException($"Vehicle with ID {route.Vehicle.Id} does not exist.", nameof(route));
            }

            _context.Routes.Update(route);
            await _context.SaveChangesAsync(cancellationToken);
            return route;
        }

        public async Task DeleteRouteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var route = await _context.Routes.FindAsync(new object[] { id }, cancellationToken);
            if (route != null)
            {
                _context.Routes.Remove(route);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<List<Driver>> GetDriversAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Drivers.ToListAsync(cancellationToken);
        }

        public async Task<List<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Vehicles.ToListAsync(cancellationToken);
        }

        public async Task<List<Route>> GetRoutesByDateAsync(DateTime routeDate, CancellationToken cancellationToken = default)
        {
            return await _context.Routes
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .Where(r => r.RouteDate.Date == routeDate.Date)
                .ToListAsync(cancellationToken);
        }

        // New method to seed the database
        public async Task SeedSampleDataAsync(CancellationToken cancellationToken = default)
        {
            // Check if data already exists to avoid duplicates

            // Only seed if there are no drivers or vehicles
            if (await _context.Drivers.AnyAsync(cancellationToken) || await _context.Vehicles.AnyAsync(cancellationToken))
                return;

            // Seed Steve McKitrick and a vehicle with simplified fields
            var steve = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Steve",
                LastName = "McKitrick"
            };
            var bus = new Vehicle
            {
                Id = Guid.NewGuid(),
                BusNumber = "99"
            };
            _context.Drivers.Add(steve);
            _context.Vehicles.Add(bus);

            // Seed the provided route
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Sample Route 5/5/25",
                RouteDate = new DateTime(2025, 5, 5),
                AMStartingMileage = 55358,
                AMEndingMileage = 55374,
                AMRiders = 36,
                PMStartMileage = 55374,
                PMEndingMileage = 55391,
                PMRiders = 31,
                Driver = steve,
                DriverId = steve.Id,
                Vehicle = bus,
                VehicleId = bus.Id
            };
            _context.Routes.Add(route);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}