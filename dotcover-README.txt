# JetBrains dotCover Setup for BusBus

## 1. Install dotCover
- Download from: https://www.jetbrains.com/dotcover/download/
- Install to the default location (C:\Program Files\JetBrains\dotCover\dotCover.exe) or note your custom path.

## 2. Run Coverage and Generate HTML Report
- Use the provided PowerShell script:

    pwsh -NoProfile -ExecutionPolicy Bypass -File ./run-dotcover-coverage.ps1

- This will:
    - Run your tests with coverage
    - Generate CoverageReport\dotCover.html
    - Open the report in Chrome

## 3. If dotCover is not in the default location
- Edit `run-dotcover-coverage.ps1` and update the `$dotCoverPath` variable to your actual install path.

## 4. Troubleshooting
- See: https://www.jetbrains.com/help/dotcover/Getting_Started_with_dotCover.html
- For command-line options: https://www.jetbrains.com/help/dotcover/Coverage-Analysis-with-Command-Line-Tool.html

## 5. Coverlet is fully uninstalled
- No conflicts with previous coverage tools.

---

You are now ready to use dotCover for accurate coverage, including Windows Forms and integration tests.
