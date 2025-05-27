using System;

namespace BusBus.Tests
{
    public class SimpleTest
    {
        public static void SimpleTest_ShouldPass()
        {
            // Arrange
            var expected = 2;

            // Act
            var actual = 1 + 1;

            // Assert - using simple assertion for now
            if (actual != expected)
            {
                throw new Exception($"Expected {expected}, but got {actual}");
            }
        }
    }
}
