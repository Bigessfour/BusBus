# BusBus Logging Maintenance Guide - VS Code Insiders

## Current Status: ✅ VERIFIED AND OPERATIONAL

**Date:** June 1, 2025
**Status:** Microsoft.Extensions.Logging fully operational in VS Code Insiders
**Verified Output Locations:** Debug Console, Terminal, Output Panel
**Ready For:** System.ObjectDisposedException debugging

---

## Quick Verification Checklist

### ✅ Confirmed Working
- [x] Microsoft.Extensions.Logging setup in Program.cs
- [x] AddDebug() and AddSimpleConsole() providers active
- [x] Debug Console output (View > Debug Console)
- [x] Terminal timestamped logs
- [x] Output Panel (.NET Core) logs
- [x] System.ObjectDisposedException debugging ready

### 🔄 Regular Maintenance Tasks

## Step 1: Verify Logging Setup Remains Active

**Quick Check:**
```powershell
# Confirm logging configuration
dotnet build BusBus.sln
# Should compile without errors
```

**Program.cs Verification:**
```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddSimpleConsole(options => { options.TimestampFormat = "HH:mm:ss.fff "; });
    builder.AddDebug();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

## Step 2: Debug Session Startup

### Primary Method (VS Code Insiders)
1. Press `F5` or Run > Start Debugging
2. Select "Debug BusBus App" configuration

### Fallback Method (Terminal)
```powershell
cd "c:\Users\steve.mckitrick\Desktop\BusBus"
$env:DOTNET_ENVIRONMENT="Development"
$env:BUSBUS_TEST_LIFECYCLE="true"
dotnet run 2>&1 | Tee-Object -FilePath debug-session.log
```

## Step 3: Output Location Reference

### 🎯 Primary: Debug Console
- **Access:** View > Debug Console (`Ctrl+Shift+D`)
- **Expected:** Lifecycle logs, exceptions, test markers
- **Look For:**
  - `🔍 [DEBUG-TEST] BusBus Application Starting`
  - `❌ [DEBUG-TEST] This is an ERROR level test log`
  - `[LIFECYCLE] Dashboard disposing`

### 🔄 Secondary: Terminal
- **Access:** View > Terminal (`Ctrl+``)
- **Expected:** Timestamped console logs
- **Format:** `HH:mm:ss.fff 🔍 [DEBUG-TEST] Message`

### 📋 Tertiary: Output Panel
- **Access:** View > Output (`Ctrl+Shift+U`)
- **Dropdown Options:**
  - ".NET Core"
  - "Log (Main)"
  - "OmniSharp Log"
- **Search:** "DEBUG-TEST", "System.ObjectDisposedException"

### 📄 File Logging (If Enabled)
- **Terminal Log:** `debug-session.log`
- **App Log:** `busbus.log` (if configured in appsettings.json)

## Step 4: System.ObjectDisposedException Debugging

### Current Fix Implementation
```csharp
// In BusBus.UI.Dashboard
private bool _disposed = false;
protected override void Dispose(bool disposing)
{
    if (_disposed) return;
    _disposed = true;
    if (disposing)
    {
        _logger.LogInformation("Dashboard resources disposed");
        if (_timer != null)
        {
            _logger.LogDebug("Stopping performance timer");
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }
    }
    base.Dispose(disposing);
}
```

### Monitoring Points
- Single "Dashboard resources disposed" message
- No duplicate disposal attempts
- Clean shutdown with "ProcessMonitor cleanup complete"
- Final count: "Active .NET instances: 0" (ideal)

## Step 5: Troubleshooting Missing Logs

### Issue: No Debug Console Output
**Solutions:**
1. Verify `AddDebug()` in Program.cs
2. Check `LogLevel.Debug` minimum level
3. Run `setup-debug-logging.ps1`
4. Verify launch.json: `"console": "integratedTerminal"`

### Issue: No Terminal Output
**Solutions:**
1. Confirm `AddSimpleConsole()` in Program.cs
2. Check environment: `$env:DOTNET_ENVIRONMENT="Development"`
3. Use terminal debugging fallback

### Issue: JSON-RPC Errors
**Solutions:**
1. Disable C# Dev Kit extension
2. Enable OmniSharp extension
3. Use terminal debugging method
4. Check VS Code Insiders known issues

