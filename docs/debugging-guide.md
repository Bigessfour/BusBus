# BusBus .NET Core Debugging Guide

## Quick Debug Commands

### VS Code Keyboard Shortcuts
- `F5` - Start Debugging
- `Ctrl+F5` - Run Without Debugging
- `F9` - Toggle Breakpoint
- `F10` - Step Over
- `F11` - Step Into
- `Shift+F11` - Step Out
- `Ctrl+Shift+F5` - Restart Debugging
- `Shift+F5` - Stop Debugging

### Debug Configurations Available
1. **".NET Core Launch (Debug)"** - Standard debugging with enhanced logging
2. **".NET Core Launch (Verbose Debug)"** - Maximum logging with stop at entry
3. **".NET Core Attach"** - Attach to running process
4. **"Test Current File"** - Debug specific test file

### Custom Tasks Available
- `build with verbose logging` - Detailed build information
- `clean and rebuild` - Fresh build
- `run with environment logging` - Run with debug environment
- `attach debugger to running process` - Show processes to attach to
- `check database connection` - Test DB connectivity
- `view recent application logs` - Show latest logs
- `generate debug report` - System and project status

## Common Debugging Scenarios

### 1. Cross-Thread Operations (UI Thread Issues)
```csharp
// Use DebugUtils to check thread safety
DebugUtils.LogThreadSafety(logger, "UI Operation");

// Check if on UI thread
if (!DebugUtils.IsOnUIThread())
{
    // Marshal to UI thread
    this.Invoke(() => UpdateUI());
}
```

### 2. Database Context Issues
```csharp
// Enable detailed EF logging in appsettings.Development.json
"Microsoft.EntityFrameworkCore": "Information",
"Microsoft.EntityFrameworkCore.Database.Command": "Information"

// Use scoped services properly
await _serviceProvider.WithScopedServiceAsync<RouteService>(async routeService =>
{
    // Database operations here
    return Task.CompletedTask; // Always return Task
});
```

### 3. Exception Debugging
```csharp
try
{
    // Your code here
}
catch (Exception ex)
{
    DebugUtils.LogExceptionContext(logger, ex);
    throw; // Re-throw to maintain stack trace
}
```

### 4. Performance Debugging
```csharp
using (DebugUtils.TimeOperation(logger, "Database Query"))
{
    // Timed operation here
}
```

## Environment Variables for Enhanced Debugging

Set these in launch.json or terminal:
```
DOTNET_ENVIRONMENT=Development
LOGGING__LOGLEVEL__DEFAULT=Debug
LOGGING__LOGLEVEL__MICROSOFT.ENTITYFRAMEWORKCORE=Information
```

## Breakpoint Best Practices

1. **Conditional Breakpoints**: Right-click breakpoint → Add condition
2. **Hit Count**: Break only after N hits
3. **Log Points**: Log without stopping execution
4. **Exception Breakpoints**: Debug → Windows → Exception Settings

## Debug Console Commands

While debugging, use these in Debug Console:
```
// Evaluate expressions
variableName
methodName()

// Quick evaluation
?expression

// Change variable values (careful!)
variableName = newValue
```

## Common Issues and Solutions

### Issue: "Cross-thread operation not valid"
**Solution**: Use `Invoke()` or `BeginInvoke()` for UI updates from background threads

### Issue: "ObjectDisposedException" with DbContext
**Solution**: Ensure proper scoping with `WithScopedServiceAsync<T>()`

### Issue: Debugger not hitting breakpoints
**Solutions**:
1. Check build configuration (Debug vs Release)
2. Ensure symbols are generated (`<DebugSymbols>true</DebugSymbols>`)
3. Restart OmniSharp: Ctrl+Shift+P → "OmniSharp: Restart OmniSharp"

### Issue: IntelliSense not working
**Solutions**:
1. Check C# extension is installed and enabled
2. Reload window: Ctrl+Shift+P → "Developer: Reload Window"
3. Check `.vscode/settings.json` for correct language server settings

## Logging Levels Guide

- **Trace**: Very detailed logs, including data values
- **Debug**: Detailed flow information
- **Information**: General application flow
- **Warning**: Potentially harmful situations
- **Error**: Error events but application can continue
- **Critical**: Very serious error events

## File Locations

- **Application Logs**: `./logs/` (if configured)
- **OmniSharp Logs**: `./omnisharp.log`
- **Build Logs**: `./build.log` (when using verbose build task)
- **Test Results**: `./TestResults/`
- **Coverage Reports**: `./coveragereport/`

## Useful Debug Watch Expressions

```csharp
// Thread information
System.Threading.Thread.CurrentThread.ManagedThreadId
System.Threading.Thread.CurrentThread.IsBackground

// UI thread check
System.Windows.Forms.Application.MessageLoop

// Current method info
System.Reflection.MethodBase.GetCurrentMethod().Name

// Stack trace
Environment.StackTrace
```

## Remote Debugging (if needed)

1. Build in Debug configuration
2. Copy files to target machine
3. Start application with debugging enabled
4. Use "Attach to Process" in VS Code
5. Select remote process

Remember: Always test in Release configuration before deployment!
