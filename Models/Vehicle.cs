using System;

namespace BusBus.Models
{
    /// <summary>
    /// Represents a vehicle in the BusBus system
    /// </summary>
    public class Vehicle
    {
        /// <summary>
        /// Gets or sets the unique identifier for the vehicle
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the bus number of the vehicle
        /// </summary>
        public string BusNumber { get; set; } = string.Empty;

        public override string ToString()
        {
            return BusNumber;
        }
    }
}
