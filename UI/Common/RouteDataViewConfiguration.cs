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
    /// Data view configuration for Routes in the dynamic DataGridView
    /// Configures the grid to display routes with AM/PM tracking, driver and vehicle data
    /// </summary>
    public class RouteDataViewConfiguration : IDataViewConfiguration<Route>
    {
        private readonly IRouteService _routeService;

        // Static readonly arrays to avoid CA1861 warning
        private static readonly string[] RouteNames = { "Truck Plaza", "East Route", "West Route", "SPED Route" };

        public string ViewName => "Route";
        public string PluralName => "Routes";
        public string Icon => "🚌";

        public RouteDataViewConfiguration(IRouteService routeService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
        }        /// <summary>
        /// Configures the columns for the Route DataGridView based on BusBus Info specifications
        /// Four routes per school day: Truck Plaza, East Route, West Route, SPED Route
        /// </summary>
        public void ConfigureColumns(DataGridView dataGrid)
        {
            ArgumentNullException.ThrowIfNull(dataGrid);

            dataGrid.Columns.Clear();
            dataGrid.AutoGenerateColumns = false;

            // Configure columns based on BusBus Info specifications
            dataGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                // Primary Key (hidden - for record tracking)
                new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    HeaderText = "Primary Key",
                    DataPropertyName = "Id",
                    Visible = false // Hidden as per BusBus Info requirements
                },

                // Date column with DD-MM-YY format and date picker
                new DataGridViewTextBoxColumn
                {
                    Name = "RouteDate",
                    HeaderText = "Date",
                    DataPropertyName = "RouteDate",
                    FillWeight = 80,
                    MinimumWidth = 80,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "dd-MM-yy" // Format as DD-MM-YY as per BusBus Info requirements
                    }
                },

                // Route Name column - Four standard routes per school day
                new DataGridViewComboBoxColumn
                {
                    Name = "RouteName",
                    HeaderText = "Route Name",
                    DataPropertyName = "Name",
                    FillWeight = 120,
                    MinimumWidth = 100,
                    DataSource = RouteNames,
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing
                },

                // Bus/Vehicle column with dropdown of available vehicles
                new DataGridViewComboBoxColumn
                {
                    Name = "Bus",
                    HeaderText = "Bus",
                    DataPropertyName = "VehicleId",
                    FillWeight = 80,
                    MinimumWidth = 70,
                    DisplayMember = "Number",
                    ValueMember = "Id",
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing
                },

                // AM Begin Mileage - auto-populated from previous route's ending mileage
                new DataGridViewTextBoxColumn
                {
                    Name = "AMBeginMileage",
                    HeaderText = "AM Begin Mileage",
                    DataPropertyName = "AMStartingMileage",
                    FillWeight = 100,
                    MinimumWidth = 80,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N0",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },

                // AM End Mileage
                new DataGridViewTextBoxColumn
                {
                    Name = "AMEndMileage",
                    HeaderText = "AM End Mileage",
                    DataPropertyName = "AMEndingMileage",
                    FillWeight = 100,
                    MinimumWidth = 80,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N0",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },

                // AM Riders - manually entered
                new DataGridViewTextBoxColumn
                {
                    Name = "AMRiders",
                    HeaderText = "AM Riders",
                    DataPropertyName = "AMRiders",
                    FillWeight = 80,
                    MinimumWidth = 60,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },

                // AM Driver - dropdown from drivers table
                new DataGridViewComboBoxColumn
                {
                    Name = "AMDriver",
                    HeaderText = "AM Driver",
                    DataPropertyName = "DriverId",
                    FillWeight = 120,
                    MinimumWidth = 100,
                    DisplayMember = "FullName",
                    ValueMember = "Id",
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing
                },

                // PM Start Mileage - mirror of AM End Mileage
                new DataGridViewTextBoxColumn
                {
                    Name = "PMStartMileage",
                    HeaderText = "PM Start Mileage",
                    DataPropertyName = "PMStartMileage",
                    FillWeight = 100,
                    MinimumWidth = 80,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N0",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },

                // PM End Mileage
                new DataGridViewTextBoxColumn
                {
                    Name = "PMEndMileage",
                    HeaderText = "PM End Mileage",
                    DataPropertyName = "PMEndingMileage",
                    FillWeight = 100,
                    MinimumWidth = 80,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N0",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },

                // PM Riders - manually entered
                new DataGridViewTextBoxColumn
                {
                    Name = "PMRiders",
                    HeaderText = "PM Riders",
                    DataPropertyName = "PMRiders",
                    FillWeight = 80,
                    MinimumWidth = 60,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },

                // PM Driver - can be different from AM driver
                new DataGridViewComboBoxColumn
                {
                    Name = "PMDriver",
                    HeaderText = "PM Driver",
                    DataPropertyName = "PMDriverId", // Need to add this property to Route model
                    FillWeight = 120,
                    MinimumWidth = 100,
                    DisplayMember = "FullName",
                    ValueMember = "Id",
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing
                }
            });
        }

        /// <summary>
        /// Loads route data with pagination
        /// </summary>
        public async Task<List<Route>> LoadDataAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _routeService.GetRoutesAsync(page, pageSize, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading routes: {ex.Message}");
                return new List<Route>();
            }
        }

        /// <summary>
        /// Gets the total count of routes
        /// </summary>
        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _routeService.GetRoutesCountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting route count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Creates a new route
        /// </summary>
        public async Task<Route> CreateAsync(Route entity, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _routeService.CreateRouteAsync(entity, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating route: {ex.Message}");
                throw; // Rethrow to show error in UI
            }
        }

        /// <summary>
        /// Updates an existing route
        /// </summary>
        public async Task<Route> UpdateAsync(Route entity, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _routeService.UpdateRouteAsync(entity, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating route: {ex.Message}");
                throw; // Rethrow to show error in UI
            }
        }        /// <summary>
                 /// Deletes a route
                 /// </summary>
        public async Task<bool> DeleteAsync(Route entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                await _routeService.DeleteRouteAsync(entity.Id, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting route: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the unique identifier for a route
        /// </summary>
        public object GetEntityId(Route entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return entity.Id;
        }        /// <summary>
        /// Creates a new route instance with default values
        /// Based on BusBus Info: Four routes per school day with default assignments
        /// </summary>
        public Route CreateNewEntity()
        {
            return new Route
            {
                Id = Guid.NewGuid(),
                Name = "Truck Plaza", // Default to first route, user can change to East Route, West Route, or SPED Route
                RouteDate = DateTime.Today,
                // Default values - mileage will be auto-populated based on previous routes
                AMStartingMileage = 0,
                AMEndingMileage = 0,
                AMRiders = 0,
                PMStartMileage = 0,
                PMEndingMileage = 0,
                PMRiders = 0,
                IsActive = true,
                // Default vehicle assignment (Bus 17 for Truck Plaza per BusBus Info)
                VehicleId = null // Will be set based on route selection and defaults
            };
        }/// <summary>
                 /// Creates a filter panel for filtering routes
                 /// </summary>
                 /// <remarks>
                 /// This is an additional helper method not part of the interface, used by the RouteListView
                 /// </remarks>
        public static Control CreateFilterPanel(Action applyFilter)
        {
            ArgumentNullException.ThrowIfNull(applyFilter);

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            // Add date filter
            var datePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Width = 100,
                Location = new System.Drawing.Point(10, 8)
            };

            var filterButton = new Button
            {
                Text = "Filter",
                Location = new System.Drawing.Point(120, 7),
                Width = 80
            };

            filterButton.Click += (s, e) => applyFilter();

            panel.Controls.Add(datePicker);
            panel.Controls.Add(filterButton);

            return panel;
        }
    }
}
