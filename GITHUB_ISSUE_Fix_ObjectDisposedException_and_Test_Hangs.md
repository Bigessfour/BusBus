---
name: Fix ObjectDisposedException and Test Hangs
about: Resolve ObjectDisposedException, stabilize test execution, and improve logging.
title: 'Fix ObjectDisposedException and Test Hangs'
labels: bug, performance, testing
assignees: ''

---

## Objective
Resolve `System.ObjectDisposedException` occurring during application shutdown, stabilize test execution to prevent hangs, ensure Ctrl+C responsiveness, and clean up logging. This impacts the stability of Drivers, Routes, and Vehicles views.

## Tasks

### 1. Fix ObjectDisposedException
- **Dashboard.cs**: Add comprehensive null checks in `Dispose(bool disposing)` and any other relevant methods (e.g., `PerformShutdownAsync`, event handlers) to prevent access to disposed objects like `CancellationTokenSource`, UI controls, timers, or other `IDisposable` fields.
- **ProcessMonitor.cs**: Ensure the `Cleanup()` and `Dispose()` methods check if resources (processes, timers) are already disposed before attempting to stop or dispose of them. Reinforce guards against acting on disposed objects.
- **Testing**:
    - Run `dotnet run` and manually test application shutdown to confirm no `ObjectDisposedException` is logged in `busbus-debug.log`.
    - Write a unit test simulating application shutdown scenarios to verify that no disposal errors occur.

### 2. Enforce Safe Test Runner Usage
- Update `TEST_SAFETY_MEASURES.md` and `OPTIMIZATION_CHANGES.md`:
    - Add prominent warnings against using `dotnet test` directly.
    - Emphasize the mandatory use of `run-tests-safely.ps1`.
- Modify `run-tests-safely.ps1`:
    - Log a clear warning if the script is run locally (not in GitHub Actions and without `-ForceLocal`), recommending GitHub runner setup for safety.
- **Testing**:
    - Execute `.\run-tests-safely.ps1` locally to confirm the new warning appears.
    - Verify Ctrl+C responsiveness during local execution (if forced).
    - Write a script test (e.g., Pester) to verify that `run-tests-safely.ps1` outputs the correct warnings.

### 3. Ensure Test Resource Cleanup
- **Test Database**:
    - Verify that `BusBus.Tests` uses a dedicated test database (e.g., `BusBusDB_Test` or an in-memory alternative) in `AppDbContext` setup for tests. If not, implement it.
- **TestBase.cs**:
    - Ensure all test classes inherit from `TestBase`.
    - Verify `TestBase` includes a `CancellationToken` for tests.
    - Ensure `TestBase.TearDown` or `Dispose` methods properly dispose of `DbContext`, any UI components created for tests, timers, and other `IDisposable` resources. Implement robust disposal logic.
- **Testing**:
    - Run tests using `.\run-tests-safely.ps1`. Monitor Task Manager for lingering `dotnet` or test-related processes.
    - Write integration tests specifically designed to verify that resources are cleaned up after test execution (e.g., check database state, mock object disposal).

### 4. Complete Logging Cleanup
- **Program.cs**:
    - Ensure `BUSBUS_DEBUG_TEST_LOGS` environment variable is checked and set to `false` by default to disable "DEBUG-TEST" prefixed logs.
- **AppDbContext Setup / Sensitive Data Logging**:
    - Modify logging for sensitive data (e.g., in `Program.cs` where `EnableSensitiveDataLogging()` is called) to ensure warnings like "Sensitive data logging is enabled" (07:59:39.075) appear only once per application session, using a static flag.
- **Testing**:
    - Run `dotnet run` and check `busbus-debug.log` to confirm no "DEBUG-TEST" logs appear by default and that sensitive data warnings appear only once.
    - Run tests via `run-tests-safely.ps1` to ensure no logging regressions and that test-specific logs are appropriately controlled.
    - Write a unit test to verify the logging cleanup behavior (e.g., mock logger and check BUSBUS_DEBUG_TEST_LOGS effect, verify single sensitive data warning).

### 5. Incremental Testing Strategy
- After each significant change or PR:
    - Run `dotnet build`.
    - Execute tests: `.\run-tests-safely.ps1 -TestFilter "TestCategory!=LongRunning" -TimeoutMinutes 5`.
    - Confirm no hangs and Ctrl+C responsiveness.
    - Manually test Drivers, Routes, and Vehicles views.
    - (If available) Use SonarQube to check for new warnings or build errors.
    - Monitor Task Manager for .NET processes during and after test runs.

## Success Criteria
- **Exception Resolved**: No `System.ObjectDisposedException` in `busbus-debug.log` or debugger output during normal operation and shutdown.
- **Test Stability**: Tests complete reliably without hangs when executed via `run-tests-safely.ps1`. Ctrl+C effectively stops test runs initiated by the script.
- **Process Control**: No orphaned `dotnet.exe` or `testhost.exe` processes requiring manual termination after test runs.
- **Functionality Preserved**: Drivers, Routes, and Vehicles views remain fully functional.
- **Logging**:
    - "DEBUG-TEST" logs are absent by default in `busbus-debug.log`.
    - Sensitive data warnings appear only once per application run.
- **Test Safety**: Safeguards in `coverlet.runsettings` (timeouts) and `TestBase.cs` (timeouts, cancellation) remain intact and effective.
- **Quality**: (If available) SonarQube scans pass without new critical warnings or build errors related to these changes.
- **Usability**: All documentation updates are clear and easy for a novice programmer to understand.

## Related Documents
- `TEST_SAFETY_MEASURES.md`
- `OPTIMIZATION_CHANGES.md`
- `README.md`
