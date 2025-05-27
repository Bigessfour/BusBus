#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
// <auto-added>
#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BusBus.Models
{    /// <summary>
     /// Represents a route in the BusBus system.
     /// </summary>
    public class Route
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();        [Timestamp]
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Required for Entity Framework RowVersion/Timestamp")]
        public byte[] RowVersion { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime RouteDate { get; set; } = DateTime.Today;

        [Range(0, int.MaxValue)]
        public int AMStartingMileage { get; set; }

        [Range(0, int.MaxValue)]
        public int AMEndingMileage { get; set; }

        [Range(0, int.MaxValue)]
        public int AMRiders { get; set; }

        [Range(0, int.MaxValue)]
        public int PMStartMileage { get; set; }

        [Range(0, int.MaxValue)]
        public int PMEndingMileage { get; set; }

        [Range(0, int.MaxValue)]
        public int PMRiders { get; set; }

        // Foreign keys for Driver and Vehicle
        public Guid? DriverId { get; set; }
        public Driver? Driver { get; set; }
        public Guid? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public string StartLocation { get; set; } = string.Empty;
        public string EndLocation { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }

        public int RouteID { get; set; }
        public string RouteName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; }

        // Computed columns from SQL Server
        public string RouteCode { get; set; } // Computed: 'RT' + RIGHT('0000' + CAST(RouteID AS VARCHAR), 4)
        public int Distance { get; set; } // Computed or calculated field

        // JSON-backed stop information
        private string _stopsJson;
        public string StopsJson
        {
            get => _stopsJson;
            set => _stopsJson = value;
        }

        public List<BusStop> Stops
        {
            get => string.IsNullOrEmpty(_stopsJson) ? new List<BusStop>() :
                   JsonSerializer.Deserialize<List<BusStop>>(_stopsJson);
            set => _stopsJson = JsonSerializer.Serialize(value);
        }

        // JSON-backed schedule information
        private string _scheduleJson;
        public string ScheduleJson
        {
            get => _scheduleJson;
            set => _scheduleJson = value;
        }

        public RouteSchedule Schedule
        {
            get => string.IsNullOrEmpty(_scheduleJson) ? new RouteSchedule() :
                   JsonSerializer.Deserialize<RouteSchedule>(_scheduleJson);
            set => _scheduleJson = JsonSerializer.Serialize(value);
        }

        // Calculated properties
        public int NumberOfStops => Stops?.Count ?? 0;
        public TimeSpan EstimatedDuration => Schedule?.EstimatedTripTime ?? TimeSpan.Zero;

        // Additional computed properties for tests
        public double TotalMiles => (AMEndingMileage - AMStartingMileage) + (PMEndingMileage - PMStartMileage);
        public int TotalRiders => AMRiders + PMRiders;
        public double AMMiles => AMEndingMileage - AMStartingMileage;
        public double PMMiles => PMEndingMileage - PMStartMileage;
        public bool HasDriver => DriverId.HasValue;
        public bool HasVehicle => VehicleId.HasValue;
    }

    public class BusStop
    {
        public int StopID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public TimeSpan ScheduledArrival { get; set; }
        public TimeSpan ScheduledDeparture { get; set; }
        public bool IsAccessible { get; set; }
        public List<string> Amenities { get; set; } = new List<string>();
    }

    public class RouteSchedule
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int FrequencyMinutes { get; set; }
        public List<string> OperatingDays { get; set; } = new List<string>();
        public TimeSpan EstimatedTripTime { get; set; }
        public string Notes { get; set; }
    }
}
