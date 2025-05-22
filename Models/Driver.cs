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
        /// Gets or sets the first name of the driver
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last name of the driver
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{FirstName} {LastName}".Trim();
        }
    }
}
