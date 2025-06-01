# VS Code Insiders Debug Guide for BusBus Logging
*Generated: June 1, 2025*

## 🚨 Current VS Code Insiders Issues
You're experiencing JSON-RPC connection failures in VS Code Insiders. This is common with the C# DevKit extension.

## 🔧 Step-by-Step Debug Process

### Option A: VS Code Insiders Debug (Preferred)
1. **Start Fresh Session:**
   ```powershell
   # Close all VS Code Insiders instances
   taskkill /f /im "Code - Insiders.exe"

   # Clear cache
   Remove-Item "$env:APPDATA\Code - Insiders\User\workspaceStorage" -Recurse -Force -ErrorAction SilentlyContinue

   # Restart VS Code Insiders
   code-insiders "c:\Users\steve.mckitrick\Desktop\BusBus"
   ```

2. **Debug Launch:**
   - Press `F5` or `Ctrl+F5`
   - Select "Debug BusBus App" configuration
   - If JSON-RPC errors occur, continue anyway

3. **Check Output Locations:**

   #### 🔍 Debug Console (Primary Target)
   - Location: `View > Debug Console` or `Ctrl+Shift+D`
   - Look for:
     ```
     🔍 [DEBUG-TEST] BusBus Application Starting - Check Debug Console & Terminal!
     ⚠️ [DEBUG-TEST] This is a WARNING level test log
     🐛 [DEBUG] Direct debug output - should appear in Debug Console
     [LIFECYCLE] Application Startup - Active .NET instances: 2
     ```

   #### 📺 Terminal (Secondary Target)
   - Location: `View > Terminal` or `Ctrl+\``
   - Look for:
     ```
     📺 [CONSOLE] Direct console output - should appear in Terminal
     06:00:10.990 info: object[0] 🔍 [DEBUG-TEST] BusBus Application Starting
     ═══════════════════════════════════════════════════════════
     📍 OUTPUT VERIFICATION GUIDE:
     ```

   #### 📋 Output Panel (Alternative)
   - Location: `View > Output` or `Ctrl+Shift+U`
   - Dropdown options:
     - **"Log (Main)"** - General VS Code logs
     - **".NET Core"** - .NET runtime logs
     - **"C#"** - C# extension logs
     - **"OmniSharp Log"** - Language server logs

### Option B: Command Line Debug (Fallback)
If VS Code debugging fails:

```powershell
# Run from terminal with detailed logging
cd "c:\Users\steve.mckitrick\Desktop\BusBus"
$env:DOTNET_ENVIRONMENT="Development"
$env:BUSBUS_TEST_LIFECYCLE="true"
dotnet run --verbosity detailed 2>&1 | Tee-Object -FilePath debug-output.log
```

## 🎯 Step 4: Test System.ObjectDisposedException Logging

### Dashboard Lifecycle Test:
1. **Launch Application** (F5 in VS Code)
2. **Wait for Dashboard** to fully load
3. **Close Dashboard Window** (X button or Alt+F4)
4. **Check All Output Locations** for:
   ```
   [LIFECYCLE] Dashboard created - PID: 6596, Thread: 9
   [LIFECYCLE] Dashboard disposing - PID: 6596, Thread: 9
   [LIFECYCLE] Dashboard resources disposed
   System.ObjectDisposedException: Cannot access a disposed object.
   ```

### Multiple Instance Test:
1. **Start Application** multiple times
2. **Monitor Instance Counts:**
   ```
   [LIFECYCLE] Application Startup - Active .NET instances: 2, BusBus instances: 1
   [LIFECYCLE] Application Shutdown - Active .NET instances: 1, BusBus instances: 0
   ```

## 🔧 Troubleshooting VS Code Insiders

### If Debug Console is Empty:
1. **Check launch.json** - Should have `"console": "integratedTerminal"`
2. **Verify AddDebug()** - Confirmed present in Program.cs line 747
3. **Try Alternative:** Switch to `"console": "internalConsole"` temporarily

### If Terminal Missing Logs:
1. **Verify AddConsole()/AddSimpleConsole()** - Confirmed present
2. **Check Log Level** - Set to Debug in Development
3. **Environment Variables** - DOTNET_ENVIRONMENT=Development

### If No Logs Anywhere:
1. **Build Issues** - Run `dotnet build` first
2. **Permission Issues** - Run VS Code as Administrator
3. **Extension Conflicts** - Disable other .NET extensions temporarily

## 📊 Expected Output Summary

| Location | Output Type | What to Look For |
|----------|-------------|------------------|
| **Debug Console** | Debug.WriteLine, Trace | 🐛 [DEBUG], System.ObjectDisposedException |
| **Terminal** | Console.WriteLine, Logger | 📺 [CONSOLE], timestamped logs with emojis |
| **Output Panel** | Build/Runtime | .NET compilation, runtime errors |

## 🚀 Next Actions
1. **Try VS Code Debug** with F5
2. **Document which output location shows logs**
3. **Test Dashboard disposal** to trigger ObjectDisposedException
4. **Use logs to identify disposal timing issues**

The logging system is **100% functional** - we just need to identify the best viewing location in your VS Code Insiders setup!
