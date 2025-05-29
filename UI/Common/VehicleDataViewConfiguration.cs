#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Data view configuration for Vehicles in the dynamic DataGridView
    /// </summary>
    public class VehicleDataViewConfiguration : IDataViewConfiguration<Vehicle>
    {
        private readonly IVehicleService _vehicleService;

        public string ViewName => "Vehicle";
        public string PluralName => "Vehicles";
        public string Icon => "🚗";

        public VehicleDataViewConfiguration(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
        }        /// <summary>
        /// Configures the columns for the Vehicle DataGridView based on BusBus Info specifications
        /// </summary>
        public void ConfigureColumns(DataGridView dataGrid)
        {
            ArgumentNullException.ThrowIfNull(dataGrid);
            dataGrid.Columns.Clear();
            dataGrid.AutoGenerateColumns = false;

            dataGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                // Primary Key (hidden - for record tracking per BusBus Info)
                new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    HeaderText = "Primary Key",
                    DataPropertyName = "Id",
                    Visible = false // Hidden as per BusBus Info requirements
                },

                // Bus Number (unique fleet number)
                new DataGridViewTextBoxColumn
                {
                    Name = "Number",
                    HeaderText = "Bus Number",
                    DataPropertyName = "Number",
                    FillWeight = 100,
                    MinimumWidth = 80,
                    ReadOnly = false
                },

                // Model Year (4-digit number)
                new DataGridViewTextBoxColumn
                {
                    Name = "ModelYear",
                    HeaderText = "Model Year",
                    DataPropertyName = "ModelYear",
                    FillWeight = 80,
                    MinimumWidth = 70,
                    ReadOnly = false
                },

                // Make (vehicle manufacturer)
                new DataGridViewTextBoxColumn
                {
                    Name = "Make",
                    HeaderText = "Make",
                    DataPropertyName = "Make",
                    FillWeight = 120,
                    MinimumWidth = 100,
                    ReadOnly = false
                },

                // Model (alphanumeric model number)
                new DataGridViewTextBoxColumn
                {
                    Name = "Model",
                    HeaderText = "Model",
                    DataPropertyName = "Model",
                    FillWeight = 150,
                    MinimumWidth = 120,
                    ReadOnly = false
                },

                // VIN Number (17-18 digit alphanumeric in caps)
                new DataGridViewTextBoxColumn
                {
                    Name = "VINNumber",
                    HeaderText = "VIN Number",
                    DataPropertyName = "VINNumber",
                    FillWeight = 180,
                    MinimumWidth = 150,
                    ReadOnly = false
                },

                // Capacity (1-100 passengers)
                new DataGridViewTextBoxColumn
                {
                    Name = "Capacity",
                    HeaderText = "Capacity",
                    DataPropertyName = "Capacity",
                    FillWeight = 80,
                    MinimumWidth = 70,
                    ReadOnly = false
                },

                // Last Inspection Date (date picker field)
                new DataGridViewTextBoxColumn
                {
                    Name = "LastInspectionDate",
                    HeaderText = "Last Inspection Date",
                    DataPropertyName = "LastInspectionDate",
                    FillWeight = 120,
                    MinimumWidth = 100,
                    ReadOnly = false,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "dd-MM-yy" // Format as DD-MM-YY
                    }
                },

                // Active/Inactive status (hidden from dropdown when inactive)
                new DataGridViewCheckBoxColumn
                {
                    Name = "IsActive",
                    HeaderText = "Active",
                    DataPropertyName = "IsActive",
                    FillWeight = 60,
                    MinimumWidth = 50,
                    ReadOnly = false
                }
            });
        }

        public async Task<List<Vehicle>> LoadDataAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _vehicleService.GetVehiclesAsync(page, pageSize, cancellationToken);
        }

        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _vehicleService.GetVehiclesCountAsync(cancellationToken);
        }

        public async Task<Vehicle> CreateAsync(Vehicle entity, CancellationToken cancellationToken = default)
        {
            return await _vehicleService.CreateAsync(entity, cancellationToken);
        }

        public async Task<Vehicle> UpdateAsync(Vehicle entity, CancellationToken cancellationToken = default)
        {
            return await _vehicleService.UpdateAsync(entity, cancellationToken);
        }
        public async Task<bool> DeleteAsync(Vehicle entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            try
            {
                await _vehicleService.DeleteAsync(entity.Id, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public object GetEntityId(Vehicle entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return entity.Id;
        }

        public Vehicle CreateNewEntity()
        {
            return new Vehicle
            {
                Number = "000",
                Capacity = 40,
                Model = "New Vehicle",
                LicensePlate = "ABC123",
                IsActive = true
            };
        }
    }
}
