using NUnit.Framework;
using BusBus.Models;
using System;

namespace BusBus.Tests.Models
{
    [TestFixture]
    public class VehicleTests
    {
        [Test]
        public void Vehicle_Constructor_InitializesProperties()
        {
            // Act
            var vehicle = new Vehicle { Number = "V-001" };

            // Assert
            Assert.That(vehicle.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(vehicle.BusNumber, Is.EqualTo("V-001"));
        }

        [Test]
        public void Vehicle_ToString_ReturnsBusNumber()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                Number = "BUS-123"
            };

            // Act
            var result = vehicle.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("BUS-123"));
        }

        [Test]
        public void Vehicle_ToString_HandlesEmptyBusNumber()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                Number = ""
            };

            // Act
            var result = vehicle.ToString();

            // Assert
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void Vehicle_SetProperties_StoresCorrectValues()
        {
            // Arrange
            var vehicle = new Vehicle { Number = "V-002" };
            var newId = Guid.NewGuid();

            // Act
            vehicle.Id = newId;
            vehicle.BusNumber = "EXPRESS-42";

            // Assert
            Assert.That(vehicle.Id, Is.EqualTo(newId));
            Assert.That(vehicle.BusNumber, Is.EqualTo("EXPRESS-42"));
        }
    }
}
