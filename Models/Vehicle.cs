#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

#nullable enable

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

        // Unique identifier for the vehicle (int for DB compatibility, Guid for legacy)
        public int VehicleId { get; set; }
        public Guid Id { get => VehicleGuid; set => VehicleGuid = value; }
        public Guid VehicleGuid { get; set; } = Guid.NewGuid();

        // Vehicle number (string, for display and legacy code)
        public string Number { get; set; } = string.Empty;
        public string BusNumber { get => Number; set => Number = value; }

        /// <summary>
        /// Gets or sets the display name for the vehicle (for UI and reporting)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets the display name for vehicle lists and UI components
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(Name) ? Name :
                                     !string.IsNullOrEmpty(Number) ? $"Bus #{Number}" :
                                     $"Vehicle {VehicleId}";

        public int Capacity { get; set; }
        public string? Model { get; set; }
        public string? LicensePlate { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Vehicle manufacturer (e.g., Blue Bird, Thomas, IC Bus)
        /// </summary>
        public string Make { get; set; } = string.Empty;

        /// <summary>
        /// Vehicle Identification Number (17-18 digit alphanumeric)
        /// </summary>
        public string VINNumber { get; set; } = string.Empty;

        /// <summary>
        /// Model year of the vehicle (4-digit number)
        /// </summary>
        public int ModelYear { get; set; }

        /// <summary>
        /// Date of last yearly inspection
        /// </summary>
        public DateTime? LastInspectionDate { get; set; }

        // (Removed duplicate LicensePlate and IsActive properties)

        // Additional properties for compatibility
        public string Status { get; set; } = "Available";
        public string MakeModel { get; set; } = string.Empty;
        public int? Year { get; set; }
        public decimal Mileage { get; set; }
        public string FuelType { get; set; } = string.Empty;
        public bool IsMaintenanceRequired { get; set; } = false;
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public byte[]? RowVersion { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public string? VehicleCode { get; set; }
        public bool MaintenanceDue { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        private string _maintenanceHistory = string.Empty;
        public string MaintenanceHistoryJson { get => _maintenanceHistory; set => _maintenanceHistory = value; }
        public List<MaintenanceRecord> MaintenanceHistory { get => string.IsNullOrEmpty(_maintenanceHistory) ? new List<MaintenanceRecord>() : JsonSerializer.Deserialize<List<MaintenanceRecord>>(_maintenanceHistory) ?? new List<MaintenanceRecord>(); set => _maintenanceHistory = JsonSerializer.Serialize(value); }
        private string _specifications = string.Empty;
        public string SpecificationsJson { get => _specifications; set => _specifications = value; }
        public VehicleSpecifications Specifications { get => string.IsNullOrEmpty(_specifications) ? new VehicleSpecifications() : JsonSerializer.Deserialize<VehicleSpecifications>(_specifications) ?? new VehicleSpecifications(); set => _specifications = JsonSerializer.Serialize(value); }
        public int VehicleAge => Year.HasValue ? DateTime.Now.Year - Year.Value : 0;
        public bool IsOld => VehicleAge > 15;
        public string LocationDescription => Latitude.HasValue && Longitude.HasValue ? $"Lat: {Latitude:F4}, Lng: {Longitude:F4}" : "Location Unknown";
        public override string ToString() { return Number; }
    }

    public class MaintenanceRecord
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
        public int Mileage { get; set; }
    }
    public class VehicleSpecifications
    {
        public string Engine { get; set; } = string.Empty;
        public string Transmission { get; set; } = string.Empty;
        public int? Wheelbase { get; set; }
        public decimal? FuelTankCapacity { get; set; }
        public decimal? MPG { get; set; }

        [NotMapped]
        public List<string> SafetyFeatures { get; set; } = new List<string>();

        [NotMapped]
        public List<string> AccessibilityFeatures { get; set; } = new List<string>();

        [NotMapped]
        public Dictionary<string, object> CustomSpecs { get; set; } = new Dictionary<string, object>();
    }
}
