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
    public class DriverFormIntegrationTests : TestBase
    {
        private IDriverService _driverService = null!;
        private DriverPanel _driverPanel = null!;
        private DriverListPanel _driverListPanel = null!;

        [TestInitialize]
        public override async Task SetUp()
        {
            await base.SetUp();
            _driverService = ServiceProvider.GetRequiredService<IDriverService>();

            // Suppress dialogs for testing
            DriverPanel.SuppressDialogsForTests = true;

            // Create UI components
            _driverPanel = new DriverPanel(_driverService);
            _driverListPanel = new DriverListPanel(_driverService);
        }

        [TestCleanup]
        public override void TearDown()
        {
            DriverPanel.SuppressDialogsForTests = false;
            _driverPanel?.Dispose();
            _driverListPanel?.Dispose();
            base.TearDown();
        }

        [TestMethod]
        // Description: Test complete form workflow: Load -> Edit -> Save for driver
        public async Task DriverPanel_CompleteWorkflow_ShouldPersistChanges()
        {
            // Arrange - Create test driver
            var originalDriver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Original",
                LastName = "Test Driver",
                LicenseNumber = "DL12345",
                PhoneNumber = "555-1234",
                Email = "original@test.com",
                Status = "Active",
                HireDate = DateTime.Today.AddYears(-1),
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                CreatedBy = "UnitTest",
                RowVersion = new byte[] { 1, 0, 0, 0 }
            };

            var createdDriver = await _driverService.CreateAsync(originalDriver);

            // Act - Load driver into panel
            _driverPanel.LoadDriver(createdDriver);

            // Simulate user editing (accessing private fields via reflection for testing)
            var firstNameField = _driverPanel.GetType().GetField("_firstNameTextBox",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var phoneField = _driverPanel.GetType().GetField("_phoneTextBox",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (firstNameField?.GetValue(_driverPanel) is TextBox firstNameTextBox)
            {
                firstNameTextBox.Text = "Updated";
            }

            if (phoneField?.GetValue(_driverPanel) is TextBox phoneTextBox)
            {
                phoneTextBox.Text = "555-9876";
            }

            // Simulate save button click
            var saveMethod = _driverPanel.GetType().GetMethod("SaveButton_Click",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (saveMethod != null)
            {
                // Use Task.Run to handle async method in sync context
                await Task.Run(async () =>
                {
                    var result = saveMethod.Invoke(_driverPanel, new object[] { this, EventArgs.Empty });
                    if (result is Task task)
                        await task;
                });
            }

            // Assert - Verify changes were saved to database
            var updatedDriver = await _driverService.GetByIdAsync(createdDriver.Id);
            updatedDriver.Should().NotBeNull();
            updatedDriver!.FirstName.Should().Be("Updated");
            updatedDriver.PhoneNumber.Should().Be("555-9876");

            // Test list panel - Verify driver is in the list
            var loadMethod = _driverListPanel.GetType().GetMethod("LoadDrivers",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (loadMethod != null)
            {
                await Task.Run(async () =>
                {
                    var result = loadMethod.Invoke(_driverListPanel, Array.Empty<object>());
                    if (result is Task task)
                        await task;
                });
            }

            // Get the grid via reflection
            var gridField = _driverListPanel.GetType().GetField("_driversGrid",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (gridField?.GetValue(_driverListPanel) is DataGridView grid)
            {
                bool driverFound = false;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Cells["FirstName"].Value?.ToString() == "Updated" &&
                        row.Cells["LastName"].Value?.ToString() == "Test Driver")
                    {
                        driverFound = true;
                        break;
                    }
                }
                driverFound.Should().BeTrue("Updated driver should be found in the grid");
            }
        }
    }
}
