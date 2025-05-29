# BusBus Dashboard Layout Tests

This test suite is designed to validate the structure, layout, and functionality of the Dashboard and DashboardView components in the BusBus application.

## Purpose

The test specifically verifies:

1. **Panel Structure**: Ensures the dashboard has the correct panel layout with header, side panel, and content areas properly arranged.
2. **Theme Application**: Confirms that the theme colors are correctly applied to all UI elements.
3. **Navigation System**: Tests that navigation between views works correctly through button events.
4. **DataGridView Configuration**: Verifies the shared DataGridView is properly configured with correct settings.
5. **CRUD Panel Setup**: Checks that the CRUD operations panel has the correct buttons and initial state.

## Test Architecture

The test uses MSTest and follows these architectural principles:

- **Mock-Based Testing**: Uses Moq to mock dependencies, avoiding the need for actual database connections.
- **Layout Testing**: Focuses on verifying the structural layout rather than visual appearance.
- **Event-Based Testing**: Tests the navigation event handling mechanisms.
- **Theme Verification**: Ensures consistent theme application across components.

## Running the Tests

To run the tests with a timeout protection mechanism (to prevent freezing), use the provided PowerShell script:

```powershell
.\run-dashboard-layout-test.ps1
```

This script runs the test with a 30-second timeout and terminates the process if it takes too long.

## Manual Test Execution

If you prefer to run the tests manually:

```powershell
dotnet test BusBus.Tests/BusBus.Tests.csproj --filter "FullyQualifiedName~DashboardLayoutTests"
```

## Design Principles

These tests follow Windows Forms best practices from Microsoft documentation:

1. **Layout Verification**: Tests that layout panels are correctly configured for responsive design.
2. **Docking and Anchoring**: Verifies that controls use appropriate docking and anchoring for responsive UI.
3. **Theme Management**: Ensures proper theme application across all UI components.
4. **Event Handling**: Tests the event-driven architecture for navigation and status updates.
5. **Control Configuration**: Verifies DataGridView and other complex controls are properly configured.

## Common Issues

If tests freeze or hang:
- Use the timeout script to automatically terminate tests that exceed 30 seconds
- Check for UI thread blocking operations
- Look for event handler issues that might create infinite loops

## Integration with CI/CD

These tests can be integrated into continuous integration pipelines by calling the timeout script and checking the exit code.
