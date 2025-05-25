using BusBus.Models;
using BusBus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusBus.Services
{
    /// <summary>
    /// Implementation of statistics service using route data
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly IRouteService _routeService;

        public StatisticsService(IRouteService routeService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
        }

        public async Task<SchoolYearStatistics> GetSchoolYearStatisticsAsync()
        {
            var schoolYearStart = GetSchoolYearStart();
            var schoolYearEnd = GetSchoolYearEnd();
            
            var routes = await _routeService.GetRoutesAsync();
            var schoolYearRoutes = routes.Where(r => r.RouteDate >= schoolYearStart && r.RouteDate <= schoolYearEnd).ToList();

            var totalMiles = schoolYearRoutes.Sum(r => CalculateRouteMiles(r));
            var totalStudents = schoolYearRoutes.Sum(r => CalculateRouteStudents(r));

            return new SchoolYearStatistics
            {
                TotalMilesDriven = totalMiles,
                TotalStudentsHauled = totalStudents,
                TotalRoutes = schoolYearRoutes.Count,
                ActiveDrivers = schoolYearRoutes.Where(r => r.DriverId.HasValue).Select(r => r.DriverId).Distinct().Count(),
                ActiveVehicles = schoolYearRoutes.Where(r => r.VehicleId.HasValue).Select(r => r.VehicleId).Distinct().Count(),
                SchoolYearStart = schoolYearStart,
                SchoolYearEnd = schoolYearEnd,
                AverageMilesPerRoute = schoolYearRoutes.Count > 0 ? (double)totalMiles / schoolYearRoutes.Count : 0,
                AverageStudentsPerRoute = schoolYearRoutes.Count > 0 ? (double)totalStudents / schoolYearRoutes.Count : 0
            };
        }

        public async Task<DateRangeStatistics> GetDateRangeStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var routes = await _routeService.GetRoutesAsync();
            var rangeRoutes = routes.Where(r => r.RouteDate >= startDate && r.RouteDate <= endDate).ToList();

            var totalMiles = rangeRoutes.Sum(r => CalculateRouteMiles(r));
            var totalStudents = rangeRoutes.Sum(r => CalculateRouteStudents(r));
            var dayCount = Math.Max(1, (endDate - startDate).Days + 1);

            return new DateRangeStatistics
            {
                TotalMilesDriven = totalMiles,
                TotalStudentsHauled = totalStudents,
                TotalRoutes = rangeRoutes.Count,
                StartDate = startDate,
                EndDate = endDate,
                AverageMilesPerDay = (double)totalMiles / dayCount,
                AverageStudentsPerDay = (double)totalStudents / dayCount
            };
        }

        public async Task<DashboardStatistics> GetDashboardStatisticsAsync()
        {
            var routes = await _routeService.GetRoutesAsync();
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var schoolYearStart = GetSchoolYearStart();

            return new DashboardStatistics
            {
                TotalMilesThisMonth = routes.Where(r => r.RouteDate >= monthStart).Sum(r => CalculateRouteMiles(r)),
                TotalStudentsThisMonth = routes.Where(r => r.RouteDate >= monthStart).Sum(r => CalculateRouteStudents(r)),
                TotalMilesThisWeek = routes.Where(r => r.RouteDate >= weekStart).Sum(r => CalculateRouteMiles(r)),
                TotalStudentsThisWeek = routes.Where(r => r.RouteDate >= weekStart).Sum(r => CalculateRouteStudents(r)),
                TotalMilesYesterday = routes.Where(r => r.RouteDate.Date == yesterday).Sum(r => CalculateRouteMiles(r)),
                TotalStudentsYesterday = routes.Where(r => r.RouteDate.Date == yesterday).Sum(r => CalculateRouteStudents(r)),
                TotalMilesSchoolYear = routes.Where(r => r.RouteDate >= schoolYearStart).Sum(r => CalculateRouteMiles(r)),
                TotalStudentsSchoolYear = routes.Where(r => r.RouteDate >= schoolYearStart).Sum(r => CalculateRouteStudents(r)),
                LastUpdated = DateTime.Now
            };
        }

        private static int CalculateRouteMiles(Route route)
        {
            var amMiles = Math.Max(0, route.AMEndingMileage - route.AMStartingMileage);
            var pmMiles = Math.Max(0, route.PMEndingMileage - route.PMStartMileage);
            return amMiles + pmMiles;
        }

        private static int CalculateRouteStudents(Route route)
        {
            return route.AMRiders + route.PMRiders;
        }

        private static DateTime GetSchoolYearStart()
        {
            var today = DateTime.Today;
            // School year typically starts in August/September
            // If current month is August or later, school year started this year
            // Otherwise, school year started last year
            var schoolYearYear = today.Month >= 8 ? today.Year : today.Year - 1;
            return new DateTime(schoolYearYear, 8, 1);
        }

        private static DateTime GetSchoolYearEnd()
        {
            var schoolYearStart = GetSchoolYearStart();
            // School year ends in June of the following year
            return new DateTime(schoolYearStart.Year + 1, 6, 30);
        }
    }
}
