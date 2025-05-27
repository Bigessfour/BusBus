using System;
using System.Collections.Generic;
using BusBus.Models;
using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace BusBus.Tests.Models
{
    [TestFixture]
    [Category(TestCategories.Unit)]
    public class ModelTests
    {
        [Test]
        [Description("Test Route model calculated properties and business logic")]
        public void Route_CalculatedProperties_ShouldReturnCorrectValues()
        {
            // Arrange
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Name = "ModelTestRoute",
                AMStartingMileage = 1000,
                AMEndingMileage = 1025,
                PMStartMileage = 1025,
                PMEndingMileage = 1060,
                AMRiders = 25,
                PMRiders = 30,
                RouteDate = DateTime.Today,
                StartLocation = "School",
                EndLocation = "Downtown",
                ScheduledTime = DateTime.Today.AddHours(7),
                RowVersion = new byte[] { 1, 0, 0, 0 },
                CreatedBy = "UnitTest",
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                RouteID = 5001,
                RouteName = "ModelTestRouteName",
                RouteCode = "RTMODEL",
                IsActive = true,
                StopsJson = "[]",
                ScheduleJson = "{}"
            };

            // Act & Assert
            route.TotalMiles.Should().Be(60); // (1025-1000) + (1060-1025) = 60
            route.TotalRiders.Should().Be(55); // 25 + 30 = 55
            route.AMMiles.Should().Be(25); // 1025 - 1000 = 25
            route.PMMiles.Should().Be(35); // 1060 - 1025 = 35
            route.HasDriver.Should().BeFalse(); // No driver assigned
            route.HasVehicle.Should().BeFalse(); // No vehicle assigned
        }

        [Test]
        [Description("Test Driver model name formatting and properties")]
        public void Driver_NameProperty_ShouldFormatNameCorrectly()
        {
            // Arrange
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Smith",
                LicenseNumber = "DTEST001",
                HireDate = DateTime.Today.AddYears(-1),
                CreatedDate = DateTime.Today,
                ModifiedDate = DateTime.Today,
                CreatedBy = "UnitTest",
                RowVersion = new byte[] { 1, 0, 0, 0 }
            };

            // Act & Assert
            driver.Name.Should().Be("John Smith");

            // Test setting name property
            driver.Name = "Jane Doe";
            driver.FirstName.Should().Be("Jane");
            driver.LastName.Should().Be("Doe");

            // Test first name only
            driver.Name = "Alice";
            driver.FirstName.Should().Be("Alice");
            driver.LastName.Should().BeEmpty();

            // Test empty name
            driver.Name = "";
            driver.FirstName.Should().Be("Alice"); // Should not change
            driver.LastName.Should().BeEmpty();

            // Test ToString() override
            driver.ToString().Should().Be("Alice");
        }

        [Test]
        [Description("Test Driver model's computed properties")]
        public void Driver_ComputedProperties_ShouldReturnCorrectValues()
        {
            // Arrange
            var driver = new Driver
            {
                FirstName = "John",
                LastName = "Smith",
                HireDate = DateTime.Today.AddYears(-2),
                LastPerformanceReview = DateTime.Today.AddMonths(-6)
            };

            // Act & Assert
            driver.YearsOfService.Should().Be(2);
            driver.NeedsPerformanceReview.Should().BeFalse(); // Less than a year since last review

            // Test needs review
            driver.LastPerformanceReview = DateTime.Today.AddYears(-2);
            driver.NeedsPerformanceReview.Should().BeTrue(); // More than a year since last review

            // Test null review date
            driver.LastPerformanceReview = null;
            driver.NeedsPerformanceReview.Should().BeTrue(); // No review
        }

        [Test]
        [Description("Test EmergencyContact and PersonalDetails JSON serialization")]
        public void Driver_JsonProperties_ShouldSerializeCorrectly()
        {
            // Arrange
            var driver = new Driver();
            var emergencyContact = new EmergencyContact
            {
                Name = "Jane Smith",
                Phone = "555-1234",
                Relationship = "Spouse",
                Address = "123 Main St"
            };

            var personalDetails = new PersonalDetails
            {
                HairColor = "Brown",
                EyeColor = "Blue",
                Height = 180,
                BloodType = "O+",
                Allergies = new List<string> { "Peanuts", "Shellfish" },
                Certifications = new List<string> { "CPR", "First Aid" },
                CustomFields = new Dictionary<string, object>
                {
                    { "ShirtSize", "XL" },
                    { "PreferredRoute", "Downtown" }
                }
            };

            // Act
            driver.EmergencyContact = emergencyContact;
            driver.PersonalDetails = personalDetails;

            // Assert
            driver.EmergencyContactJson.Should().NotBeNullOrEmpty();
            driver.PersonalDetailsJson.Should().NotBeNullOrEmpty();

            // Test JSON serialization
            var deserializedEmergencyContact = JsonSerializer.Deserialize<EmergencyContact>(driver.EmergencyContactJson);
            deserializedEmergencyContact.Should().NotBeNull();
            deserializedEmergencyContact.Name.Should().Be("Jane Smith");
            deserializedEmergencyContact.Phone.Should().Be("555-1234");

            var deserializedPersonalDetails = JsonSerializer.Deserialize<PersonalDetails>(driver.PersonalDetailsJson);
            deserializedPersonalDetails.Should().NotBeNull();
            deserializedPersonalDetails.HairColor.Should().Be("Brown");
            deserializedPersonalDetails.EyeColor.Should().Be("Blue");
            deserializedPersonalDetails.Allergies.Should().Contain("Peanuts");
            deserializedPersonalDetails.CustomFields.Should().ContainKey("ShirtSize");
            deserializedPersonalDetails.CustomFields["ShirtSize"].Should().Be("XL");
        }

        [Test]
        [Description("Test Vehicle model properties and validation")]
        public void Vehicle_Properties_ShouldWorkCorrectly()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                Number = "BUS101",
                Model = "School Bus Model XL",
                MakeModel = "Blue Bird",
                Year = 2023,
                LicensePlate = "SCH-101",
                Capacity = 72,
                IsActive = true,
                Mileage = 5000
            };

            // Act & Assert
            vehicle.DisplayName.Should().Be("BUS101 - School Bus Model XL");
            vehicle.IsActive.Should().BeTrue();
            vehicle.ToString().Should().Be("BUS101 - School Bus Model XL");

            // Test changing status
            vehicle.IsActive = false;
            vehicle.IsActive.Should().BeFalse();
        }

        [Test]
        [Description("Test CustomField model")]
        public void CustomField_Properties_ShouldWorkCorrectly()
        {
            // Arrange
            var customField = new CustomField
            {
                Name = "preferredRoute",
                Label = "Preferred Route",
                Type = "select",
                Required = true,
                DefaultValue = "Downtown"
            };

            // Add some options
            customField.Options.Add("Downtown");
            customField.Options.Add("Uptown");
            customField.Options.Add("Suburban");

            // Act & Assert
            customField.Name.Should().Be("preferredRoute");
            customField.Label.Should().Be("Preferred Route");
            customField.Type.Should().Be("select");
            customField.Required.Should().BeTrue();
            customField.DefaultValue.Should().Be("Downtown");
            customField.Options.Should().HaveCount(3);
            customField.Options.Should().Contain("Downtown");
            customField.Options.Should().Contain("Uptown");
            customField.Options.Should().Contain("Suburban");
        }
    }
}
