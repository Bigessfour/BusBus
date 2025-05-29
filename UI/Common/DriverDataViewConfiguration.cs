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
    /// Data view configuration for Drivers in the dynamic DataGridView
    /// </summary>
    public class DriverDataViewConfiguration : IDataViewConfiguration<Driver>
    {
        private readonly IDriverService _driverService;

        // Static readonly arrays to avoid CA1861 warning
        private static readonly string[] LicenseTypes = { "CDL", "Passenger" };

        public string ViewName => "Driver";
        public string PluralName => "Drivers";
        public string Icon => "👥";

        public DriverDataViewConfiguration(IDriverService driverService)
        {
            _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
        }        /// <summary>
        /// Configures the columns for the Driver DataGridView based on BusBus Info specifications
        /// </summary>
        public void ConfigureColumns(DataGridView dataGrid)
        {
            ArgumentNullException.ThrowIfNull(dataGrid);

            dataGrid.Columns.Clear();
            dataGrid.AutoGenerateColumns = false;

            dataGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                // Driver ID (hidden - primary key for record tracking)
                new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    HeaderText = "Driver ID",
                    DataPropertyName = "Id",
                    Visible = false // Hidden as per BusBus Info requirements
                },

                // Combined First Name + Last Name display as "First Name Last Name"
                new DataGridViewTextBoxColumn
                {
                    Name = "FullName",
                    HeaderText = "Driver Name",
                    DataPropertyName = "FullName", // Uses computed FullName property
                    FillWeight = 200,
                    MinimumWidth = 150,
                    ReadOnly = true // Combined field is display-only
                },

                // First Name (for editing, but may be hidden in view depending on implementation)
                new DataGridViewTextBoxColumn
                {
                    Name = "FirstName",
                    HeaderText = "First Name",
                    DataPropertyName = "FirstName",
                    FillWeight = 150,
                    MinimumWidth = 100,
                    ReadOnly = false,
                    Visible = false // Hidden - editing handled through forms
                },

                // Last Name (for editing, but may be hidden in view depending on implementation)
                new DataGridViewTextBoxColumn
                {
                    Name = "LastName",
                    HeaderText = "Last Name",
                    DataPropertyName = "LastName",
                    FillWeight = 150,
                    MinimumWidth = 100,
                    ReadOnly = false,
                    Visible = false // Hidden - editing handled through forms
                },

                // Phone Number
                new DataGridViewTextBoxColumn
                {
                    Name = "PhoneNumber",
                    HeaderText = "Phone",
                    DataPropertyName = "PhoneNumber",
                    FillWeight = 120,
                    MinimumWidth = 100,
                    ReadOnly = false
                },

                // Email
                new DataGridViewTextBoxColumn
                {
                    Name = "Email",
                    HeaderText = "Email",
                    DataPropertyName = "Email",
                    FillWeight = 180,
                    MinimumWidth = 150,
                    ReadOnly = false
                },

                // License Type - dropdown with CDL or Passenger per BusBus Info
                new DataGridViewComboBoxColumn
                {
                    Name = "LicenseType",
                    HeaderText = "License Type",
                    DataPropertyName = "LicenseType",
                    FillWeight = 120,
                    MinimumWidth = 100,
                    DataSource = LicenseTypes,
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing
                }
            });
        }

        public async Task<List<Driver>> LoadDataAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _driverService.GetDriversAsync(page, pageSize, cancellationToken);
        }

        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _driverService.GetDriversCountAsync(cancellationToken);
        }

        public async Task<Driver> CreateAsync(Driver entity, CancellationToken cancellationToken = default)
        {
            return await _driverService.CreateAsync(entity, cancellationToken);
        }

        public async Task<Driver> UpdateAsync(Driver entity, CancellationToken cancellationToken = default)
        {
            return await _driverService.UpdateAsync(entity, cancellationToken);
        }

        public async Task<bool> DeleteAsync(Driver entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                await _driverService.DeleteAsync(entity.Id, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public object GetEntityId(Driver entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return entity.Id;
        }

        public Driver CreateNewEntity()
        {
            return new Driver
            {
                FirstName = "New",
                LastName = "Driver",
                LicenseNumber = "000000000"
            };
        }
    }
}
