# BusBus Testing Guide - NUnit Framework

This document provides an overview of the testing strategy for the BusBus application using **NUnit as the exclusive testing framework**, including patterns, utilities, and guidelines for creating new tests.

## Table of Contents

1. [Testing Structure](#testing-structure)
2. [NUnit Framework Setup](#nunit-framework-setup)
3. [Key Testing Components](#key-testing-components)
4. [Test Patterns](#test-patterns)
5. [Creating a New Test](#creating-a-new-test)
6. [Code Coverage](#code-coverage)
7. [Common Testing Scenarios](#common-testing-scenarios)
8. [Using the Test Context Tool](#using-the-test-context-tool)

## Testing Structure

The BusBus application uses **NUnit 3.14.0** as its exclusive testing framework, complemented by:

- **AutoFixture and AutoMoq** for test data generation
- **NSubstitute** for mocking dependencies
- **Entity Framework Core In-Memory Provider** for database testing
- **Coverlet** for code coverage analysis

⚠️ **Important**: This project uses **NUnit exclusively**. Do not mix with xUnit or MSTest frameworks.

Tests are organized in the `BusBus.Tests` project with files structured to mirror the main project:

```
BusBus.Tests/
├── TestBase.cs                 # Base test class with common setup
├── BasicConnectionTest.cs      # Database connectivity tests
├── DashboardTests.cs           # UI component tests
├── RouteListPanelTests.cs      # UI component tests
├── RoutePanelTests.cs          # UI component tests
├── RouteServiceTests.cs        # Service layer tests
├── [Class]Tests.cs             # Tests for specific classes
└── IntegrationTests/           # Integration tests
```

## NUnit Framework Setup

### Package References
The following NUnit packages are configured in `BusBus.Tests.csproj`:
```xml
<PackageReference Include="NUnit" Version="3.14.0" />
<PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.6.0" />
```

### Test Class Structure
All test classes must follow this NUnit pattern:
```csharp
using NUnit.Framework;

namespace BusBus.Tests
{
    [TestFixture]
    public class YourClassTests : TestBase
    {
        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            // Additional setup
        }

        [Test]
        public void MethodName_WhenCondition_ThenExpectedBehavior()
        {
            // Arrange, Act, Assert
        }

        [TearDown]
        public override void TearDown()
        {
            // Cleanup
            base.TearDown();
        }
    }
}
```

## Key Testing Components

### TestBase

The `TestBase` class provides common functionality for tests:

- Sets up dependency injection
- Creates in-memory database instances 
- Configures AutoFixture
- Handles cleanup after tests

```csharp
public abstract class TestBase
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected IFixture Fixture { get; private set; }
    protected AppDbContext DbContext { get; private set; }
    
    [SetUp]
    public virtual void Setup()
    {
        // Sets up services and test data
    }
    
    [TearDown]
    public virtual void TearDown()
    {
        // Cleanup resources
    }
}
```

### In-Memory Database Testing

Tests use EF Core's in-memory database provider to simulate data operations without requiring a real database:

```csharp
protected AppDbContext CreateInMemoryDatabase()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    return new AppDbContext(options);
}
```

## Test Patterns

### 1. Unit Tests

Unit tests follow the Arrange-Act-Assert pattern:

```csharp
[Test]
public void MethodName_TestScenario_ExpectedOutcome()
{
    // Arrange - set up test data and conditions
    var entity = new Entity { Property = "Value" };
    
    // Act - call the method or functionality being tested
    var result = methodUnderTest(entity);
    
    // Assert - verify the expected outcome
    Assert.That(result, Is.EqualTo(expectedValue));
}
```

### 2. Mocking Dependencies

Use NSubstitute for creating mocks:

```csharp
private IRouteService CreateMockRouteService()
{
    return NSubstitute.Substitute.For<IRouteService>();
}
```

For more complex mocking with Moq:

```csharp
var mockService = new Mock<IRouteService>();
mockService.Setup(x => x.GetRoutesAsync())
    .ReturnsAsync(new List<Route> { /* test data */ });
```

### 3. Asynchronous Tests

For async method testing:

```csharp
[Test]
public async Task AsyncMethod_Scenario_ExpectedOutcome()
{
    // Arrange
    var service = _serviceProvider.GetRequiredService<IService>();
    
    // Act
    var result = await service.DoSomethingAsync();
    
    // Assert
    Assert.That(result, Is.Not.Null);
}
```

### 4. Exception Testing

For testing exceptions:

```csharp
[Test]
public void Method_UnderCondition_ThrowsException()
{
    // Arrange
    var service = CreateServiceWithErrorCondition();
    
    // Act & Assert
    var exception = Assert.ThrowsAsync<ExpectedException>(() => service.MethodThatThrows());
    Assert.That(exception.Message, Does.Contain("expected error message"));
}
```

## Creating a New Test

When creating a new test:

1. **Identify the appropriate test file**:
   - Create a new file named `[Class]Tests.cs` if testing a new class
   - Add to an existing file if testing a new method of an already-tested class

2. **Choose the right base class**:
   - Inherit from `TestBase` for most tests
   - Create a specialized setup if you need a different configuration

3. **Test naming convention**:
   - Use the format: `MethodName_Scenario_ExpectedOutcome`
   - Example: `GetRoutes_WithActiveFilter_ReturnsOnlyActiveRoutes`

4. **Test implementation steps**:
   - Set up dependencies and test data
   - Execute the code under test
   - Assert the expected results
   - Clean up any resources not handled by TearDown

### Example: Creating a new service test

```csharp
[Test]
public async Task GetActiveRoutes_WithNoFilter_ReturnsAllActiveRoutes()
{
    // Arrange
    using var context = CreateInMemoryDatabase();
    
    // Add test data
    context.Routes.Add(new Route { Id = 1, Name = "Route 1", IsActive = true });
    context.Routes.Add(new Route { Id = 2, Name = "Route 2", IsActive = false });
    await context.SaveChangesAsync();
    
    var service = new RouteService(() => context);
    
    // Act
    var result = await service.GetActiveRoutesAsync();
    
    // Assert
    Assert.That(result.Count, Is.EqualTo(1));
    Assert.That(result[0].Name, Is.EqualTo("Route 1"));
}
```

### Example: Creating a new UI component test

```csharp
[Test]
public void RoutePanel_WhenRouteIsSelected_DisplaysRouteDetails()
{
    // Arrange
    var mockRouteService = NSubstitute.Substitute.For<IRouteService>();
    var route = new Route { Id = 1, Name = "Test Route" };
    
    // Act
    var panel = new RoutePanel(mockRouteService);
    panel.DisplayRoute(route);
    
    // Assert
    Assert.That(panel.CurrentRouteId, Is.EqualTo(1));
    Assert.That(panel.RouteNameLabel.Text, Is.EqualTo("Test Route"));
}
```

## Code Coverage

Tests generate code coverage reports using Coverlet. Coverage reports are important to:

1. Identify untested code
2. Ensure high-value components have thorough test coverage
3. Track testing progress over time

To run tests with coverage:

```cmd
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

To generate a HTML coverage report:

```cmd
reportgenerator -reports:./BusBus.Tests/coverage.cobertura.xml -targetdir:./CoverageReport -reporttypes:Html
```

## Using the Test Context Tool

To help streamline test creation and provide better context, the BusBus project includes a Test Context Collection Tool. This tool gathers information about your codebase and testing to provide a comprehensive view of what needs to be tested.

### When to Use the Test Context Tool

Use this tool when:
1. Starting to create tests for a new class
2. Wanting to understand the current test coverage
3. Looking for examples of existing test patterns
4. Working with an AI assistant to create tests

### How to Use the Tool

#### From Command Line

```cmd
# Generate a general test context report
prepare-test-context.cmd

# Generate a report for a specific class
prepare-test-context.cmd ClassName
```

#### From VS Code Tasks

1. Press `Ctrl+Shift+P` to open the Command Palette
2. Type `Tasks: Run Task`
3. Select `Prepare Test Context`
4. If prompted, enter the name of the class to analyze

### Understanding the Test Context Report

The generated report includes:

1. **Coverage Summary**: Overall code coverage metrics for the project
2. **Class Information**: Details about the specific class being tested
   - File path and coverage metrics
   - Class signature
   - Method signatures
3. **Existing Tests**: Information about existing tests for the class or similar tests
4. **Test Patterns Reference**: Summary of testing patterns used in the project
5. **Recommended Testing Approach**: Suggestions for testing the class

### Using the Report with AI Assistance

When working with an AI assistant to create tests:

1. Generate the test context report for your class
2. Share the report with the assistant
3. Ask for help designing tests based on this context

The assistant will be able to quickly understand:
- What you're trying to test
- How similar components are tested in your project
- Which methods need coverage
- What patterns to follow for consistency

This approach ensures that new tests align with your existing testing practices while covering all necessary functionality.

## Common Testing Scenarios

### 1. Testing Database Operations

```csharp
[Test]
public async Task Database_WhenSavingEntity_CanRetrieveItCorrectly()
{
    // Arrange
    using var context = CreateInMemoryDatabase();
    var entity = new Route { Name = "New Route", Description = "Test" };
    
    // Act
    context.Routes.Add(entity);
    await context.SaveChangesAsync();
    
    var retrievedEntity = await context.Routes.FirstOrDefaultAsync(r => r.Name == "New Route");
    
    // Assert
    Assert.That(retrievedEntity, Is.Not.Null);
    Assert.That(retrievedEntity.Description, Is.EqualTo("Test"));
}
```

### 2. Testing UI Event Handlers

```csharp
[Test]
public void RoutePanel_WhenRouteSelectionChanges_FiresEvent()
{
    // Arrange
    var mockRouteService = NSubstitute.Substitute.For<IRouteService>();
    var panel = new RoutePanel(mockRouteService);
    
    int eventCallCount = 0;
    var selectedRoute = new Route { Id = 5 };
    
    panel.RouteSelected += (sender, args) => {
        eventCallCount++;
        Assert.That(args.RouteId, Is.EqualTo(5));
    };
    
    // Act
    panel.TriggerRouteSelection(selectedRoute);
    
    // Assert
    Assert.That(eventCallCount, Is.EqualTo(1));
}
```

### 3. Testing Cancellation Tokens

```csharp
[Test]
public void ServiceMethod_WithCancellationToken_RespectsToken()
{
    // Arrange
    var cancellationTokenSource = new CancellationTokenSource();
    var service = _serviceProvider.GetRequiredService<IRouteService>();
    cancellationTokenSource.Cancel();

    // Act & Assert
    Assert.ThrowsAsync<OperationCanceledException>(
        () => service.GetRoutesAsync(cancellationTokenSource.Token));
}
```
