
using System;
using System.ComponentModel.DataAnnotations;

namespace BusBus.Models
{    /// <summary>
     /// Represents a route in the BusBus system.
     /// </summary>
    public class Route
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime RouteDate { get; set; } = DateTime.Today;

        [Range(0, int.MaxValue)]
        public int AMStartingMileage { get; set; }

        [Range(0, int.MaxValue)]
        public int AMEndingMileage { get; set; }

        [Range(0, int.MaxValue)]
        public int AMRiders { get; set; }

        [Range(0, int.MaxValue)]
        public int PMStartMileage { get; set; }

        [Range(0, int.MaxValue)]
        public int PMEndingMileage { get; set; }

        [Range(0, int.MaxValue)]
        public int PMRiders { get; set; }

        // Foreign keys for Driver and Vehicle
        public Guid? DriverId { get; set; }
        public Driver? Driver { get; set; }
        public Guid? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
    }
}
