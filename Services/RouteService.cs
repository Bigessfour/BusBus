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
using Microsoft.Extensions.Logging;
using BusBus.Common;

namespace BusBus.Services
{
    public class RouteService : IRouteService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RouteService> _logger;

        public RouteService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = serviceProvider.GetRequiredService<ILogger<RouteService>>();
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

            // Seed realistic school bus drivers based on BusBus Info
            var driver1 = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Robert",
                LastName = "Johnson",
                Name = "Robert Johnson", // Required field for database
                PhoneNumber = "(555) 123-4567",
                Email = "robert.johnson@school.edu",
                LicenseType = "CDL", // From BusBus Info: CDL or Passenger License
                LicenseNumber = "CDL123456789",
                DriverName = "Robert Johnson",
                DriverID = 101,
                ContactInfo = "(555) 123-4567",
                Status = "Active",
                HireDate = DateTime.UtcNow.AddYears(-3),
                EmergencyContactJson = "{\"name\":\"Sarah Johnson\",\"phone\":\"(555) 987-6543\",\"relationship\":\"Spouse\"}",
                PersonalDetailsJson = "{\"name\":\"Robert Johnson\",\"address\":\"123 Main St, Springfield\",\"city\":\"Springfield\",\"state\":\"IL\",\"zipCode\":\"62701\"}",
                CreatedBy = "System",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RowVersion = new byte[8],
                SalaryGrade = 3,
                LastPerformanceReview = DateTime.UtcNow.AddMonths(-6),
                PerformanceMetricsJson = "{\"safety_score\":95,\"punctuality\":98}",
                PerformanceScore = 96.5m // Set explicit performance score
            };

            var driver2 = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Maria",
                LastName = "Rodriguez",
                Name = "Maria Rodriguez", // Required field for database
                PhoneNumber = "(555) 234-5678",
                Email = "maria.rodriguez@school.edu",
                LicenseType = "CDL", // From BusBus Info: CDL or Passenger License
                LicenseNumber = "CDL987654321",
                DriverName = "Maria Rodriguez",
                DriverID = 102,
                ContactInfo = "(555) 234-5678",
                Status = "Active",
                HireDate = DateTime.UtcNow.AddYears(-5),
                EmergencyContactJson = "{\"name\":\"Carlos Rodriguez\",\"phone\":\"(555) 876-5432\",\"relationship\":\"Spouse\"}",
                PersonalDetailsJson = "{\"name\":\"Maria Rodriguez\",\"address\":\"456 Oak Ave, Springfield\",\"city\":\"Springfield\",\"state\":\"IL\",\"zipCode\":\"62702\"}",
                CreatedBy = "System",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RowVersion = new byte[8],
                SalaryGrade = 4,
                LastPerformanceReview = DateTime.UtcNow.AddMonths(-3),
                PerformanceMetricsJson = "{\"safety_score\":100,\"punctuality\":97}",
                PerformanceScore = 98.5m // Set explicit performance score
            };

            // Seed realistic school buses based on BusBus Info
            var vehicle1 = new Vehicle
            {
                Id = Guid.NewGuid(),
                VehicleId = 17, // From BusBus Info: "Truck Plaza is assigned Bus 17"
                Number = "17",
                BusNumber = "17",
                Name = "Bus 17",
                Capacity = 72, // Typical school bus capacity
                Status = "Available",
                Model = "3800",
                MakeModel = "Blue Bird All American",
                Make = "Blue Bird",
                Year = 2020,
                FuelType = "Diesel",
                LicensePlate = "SCH-017",
                VehicleCode = "BB017",
                Mileage = 45000.0m,
                MaintenanceHistoryJson = "[{\"date\":\"2025-01-15\",\"type\":\"Oil Change\",\"mileage\":44500}]",
                SpecificationsJson = "{\"engine\":\"Cummins ISB 6.7L\",\"transmission\":\"Allison 2000\"}",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RowVersion = new byte[8],
                LastMaintenanceDate = DateTime.UtcNow.AddMonths(-2),
                NextMaintenanceDate = DateTime.UtcNow.AddMonths(1),
                MaintenanceDue = false,
                IsMaintenanceRequired = false,
                Latitude = 0.0,
                Longitude = 0.0,
                LastLocationUpdate = DateTime.UtcNow
            };

            var vehicle2 = new Vehicle
            {
                Id = Guid.NewGuid(),
                VehicleId = 23,
                Number = "23",
                BusNumber = "23",
                Name = "Bus 23",
                Capacity = 66,
                Status = "Available",
                Model = "C2",
                MakeModel = "Thomas C2 Jouley",
                Make = "Thomas",
                Year = 2022,
                FuelType = "Electric",
                LicensePlate = "SCH-023",
                VehicleCode = "BB023",
                Mileage = 22000.0m,
                MaintenanceHistoryJson = "[{\"date\":\"2025-03-10\",\"type\":\"Inspection\",\"mileage\":21800}]",
                SpecificationsJson = "{\"battery\":\"Lithium-ion 220kWh\",\"range\":\"140 miles\"}",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RowVersion = new byte[8],
                LastMaintenanceDate = DateTime.UtcNow.AddMonths(-1),
                NextMaintenanceDate = DateTime.UtcNow.AddMonths(2),
                MaintenanceDue = false,
                IsMaintenanceRequired = false,
                Latitude = 0.0,
                Longitude = 0.0,
                LastLocationUpdate = DateTime.UtcNow
            };

            // Seed realistic school routes based on BusBus Info
            // "There are 4 routes that operate on any given school day: Truck Plaza, East Route, West Route, SPED Route"
            var truckPlazaRoute = new Route
            {
                Id = Guid.NewGuid(),
                RouteID = 1,
                Name = "Truck Plaza Route",
                RouteName = "Truck Plaza Route", // From BusBus Info
                RouteCode = "TRUCK01",
                RouteDate = DateTime.Today,
                StartLocation = "Truck Plaza",
                EndLocation = "School",
                ScheduledTime = DateTime.Today.AddHours(7).AddMinutes(30), // 7:30 AM
                AMStartingMileage = 45000,
                AMEndingMileage = 45025,
                PMStartMileage = 45025,
                PMEndingMileage = 45050,
                AMRiders = 35,
                PMRiders = 38,
                DriverId = driver1.Id,
                VehicleId = vehicle1.Id, // Bus 17 assigned to Truck Plaza
                CreatedBy = "System",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RowVersion = new byte[8],
                Distance = 25,
                StopsJson = "[{\"name\":\"Truck Plaza\",\"time\":\"07:30\"},{\"name\":\"Main St\",\"time\":\"07:45\"},{\"name\":\"School\",\"time\":\"08:00\"}]",
                ScheduleJson = "{\"EstimatedTripTime\":\"00:30:00\",\"Type\":\"Regular\"}"
            };

            var eastRoute = new Route
            {
                Id = Guid.NewGuid(),
                RouteID = 2,
                Name = "East Route",
                RouteName = "East Route", // From BusBus Info
                RouteCode = "EAST01",
                RouteDate = DateTime.Today,
                StartLocation = "East Side",
                EndLocation = "School",
                ScheduledTime = DateTime.Today.AddHours(7).AddMinutes(45), // 7:45 AM
                AMStartingMileage = 22000,
                AMEndingMileage = 22020,
                PMStartMileage = 22020,
                PMEndingMileage = 22040,
                AMRiders = 28,
                PMRiders = 32,
                DriverId = driver2.Id,
                VehicleId = vehicle2.Id, // Bus 23
                CreatedBy = "System",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                RowVersion = new byte[8],
                Distance = 20,
                StopsJson = "[{\"name\":\"East Side Depot\",\"time\":\"07:45\"},{\"name\":\"Oak Street\",\"time\":\"07:55\"},{\"name\":\"School\",\"time\":\"08:10\"}]",
                ScheduleJson = "{\"EstimatedTripTime\":\"00:25:00\",\"Type\":\"Regular\"}"
            };

            dbContext.Drivers.AddRange(driver1, driver2);
            dbContext.Vehicles.AddRange(vehicle1, vehicle2);
            dbContext.Routes.AddRange(truckPlazaRoute, eastRoute);

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

            // if (string.IsNullOrEmpty(route.ScheduleJson))
            //     route.ScheduleJson = "{\"EstimatedTripTime\":\"00:30:00\"}"; // Removed for schedule scrub

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

            _logger.LogInformation("Executing optimized route query with related data");
            var routes = await dbContext.Routes
                .AsNoTracking()
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} routes from database", routes.Count);
            return routes;
        }
        public async Task<List<Route>> GetRoutesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            _logger.LogInformation("Executing paged route query - Page: {Page}, PageSize: {PageSize}", page, pageSize);
            var routes = await dbContext.Routes
                .AsNoTracking()
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .OrderByDescending(r => r.RouteDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} routes for page {Page}", routes.Count, page);
            return routes;
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
            return await GetRoutesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            await DeleteRouteAsync(id, cancellationToken);
        }

        public async Task<Route> CreateAsync(Route route, CancellationToken cancellationToken)
        {
            return await CreateRouteAsync(route, cancellationToken);
        }

        public async Task<Route> UpdateAsync(Route route, CancellationToken cancellationToken)
        {
            return await UpdateRouteAsync(route, cancellationToken);
        }
    }
}
