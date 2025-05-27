using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DriverServiceTests : TestBase
    {
        private IDriverService _driverService;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            _driverService = ServiceProvider.GetRequiredService<IDriverService>();
        }

        [Test]
        [Description("Test creating, retrieving, updating, and deleting a driver")]
        public async Task DriverService_CompleteLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var testDriver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Driver",
                LicenseNumber = "DL12345",
                PhoneNumber = "555-1234",
                Email = "test@example.com",
                Status = "Active",
                HireDate = DateTime.Today.AddYears(-2),
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                CreatedBy = "UnitTest",
                RowVersion = new byte[] { 1, 0, 0, 0 }
            };

            // Act - Create
            var createdDriver = await _driverService.CreateAsync(testDriver);

            // Assert - Create
            createdDriver.Should().NotBeNull();
            createdDriver.Id.Should().Be(testDriver.Id);
            createdDriver.FirstName.Should().Be("Test");
            createdDriver.LastName.Should().Be("Driver");

            // Act - Get
            var retrievedDriver = await _driverService.GetByIdAsync(testDriver.Id);

            // Assert - Get
            retrievedDriver.Should().NotBeNull();
            retrievedDriver.Id.Should().Be(testDriver.Id);
            retrievedDriver.LicenseNumber.Should().Be("DL12345");

            // Act - Update
            retrievedDriver.FirstName = "Updated";
            retrievedDriver.PhoneNumber = "555-9876";
            var updatedDriver = await _driverService.UpdateAsync(retrievedDriver);

            // Assert - Update
            updatedDriver.FirstName.Should().Be("Updated");
            updatedDriver.PhoneNumber.Should().Be("555-9876");

            // Verify update with a fresh get
            var verifyDriver = await _driverService.GetByIdAsync(testDriver.Id);
            verifyDriver.FirstName.Should().Be("Updated");

            // Act - Count and Paging
            var count = await _driverService.GetCountAsync();
            count.Should().BeGreaterThan(0);

            var pagedDrivers = await _driverService.GetPagedAsync(1, 10);
            pagedDrivers.Should().Contain(d => d.Id == testDriver.Id);

            // Act - Delete
            await _driverService.DeleteAsync(testDriver.Id);

            // Assert - Delete
            var deletedDriver = await _driverService.GetByIdAsync(testDriver.Id);
            deletedDriver.Should().BeNull();
        }

        [Test]
        [Description("Test driver validation for valid and invalid entities")]
        public void DriverService_ValidateEntity_ShouldReturnCorrectResults()
        {
            // Arrange
            var validDriver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Valid",
                LastName = "Driver",
                LicenseNumber = "LIC123",
                HireDate = DateTime.Today.AddYears(-1),
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                CreatedBy = "UnitTest",
                RowVersion = new byte[] { 1, 0, 0, 0 }
            };

            var noNameDriver = new Driver
            {
                Id = Guid.NewGuid(),
                LicenseNumber = "LIC123",
                HireDate = DateTime.Today.AddYears(-1),
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                CreatedBy = "UnitTest",
                RowVersion = new byte[] { 1, 0, 0, 0 }
            };

            var noLicenseDriver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "Invalid",
                LastName = "Driver",
                HireDate = DateTime.Today.AddYears(-1),
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                CreatedBy = "UnitTest",
                RowVersion = new byte[] { 1, 0, 0, 0 }
            };

            // Act & Assert
            var validResult = _driverService.ValidateEntity(validDriver);
            validResult.IsValid.Should().BeTrue();
            validResult.ErrorMessage.Should().BeEmpty();

            var noNameResult = _driverService.ValidateEntity(noNameDriver);
            noNameResult.IsValid.Should().BeFalse();
            noNameResult.ErrorMessage.Should().Contain("name");

            var noLicenseResult = _driverService.ValidateEntity(noLicenseDriver);
            noLicenseResult.IsValid.Should().BeFalse();
            noLicenseResult.ErrorMessage.Should().Contain("License");
        }

        [Test]
        [Description("Test persistence of PersonalDetails and EmergencyContact JSON properties")]
        public async Task DriverService_JsonProperties_ShouldPersistCorrectly()
        {
            // Arrange
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "JSON",
                LastName = "Test",
                LicenseNumber = "JSON123",
                HireDate = DateTime.Today.AddYears(-1),
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                CreatedBy = "UnitTest",
                RowVersion = new byte[] { 1, 0, 0, 0 }
            };

            driver.EmergencyContact = new EmergencyContact
            {
                Name = "Emergency Contact",
                Phone = "911",
                Relationship = "Relative",
                Address = "123 Emergency St"
            };

            driver.PersonalDetails = new PersonalDetails
            {
                HairColor = "Brown",
                EyeColor = "Blue",
                Height = 180,
                BloodType = "O+",
                MedicalNotes = "None",
                Allergies = new List<string> { "Dust", "Pollen" },
                Certifications = new List<string> { "First Aid", "CPR" },
                CustomFields = new Dictionary<string, object>
                {
                    { "ShirtSize", "XL" },
                    { "PreferredShift", "Morning" }
                }
            };

            // Act
            var savedDriver = await _driverService.CreateAsync(driver);
            var retrievedDriver = await _driverService.GetByIdAsync(driver.Id);

            // Assert
            retrievedDriver.Should().NotBeNull();

            // Emergency Contact
            retrievedDriver.EmergencyContact.Name.Should().Be("Emergency Contact");
            retrievedDriver.EmergencyContact.Phone.Should().Be("911");
            retrievedDriver.EmergencyContact.Relationship.Should().Be("Relative");

            // Personal Details
            retrievedDriver.PersonalDetails.HairColor.Should().Be("Brown");
            retrievedDriver.PersonalDetails.EyeColor.Should().Be("Blue");
            retrievedDriver.PersonalDetails.Height.Should().Be(180);
            retrievedDriver.PersonalDetails.Allergies.Should().Contain("Dust");
            retrievedDriver.PersonalDetails.Certifications.Should().Contain("CPR");

            // Custom Fields
            retrievedDriver.PersonalDetails.CustomFields.Should().ContainKey("ShirtSize");
            retrievedDriver.PersonalDetails.CustomFields["ShirtSize"].Should().Be("XL");
        }
    }
}
