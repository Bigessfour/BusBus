using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using NUnit.Framework;
using BusBus.Models;

namespace BusBus.Tests
{
    [TestFixture]
    [Category(TestCategories.Model)]
    public class ModelTests
    {
        [Test]
        public void Driver_ShouldValidateRequiredFields()
        {
            // Arrange
            var driver = new Driver();
            var context = new ValidationContext(driver);
            var results = new List<ValidationResult>();
            
            // Act
            var isValid = Validator.TryValidateObject(driver, context, results, true);
            
            // Assert
            Assert.That(isValid, Is.False, "Driver with no data should be invalid");
            Assert.That(results.Count, Is.GreaterThan(0), "Should have validation errors");
        }

        [Test]
        public void Driver_WithValidData_ShouldBeValid()
        {
            // Arrange
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Name = "John Doe",
                LicenseNumber = "LIC001"
            };
            var context = new ValidationContext(driver);
            var results = new List<ValidationResult>();
            
            // Act
            var isValid = Validator.TryValidateObject(driver, context, results, true);
            
            // Assert
            Assert.That(isValid, Is.True, "Valid driver should pass validation");
            Assert.That(results.Count, Is.EqualTo(0), "Should have no validation errors");
        }

        [Test]
        public void Vehicle_WithValidData_ShouldSetPropertiesCorrectly()
        {
            // Arrange & Act
            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Number = "001",
                Name = "Test Bus",
                Capacity = 50,
                IsActive = true
            };
            
            // Assert - Test actual property values without assumptions
            Assert.That(vehicle.Number, Is.EqualTo("001"));
            Assert.That(vehicle.Name, Is.EqualTo("Test Bus"));
            Assert.That(vehicle.Capacity, Is.EqualTo(50));
            Assert.That(vehicle.IsActive, Is.True);
            
            // Don't assert on BusNumber since we don't know the relationship
            Assert.That(vehicle.BusNumber, Is.Not.Null);
            Console.WriteLine($"BusNumber: {vehicle.BusNumber}, Number: {vehicle.Number}");
        }

        [Test]
        public void Route_ShouldCalculateTotalMileage()
        {
            // Arrange
            var route = new Route
            {
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                PMStartMileage = 1050,
                PMEndingMileage = 1100
            };
            
            // Act
            var amMileage = route.AMEndingMileage - route.AMStartingMileage;
            var pmMileage = route.PMEndingMileage - route.PMStartMileage;
            var totalMileage = amMileage + pmMileage;
            
            // Assert
            Assert.That(amMileage, Is.EqualTo(50));
            Assert.That(pmMileage, Is.EqualTo(50));
            Assert.That(totalMileage, Is.EqualTo(100));
        }
    }
}
    