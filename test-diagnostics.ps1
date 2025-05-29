# Diagnose and run tests with timeout protection
# This script helps identify which tests might be causing hanging issues

param(
    [Parameter(Mandatory=$false)]
    [string]$TestCategory = "",

    [Parameter(Mandatory=$false)]
    [string]$TestName = "",

    [Parameter(Mandatory=$false)]
    [string]$TestFile = "",

    [Parameter(Mandatory=$false)]
    [int]$TimeoutSeconds = 30,

    [switch]$ListTests = $false,

    [switch]$ListCategories = $false,

    [switch]$DiagnoseHangingTests = $false,

    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"
$global:hangingTestsDetected = @()

# Colors for better readability
$colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "Cyan"
    Debug = "Gray"
}

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color
    )

    Write-Host $Message -ForegroundColor $Color
}

function Run-ProcessWithTimeout {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [int]$Timeout,
        [string]$Description
    )

    Write-ColorMessage "Running: $Description" $colors.Info
    Write-ColorMessage "Command: $Command $($Arguments -join ' ')" $colors.Debug
    Write-ColorMessage "Timeout: ${Timeout}s" $colors.Debug

    $startTime = Get-Date
    $process = Start-Process -FilePath $Command -ArgumentList $Arguments -NoNewWindow -PassThru

    # Wait for completion with timeout
    $completed = $false
    $outputLog = ""

    try {
        # Check if process completes before timeout
        while (-not $completed -and ((Get-Date) - $startTime).TotalSeconds -lt $Timeout) {
            $completed = $process.HasExited
            if (-not $completed) {
                Start-Sleep -Milliseconds 100
            }
        }

        if (-not $completed) {
            Write-ColorMessage "`nPROCESS TIMEOUT DETECTED after $Timeout seconds." $colors.Error
            Write-ColorMessage "Forcibly terminating process..." $colors.Warning

            try {
                $process.Kill($true)  # Kill process tree
                Write-ColorMessage "Process terminated successfully." $colors.Warning

                # Record hanging test info
                if ($Description -match "Test: (.+)") {
                    $global:hangingTestsDetected += $Matches[1]
                }
            }
            catch {
                Write-ColorMessage "Failed to terminate process gracefully. Trying alternative method..." $colors.Error

                # Use more aggressive approach
                Start-Process -FilePath "taskkill" -ArgumentList "/F", "/T", "/PID", $process.Id -NoNewWindow -Wait
            }

            return @{
                Success = $false
                ExitCode = -1
                TimedOut = $true
                RunTime = [math]::Round(((Get-Date) - $startTime).TotalSeconds, 2)
                Output = "PROCESS TIMED OUT AND WAS TERMINATED"
            }
        }
        else {
            # Process completed naturally
            $exitCode = $process.ExitCode
            $runTime = [math]::Round(((Get-Date) - $startTime).TotalSeconds, 2)

            return @{
                Success = ($exitCode -eq 0)
                ExitCode = $exitCode
                TimedOut = $false
                RunTime = $runTime
                Output = $outputLog
            }
        }
    }
    catch {
        Write-ColorMessage "Error during process execution: $($_.Exception.Message)" $colors.Error
        return @{
            Success = $false
            ExitCode = -1
            TimedOut = $false
            RunTime = [math]::Round(((Get-Date) - $startTime).TotalSeconds, 2)
            Output = "ERROR: $($_.Exception.Message)"
        }
    }
    finally {
        # Ensure process is terminated if still running
        if ($process -and -not $process.HasExited) {
            try { $process.Kill() } catch { }
        }

        # Check for any remaining dotnet processes that might be hung
        $hangingProcesses = Get-Process -Name "dotnet" |
            Where-Object { $_.MainWindowTitle -eq "" -and $_.StartTime -gt $startTime }

        if ($hangingProcesses -and $hangingProcesses.Count -gt 0) {
            Write-ColorMessage "Found $($hangingProcesses.Count) potentially hanging dotnet processes. Terminating..." $colors.Warning
            foreach ($proc in $hangingProcesses) {
                try {
                    $proc.Kill()
                    Write-ColorMessage "Terminated process $($proc.Id)" $colors.Debug
                }
                catch {
                    Write-ColorMessage "Could not terminate process $($proc.Id)" $colors.Error
                }
            }
        }
    }
}

