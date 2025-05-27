using System;
using System.Threading.Tasks;
using BusBus.Models;
using BusBus.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BusBus.Tests.Services
{
    [TestFixture]
    [Category(TestCategories.Service)]
    [Category(TestCategories.Unit)]
    public class VehicleServiceTests : TestBase
    {
        private IVehicleService _vehicleService;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _vehicleService = ServiceProvider.GetRequiredService<IVehicleService>();
        }

        [Test]
        [Description("Test creating, retrieving, updating, and deleting a vehicle")]
        public async Task VehicleService_CompleteLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var testVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Number = "TEST101",
                Model = "Test School Bus",
                MakeModel = "Test Manufacturer Model",
                Year = DateTime.Now.Year,
                LicensePlate = "TST-101",
                Capacity = 72,
                IsActive = true,
                Mileage = 5000,
                Status = "Available",
                FuelType = "Diesel",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                VehicleCode = "VTEST101"
            };

            // Act - Create
            var createdVehicle = await _vehicleService.CreateAsync(testVehicle);

            // Assert - Create
            createdVehicle.Should().NotBeNull();
            createdVehicle.Id.Should().Be(testVehicle.Id);
            createdVehicle.Number.Should().Be("TEST101");
            createdVehicle.Model.Should().Be("Test School Bus");

            // Act - Get
            var retrievedVehicle = await _vehicleService.GetByIdAsync(testVehicle.Id);

            // Assert - Get
            retrievedVehicle.Should().NotBeNull();
            retrievedVehicle.Id.Should().Be(testVehicle.Id);
            retrievedVehicle.LicensePlate.Should().Be("TST-101");
            retrievedVehicle.Capacity.Should().Be(72);

            // Act - Update
            retrievedVehicle.Number = "TEST102";
            retrievedVehicle.Capacity = 80;
            retrievedVehicle.Mileage = 6000;
            var updatedVehicle = await _vehicleService.UpdateAsync(retrievedVehicle);

            // Assert - Update
            updatedVehicle.Number.Should().Be("TEST102");
            updatedVehicle.Capacity.Should().Be(80);
            updatedVehicle.Mileage.Should().Be(6000);

            // Verify update with a fresh get
            var verifyVehicle = await _vehicleService.GetByIdAsync(testVehicle.Id);
            verifyVehicle.Number.Should().Be("TEST102");
            verifyVehicle.Capacity.Should().Be(80);

            // Act - Count and Paging
            var count = await _vehicleService.GetCountAsync();
            count.Should().BeGreaterThan(0);

            var pagedVehicles = await _vehicleService.GetPagedAsync(1, 10);
            pagedVehicles.Should().Contain(v => v.Id == testVehicle.Id);

            // Act - Delete
            await _vehicleService.DeleteAsync(testVehicle.Id);

            // Assert - Delete
            var deletedVehicle = await _vehicleService.GetByIdAsync(testVehicle.Id);
            deletedVehicle.Should().BeNull();
        }

        [Test]
        [Description("Test vehicle validation for valid and invalid entities")]
        public void VehicleService_ValidateEntity_ShouldReturnCorrectResults()
        {
            // Arrange
            var validVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Number = "V001",
                Capacity = 60,
                Model = "Valid Model",
                Status = "Available",
                FuelType = "Diesel",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                VehicleCode = "V001"
            };

            var noNumberVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Capacity = 60,
                Model = "No Number Model",
                Status = "Available",
                FuelType = "Diesel",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                VehicleCode = "NONUMBER"
            };

            var negativeCapacityVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Number = "V002",
                Capacity = -10,
                Model = "Negative Capacity Model",
                Status = "Available",
                FuelType = "Diesel",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                VehicleCode = "V002"
            };

            // Act & Assert
            var validResult = _vehicleService.ValidateEntity(validVehicle);
            validResult.IsValid.Should().BeTrue();
            validResult.ErrorMessage.Should().BeEmpty();

            var noNumberResult = _vehicleService.ValidateEntity(noNumberVehicle);
            noNumberResult.IsValid.Should().BeFalse();
            noNumberResult.ErrorMessage.Should().Contain("number");

            var negativeCapacityResult = _vehicleService.ValidateEntity(negativeCapacityVehicle);
            negativeCapacityResult.IsValid.Should().BeFalse();
            negativeCapacityResult.ErrorMessage.Should().Contain("Capacity");
        }

        [Test]
        [Description("Test vehicle status changes")]
        public async Task VehicleService_StatusChanges_ShouldUpdateCorrectly()
        {
            // Arrange
            var testVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Number = "STATUS101",
                Model = "Status Test Bus",
                Capacity = 65,
                IsActive = true,
                Status = "Available",
                FuelType = "Diesel",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RowVersion = new byte[] { 1, 0, 0, 0 },
                VehicleCode = "STATUS101"
            };

            // Create the vehicle
            await _vehicleService.CreateAsync(testVehicle);

            // Act - Set inactive
            var retrievedVehicle = await _vehicleService.GetByIdAsync(testVehicle.Id);
            retrievedVehicle.IsActive = false;
            await _vehicleService.UpdateAsync(retrievedVehicle);

            // Assert - Inactive status
            var inactiveVehicle = await _vehicleService.GetByIdAsync(testVehicle.Id);
            inactiveVehicle.IsActive.Should().BeFalse();

            // Act - Set active again
            inactiveVehicle.IsActive = true;
            await _vehicleService.UpdateAsync(inactiveVehicle);

            // Assert - Active status
            var reactivatedVehicle = await _vehicleService.GetByIdAsync(testVehicle.Id);
            reactivatedVehicle.IsActive.Should().BeTrue();
        }
    }
}
