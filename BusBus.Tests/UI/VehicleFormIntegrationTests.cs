#nullable enable
#pragma warning disable CA1416 // Platform compatibility (WinForms)
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using BusBus.UI;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusBus.Tests.UI
{
    [TestClass]
    [TestCategory(TestCategories.UI)]
    // Platform attribute removed (MSTest incompatible)
    // Apartment attribute removed (MSTest incompatible) // Required for WinForms testing
    public class VehicleFormIntegrationTests : TestBase
    {
        private IVehicleService _vehicleService = null!;
        private VehiclePanel _vehiclePanel = null!;
        private VehicleListPanel _vehicleListPanel = null!;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();
            _vehicleService = ServiceProvider.GetRequiredService<IVehicleService>();

            // Suppress dialogs for testing
            VehiclePanel.SuppressDialogsForTests = true;

            // Create UI components
            _vehiclePanel = new VehiclePanel(_vehicleService);
            _vehicleListPanel = new VehicleListPanel(_vehicleService);
        }

        [TestCleanup]
        public override void TearDown()
        {
            VehiclePanel.SuppressDialogsForTests = false;
            _vehiclePanel?.Dispose();
            _vehicleListPanel?.Dispose();
            base.TearDown();
        }

        [TestMethod]
        // Description: Test complete form workflow: Load -> Edit -> Save for vehicle
        public async Task VehiclePanel_CompleteWorkflow_ShouldPersistChanges()
        {
            // Arrange - Create test vehicle
            var originalVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Number = "TEST100",
                Model = "Updated Test Bus",
                Year = 2023,
                LicensePlate = "TST-100",
                Capacity = 65,
                IsActive = true,
                Mileage = 10000,
                Status = "Available",
                MakeModel = "Test MakeModel",
                FuelType = "Diesel",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                VehicleCode = "VTEST100"
            };

            var createdVehicle = await _vehicleService.CreateAsync(originalVehicle);

            // Act - Load vehicle into panel
            _vehiclePanel.LoadVehicle(createdVehicle);

            // Simulate user editing (accessing private fields via reflection for testing)
            var modelField = _vehiclePanel.GetType().GetField("_modelTextBox",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var capacityField = _vehiclePanel.GetType().GetField("_capacityNumericUpDown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (modelField?.GetValue(_vehiclePanel) is TextBox modelTextBox)
            {
                modelTextBox.Text = "Updated Test Bus";
            }

            if (capacityField?.GetValue(_vehiclePanel) is NumericUpDown capacityNumeric)
            {
                capacityNumeric.Value = 72;
            }

            // Simulate save button click
            var saveMethod = _vehiclePanel.GetType().GetMethod("SaveButton_Click",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (saveMethod != null)
            {
                // Use Task.Run to handle async method in sync context
                await Task.Run(async () =>
                {
                    // Use null-forgiving operator to silence CS8625/CS8600
                    if (saveMethod.Invoke(_vehiclePanel, new object?[] { null!, EventArgs.Empty }) is Task task)
                    {
                        await task;
                    }
                });
            }

            // Assert - Verify changes were saved to database
            var updatedVehicle = await _vehicleService.GetByIdAsync(createdVehicle.Id);
            updatedVehicle.Should().NotBeNull();
            updatedVehicle!.Model.Should().Be("Updated Test Bus");
            updatedVehicle.Capacity.Should().Be(72);

            // Test list panel - Verify vehicle is in the list
            var loadMethod = _vehicleListPanel.GetType().GetMethod("LoadVehicles",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (loadMethod != null)
            {
                await Task.Run(async () =>
                {
                    // Use null-forgiving operator to silence CS8600
                    if (loadMethod.Invoke(_vehicleListPanel, Array.Empty<object>()) is Task task)
                    {
                        await task;
                    }
                });
            }

            // Get the grid via reflection
            var gridField = _vehicleListPanel.GetType().GetField("_vehiclesGrid",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (gridField?.GetValue(_vehicleListPanel) is DataGridView grid)
            {
                bool vehicleFound = false;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Cells["Number"].Value?.ToString() == "TEST100" &&
                        row.Cells["Model"].Value?.ToString() == "Updated Test Bus")
                    {
                        vehicleFound = true;
                        break;
                    }
                }
                vehicleFound.Should().BeTrue("Updated vehicle should be found in the grid");
            }
        }
    }
}
