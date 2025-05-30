#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable

using System;
using BusBus.Models;

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
        public string RouteName { get; set; } = string.Empty; // Maps to Route.Name
        public string Name { get; set; } = string.Empty; // Also maps to Route.Name, kept for compatibility if used elsewhere
        public string StartLocation { get; set; } = string.Empty; // Maps to Route.StartLocation
        public string EndLocation { get; set; } = string.Empty; // Maps to Route.EndLocation
        public decimal Distance { get; set; } // Maps to Route.Distance (int), implicit conversion
        public TimeSpan? Duration { get; set; } // Was TimeSpan, now nullable. No direct field in Route.cs for EstimatedDuration
        public DateTime RouteDate { get; set; } // Maps to Route.RouteDate

        // Vehicle information
        public Guid? VehicleId { get; set; }
        public string VehicleAssignment { get; set; } = string.Empty; // Maps from Route.Vehicle.Name

        // AM shift data
        public int AMStartingMileage { get; set; }
        public int AMEndingMileage { get; set; }
        public int AMRiders { get; set; }
        public Guid? AMDriverId { get; set; } // Maps to Route.DriverId

        // PM shift data
        public int PMStartMileage { get; set; }
        public int PMEndingMileage { get; set; }
        public int PMRiders { get; set; }
        public Guid? PMDriverId { get; set; } // Maps to Route.PMDriverId

        // Additional properties required by UI
        public DateTime ScheduledTime { get; set; } // Maps to Route.ScheduledTime
        public DateTime TripDate { get; set; } // Maps to Route.RouteDate
        public Guid? DriverId { get; set; } // Maps to Route.DriverId (main driver for the route)

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
                RouteName = route.Name ?? string.Empty,
                Name = route.Name ?? string.Empty,
                StartLocation = route.StartLocation ?? string.Empty,
                EndLocation = route.EndLocation ?? string.Empty,
                Distance = route.Distance, // int to decimal is an implicit conversion
                Duration = null, // No EstimatedDuration in Route.cs, set to null
                RouteDate = route.RouteDate,
                VehicleId = route.VehicleId,
                VehicleAssignment = route.Vehicle?.Name ?? string.Empty, // Assuming Vehicle has a Name property
                AMStartingMileage = route.AMStartingMileage,
                AMEndingMileage = route.AMEndingMileage,
                AMRiders = route.AMRiders,
                AMDriverId = route.DriverId, // AM Driver is the main DriverId
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
            var route = new Route
            {
                Id = Id,
                RouteCode = RouteNumber,
                Name = Name, // Or RouteName, ensure consistency
                StartLocation = StartLocation,
                EndLocation = EndLocation,
                // Route.Distance is int and likely computed/readonly in DB. Avoid setting if so.
                // If it needs to be set, ensure conversion: (int)Distance
                // For now, assuming it's not set from DTO directly if it's a computed field.
                // EstimatedDuration is not in Route.cs
                RouteDate = RouteDate,
                VehicleId = VehicleId,
                // VehicleAssignment is for display, not directly mapped back to a simple string field in Route.
                // The Vehicle relationship is managed by VehicleId.
                AMStartingMileage = AMStartingMileage,
                AMEndingMileage = AMEndingMileage,
                AMRiders = AMRiders,
                DriverId = AMDriverId, // Main DriverId is AMDriverId from DTO
                PMStartMileage = PMStartMileage,
                PMEndingMileage = PMEndingMileage,
                PMRiders = PMRiders,
                PMDriverId = PMDriverId,
                ScheduledTime = ScheduledTime,
                // RouteDate is already set. TripDate from DTO maps to Route.RouteDate.
                // If DriverId on DTO is the primary one, ensure it's mapped to Route.DriverId
            };
            if (DriverId.HasValue) // If DTO's DriverId is specifically set, use it.
            {
                route.DriverId = DriverId;
            }

            // Handle Route.Distance if it's meant to be writable
            // route.Distance = (int)this.Distance;

            return route;
        }
    }
}
