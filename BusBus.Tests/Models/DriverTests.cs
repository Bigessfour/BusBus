using NUnit.Framework;
using BusBus.Models;
using System;

namespace BusBus.Tests.Models
{
    [TestFixture]
    public class DriverTests
    {
        [Test]
        public void Driver_Constructor_InitializesProperties()
        {
            // Act
            var driver = new Driver();

            // Assert
            Assert.That(driver.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(driver.FirstName, Is.EqualTo(string.Empty));
            Assert.That(driver.LastName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Driver_ToString_ReturnsFormattedName()
        {
            // Arrange
            var driver = new Driver
            {
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var result = driver.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("John Doe"));
        }

        [Test]
        public void Driver_ToString_HandlesEmptyFirstName()
        {
            // Arrange
            var driver = new Driver
            {
                FirstName = "",
                LastName = "Doe"
            };

            // Act
            var result = driver.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("Doe"));
        }

        [Test]
        public void Driver_ToString_HandlesEmptyLastName()
        {
            // Arrange
            var driver = new Driver
            {
                FirstName = "John",
                LastName = ""
            };

            // Act
            var result = driver.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("John"));
        }

        [Test]
        public void Driver_ToString_HandlesEmptyNames()
        {
            // Arrange
            var driver = new Driver
            {
                FirstName = "",
                LastName = ""
            };

            // Act
            var result = driver.ToString();

            // Assert
            Assert.That(result, Is.EqualTo(""));
        }
    }
}
