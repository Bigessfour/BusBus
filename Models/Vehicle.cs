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
        /// Gets or sets the name/model of the vehicle
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the registration number of the vehicle
        /// </summary>
        public string RegistrationNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the capacity of the vehicle
        /// </summary>
        public int Capacity { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