function List-TestCategories {
    Write-ColorMessage "`n=== Finding Test Categories in BusBus.Tests ===" $colors.Info

    # Look for [TestCategory("X")] attributes in all test files
    $categories = @()
    $testFiles = Get-ChildItem -Path "BusBus.Tests" -Recurse -Filter "*.cs"

    foreach ($file in $testFiles) {
        $content = Get-Content -Path $file.FullName -Raw
        $matches = [regex]::Matches($content, '\[TestCategory\s*\(\s*"([^"]+)"\s*\)\]')

        foreach ($match in $matches) {
            $category = $match.Groups[1].Value
            if ($category -notin $categories) {
                $categories += $category
            }
        }

        # Also look for TestCategory values without quotes
        $matches = [regex]::Matches($content, '\[TestCategory\s*\(\s*(\w+)\s*\)\]')
        foreach ($match in $matches) {
            $category = $match.Groups[1].Value
            if ($category -notin $categories) {
                $categories += $category
            }
        }
    }

    Write-ColorMessage "`nFound $($categories.Count) test categories:" $colors.Success
    foreach ($category in ($categories | Sort-Object)) {
        Write-Host "  - $category"
    }

    return $categories
}

function List-TestMethods {
    param(
        [string]$Category = "",
        [string]$File = ""
    )

    Write-ColorMessage "`n=== Finding Test Methods in BusBus.Tests ===" $colors.Info

    if ($Category) {
        Write-ColorMessage "Filtering by category: $Category" $colors.Debug
    }

    if ($File) {
        Write-ColorMessage "Filtering by file: $File" $colors.Debug
    }

    # Filter files if needed
    $testFiles = Get-ChildItem -Path "BusBus.Tests" -Recurse -Filter "*.cs"
    if ($File) {
        $testFiles = $testFiles | Where-Object { $_.Name -like "*$File*" }
    }

    $testMethods = @()

    foreach ($file in $testFiles) {
        $content = Get-Content -Path $file.FullName -Raw
        $fileMatches = $content

        # Filter by category if needed
        if ($Category) {
            # Check if file has the category
            $categoryMatches = [regex]::Matches($content, '\[TestCategory\s*\(\s*"' + [regex]::Escape($Category) + '"\s*\)\]')
            if ($categoryMatches.Count -eq 0) {
                $categoryMatches = [regex]::Matches($content, '\[TestCategory\s*\(\s*' + [regex]::Escape($Category) + '\s*\)\]')
                if ($categoryMatches.Count -eq 0) {
                    continue
                }
            }
        }

        # Find test methods
        $methodMatches = [regex]::Matches($content, '\[TestMethod\][^\}]*?public\s+(?:async\s+)?(?:void|Task)\s+(\w+)\s*\(')

        foreach ($match in $methodMatches) {
            $methodName = $match.Groups[1].Value
            $methodContext = $match.Value

            # If filtering by category, check if method or its class has the category
            if ($Category) {
                # Find class for this method
                $classMatch = [regex]::Match($content, 'class\s+(\w+)(?:\s*:\s*\w+(?:\s*,\s*\w+)*)?\s*\{[^\}]*?' + [regex]::Escape($methodContext))

                if ($classMatch.Success) {
                    $className = $classMatch.Groups[1].Value
                    $classStartPos = $classMatch.Index
                    $methodPos = $content.IndexOf($methodContext, $classStartPos)

                    # Check if method has category attribute
                    $methodHasCategory = $false
                    $classHasCategory = $false

                    # Look at content between class declaration and method
                    $contextToCheck = $content.Substring($classStartPos, $methodPos - $classStartPos)

                    # Check if class has the category
                    $classCategoryMatches = [regex]::Matches($contextToCheck, '\[TestCategory\s*\(\s*"' + [regex]::Escape($Category) + '"\s*\)\]')
                    if ($classCategoryMatches.Count -gt 0) {
                        $classHasCategory = $true
                    }
                    else {
                        $classCategoryMatches = [regex]::Matches($contextToCheck, '\[TestCategory\s*\(\s*' + [regex]::Escape($Category) + '\s*\)\]')
                        if ($classCategoryMatches.Count -gt 0) {
                            $classHasCategory = $true
                        }
                    }

                    # Check if method has the category
                    $contextBeforeMethod = $content.Substring($methodPos - 200, 200)
                    $methodCategoryMatches = [regex]::Matches($contextBeforeMethod, '\[TestCategory\s*\(\s*"' + [regex]::Escape($Category) + '"\s*\)\]')
                    if ($methodCategoryMatches.Count -gt 0) {
                        $methodHasCategory = $true
                    }
                    else {
                        $methodCategoryMatches = [regex]::Matches($contextBeforeMethod, '\[TestCategory\s*\(\s*' + [regex]::Escape($Category) + '\s*\)\]')
                        if ($methodCategoryMatches.Count -gt 0) {
                            $methodHasCategory = $true
                        }
                    }

                    if (-not ($methodHasCategory -or $classHasCategory)) {
                        continue
                    }
                }
            }

            $testMethods += [PSCustomObject]@{
                MethodName = $methodName
                FileName = $file.Name
                FilePath = $file.FullName
                FileRelativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
            }
        }
    }

    Write-ColorMessage "`nFound $($testMethods.Count) test methods:" $colors.Success

    $testMethods = $testMethods | Sort-Object -Property FileName, MethodName

    $currentFile = ""
    foreach ($test in $testMethods) {
        if ($test.FileName -ne $currentFile) {
            Write-Host ""
            Write-ColorMessage "File: $($test.FileRelativePath)" $colors.Info
            $currentFile = $test.FileName
        }

        Write-Host "  - $($test.MethodName)"
    }

    return $testMethods
}

