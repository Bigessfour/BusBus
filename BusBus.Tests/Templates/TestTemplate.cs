using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using BusBus.Models;
using BusBus.Services;

namespace BusBus.Tests.Templates
{
    /// <summary>
    /// Template for creating new test classes
    /// </summary>
    [TestFixture]
    public class TestClassTemplate : TestBase
    {
        // Declare mocked dependencies as nullable or required
        private IServiceInterface? _mockService;

        [SetUp]
        public override void Setup()
        {
            // Always call base setup first
            base.Setup();

            // Initialize mocks and dependencies
            _mockService = Substitute.For<IServiceInterface>();
        }

        [Test, Timeout(5000)]
        public void MethodName_Scenario_ExpectedBehavior()
        {
            // Arrange
            // Create objects and set up the test scenario

            // Act
            // Call the method under test

            // Assert
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task AsyncMethodName_Scenario_ExpectedBehavior()
        {
            // Arrange
            // Create objects and set up the test scenario

            // Act
            // Await the async method under test

            // Assert
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [TestCase(1, true)]
        [TestCase(2, false)]
        public void MethodName_WhenGivenParameter_ReturnsExpectedResult(int input, bool expected)
        {
            // Arrange
            // Create objects and set up the test scenario

            // Act
            // Call the method under test with parameters

            // Assert
            Assert.That(actualResult, Is.EqualTo(expected));
        }
    }
}
        }
    }
}
