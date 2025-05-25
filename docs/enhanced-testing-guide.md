# BusBus Testing Guide

## Overview

This guide explains the testing approach for the BusBus application. We've implemented a comprehensive testing strategy that includes unit tests, integration tests, and infrastructure for test data generation and assertions.

## Testing Principles

1. **Test Independence**: Each test should be independent and not rely on the state from other tests.
2. **Real Infrastructure**: Integration tests use real SQL Server instances in containers for accurate testing.
3. **Clean State**: Each test starts with a clean database state to avoid test interference.
4. **Test Categories**: Tests are categorized (Unit, Integration, etc.) to allow selective running.
5. **Readable Assertions**: We use FluentAssertions for more readable and maintainable assertions.
6. **Automated Data Generation**: We use AutoFixture to generate test data automatically.

## Test Types

### Unit Tests

Unit tests focus on testing individual components in isolation, typically mocking external dependencies. These tests should be fast and not rely on external resources like databases or network connections.

Example:
```csharp
[TestFixture]
[Category(Unit)]
public class RouteServiceTests
{
    [Test]
    public async Task GetRouteByIdAsync_WhenRouteExists_ShouldReturnRoute()
    {
        // Arrange, Act, Assert
    }
}
```

### Integration Tests

Integration tests verify that components work correctly together with real implementations. Our integration tests use SQL Server containers to test against a real database.

Example:
```csharp
[TestFixture]
[Category(Integration)]
[Category(RequiresSqlServer)]
public class RouteIntegrationTests : DatabaseIntegrationTest
{
    [Test]
    public async Task Create_Route_ShouldPersistToDatabase()
    {
        // Arrange, Act, Assert
    }
}
```

## Test Infrastructure

### SqlServerContainerTest

Base class for tests that need a real SQL Server instance. It manages the lifecycle of a SQL Server container using Testcontainers.

```csharp
public abstract class SqlServerContainerTest : IAsyncDisposable
{
    protected string ConnectionString { get; private set; }
    // ...
}
```

### DatabaseIntegrationTest

Builds on SqlServerContainerTest to provide a clean database for each test. It handles database setup and teardown.

```csharp
public abstract class DatabaseIntegrationTest : SqlServerContainerTest
{
    protected AppDbContext DbContext { get; private set; }
    // ...
}
```

### TestFixtureFactory

Creates test fixtures and test data for use in tests. Uses AutoFixture to generate random data.

```csharp
public class TestFixtureFactory
{
    public T Create<T>()
    {
        // Create instance with random data
    }
    
    public Route CreateRoute(...)
    {
        // Create Route with specific properties
    }
}
```

### AssertionExtensions

Provides extension methods for common assertions using FluentAssertions.

```csharp
public static class AssertionExtensions
{
    public static void ShouldMatchRoute(this Route actual, Route expected)
    {
        // Assert all properties match
    }
}
```

## Test Categories

We use the following test categories:

- **Unit**: Fast tests that don't require external resources
- **Integration**: Tests that validate integration with real external systems
- **E2E**: End-to-end tests that validate complete workflows
- **Performance**: Tests that measure system performance
- **RequiresSqlServer**: Tests that require a SQL Server container
- **Smoke**: Basic functionality tests
- **LongRunning**: Tests that take a long time to run
- **Flaky**: Tests that are known to fail intermittently

## Running Tests

### Running All Tests

```
dotnet test --verbosity minimal
```

### Running Unit Tests Only

```
dotnet test --filter "Category=Unit"
```

### Running Integration Tests Only

```
dotnet test --filter "Category=Integration"
```

### Running Tests with Coverage

```
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## CI/CD Integration

Our GitHub Actions workflow automatically runs tests on every push and pull request. It:

1. Builds the application
2. Runs unit tests
3. Runs integration tests with SQL Server containers
4. Generates a code coverage report
5. Uploads test results and coverage reports as artifacts

## Best Practices

1. **Name tests properly**: Use a naming convention like `[MethodName]_[Scenario]_[ExpectedResult]`
2. **Follow AAA pattern**: Arrange, Act, Assert
3. **Test one thing per test**: Each test should verify a single behavior
4. **Don't test the framework**: Avoid testing framework features that are already tested
5. **Use test data generators**: Use TestFixtureFactory to create test data
6. **Use readable assertions**: Use FluentAssertions for more readable assertions
7. **Categorize your tests**: Apply appropriate category attributes to tests
8. **Clean up after tests**: Ensure tests clean up resources properly

## Extending the Test Suite

When adding new tests:

1. Decide whether you need a unit test or integration test
2. Use the appropriate base class (none for unit tests, DatabaseIntegrationTest for integration)
3. Apply appropriate category attributes
4. Use TestFixtureFactory to generate test data
5. Use AssertionExtensions for readable assertions
6. Follow the AAA pattern
