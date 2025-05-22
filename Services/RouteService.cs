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
            if (await _context.Routes.AnyAsync(cancellationToken) || 
                await _context.Drivers.AnyAsync(cancellationToken) || 
                await _context.Vehicles.AnyAsync(cancellationToken))
            {
                return; // Database already seeded
            }

            // Seed Drivers
            var driver1 = new Driver
            {
                Id = Guid.NewGuid(),
                Name = "John Doe"
            };
            var driver2 = new Driver
            {
                Id = Guid.NewGuid(),
                Name = "Jane Smith"
            };
            _context.Drivers.AddRange(driver1, driver2);

            // Seed Vehicles
            var vehicle1 = new Vehicle
            {
                Id = Guid.NewGuid(),
                Name = "Bus A"
            };
            var vehicle2 = new Vehicle
            {
                Id = Guid.NewGuid(),
                Name = "Bus B"
            };
            _context.Vehicles.AddRange(vehicle1, vehicle2);

            // Seed Routes
            var route1 = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Route 1",
                RouteDate = DateTime.Today,
                AMStartingMileage = 1000,
                AMEndingMileage = 1100,
                AMRiders = 20,
                PMStartMileage = 1100,
                PMEndingMileage = 1200,
                PMRiders = 15,
                Driver = driver1,
                DriverId = driver1.Id,
                Vehicle = vehicle1,
                VehicleId = vehicle1.Id
            };
            var route2 = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Route 2",
                RouteDate = DateTime.Today.AddDays(-1),
                AMStartingMileage = 2000,
                AMEndingMileage = 2100,
                AMRiders = 25,
                PMStartMileage = 2100,
                PMEndingMileage = 2200,
                PMRiders = 18,
                Driver = driver2,
                DriverId = driver2.Id,
                Vehicle = vehicle2,
                VehicleId = vehicle2.Id
            };
            _context.Routes.AddRange(route1, route2);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}