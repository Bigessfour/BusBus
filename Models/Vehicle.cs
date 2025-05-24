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
        [System.ComponentModel.DataAnnotations.Required]
        public Guid Id { get; set; } = Guid.NewGuid();



        [System.ComponentModel.DataAnnotations.Required]
        public required string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the bus number (for compatibility with existing code)
        /// </summary>
        public string BusNumber
        {
            get => Number;
            set => Number = value;
        }

        /// <summary>
        /// Gets or sets the display name for the vehicle (for UI and reporting)
        /// </summary>
        public string Name
        {
            get => Number;
            set => Number = value;
        }

        public int Capacity { get; set; }

        public string? Model { get; set; }

        public string? LicensePlate { get; set; }

        public bool IsActive { get; set; } = true;

        public override string ToString()
        {
            return Number;
        }
    }
}
