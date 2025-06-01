# VS Code Insiders Debug Console & Terminal Logging Verification Guide

## Quick Verification Steps

### 1. Build the Application
```powershell
dotnet build
```

### 2. Start Debugging in VS Code Insiders
- Open VS Code Insiders
- Press `F5` or select `Run > Start Debugging`
- Select "Debug BusBus App" configuration

### 3. Expected Log Outputs

#### In Debug Console (`View > Debug Console` or `Ctrl+Shift+D` > Debug Console tab):
```
🔍 [DEBUG-TEST] BusBus Application Starting - Check Debug Console & Terminal!
⚠️ [DEBUG-TEST] This is a WARNING level test log
❌ [DEBUG-TEST] This is an ERROR level test log (not a real error)
🐛 [DEBUG] Direct debug output - should appear in Debug Console
[LIFECYCLE] Application Startup - Active .NET instances: X, BusBus instances: Y, Current PID: ZZZZZ
```

#### In Integrated Terminal (`View > Terminal` or `Ctrl+`` `):
```
📺 [CONSOLE] Direct console output - should appear in Terminal
🔍 [DEBUG-TEST] BusBus Application Starting - Check Debug Console & Terminal!
⚠️ [DEBUG-TEST] This is a WARNING level test log
❌ [DEBUG-TEST] This is an ERROR level test log (not a real error)
```

#### In Output Panel (`View > Output` or `Ctrl+Shift+U`):
- Dropdown: Select ".NET Core" or "Log (Main)"
- Should see similar logs as above

## Troubleshooting

### No Logs in Debug Console
1. **Check AddDebug() Provider**:
   - Verify `builder.AddDebug()` is in Program.cs ✅ (Already configured)

2. **Check launch.json**:
   - Ensure `"console": "integratedTerminal"` ✅ (Updated)

3. **VS Code Insiders Issue**:
   - Try VS Code Stable version to compare
   - Check VS Code Insiders version (Help > About)

### No Logs in Terminal
1. **Check AddConsole() Provider**:
   - Verify `builder.AddConsole()` is in Program.cs ✅ (Already configured)

2. **Log Level Issue**:
   - Check environment: Development = Debug level, Production = Information level
   - Current setting: `DOTNET_ENVIRONMENT=Development` ✅

### Alternative Verification Methods

#### Method 1: Force Enable Test Lifecycle Logging
Set environment variable before debugging:
```powershell
$env:BUSBUS_TEST_LIFECYCLE = "true"
```
Then press F5 to debug.

#### Method 2: Check Output Panel
1. `View > Output` (`Ctrl+Shift+U`)
2. Dropdown: Select ".NET Core"
3. Look for logs there if Debug Console/Terminal are empty

#### Method 3: External Console
Change launch.json temporarily:
```json
"console": "externalTerminal"
```
This opens a separate console window.

## Expected Form Lifecycle Logs

When Dashboard loads and closes:
```
[LIFECYCLE] HighQualityFormTemplate created - Type: Dashboard, PID: XXXXX
[LIFECYCLE] Dashboard created - PID: XXXXX, Thread: 1
[LIFECYCLE] Dashboard closing - Reason: UserClosing, PID: XXXXX
[LIFECYCLE] Dashboard disposing - PID: XXXXX, Thread: 1
[LIFECYCLE] Application Shutdown - Active .NET instances: X, BusBus instances: 0, Current PID: XXXXX
```

## VS Code Insiders Specific Notes

### Known Issues (Jan-Mar 2025)
- Debug Console may not show all logs in Insiders 1.97-1.99
- Workaround: Use integrated Terminal or Output panel
- GitHub issue: https://github.com/microsoft/vscode/issues/XXXXX

### Recommended Settings
Add to VS Code settings.json:
```json
{
    "debug.console.wordWrap": false,
    "debug.console.fontSize": 12,
    "debug.internalConsoleOptions": "openOnSessionStart"
}
```

## Verification Checklist

- [ ] Build succeeds (`dotnet build`)
- [ ] F5 starts debugging without errors
- [ ] Test logs appear in Debug Console
- [ ] Test logs appear in Terminal
- [ ] Form lifecycle logs appear when opening/closing forms
- [ ] Instance count logs show during startup/shutdown
- [ ] No VS Code Insiders-specific issues blocking logging

## Success Criteria

✅ You should see colorful test logs with emojis in both Debug Console and Terminal
✅ Instance counting logs should show .NET process tracking
✅ Form lifecycle logs should appear when Dashboard loads/closes
✅ Different log levels (Info, Warning, Error) should be visible

If any of these fail, check the troubleshooting steps above or try the alternative verification methods.
