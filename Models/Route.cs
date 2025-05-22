
using System;
using System.ComponentModel.DataAnnotations;

namespace BusBus.Models
{    /// <summary>
     /// Represents a route in the BusBus system.
     /// </summary>
    public class Route
    {        /// <summary>
             /// Gets or sets the unique identifier for the route.
             /// </summary>        
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the name of the route.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date of the route.
        /// </summary>
        [Required]
        public DateTime RouteDate { get; set; } = DateTime.Today;        /// <summary>
                                                                         /// Gets or sets the starting mileage for the AM session.
                                                                         /// </summary>
        [Range(0, int.MaxValue)]
        public int AMStartingMileage { get; set; }

        /// <summary>
        /// Gets or sets the ending mileage for the AM session.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int AMEndingMileage { get; set; }

        /// <summary>
        /// Gets or sets the number of riders for the AM session.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int AMRiders { get; set; }

        /// <summary>
        /// Gets or sets the starting mileage for the PM session.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int PMStartMileage { get; set; }

        /// <summary>
        /// Gets or sets the ending mileage for the PM session.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int PMEndingMileage { get; set; }

        /// <summary>
        /// Gets or sets the number of riders for the PM session.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int PMRiders { get; set; }        /// <summary>
                                                 /// Gets or sets the driver assigned to the route.
                                                 /// </summary>
        public Driver? Driver { get; set; }

        /// <summary>
        /// Gets or sets the driver ID for the route.
        /// </summary>
        public Guid? DriverId { get; set; }

        /// <summary>
        /// Gets or sets the vehicle assigned to the route.
        /// </summary>
        public Vehicle? Vehicle { get; set; }

        /// <summary>
        /// Gets or sets the vehicle ID for the route.
        /// </summary>
        public Guid? VehicleId { get; set; }
    }
}
