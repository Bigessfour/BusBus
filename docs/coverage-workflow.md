# Coverage Report Viewing Guide

## Preferred Method: Text Summary

To view test coverage results, use the text summary which displays clearly in the terminal:

```cmd
type "c:\Users\steve.mckitrick\Desktop\BusBus\TestResults\CoverageReport\Summary.txt"
```

This provides:
- Overall coverage percentages (Line, Branch, Method)
- Coverage breakdown by component/class
- Clear, readable format without HTML rendering issues

## Coverage Workflow

1. **Run Coverage Analysis:**
   ```cmd
   C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -File run-coverage.ps1
   ```

2. **View Results:**
   ```cmd
   type "c:\Users\steve.mckitrick\Desktop\BusBus\TestResults\CoverageReport\Summary.txt"
   ```

3. **For Detailed Line-by-Line Analysis:**
   - HTML files are available in `TestResults\CoverageReport\`
   - Open individual class files (e.g., `BusBus_Calculator.html`) in external browser if needed

## Current Achievement
- ✅ Calculator: 100% coverage with comprehensive test suite
- 📈 Overall project coverage improved to 7.9% line coverage

## Next Testing Priorities
1. ShutdownMonitor (critical infrastructure)
2. RouteDisplayDTO (simple data model)
3. Theme/UI constants (configuration classes)
