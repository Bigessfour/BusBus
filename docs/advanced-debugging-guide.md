# BusBus Advanced Debugging Guide

This guide provides a comprehensive overview of the debugging tools and techniques available for the BusBus application, specifically focusing on thread safety and database context issues.

## Table of Contents

1. [Introduction](#introduction)
2. [Thread Safety Debugging](#thread-safety-debugging)
3. [Database Context Management](#database-context-management)
4. [Resource Tracking](#resource-tracking)
5. [Diagnostic Tools](#diagnostic-tools)
6. [VS Code Tasks](#vs-code-tasks)
7. [Command Line Arguments](#command-line-arguments)
8. [Best Practices](#best-practices)

## Introduction

The BusBus application has been enhanced with comprehensive debugging tools to help diagnose and fix two primary issues:

1. **Cross-thread operations**: Updating UI controls from background threads without proper thread synchronization
2. **Database context disposal issues**: Problems with DbContext lifecycle management and disposal

This guide explains how to use these tools to identify and fix these issues in the application.

## Thread Safety Debugging

### ThreadSafetyMonitor

The `ThreadSafetyMonitor` class tracks threads and cross-thread operations to help identify potential thread safety issues.

Key features:
- Thread registration and tracking
- Cross-thread operation detection
- UI thread identification
- Thread safety enforcement

Example usage:
```csharp
// Register a background thread
ThreadSafetyMonitor.RegisterThread("Background Data Loading Thread");

// Check if we're on the UI thread
if (!ThreadSafetyMonitor.IsOnUiThread())
{
    // Use the thread-safe UI utilities
    ThreadSafeUI.Invoke(control, () => {
        control.Text = "Updated safely from background thread";
    });
}
```

### ThreadSafeUI

The `ThreadSafeUI` class provides thread-safe methods for updating UI controls from background threads.

Key methods:
- `Invoke`: Synchronously execute an action on the UI thread
- `BeginInvoke`: Asynchronously execute an action on the UI thread
- `UpdateText`: Safely update the Text property of a control
- `Update<T>`: Generic method to update any control property

Example usage:
```csharp
// Safely update a control's text from a background thread
ThreadSafeUI.UpdateText(myLabel, "Updated from background thread");

// Safely invoke a more complex UI update
ThreadSafeUI.Invoke(myForm, () => {
    myForm.Text = "Updated title";
    myForm.BackColor = Color.LightBlue;
    myListBox.Items.Add("New item");
});
```

## Database Context Management

### DbContextManager

The `DbContextManager` class provides a safe way to manage DbContext instances, ensuring proper scoping and disposal.

Key features:
- Thread-safe context access
- Automatic context creation and disposal
- Transient error handling with retry logic
- Safe context resetting

Example usage:
```csharp
// Create a DbContextManager
using var dbManager = new DbContextManager(serviceProvider, logger);

// Execute a database operation
var routes = await dbManager.ExecuteAsync(async db => {
    return await db.Routes.ToListAsync();
});

// Execute with retry for transient errors
var count = await dbManager.ExecuteWithRetryAsync(async db => {
    return await db.Drivers.CountAsync();
}, maxRetries: 3);
```

### ServiceScopeExtensions

Additional extension methods for working with service scopes and database contexts:

```csharp
// Execute an operation with a fresh DbContext
await ServiceScopeExtensions.ExecuteWithFreshContextAsync(serviceProvider,
    async context => {
        // Use context here
        var routes = await context.Routes.ToListAsync();
        return routes;
    });
```

## Resource Tracking

### ResourceTracker

The `ResourceTracker` class helps track disposable resources to ensure proper cleanup and identify potential resource leaks.

Key features:
- Resource tracking with detailed diagnostics
- Automatic resource disposal
- Resource leak detection
- Stack trace capture for resource creation

Example usage:
```csharp
// Track a disposable resource
var font = new Font("Arial", 12);
Guid resourceId = ResourceTracker.TrackResource(font, "Font", "Main form heading font");

// Later, release the resource
ResourceTracker.ReleaseResource(resourceId);

// Automatic tracking and disposal
using (ResourceTracker.CreateAutoTracker(
    new Font("Arial", 12), "Font", "Temporary font"))
{
    // Use the font here
}
// Font is automatically released when the using block ends
```

## Diagnostic Tools

### DiagnosticsTester

The `DiagnosticsTester` form provides interactive tools to test and diagnose various aspects of the application.

Launch from the command line:
```
dotnet run --project BusBus.csproj -- --debug-console
```

Features:
- Thread safety testing
- Database connection testing
- Resource tracking testing
- Cross-thread operation simulation

### Debug Utilities

The `DebugUtils` class provides various debugging utilities:

- Logging context information
- Performance timing
- Thread safety checking
- Debug break points

Example usage:
```csharp
// Log detailed context information
DebugUtils.LogContext(logger);

// Time an operation
using (DebugUtils.TimeOperation(logger, "Database query"))
{
    // Operation to time
    var results = await dbContext.Routes.ToListAsync();
}
```

## VS Code Tasks

Several VS Code tasks have been added to help with debugging:

- **Analyze Thread Safety Issues**: Scans code for potential thread safety issues
- **Check Database Context Disposal**: Identifies potential DbContext disposal issues
- **Check Resource Leaks**: Finds potential resource leaks
- **Verify Thread-Safe UI Updates**: Checks for unsafe UI updates from background threads
- **Debug Database Connection Exhaustively**: Runs detailed database connection tests

To run these tasks:
1. Press `Ctrl+Shift+P` to open the command palette
2. Type "Tasks: Run Task"
3. Select the desired debugging task

## Command Line Arguments

The application supports several command line arguments for debugging:

- `--debug-db`: Run database connection tests
- `--debug-threads`: Launch thread monitoring console
- `--debug-resources`: Launch resource monitoring console
- `--debug-console`: Launch the comprehensive diagnostics tester
- `--verbose`: Enable verbose logging

Example:
```
dotnet run --project BusBus.csproj -- --debug-db --verbose
```

## Best Practices

### Thread Safety

1. **Always check InvokeRequired**: When updating UI controls from background threads
   ```csharp
   if (control.InvokeRequired)
   {
       control.Invoke(() => control.Text = "Updated");
   }
   else
   {
       control.Text = "Updated";
   }
   ```

2. **Use ThreadSafeUI**: Simplifies thread-safe UI updates
   ```csharp
   ThreadSafeUI.UpdateText(control, "Updated safely");
   ```

3. **Register background threads**: For better debugging
   ```csharp
   ThreadSafetyMonitor.RegisterThread("Data Loading Thread");
   ```

### Database Context Management

1. **Use using blocks**: Ensure proper context disposal
   ```csharp
   using (var context = serviceProvider.GetRequiredService<AppDbContext>())
   {
       // Use context here
   }
   ```

2. **Use service scopes**: Create proper scopes for DbContext instances
   ```csharp
   using (var scope = serviceProvider.CreateScope())
   {
       var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
       // Use context here
   }
   ```

3. **Use DbContextManager**: For advanced context management
   ```csharp
   using var dbManager = new DbContextManager(serviceProvider, logger);
   var result = await dbManager.ExecuteAsync(async db => {
       return await db.Routes.ToListAsync();
   });
   ```

### Resource Management

1. **Track disposable resources**: Especially those with longer lifetimes
   ```csharp
   var resourceId = ResourceTracker.TrackResource(resource, "Type", "Description");
   ```

2. **Use auto-tracking**: For automatic resource disposal
   ```csharp
   using (ResourceTracker.CreateAutoTracker(resource, "Type", "Description"))
   {
       // Use resource here
   }
   ```

3. **Check for leaks**: Periodically check for resource leaks
   ```csharp
   ResourceTracker.LogLeakedResources();
   ```

## Conclusion

By using these debugging tools and following the best practices, you can effectively diagnose and fix thread safety and database context issues in the BusBus application. For further assistance, consult the code documentation or run the diagnostic tools described in this guide.