function Run-SingleTest {
    param(
        [string]$TestMethod,
        [int]$Timeout = 30
    )

    $testArgs = @(
        "test",
        "BusBus.Tests",
        "--filter",
        "FullyQualifiedName~$TestMethod",
        "--no-build",
        "--verbosity",
        "detailed"
    )

    Write-ColorMessage "`n=== Running Test: $TestMethod ===" $colors.Info

    $result = Run-ProcessWithTimeout -Command "dotnet" -Arguments $testArgs -Timeout $Timeout -Description "Test: $TestMethod"

    if ($result.TimedOut) {
        Write-ColorMessage "`n❌ TEST TIMED OUT after $($result.RunTime) seconds!" $colors.Error
        Write-ColorMessage "This test is likely causing hanging issues." $colors.Warning
        return $false
    }
    elseif ($result.Success) {
        Write-ColorMessage "`n✅ Test completed successfully in $($result.RunTime) seconds." $colors.Success
        return $true
    }
    else {
        Write-ColorMessage "`n❌ Test failed with exit code $($result.ExitCode) in $($result.RunTime) seconds." $colors.Error
        return $false
    }
}

function Diagnose-HangingTests {
    Write-ColorMessage "`n=== DIAGNOSING POTENTIALLY HANGING TESTS ===" $colors.Info
    Write-ColorMessage "This will run selected tests with timeouts to identify which ones might be hanging." $colors.Warning

    $global:hangingTestsDetected = @()
    $testMethods = List-TestMethods

    # First look for suspicious patterns in test files
    Write-ColorMessage "`nScanning for suspicious patterns in test files..." $colors.Info
    $suspiciousFiles = @()

    foreach ($test in $testMethods) {
        $content = Get-Content -Path $test.FilePath -Raw

        # Look for suspicious patterns
        $hasInfiniteLoopPattern = $content -match "while\s*\(\s*true\s*\)" -or $content -match "for\s*\(\s*;;\s*\)"
        $hasThreadSleepInfinite = $content -match "Thread\.Sleep\s*\(\s*Timeout\.Infinite\s*\)"
        $hasTaskDelay = $content -match "Task\.Delay\s*\(\s*-1\s*\)"
        $hasReflection = $content -match "\.GetType\(\)" -or $content -match "System\.Reflection"
        $hasUIThread = $content -match "Application\.Run\s*\(" -or $content -match "ShowDialog\s*\("

        if ($hasInfiniteLoopPattern -or $hasThreadSleepInfinite -or $hasTaskDelay -or
            ($hasReflection -and $hasUIThread)) {

            if ($test.FilePath -notin $suspiciousFiles) {
                $suspiciousFiles += $test.FilePath
            }
        }
    }

    if ($suspiciousFiles.Count -gt 0) {
        Write-ColorMessage "`nFound $($suspiciousFiles.Count) potentially suspicious test files:" $colors.Warning
        foreach ($file in $suspiciousFiles) {
            Write-Host "  - $($file.Replace((Get-Location).Path + "\", ""))"
        }
    }

    # Try to run a small subset of tests to check for hanging
    $testSubset = $testMethods | Where-Object {
        $_.FileName -match "(Simple|Basic|Unit)" -or
        $_.MethodName -match "^(Simple|Basic|Test)"
    }

    if ($testSubset.Count -eq 0) {
        $testSubset = $testMethods | Select-Object -First 5
    }

    Write-ColorMessage "`nRunning a subset of $($testSubset.Count) tests to check for hanging..." $colors.Info

    foreach ($test in $testSubset) {
        $success = Run-SingleTest -TestMethod $test.MethodName -Timeout 20

        if (-not $success -and $test.MethodName -notin $global:hangingTestsDetected) {
            $global:hangingTestsDetected += $test.MethodName
        }
    }

    if ($global:hangingTestsDetected.Count -gt 0) {
        Write-ColorMessage "`n⚠️ HANGING TESTS DETECTED ⚠️" $colors.Error
        Write-ColorMessage "The following tests timed out and may be causing hanging issues:" $colors.Warning

        foreach ($test in $global:hangingTestsDetected) {
            Write-Host "  - $test"
        }

        # Create a script to run tests safely
        $safeTestScript = @"
# Run tests safely with timeout protection
# Auto-generated from diagnostic results

param(
    [switch]`$SkipHangingTests = `$true
)

# Tests identified as potentially hanging
`$hangingTests = @(
$($global:hangingTestsDetected | ForEach-Object { "    '$_'" } | Out-String)
)

Write-Host "Running tests with hanging test protection..." -ForegroundColor Cyan

# Build first
dotnet build BusBus.Tests

# Create filter to exclude hanging tests
`$filter = ""
if (`$SkipHangingTests -and `$hangingTests.Count -gt 0) {
    `$filter = `$hangingTests | ForEach-Object { "FullyQualifiedName!~$_" }
    `$filter = `$filter -join "&"

    Write-Host "Excluding $(`$hangingTests.Count) potentially hanging tests" -ForegroundColor Yellow
}

# Run the tests with timeout protection
`$process = Start-Process -FilePath "dotnet" -ArgumentList "test", "BusBus.Tests", "--no-build", "--filter", `$filter -NoNewWindow -PassThru

# Wait with timeout (2 minutes)
`$completed = `$process.WaitForExit(120000)

if (-not `$completed) {
    Write-Host "Tests still running after 2 minutes. Terminating..." -ForegroundColor Red
    `$process.Kill()
    exit 1
}

if (`$process.ExitCode -eq 0) {
    Write-Host "All tests completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Tests failed with exit code `$(`$process.ExitCode)" -ForegroundColor Red
}
"@

        $safeTestScriptPath = "run-safe-tests.ps1"
        Set-Content -Path $safeTestScriptPath -Value $safeTestScript

        Write-ColorMessage "`nCreated a safe test runner script: $safeTestScriptPath" $colors.Success
        Write-ColorMessage "Run this script to execute tests while skipping the tests that might hang." $colors.Info
    }
    else {
        Write-ColorMessage "`nNo hanging tests detected in the subset that was tested." $colors.Success
    }
}

function Run-TestsByCategory {
    param(
        [string]$Category,
        [int]$Timeout = 30
    )

    $testArgs = @(
        "test",
        "BusBus.Tests",
        "--filter",
        "TestCategory=$Category",
        "--no-build",
        "--verbosity",
        "detailed"
    )

    Write-ColorMessage "`n=== Running Tests in Category: $Category ===" $colors.Info

    $result = Run-ProcessWithTimeout -Command "dotnet" -Arguments $testArgs -Timeout $Timeout -Description "Tests in category: $Category"

    if ($result.TimedOut) {
        Write-ColorMessage "`n❌ TESTS TIMED OUT after $($result.RunTime) seconds!" $colors.Error
        Write-ColorMessage "This category may contain tests causing hanging issues." $colors.Warning
        return $false
    }
    elseif ($result.Success) {
        Write-ColorMessage "`n✅ All tests completed successfully in $($result.RunTime) seconds." $colors.Success
        return $true
    }
    else {
        Write-ColorMessage "`n❌ Tests failed with exit code $($result.ExitCode) in $($result.RunTime) seconds." $colors.Error
        return $false
    }
}

# Main script execution logic
$scriptPath = Get-Location
Write-ColorMessage "Current directory: $scriptPath" $colors.Debug

if (-not (Test-Path "BusBus.Tests")) {
    Write-ColorMessage "BusBus.Tests directory not found. Are you in the right directory?" $colors.Error
    exit 1
}

# Print script header
Write-ColorMessage "`n=== BusBus Test Diagnostic Tool ===" $colors.Info
Write-ColorMessage "This script helps diagnose and run tests safely with timeout protection." $colors.Info

# List test categories if requested
if ($ListCategories) {
    List-TestCategories
    exit 0
}

# List test methods if requested
if ($ListTests) {
    List-TestMethods -Category $TestCategory -File $TestFile
    exit 0
}

# Diagnose hanging tests if requested
if ($DiagnoseHangingTests) {
    Diagnose-HangingTests
    exit 0
}

# Run tests by category
if ($TestCategory) {
    Run-TestsByCategory -Category $TestCategory -Timeout $TimeoutSeconds
    exit 0
}

# Run a specific test
if ($TestName) {
    Run-SingleTest -TestMethod $TestName -Timeout $TimeoutSeconds
    exit 0
}

# If no specific action was requested, show usage
Write-ColorMessage "`nUsage:" $colors.Info
Write-ColorMessage "  .\test-diagnostics.ps1 -ListCategories" $colors.Debug
Write-ColorMessage "  .\test-diagnostics.ps1 -ListTests [-TestCategory Category] [-TestFile FileName]" $colors.Debug
Write-ColorMessage "  .\test-diagnostics.ps1 -DiagnoseHangingTests" $colors.Debug
Write-ColorMessage "  .\test-diagnostics.ps1 -TestCategory CategoryName [-TimeoutSeconds 30]" $colors.Debug
Write-ColorMessage "  .\test-diagnostics.ps1 -TestName TestMethodName [-TimeoutSeconds 30]" $colors.Debug
