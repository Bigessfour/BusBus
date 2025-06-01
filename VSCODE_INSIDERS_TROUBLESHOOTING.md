# VS Code Insiders Troubleshooting Guide for BusBus Logging
## Project System Issues and Microsoft.Extensions.Logging Verification

This guide addresses the JSON-RPC connection failures and project system issues while ensuring Microsoft.Extensions.Logging output is still accessible.

## 🚨 Current Issues Detected

### Project System Error Analysis
```
System.AggregateException: Project system data flow 'ProjectBuildSnapshotService' closed
StreamJsonRpc.ConnectionLostException: JSON-RPC connection lost
```

**Root Cause**: C# DevKit extension communication failure with project system server.

## 🔧 Immediate Fixes

### Step 1: VS Code Insiders Reset
```powershell
# Close VS Code Insiders completely
# Run this in PowerShell:
Get-Process "Code - Insiders" -ErrorAction SilentlyContinue | Stop-Process -Force

# Clear extension host cache
Remove-Item "$env:APPDATA\Code - Insiders\logs" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:APPDATA\Code - Insiders\CachedExtensions" -Recurse -Force -ErrorAction SilentlyContinue
```

### Step 2: Disable Problematic Extensions Temporarily
1. Open VS Code Insiders
2. Press `Ctrl+Shift+X` (Extensions)
3. Temporarily disable:
   - C# Dev Kit
   - C# extension
   - IntelliCode for C# Dev Kit
4. Restart VS Code Insiders
5. Re-enable one by one to identify the problematic extension

### Step 3: Alternative Debug Launch
Since the integrated debugging may be affected, use these alternatives:

#### Option A: Manual Terminal Launch
```powershell
# In VS Code Terminal (Ctrl+`)
cd "c:\Users\steve.mckitrick\Desktop\BusBus"
$env:DOTNET_ENVIRONMENT="Development"
dotnet run --project BusBus.csproj --configuration Debug
```

#### Option B: Use Simple Launch Configuration
Create `.vscode/launch-simple.json`:
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Simple BusBus Debug",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/bin/Debug/net8.0-windows/BusBus.exe",
            "args": [],
            "cwd": "${workspaceFolder}",
            "console": "externalTerminal",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Development"
            }
        }
    ]
}
```

## 📍 Microsoft.Extensions.Logging Output Verification

### Confirmed Configuration ✅
Your BusBus application has correctly configured:
- `AddConsole()` ✅
- `AddDebug()` ✅
- `AddSimpleConsole()` ✅
- Test logs with emojis ✅
- Lifecycle tracking ✅

### Output Locations (Despite VS Code Issues)

#### 1. 🔍 Debug Console (Primary)
**Access**: `Ctrl+Shift+D` → Start Debugging → Debug Console tab
**Expected Logs**:
```
🔍 [DEBUG-TEST] BusBus Application Starting
⚠️ [DEBUG-TEST] This is a WARNING level test log
❌ [DEBUG-TEST] This is an ERROR level test log
[LIFECYCLE] Application Startup - Active .NET instances: X
```

#### 2. 📺 Integrated Terminal (Secondary)
**Access**: `Ctrl+` ` (Terminal)
**Expected Logs**:
```
📺 [CONSOLE] Direct console output
Environment: Development
Active .NET instances: X, BusBus instances: Y
```

#### 3. 📋 Output Panel (Alternative)
**Access**: `Ctrl+Shift+U` → Select ".NET Core" or "C#"
**Expected Logs**: Build output, runtime diagnostics

#### 4. 🐛 External Debug Output
**Access**: External debugging tools or DebugView
**Expected**: `Debug.WriteLine()` output

## 🎯 Step-by-Step Verification Process

### Immediate Testing (Run this script)
```powershell
# Navigate to BusBus directory
cd "c:\Users\steve.mckitrick\Desktop\BusBus"

# Run the verification script
.\verify-logging-output.ps1 -QuickTest -FullDiagnostics
```

### Manual Verification Steps

1. **Build Test**:
   ```powershell
   dotnet build BusBus.sln
   ```

2. **Quick Run Test**:
   ```powershell
   $env:DOTNET_ENVIRONMENT="Development"
   dotnet run --project BusBus.csproj --configuration Debug
   ```
   - Look for test logs in first 5 seconds
   - Close after seeing startup logs

3. **VS Code Debug Test** (if extensions working):
   - Press `F5` (Start Debugging)
   - Check Debug Console for test logs
   - Check Terminal for console output

## 🔧 System.ObjectDisposedException Debugging

### Expected Log Pattern
```
[LIFECYCLE] Dashboard disposing - PID: XXXX, Thread: X
[LIFECYCLE] Dashboard resources disposed
[LIFECYCLE] Application Shutdown - Active .NET instances: X
```

### Disposal Issues to Look For
```
❌ Multiple "Dashboard resources disposed" (double disposal)
❌ "ObjectDisposedException" after disposal logs
❌ High instance count at shutdown
```

## 🚨 If Logs Are Missing

### Debug Console Empty
- Check launch.json: `"console": "integratedTerminal"`
- Verify `AddDebug()` in logging configuration
- Try: `Developer: Reset Debug Console` (Command Palette)

### Terminal Empty
- Confirm `AddConsole()` and `AddSimpleConsole()`
- Check environment: `DOTNET_ENVIRONMENT=Development`
- Verify log level: `LogLevel.Debug` minimum

### Output Panel Wrong Channel
- Cycle through dropdown: "Log (Main)", ".NET Core", "C#"
- Look in "Extension Host" for extension errors

## 🎯 Success Criteria

✅ **Verification Complete When You See**:
1. Test logs in Debug Console: `🔍 [DEBUG-TEST]`
2. Console output in Terminal: `📺 [CONSOLE]`
3. Lifecycle logs: `[LIFECYCLE] Application Startup`
4. Instance counting working
5. Dashboard disposal logging without double-disposal
6. No ObjectDisposedException after proper disposal

## 📞 Next Steps
1. Run the verification script: `.\verify-logging-output.ps1 -QuickTest`
2. If VS Code issues persist, use external terminal for debugging
3. Focus on log content rather than VS Code UI issues
4. Use logs to identify and fix the ObjectDisposedException root cause

The logging system is correctly configured - the VS Code UI issues won't prevent the actual logging functionality from working.
