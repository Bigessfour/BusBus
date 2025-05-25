using System;

namespace BusBus.Models
{
    /// <summary>
    /// Data transfer object for displaying route information in the UI.
    /// Contains computed properties not present in the base Route model.
    /// </summary>
    public class RouteDisplayDTO
    {
        // For DataGridViewComboBoxColumn binding
        public Guid? DriverId { get; set; }
        public Guid? VehicleId { get; set; }
        public Guid Id { get; set; } // Changed from int to Guid to match Route.Id type
        public string Name { get; set; } = string.Empty;
        public DateTime RouteDate { get; set; }
        public int AMStartingMileage { get; set; }
        public int AMEndingMileage { get; set; }
        public int AMRiders { get; set; }
        public int PMStartMileage { get; set; }
        public int PMEndingMileage { get; set; }
        public int PMRiders { get; set; }

        // Additional route properties
        public string StartLocation { get; set; } = string.Empty;
        public string EndLocation { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public DateTime TripDate { get; set; }

        // Navigation properties
        public Driver? Driver { get; set; }
        public Vehicle? Vehicle { get; set; }

        // Display-specific properties
        public string DriverName { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new RouteDisplayDTO from a Route entity
        /// </summary>
        public static RouteDisplayDTO FromRoute(Route route)
        {
            ArgumentNullException.ThrowIfNull(route);
            return new RouteDisplayDTO
            {
                Id = route.Id,
                Name = route.Name,
                RouteDate = route.RouteDate,
                AMStartingMileage = route.AMStartingMileage,
                AMEndingMileage = route.AMEndingMileage,
                AMRiders = route.AMRiders,
                PMStartMileage = route.PMStartMileage,
                PMEndingMileage = route.PMEndingMileage,
                PMRiders = route.PMRiders,
                StartLocation = route.StartLocation,
                EndLocation = route.EndLocation,
                ScheduledTime = route.ScheduledTime,
                TripDate = route.RouteDate, // Map RouteDate to TripDate
                DriverName = route.Driver != null ? $"{route.Driver.FirstName?.Trim()} {route.Driver.LastName?.Trim()}".Trim() : "Unassigned",
                VehicleName = !string.IsNullOrWhiteSpace(route.Vehicle?.BusNumber) ? route.Vehicle.BusNumber : "Unassigned",
                DriverId = route.DriverId,
                VehicleId = route.VehicleId,
                Driver = route.Driver,
                Vehicle = route.Vehicle
            };
        }

        /// <summary>
        /// Converts this DTO back to a Route entity (for update/save)
        /// </summary>
        public Route ToRoute()
        {
            return new Route
            {
                Id = this.Id,
                Name = this.Name,
                RouteDate = this.RouteDate,
                AMStartingMileage = this.AMStartingMileage,
                AMEndingMileage = this.AMEndingMileage,
                AMRiders = this.AMRiders,
                PMStartMileage = this.PMStartMileage,
                PMEndingMileage = this.PMEndingMileage,
                PMRiders = this.PMRiders,
                StartLocation = this.StartLocation,
                EndLocation = this.EndLocation,
                ScheduledTime = this.ScheduledTime,
                DriverId = this.DriverId,
                VehicleId = this.VehicleId
                // Driver and Vehicle must be set by lookup elsewhere if needed
            };
        }
    }
}
