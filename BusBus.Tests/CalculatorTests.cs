using NUnit.Framework;
using System;
using BusBus.Utils;

namespace BusBus.Tests
{
    [TestFixture]
    public class CalculatorTests
    {
        [Test, Timeout(5000)]
        public void ShouldAddTwoNumbers()
        {
            // Arrange & Act
            var result = Calculator.Add(2, 3);

            // Assert
            Assert.That(result, Is.EqualTo(5));
        }
        [TestCase(0, 0, 0)]
        [TestCase(1, 1, 2)]
        [TestCase(-1, 1, 0)]
        public void ShouldAddMultipleNumberCombinations(int a, int b, int expected)
        {
            // Arrange & Act
            var result = Calculator.Add(a, b);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
