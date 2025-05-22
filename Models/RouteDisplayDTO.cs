using System;

namespace BusBus.Models
{
    /// <summary>
    /// Data transfer object for displaying route information in the UI.
    /// Contains computed properties not present in the base Route model.
    /// </summary>
    public class RouteDisplayDTO
    {
        public Guid Id { get; set; } // Changed from int to Guid to match Route.Id type
        public string Name { get; set; } = string.Empty;
        public DateTime RouteDate { get; set; }
        public int AMStartingMileage { get; set; }
        public int AMEndingMileage { get; set; }
        public int AMRiders { get; set; }
        public int PMStartMileage { get; set; }
        public int PMEndingMileage { get; set; }
        public int PMRiders { get; set; }

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
                DriverName = route.Driver?.Name ?? "Unassigned",
                VehicleName = route.Vehicle?.Name ?? "Unassigned"
            };
        }
    }
}
