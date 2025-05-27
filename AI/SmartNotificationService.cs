#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using BusBus.Models;

namespace BusBus.AI
{
    public class SmartNotificationService
    {
        private readonly GrokService grokService;

        public SmartNotificationService()
        {
            grokService = new GrokService();
        }

        public async Task<List<SmartAlert>> GenerateSmartAlertsAsync()
        {
            var alerts = new List<SmartAlert>();

            // Maintenance alerts
            var maintenanceAlerts = await GenerateMaintenanceAlertsAsync();
            alerts.AddRange(maintenanceAlerts);

            // Driver performance alerts
            var driverAlerts = await GenerateDriverAlertsAsync();
            alerts.AddRange(driverAlerts);

            // Route optimization alerts
            var routeAlerts = await GenerateRouteAlertsAsync();
            alerts.AddRange(routeAlerts);

            return alerts;
        }

        private async Task<List<SmartAlert>> GenerateMaintenanceAlertsAsync()
        {
            var vehicles = DatabaseManager.GetAllVehicles();
            var alerts = new List<SmartAlert>();

            foreach (var vehicle in vehicles)
            {
                if (vehicle.MaintenanceDue)
                {
                    var aiInsight = await grokService.AnalyzeMaintenancePatternAsync(
                        $"Vehicle {vehicle.Number} is due for maintenance. Age: {vehicle.VehicleAge} years, Mileage: {vehicle.Mileage}");

                    alerts.Add(new SmartAlert
                    {
                        Type = AlertType.Maintenance,
                        Priority = Priority.High,
                        Title = $"Smart Maintenance Alert: {vehicle.Number}",
                        Message = aiInsight,
                        VehicleId = vehicle.VehicleId
                    });
                }
            }

            return alerts;
        }

        private async Task<List<SmartAlert>> GenerateDriverAlertsAsync()
        {
            var drivers = DatabaseManager.GetAllDrivers();
            var alerts = new List<SmartAlert>();

            foreach (var driver in drivers)
            {
                if (driver.NeedsPerformanceReview)
                {
                    var aiInsight = await grokService.GenerateDriverInsightsAsync(
                        $"Driver {driver.DriverName} needs performance review. Years of service: {driver.YearsOfService}, Performance score: {driver.PerformanceScore}");

                    alerts.Add(new SmartAlert
                    {
                        Type = AlertType.Performance,
                        Priority = Priority.Medium,
                        Title = $"Performance Review Due: {driver.DriverName}",
                        Message = aiInsight,
                        DriverId = driver.DriverID
                    });
                }
            }

            return alerts;
        }

        private async Task<List<SmartAlert>> GenerateRouteAlertsAsync()
        {
            // Analyze route efficiency
            var routes = DatabaseManager.GetAllRoutes();
            var alerts = new List<SmartAlert>();

            // This would analyze ridership patterns, delays, etc.
            var routeData = "Sample route efficiency data";
            var aiInsight = await grokService.OptimizeRouteAsync(routeData, "ridership");

            alerts.Add(new SmartAlert
            {
                Type = AlertType.RouteOptimization,
                Priority = Priority.Low,
                Title = "Route Optimization Opportunity",
                Message = aiInsight
            });

            return alerts;
        }
    }

    public class SmartAlert
    {
        public AlertType Type { get; set; }
        public Priority Priority { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int? VehicleId { get; set; }
        public int? DriverId { get; set; }
        public int? RouteId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum AlertType
    {
        Maintenance,
        Performance,
        RouteOptimization,
        Safety,
        Cost
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
