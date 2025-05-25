using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using BusBus.Tests.Common;
using BusBus.Models;
using System.Linq;

namespace BusBus.Tests.UnitTests
{
    [TestFixture]
    public class RouteTests : TestBase
    {
        [Test]
        public async Task CanSaveAndRetrieveRoute()
        {
            // Arrange
            var context = GetDbContext();
            var newRoute = new Route
            {
                Id = Guid.NewGuid(),
                Name = "Test Route",
                RouteDate = DateTime.Today,
                ScheduledTime = DateTime.Today.AddHours(9),
                StartLocation = "Start Test",
                EndLocation = "End Test",
                AMStartingMileage = 1000,
                AMEndingMileage = 1050,
                PMStartMileage = 1050,
                PMEndingMileage = 1100,
                AMRiders = 25,
                PMRiders = 30
            };

            // Act
            context.Routes.Add(newRoute);
            await context.SaveChangesAsync();
            
            var retrievedRoute = await context.Routes.FindAsync(newRoute.Id);

            // Assert
            Assert.That(retrievedRoute, Is.Not.Null, "Route should be retrieved from database");
            Assert.That(retrievedRoute.Name, Is.EqualTo("Test Route"), "Route name should match");
            Assert.That(retrievedRoute.StartLocation, Is.EqualTo("Start Test"), "Start location should match");
            Assert.That(retrievedRoute.EndLocation, Is.EqualTo("End Test"), "End location should match");
        }
        
        [Test]
        public void Route_RequiredPropertiesValidation()
        {
            // Arrange
            var route = new Route
            {
                // Missing required properties
            };
            
            // Act & Assert
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(route);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(route, validationContext, validationResults, true);
            
            Assert.That(isValid, Is.False, "Route should fail validation when missing required properties");
            TestContext.WriteLine($"Validation errors: {string.Join(", ", validationResults.Select(r => r.ErrorMessage))}");
        }
    }
}
