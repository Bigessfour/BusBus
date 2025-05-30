#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable

using System;

namespace BusBus.Models
{
    /// <summary>
    /// Data transfer object for displaying route information in UI grids
    /// </summary>
    public class RouteDisplayDTO
    {
        // Core route properties
        public Guid Id { get; set; }
        public string RouteNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string StartLocation { get; set; } = string.Empty;
        public string EndLocation { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime RouteDate { get; set; }

        // Vehicle information
        public Guid? VehicleId { get; set; }
        public string VehicleAssignment { get; set; } = string.Empty;

        // AM shift data
        public int AMStartingMileage { get; set; }
        public int AMEndingMileage { get; set; }
        public int AMRiders { get; set; }
        public Guid? AMDriverId { get; set; }

        // PM shift data
        public int PMStartMileage { get; set; }
        public int PMEndingMileage { get; set; }
        public int PMRiders { get; set; }
        public Guid? PMDriverId { get; set; }

        // Additional properties required by UI
        public DateTime ScheduledTime { get; set; }
        public DateTime TripDate { get; set; }
        public Guid? DriverId { get; set; }

        /// <summary>
        /// Creates a RouteDisplayDTO from a Route entity
        /// </summary>
        public static RouteDisplayDTO FromRoute(Route route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));

            return new RouteDisplayDTO
            {
                Id = route.Id,
                RouteNumber = route.RouteCode ?? string.Empty,
                RouteName = route.Name ?? string.Empty, // Use Name from Route for RouteName DTO property
                Name = route.Name ?? string.Empty,
                StartLocation = route.StartLocation ?? string.Empty, // Use StartLocation from Route
                EndLocation = route.EndLocation ?? string.Empty, // Use EndLocation from Route
                Distance = (decimal)route.TotalMiles, // Cast double to decimal
                Duration = route.EstimatedDuration, // Directly use TimeSpan
                RouteDate = route.RouteDate, // Use RouteDate from Route
                VehicleId = route.VehicleId,
                VehicleAssignment = route.Vehicle?.Name ?? string.Empty, // Get Vehicle name if available
                AMStartingMileage = route.AMStartingMileage,
                AMEndingMileage = route.AMEndingMileage,
                AMRiders = route.AMRiders,
                AMDriverId = route.DriverId, // Assuming AMDriverId maps to the main DriverId
                PMStartMileage = route.PMStartMileage,
                PMEndingMileage = route.PMEndingMileage,
                PMRiders = route.PMRiders,
                PMDriverId = route.PMDriverId,
                ScheduledTime = route.ScheduledTime,
                TripDate = route.RouteDate,
                DriverId = route.DriverId
            };
        }

        /// <summary>
        /// Converts the DTO back to a Route entity
        /// </summary>
        public Route ToRoute()
        {
            return new Route
            {
                Id = Id,
                RouteCode = RouteNumber,
                Name = Name,
                StartLocation = StartLocation, // Map back to StartLocation
                EndLocation = EndLocation, // Map back to EndLocation
                // TotalMiles is a calculated property in Route, so no direct assignment
                // EstimatedDuration is a calculated property in Route, so no direct assignment
                RouteDate = RouteDate, // Map back to RouteDate
                VehicleId = VehicleId,
                // VehicleAssignment is not directly on Route, it's through Vehicle.Name
                AMStartingMileage = AMStartingMileage,
                AMEndingMileage = AMEndingMileage,
                AMRiders = AMRiders,
                DriverId = AMDriverId, // Assuming AMDriverId maps to the main DriverId
                PMStartMileage = PMStartMileage,
                PMEndingMileage = PMEndingMileage,
                PMRiders = PMRiders,
                PMDriverId = PMDriverId,
                ScheduledTime = ScheduledTime
                // RouteDate is already mapped
                // DriverId is already mapped
            };
        }
    }
}
