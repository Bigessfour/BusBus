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
        [System.ComponentModel.DataAnnotations.Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [System.ComponentModel.DataAnnotations.Required]
        public string FirstName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string LastName { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{FirstName} {LastName}".Trim();
        }
    }
}