### Issue: No Output Panel Logs
**Solutions:**
1. Cycle through dropdown options
2. Filter search for "BusBus" or "DEBUG-TEST"
3. Clear and restart debugging session

## Quick Commands Reference

```powershell
# Build and verify
dotnet build BusBus.sln

# Run with full logging
$env:DOTNET_ENVIRONMENT="Development"
$env:BUSBUS_TEST_LIFECYCLE="true"
dotnet run

# Check for processes
Get-Process | Where-Object {$_.ProcessName -like "*BusBus*"}

# Setup debug logging
.\setup-debug-logging.ps1

# Verify logging output
.\verify-logging-output.ps1
```

## Success Indicators

### ✅ All Systems Go
- Build completes without errors
- Debug session starts successfully
- Test logs appear in Debug Console
- Timestamped logs in Terminal
- Clean application shutdown
- No System.ObjectDisposedException

### ⚠️ Need Investigation
- Missing logs in any output location
- JSON-RPC connection errors
- System.ObjectDisposedException still occurring
- Multiple "Dashboard resources disposed" messages

---

## Debugging Scenario: System.ObjectDisposedException

**Objective**: Diagnose and fix `System.ObjectDisposedException` during Dashboard shutdown using Microsoft.Extensions.Logging output across all available locations.

### Prerequisites
- ✅ Microsoft.Extensions.Logging verified working
- ✅ Debug Console, Terminal, and Output Panel accessible
- ✅ VS Code Insiders with F5 debugging functional

### Step-by-Step Debugging Process

