#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
// <auto-added>
#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;

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

        public int DriverID { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime HireDate { get; set; }
        public DateTime? LastPerformanceReview { get; set; }
        public int SalaryGrade { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // For optimistic concurrency

        // JSON-backed properties leveraging SQL Server's JSON support
        private string _emergencyContact = string.Empty;
        public string EmergencyContactJson
        {
            get => _emergencyContact;
            set => _emergencyContact = value;
        }

        public EmergencyContact EmergencyContact
        {
            get => string.IsNullOrEmpty(_emergencyContact) ? new EmergencyContact() :
                   JsonSerializer.Deserialize<EmergencyContact>(_emergencyContact) ?? new EmergencyContact();
            set => _emergencyContact = JsonSerializer.Serialize(value);
        }

        private string _personalDetails = string.Empty;
        public string PersonalDetailsJson
        {
            get => _personalDetails;
            set => _personalDetails = value;
        }

        public PersonalDetails PersonalDetails
        {
            get => string.IsNullOrEmpty(_personalDetails) ? new PersonalDetails() :
                   JsonSerializer.Deserialize<PersonalDetails>(_personalDetails) ?? new PersonalDetails();
            set => _personalDetails = JsonSerializer.Serialize(value);
        }

        // Computed property from SQL Server function
        public decimal PerformanceScore { get; set; } = 5.0m;

        // Calculated properties
        public int YearsOfService => DateTime.Now.Year - HireDate.Year;
        public bool NeedsPerformanceReview =>
            !LastPerformanceReview.HasValue || LastPerformanceReview.Value.AddYears(1) < DateTime.Now;
    }

    public class EmergencyContact
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class PersonalDetails
    {
        public string HairColor { get; set; } = string.Empty;
        public string EyeColor { get; set; } = string.Empty;
        public int? Height { get; set; } // in cm
        public string BloodType { get; set; } = string.Empty;
        public List<string> Allergies { get; set; } = new List<string>();
        public string MedicalNotes { get; set; } = string.Empty;
        public List<string> Certifications { get; set; } = new List<string>();

        // This property is serialized as JSON and not a navigation property
        [System.Text.Json.Serialization.JsonInclude]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
    }
}
