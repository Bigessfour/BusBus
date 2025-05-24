# BusBus Developer Quick Start Guide

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio Code with C# extension
- SQL Server (optional - project uses in-memory DB for tests)

### Building the Project
```bash
# Clone and build
cd BusBus
dotnet build

# Run tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### VS Code Tasks
Use these predefined tasks in VS Code:
- **Build**: `shell: build BusBus solution`
- **Test**: `process: test`
- **Coverage**: `process: Run Tests with Coverage`
- **Coverage Report**: `shell: Generate and Open Coverage Report`

## Testing Standards

### Framework: NUnit Only
- ✅ Use `[Test]` for simple tests
- ✅ Use `[TestCase]` for parameterized tests  
- ✅ Use `[TestFixture]` for test classes
- ✅ Inherit from `TestBase` for database tests
- ✅ Use `Assert.That()` modern syntax

### Test Template
New tests should follow `docs/test-template.cs`:
```csharp
[TestFixture]
public class MyClassTests : TestBase
{
    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        // Additional setup
    }

    [Test]
    public void MyMethod_WhenCondition_ShouldExpectedResult()
    {
        // Arrange
        // Act  
        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
}
```

## Project Structure

```
BusBus/
├── UI/                    # Windows Forms components
├── Models/               # Entity models (Route, Driver, Vehicle)
├── Services/             # Business logic (RouteService)
├── DataAccess/           # Entity Framework DbContext
├── Utils/                # Utilities and helpers
└── BusBus.Tests/         # All test files
    ├── UI/               # UI component tests
    ├── Services/         # Service layer tests
    ├── DataAccess/       # Database tests
    └── Models/           # Model tests
```

## Development Workflow

### Adding New Features
1. **Write Tests First**: Create tests in appropriate `BusBus.Tests` subfolder
2. **Follow TDD**: Red → Green → Refactor
3. **Check Coverage**: Aim for >70% line coverage
4. **Run Full Test Suite**: Ensure no regressions

### Database Changes
1. **Add Migration**: `dotnet ef migrations add MyMigration`
2. **Update Tests**: Modify test data if schema changes
3. **Test In-Memory**: Verify tests still pass with in-memory DB
4. **Test Real DB**: Validate against SQL Server if available

### UI Development
1. **Windows Forms**: Use Visual Studio for form designer
2. **Test Coverage**: Add unit tests for form logic
3. **Event Handling**: Test user interactions
4. **Theme Support**: Ensure new components respect theme system

## Current Coverage Targets

**Current Status**: 38% line coverage, 25% branch coverage

**Target Areas for Improvement**:
- `Dashboard.cs` - UI logic needs more tests
- `RouteService.cs` - Edge cases and error handling
- `RoutePanel.cs` - Form interaction logic
- Exception handling paths - Currently low branch coverage

## Common Commands

```bash
# Build and run
dotnet build && dotnet run

# Test specific class
dotnet test --filter "DashboardTests"

# Generate coverage report
reportgenerator -reports:BusBus.Tests/coverage.cobertura.xml -targetdir:CoverageReport -reporttypes:Html

# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

## Debugging Tips

### Test Failures
- Check Entity Framework context state
- Verify in-memory database vs real database behavior
- Use `dbContext.ChangeTracker.Clear()` to reset tracking

### Build Issues
- Ensure docs/ and scripts/ folders excluded from build
- Check for multiple Main() methods
- Verify package references are consistent

### Coverage Issues
- Use `[ExcludeFromCodeCoverage]` for test utilities
- Focus on public API coverage first
- Branch coverage requires testing all conditional paths

## Next Development Priorities

1. **Increase Test Coverage** - Target 70%+ line coverage
2. **UI Testing** - Add comprehensive Windows Forms tests  
3. **Integration Tests** - Test with real database
4. **Performance** - Optimize database queries and UI responsiveness
5. **Documentation** - Complete API documentation

---
**Last Updated**: May 24, 2025  
**Status**: Ready for active development
