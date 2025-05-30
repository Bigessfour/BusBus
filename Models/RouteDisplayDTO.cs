#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable

using System;
using BusBus.Data.Models;

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
                RouteName = route.Name ?? string.Empty,
                Name = route.Name ?? string.Empty,
                StartLocation = route.Origin ?? string.Empty,
                EndLocation = route.Destination ?? string.Empty,
                Distance = route.TotalMiles,
                Duration = TimeSpan.FromMinutes(route.EstimatedDuration),
                RouteDate = route.Date,
                VehicleId = route.VehicleId,
                VehicleAssignment = route.VehicleAssignment ?? string.Empty,
                AMStartingMileage = route.AMStartingMileage,
                AMEndingMileage = route.AMEndingMileage,
                AMRiders = route.AMRiders,
                AMDriverId = route.AMDriverId,
                PMStartMileage = route.PMStartMileage,
                PMEndingMileage = route.PMEndingMileage,
                PMRiders = route.PMRiders,
                PMDriverId = route.PMDriverId,
                ScheduledTime = route.ScheduledTime,
                TripDate = route.RouteDate, // Map RouteDate to TripDate
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
                Origin = StartLocation,
                Destination = EndLocation,
                TotalMiles = Distance,
                EstimatedDuration = (int)Duration.TotalMinutes,
                Date = RouteDate,
                VehicleId = VehicleId,
                VehicleAssignment = VehicleAssignment,
                AMStartingMileage = AMStartingMileage,
                AMEndingMileage = AMEndingMileage,
                AMRiders = AMRiders,
                AMDriverId = AMDriverId,
                PMStartMileage = PMStartMileage,
                PMEndingMileage = PMEndingMileage,
                PMRiders = PMRiders,
                PMDriverId = PMDriverId,
                ScheduledTime = ScheduledTime,
                RouteDate = TripDate, // Map TripDate back to RouteDate
                DriverId = DriverId
            };
        }
    }
}
