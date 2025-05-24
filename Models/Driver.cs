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

        /// <summary>
        /// Gets or sets the display name for the driver (for UI and reporting)
        /// </summary>
        public string Name
        {
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var parts = value.Split(' ', 2);
                    FirstName = parts[0];
                    LastName = parts.Length > 1 ? parts[1] : string.Empty;
                }
            }
        }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string LicenseNumber { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{FirstName} {LastName}".Trim();
        }
    }
}
