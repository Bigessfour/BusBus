using System;

namespace BusBus.Models
{
    /// <summary>
    /// Represents a bus driver in the BusBus system
    /// </summary>
    public class Driver
    {
        /// <summary>
        /// Gets or sets the unique identifier for the driver
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the name of the driver
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the employee ID of the driver
        /// </summary>
        public string EmployeeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the license number of the driver
        /// </summary>
        public string LicenseNumber { get; set; } = string.Empty;

        public override string ToString()
        {
            return Name;
        }
    }
}
