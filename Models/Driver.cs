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
        public string LastName { get; set; } = string.Empty;        /// <summary>
                                                                    /// Gets or sets the display name for the driver (for UI and reporting)
                                                                    /// </summary>
        public string Name
        {
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    FirstName = parts.Length > 0 ? parts[0] : string.Empty;
                    LastName = parts.Length > 1 ? parts[1] : string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the full name for display in data grids (same as Name property)
        /// Per BusBus Info: "First Name Last Name" format
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }        public string LicenseNumber { get; set; } = string.Empty;        /// <summary>
                                                                         /// Type of license held by the driver (CDL or Passenger)
                                                                         /// </summary>
        public string LicenseType { get; set; } = "CDL";

        public override string ToString()
        {
            return $"{FirstName} {LastName}".Trim();
        }

        public int DriverID { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Indicates if the driver is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indicates if the driver needs a performance review
        /// </summary>
        public bool NeedsPerformanceReview => LastPerformanceReview == null ||
                                             (DateTime.UtcNow - LastPerformanceReview.Value).TotalDays > 365;

        /// <summary>
        /// Gets the years of service for the driver
        /// </summary>
        public int YearsOfService => (int)((DateTime.UtcNow - HireDate).TotalDays / 365.25);

        /// <summary>
        /// Gets the overall performance score for the driver
        /// </summary>
        public double PerformanceScore => (PerformanceMetrics.SafetyScore +
                                          PerformanceMetrics.PunctualityScore +
                                          PerformanceMetrics.CustomerServiceScore) / 3.0;

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
        }        private string _performanceMetrics = string.Empty;
        public string PerformanceMetricsJson
        {
            get => _performanceMetrics;
            set => _performanceMetrics = value;
        }

        public PerformanceMetrics PerformanceMetrics
        {
            get => string.IsNullOrEmpty(_performanceMetrics) ? new PerformanceMetrics() :
                   JsonSerializer.Deserialize<PerformanceMetrics>(_performanceMetrics) ?? new PerformanceMetrics();
            set => _performanceMetrics = JsonSerializer.Serialize(value);
        }

        public Driver()
        {
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
            HireDate = DateTime.Today;
        }
    }    /// <summary>
         /// Emergency contact information for a driver
         /// </summary>
    public class EmergencyContact
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty; // Alternative property name used by tests
        public string Relationship { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    /// <summary>
    /// Personal details for a driver
    /// </summary>
    public class PersonalDetails
    {
        public string Name { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string HairColor { get; set; } = string.Empty;
    }

    /// <summary>
    /// Performance metrics for a driver
    /// </summary>
    public class PerformanceMetrics
    {
        public int Id { get; set; } // Primary key - will be hidden from grid views
        public double SafetyScore { get; set; }
        public double PunctualityScore { get; set; }
        public double CustomerServiceScore { get; set; }
        public int TotalTrips { get; set; }
        public int AccidentCount { get; set; }
    }
}