#### 1. Start Debugging Session
**Action:**
- Press `F5` in VS Code Insiders, select "Debug BusBus App"
- Open Debug Console (`Ctrl+Shift+D`)
- Open Terminal (`Ctrl+``) for timestamped logs
- Open Output Panel (`Ctrl+Shift+U`) and select ".NET Core"

**Expected Initial Logs:**
```
🔍 [DEBUG-TEST] BusBus Application Starting
[LIFECYCLE] Application initialized
[LIFECYCLE] Dashboard created
```

#### 2. Trigger the Exception
**Action:**
- Navigate to Dashboard in the running application
- Close Dashboard using the X button or File > Exit
- Monitor all output locations simultaneously

**Watch For:**
- Debug Console: Exception stack trace after "[LIFECYCLE] Dashboard disposing"
- Terminal: Timestamped disposal attempts
- Output Panel: .NET runtime errors

#### 3. Analyze Log Patterns

**🔍 Normal Shutdown Pattern:**
```
[LIFECYCLE] Dashboard disposing
Dashboard resources disposed
[LIFECYCLE] ProcessMonitor cleanup starting
[LIFECYCLE] ProcessMonitor cleanup complete
[LIFECYCLE] Application Shutdown - Active .NET instances: 1
```

**❌ Problem Pattern (Double Disposal):**
```
[LIFECYCLE] Dashboard disposing
Dashboard resources disposed
Dashboard resources disposed  <-- DUPLICATE
System.ObjectDisposedException: Cannot access a disposed object
   at BusBus.UI.Dashboard.Dispose(Boolean disposing)
[LIFECYCLE] ProcessMonitor cleanup complete
[LIFECYCLE] Application Shutdown - Active .NET instances: 5  <-- LINGERING
```

**🔍 Timer-Related Issues:**
```
[DEBUG] Stopping performance timer
System.ObjectDisposedException: Timer has been disposed
   at System.Threading.Timer.Change(Int32 dueTime, Int32 period)
```

#### 4. Set Strategic Breakpoints
**Locations:**
- `BusBus.UI.Dashboard.Dispose(bool disposing)` - Entry point
- Timer disposal code - Where exception occurs
- `BusBus.Utils.ProcessMonitor.OnApplicationExit` - Event handler

**Debug Process:**
1. Set breakpoints, restart debugging (`F5`)
2. Navigate to Dashboard and trigger closure
3. Step through disposal sequence
4. Monitor variable states in Debug Console

#### 5. Identify Root Causes

**Common Issues:**
- **Double Disposal**: Multiple calls to `Dispose()` without guard
- **Timer Access**: Accessing disposed timer in background thread
- **Event Handlers**: Lingering event subscriptions causing callbacks
- **Resource Cleanup Order**: Incorrect sequence of resource disposal

#### 6. Apply Comprehensive Fix

**Dashboard Disposal Guard:**
```csharp
// In BusBus.UI.Dashboard
private bool _disposed = false;
private readonly object _disposeLock = new object();

protected override void Dispose(bool disposing)
{
    lock (_disposeLock)
    {
        if (_disposed)
        {
            _logger.LogDebug("Dashboard already disposed - skipping");
            return;
        }
        _disposed = true;
    }

    if (disposing)
    {
        _logger.LogInformation("Dashboard resources disposed");

        if (_timer != null)
        {
            _logger.LogDebug("Stopping performance timer");
            try
            {
                _timer.Stop();
                _timer.Dispose();
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogDebug("Timer already disposed: {Message}", ex.Message);
            }
            finally
            {
                _timer = null;
            }
        }

        // Detach event handlers
        if (_processMonitor != null)
        {
            _logger.LogDebug("Detaching process monitor events");
            _processMonitor.ProcessExit -= OnProcessExit;
        }
    }

    base.Dispose(disposing);
    _logger.LogDebug("Dashboard base disposal complete");
}
```

**ProcessMonitor Event Cleanup:**
```csharp
// In BusBus.Utils.ProcessMonitor
public void Dispose()
{
    if (!_disposed)
    {
        _logger.LogDebug("ProcessMonitor cleanup starting");

        // Detach all event handlers before disposal
        try
        {
            Application.ApplicationExit -= OnApplicationExit;
            _logger.LogDebug("Application exit handler detached");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error detaching exit handler: {Message}", ex.Message);
        }

        _disposed = true;
        _logger.LogDebug("ProcessMonitor cleanup complete");
    }
}
```

#### 7. Verification Testing

**Test Process:**
1. Rebuild application (`dotnet build`)
2. Start fresh debug session (`F5`)
3. Load Dashboard, then close it
4. Monitor all output locations

**✅ Success Indicators:**
```
[LIFECYCLE] Dashboard disposing
Dashboard resources disposed           <-- SINGLE occurrence
[DEBUG] Stopping performance timer    <-- Clean timer disposal
[DEBUG] Application exit handler detached
[LIFECYCLE] ProcessMonitor cleanup complete
[LIFECYCLE] Application Shutdown - Active .NET instances: 1  <-- Clean shutdown
```

**❌ Still Need Investigation:**
- Multiple "Dashboard resources disposed" messages
- Any `System.ObjectDisposedException` in Debug Console
- "Active .NET instances" count > 1 at shutdown
- Timer-related exceptions in Output Panel

#### 8. Advanced Debugging Techniques

**Memory Leak Detection:**
```csharp
// Add to Program.cs for advanced monitoring
_logger.LogInformation("GC Memory before disposal: {Memory} KB",
    GC.GetTotalMemory(false) / 1024);
GC.Collect();
GC.WaitForPendingFinalizers();
_logger.LogInformation("GC Memory after cleanup: {Memory} KB",
    GC.GetTotalMemory(true) / 1024);
```

**Process Monitoring:**
```powershell
# Run alongside debugging to monitor process lifecycle
Get-Process | Where-Object {$_.ProcessName -like "*BusBus*"} |
    Select-Object ProcessName, Id, WorkingSet, Handles
```

### Output Location Summary for Debugging

| Location | Access | Best For | Search Terms |
|----------|--------|----------|--------------|
| **Debug Console** | `Ctrl+Shift+D` | Exception stack traces, lifecycle logs | `System.ObjectDisposedException`, `[LIFECYCLE]` |
| **Terminal** | `Ctrl+`` | Timestamped sequence analysis | `Dashboard resources disposed`, Timer logs |
| **Output Panel** | `Ctrl+Shift+U` → ".NET Core" | .NET runtime errors, GC information | `ObjectDisposedException`, `Finalizer` |
| **Log File** | `debug-session.log` | Complete session review | Full exception context |

### Quick Verification Command
```powershell
# Use the verification script to confirm logging works
.\verify-busbus-logging.ps1 -Detailed
```

**Expected Output:** No `System.ObjectDisposedException`, single disposal messages, clean shutdown with "Active .NET instances: 1"

---

## Quick Verification Command

```powershell
# One-liner to verify logging is working
cd "c:\Users\steve.mckitrick\Desktop\BusBus" && dotnet build && echo "Build OK - Ready for F5 debug"
```

**Last Verified:** June 1, 2025
**Next Check:** When debugging issues or after VS Code updates
