# BusBus.Tests

This project contains the testing infrastructure for the BusBus application.

## Project Structure

The test project is organized into the following folders:

- **Common**: Contains shared test utilities and base classes
  - `TestBase.cs`: Base class for all tests
  - `DatabaseSetupHelper.cs`: Utilities for database setup and cleanup
  - `MockHelper.cs`: Factory methods for creating mocks

- **UnitTests**: Contains unit tests for individual components
  - `RouteTests.cs`: Tests for the Route entity
  - `RouteServiceTests.cs`: Tests for the RouteService

- **IntegrationTests**: Contains tests that verify multiple components working together
  - `DatabaseConnectionTest.cs`: Tests for database connectivity

- **UITests**: Contains tests for UI components

## Test Setup

Tests in this project use:
- NUnit 3.14.0 as the testing framework
- Moq for mocking dependencies
- Real SQL Server database for integration tests
- Transaction scopes for test isolation

## Running Tests

To run all tests:

```bash
dotnet test --verbosity minimal
```

To run tests with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage" --verbosity minimal
```

To generate a coverage report:

```bash
reportgenerator -reports:./coverage.cobertura.xml -targetdir:./CoverageReport -reporttypes:Html
```

## Test Patterns

### Naming Convention

Tests follow the naming convention:

```
MethodName_WhenCondition_ThenExpectedBehavior
```

### Test Structure

Tests follow the Arrange-Act-Assert pattern:

```csharp
// Arrange
// Set up test data and conditions

// Act
// Call the method being tested

// Assert
// Verify the expected outcome
```

## Common Test Scenarios

### Testing with Moq

```csharp
var mockService = new Mock<IRouteService>();
mockService.Setup(m => m.GetRoutesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(new List<Route>());
```

### Testing Entity Validation

```csharp
var validationContext = new ValidationContext(entity);
var validationResults = new List<ValidationResult>();
bool isValid = Validator.TryValidateObject(entity, validationContext, validationResults, true);
```

### Testing Database Operations

```csharp
var context = GetDbContext();
context.Entities.Add(entity);
await context.SaveChangesAsync();
var result = await context.Entities.FindAsync(entity.Id);
```
