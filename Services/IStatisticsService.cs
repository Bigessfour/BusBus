using BusBus.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusBus.Services
{
    /// <summary>
    /// Service for calculating various statistics from application data
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Get statistics for the current school year
        /// </summary>
        Task<SchoolYearStatistics> GetSchoolYearStatisticsAsync();
        
        /// <summary>
        /// Get statistics for a specific date range
        /// </summary>
        Task<DateRangeStatistics> GetDateRangeStatisticsAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Get quick dashboard statistics
        /// </summary>
        Task<DashboardStatistics> GetDashboardStatisticsAsync();
    }

    /// <summary>
    /// Statistics for a school year
    /// </summary>
    public class SchoolYearStatistics
    {
        public int TotalMilesDriven { get; set; }
        public int TotalStudentsHauled { get; set; }
        public int TotalRoutes { get; set; }
        public int ActiveDrivers { get; set; }
        public int ActiveVehicles { get; set; }
        public DateTime SchoolYearStart { get; set; }
        public DateTime SchoolYearEnd { get; set; }
        public double AverageMilesPerRoute { get; set; }
        public double AverageStudentsPerRoute { get; set; }
    }

    /// <summary>
    /// Statistics for a specific date range
    /// </summary>
    public class DateRangeStatistics
    {
        public int TotalMilesDriven { get; set; }
        public int TotalStudentsHauled { get; set; }
        public int TotalRoutes { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double AverageMilesPerDay { get; set; }
        public double AverageStudentsPerDay { get; set; }
    }

    /// <summary>
    /// Quick statistics for dashboard display
    /// </summary>
    public class DashboardStatistics
    {
        public int TotalMilesThisMonth { get; set; }
        public int TotalStudentsThisMonth { get; set; }
        public int TotalMilesThisWeek { get; set; }
        public int TotalStudentsThisWeek { get; set; }
        public int TotalMilesYesterday { get; set; }
        public int TotalStudentsYesterday { get; set; }
        public int TotalMilesSchoolYear { get; set; }
        public int TotalStudentsSchoolYear { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
