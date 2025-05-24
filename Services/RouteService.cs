using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusBus.Services;
using BusBus.Models;

namespace BusBus
{
    public class RouteService : IRouteService
    {
        private readonly List<Route> _routes;
        private readonly List<Driver> _drivers;
        private readonly List<Vehicle> _vehicles;

        public RouteService()
        {
            _routes = new List<Route>();
            _drivers = new List<Driver>();
            _vehicles = new List<Vehicle>();
        }

        public async Task SeedSampleDataAsync(CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                if (_drivers.Count == 0)
                {
                    _drivers.AddRange(new[]
                    {
                        new Driver { Id = Guid.NewGuid(), Name = "John Doe", LicenseNumber = "DL001" },
                        new Driver { Id = Guid.NewGuid(), Name = "Jane Smith", LicenseNumber = "DL002" }
                    });
                }

                if (_vehicles.Count == 0)
                {
                    _vehicles.AddRange(new[]
                    {
                        new Vehicle { Id = Guid.NewGuid(), Number = "BUS001", Capacity = 50 },
                        new Vehicle { Id = Guid.NewGuid(), Number = "BUS002", Capacity = 40 }
                    });
                }

                if (_routes.Count == 0)
                {
                    _routes.AddRange(new[]
                    {
                        new Route
                        {
                            Id = Guid.NewGuid(),
                            Name = "Route 1",
                            StartLocation = "Downtown",
                            EndLocation = "Airport",
                            ScheduledTime = DateTime.Now.AddHours(1),
                            DriverId = _drivers.First().Id,
                            VehicleId = _vehicles.First().Id
                        },
                        new Route
                        {
                            Id = Guid.NewGuid(),
                            Name = "Route 2",
                            StartLocation = "Mall",
                            EndLocation = "University",
                            ScheduledTime = DateTime.Now.AddHours(2),
                            DriverId = _drivers.Last().Id,
                            VehicleId = _vehicles.Last().Id
                        }
                    });
                }
            }, cancellationToken);
        }

        public async Task<Route> CreateRouteAsync(Route route, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                if (route.Id == Guid.Empty)
                    route.Id = Guid.NewGuid();

                _routes.Add(route);
                return route;
            }, cancellationToken);
        }

        public async Task<List<Route>> GetRoutesAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => _routes.ToList(), cancellationToken);
        }

        public async Task<List<Route>> GetRoutesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => _routes.Skip((page - 1) * pageSize).Take(pageSize).ToList(), cancellationToken);
        }

        public async Task<int> GetRoutesCountAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => _routes.Count, cancellationToken);
        }

        public async Task<Route?> GetRouteByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => _routes.FirstOrDefault(r => r.Id == id), cancellationToken);
        }

        public async Task<Route> UpdateRouteAsync(Route route, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var existingRoute = _routes.FirstOrDefault(r => r.Id == route.Id);
                if (existingRoute != null)
                {
                    var index = _routes.IndexOf(existingRoute);
                    _routes[index] = route;
                }
                return route;
            }, cancellationToken);
        }

        public async Task DeleteRouteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                var route = _routes.FirstOrDefault(r => r.Id == id);
                if (route != null)
                {
                    _routes.Remove(route);
                }
            }, cancellationToken);
        }

        public async Task<List<Driver>> GetDriversAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() => _drivers.ToList(), cancellationToken);
        }

        public async Task<List<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() => _vehicles.ToList(), cancellationToken);
        }

        public async Task<List<Route>> GetRoutesByDateAsync(DateTime routeDate, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
                _routes.Where(r => r.ScheduledTime.Date == routeDate.Date).ToList(),
                cancellationToken);
        }
    }
}
