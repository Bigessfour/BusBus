using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return

namespace BusBus.Models
{
    public class Maintenance
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; }
        public string MaintenanceType { get; set; }
        public DateTime MaintenanceDate { get; set; } = DateTime.Now;
        public decimal Cost { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int? TechnicianId { get; set; }
        public string TechnicianName { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int? MileageAtService { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public byte[] RowVersion { get; set; }

        // JSON-backed parts and labor details
        private string _partsUsedJson;
        public string PartsUsedJson
        {
            get => _partsUsedJson;
            set => _partsUsedJson = value;
        }

        public List<PartUsed> PartsUsed
        {
            get => string.IsNullOrEmpty(_partsUsedJson) ? new List<PartUsed>() :
                   JsonSerializer.Deserialize<List<PartUsed>>(_partsUsedJson) ?? new List<PartUsed>();
            set => _partsUsedJson = JsonSerializer.Serialize(value);
        }

        private string _laborDetailsJson;
        public string LaborDetailsJson
        {
            get => _laborDetailsJson;
            set => _laborDetailsJson = value;
        }

        public LaborDetails LaborDetails
        {
            get => string.IsNullOrEmpty(_laborDetailsJson) ? new LaborDetails() :
                   JsonSerializer.Deserialize<LaborDetails>(_laborDetailsJson) ?? new LaborDetails();
            set => _laborDetailsJson = JsonSerializer.Serialize(value);
        }

        // Calculated properties
        public bool IsCompleted => Status == "Completed" && CompletedDate.HasValue;
        public TimeSpan? Duration => CompletedDate.HasValue ? CompletedDate.Value - MaintenanceDate : null;
        public decimal TotalPartsCost => PartsUsed?.Sum(p => p.Cost * p.Quantity) ?? 0;
        public decimal TotalLaborCost => LaborDetails?.TotalLaborCost ?? 0;
        public bool IsWarrantyWork => Description?.Contains("warranty", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public class PartUsed
    {
        public string PartNumber { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        public string Supplier { get; set; }
        public string WarrantyInfo { get; set; }
    }

    public class LaborDetails
    {
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public string TasksPerformed { get; set; }
        public List<string> Technicians { get; set; } = new List<string>();
        public decimal TotalLaborCost => HoursWorked * HourlyRate;
    }
}
