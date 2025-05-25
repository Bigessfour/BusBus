using System;
using NUnit.Framework;
using BusBus.Utils;

namespace BusBus.Tests.Unit
{
    [TestFixture]
    [Category(TestCategories.Unit)]
    public class CalculatorTests
    {
        #region Add Method Tests

        [Test]
        public void Add_WithPositiveNumbers_ReturnsCorrectSum()
        {
            // Arrange
            int a = 5;
            int b = 3;
            int expected = 8;

            // Act
            int result = Calculator.Add(a, b);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Add_WithNegativeNumbers_ReturnsCorrectSum()
        {
            // Arrange
            int a = -5;
            int b = -3;
            int expected = -8;

            // Act
            int result = Calculator.Add(a, b);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Add_WithMixedSignNumbers_ReturnsCorrectSum()
        {
            // Arrange
            int a = 10;
            int b = -3;
            int expected = 7;

            // Act
            int result = Calculator.Add(a, b);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Add_WithZero_ReturnsOtherNumber()
        {
            // Arrange
            int a = 0;
            int b = 42;
            int expected = 42;

            // Act
            int result = Calculator.Add(a, b);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Add_WithMaxValues_HandlesOverflow()
        {
            // Arrange
            int a = int.MaxValue;
            int b = 1;

            // Act & Assert - This will overflow in unchecked context
            // In a real scenario, you might want to handle this differently
            Assert.DoesNotThrow(() => Calculator.Add(a, b));
        }

        #endregion

        #region CalculateMileageDifference Method Tests

        [Test]
        public void CalculateMileageDifference_WithValidRange_ReturnsCorrectDifference()
        {
            // Arrange
            int startMileage = 1000;
            int endMileage = 1250;
            int expected = 250;

            // Act
            int result = Calculator.CalculateMileageDifference(startMileage, endMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateMileageDifference_WithNegativeDifference_ReturnsZero()
        {
            // Arrange - End mileage less than start (invalid scenario)
            int startMileage = 1500;
            int endMileage = 1200;
            int expected = 0;

            // Act
            int result = Calculator.CalculateMileageDifference(startMileage, endMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateMileageDifference_WithSameMileage_ReturnsZero()
        {
            // Arrange
            int startMileage = 1000;
            int endMileage = 1000;
            int expected = 0;

            // Act
            int result = Calculator.CalculateMileageDifference(startMileage, endMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateMileageDifference_WithZeroStart_ReturnsEndMileage()
        {
            // Arrange
            int startMileage = 0;
            int endMileage = 500;
            int expected = 500;

            // Act
            int result = Calculator.CalculateMileageDifference(startMileage, endMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateMileageDifference_WithNegativeValues_HandlesCorrectly()
        {
            // Arrange - Testing edge case with negative mileage values
            int startMileage = -100;
            int endMileage = 200;
            int expected = 300;

            // Act
            int result = Calculator.CalculateMileageDifference(startMileage, endMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateMileageDifference_WithBothNegativeValues_ReturnsZeroWhenInvalid()
        {
            // Arrange
            int startMileage = -50;
            int endMileage = -100;
            int expected = 0;

            // Act
            int result = Calculator.CalculateMileageDifference(startMileage, endMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        #region CalculateTotalDailyMileage Method Tests

        [Test]
        public void CalculateTotalDailyMileage_WithValidInputs_ReturnsSum()
        {
            // Arrange
            int amMileage = 125;
            int pmMileage = 130;
            int expected = 255;

            // Act
            int result = Calculator.CalculateTotalDailyMileage(amMileage, pmMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateTotalDailyMileage_WithZeroValues_ReturnsZero()
        {
            // Arrange
            int amMileage = 0;
            int pmMileage = 0;
            int expected = 0;

            // Act
            int result = Calculator.CalculateTotalDailyMileage(amMileage, pmMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateTotalDailyMileage_WithOnlyAmMileage_ReturnsAmMileage()
        {
            // Arrange
            int amMileage = 150;
            int pmMileage = 0;
            int expected = 150;

            // Act
            int result = Calculator.CalculateTotalDailyMileage(amMileage, pmMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateTotalDailyMileage_WithOnlyPmMileage_ReturnsPmMileage()
        {
            // Arrange
            int amMileage = 0;
            int pmMileage = 175;
            int expected = 175;

            // Act
            int result = Calculator.CalculateTotalDailyMileage(amMileage, pmMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateTotalDailyMileage_WithNegativeValues_HandlesCorrectly()
        {
            // Arrange - Edge case: negative mileage (might represent error conditions)
            int amMileage = -50;
            int pmMileage = 100;
            int expected = 50;

            // Act
            int result = Calculator.CalculateTotalDailyMileage(amMileage, pmMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateTotalDailyMileage_WithLargeValues_ReturnsCorrectSum()
        {
            // Arrange
            int amMileage = 999999;
            int pmMileage = 888888;
            int expected = 1888887;

            // Act
            int result = Calculator.CalculateTotalDailyMileage(amMileage, pmMileage);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        #region Integration/Realistic Scenario Tests

        [Test]
        public void Calculator_RealisticBusRouteScenario_WorksCorrectly()
        {
            // Arrange - Realistic bus route scenario
            int morningStartMileage = 45678;
            int morningEndMileage = 45703;  // 25 miles AM route
            int afternoonStartMileage = 45703;
            int afternoonEndMileage = 45728;  // 25 miles PM route

            // Act
            int amMileage = Calculator.CalculateMileageDifference(morningStartMileage, morningEndMileage);
            int pmMileage = Calculator.CalculateMileageDifference(afternoonStartMileage, afternoonEndMileage);
            int totalDailyMileage = Calculator.CalculateTotalDailyMileage(amMileage, pmMileage);

            // Assert
            Assert.That(amMileage, Is.EqualTo(25));
            Assert.That(pmMileage, Is.EqualTo(25));
            Assert.That(totalDailyMileage, Is.EqualTo(50));
        }

        [Test]
        public void Calculator_ErrorConditionWithInvalidMileage_HandlesGracefully()
        {
            // Arrange - Error condition: odometer reset or reading error
            int startMileage = 50000;
            int endMileage = 100;  // Impossible scenario - odometer went backwards

            // Act
            int mileageDifference = Calculator.CalculateMileageDifference(startMileage, endMileage);

            // Assert - Should return 0 to prevent negative mileage
            Assert.That(mileageDifference, Is.EqualTo(0));
        }

        #endregion
    }
}
