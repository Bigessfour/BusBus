# Incremental Test Runner for BusBus Project
# This script helps roll out tests gradually to avoid overwhelming the system

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("phase1", "phase2", "phase3", "all", "status")]
    [string]$Phase = "status", [Parameter(Mandatory = $false)]
    [switch]$DetailedOutput,

    [Parameter(Mandatory = $false)]
    [switch]$Coverage
)

# Colors for output
$Colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error   = "Red"
    Info    = "Cyan"
    Header  = "Magenta"
}

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Colors[$Color]
}

function Show-Header {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "=" * 60 -Color "Header"
    Write-ColorOutput "  $Title" -Color "Header"
    Write-ColorOutput "=" * 60 -Color "Header"
    Write-Host ""
}

function Test-Prerequisites {
    Write-ColorOutput "Checking prerequisites..." -Color "Info"

    # Check if we're in the right directory
    if (-not (Test-Path "BusBus.sln")) {
        Write-ColorOutput "Error: Not in BusBus project directory!" -Color "Error"
        return $false
    }

    # Check if solution builds
    Write-ColorOutput "Building solution..." -Color "Info"
    $buildResult = dotnet build BusBus.sln --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "Build failed! Please fix build errors first." -Color "Error"
        return $false
    }

    Write-ColorOutput "Prerequisites check passed!" -Color "Success"
    return $true
}

function Run-TestPhase {
    param(
        [string]$PhaseName,
        [string]$Description,
        [string[]]$TestFilters,
        [bool]$RunCoverage = $false
    )

    Show-Header "Phase: $PhaseName - $Description"

    $totalTests = 0
    $passedTests = 0
    $failedTests = 0

    foreach ($filter in $TestFilters) {
        Write-ColorOutput "Running tests: $filter" -Color "Info"

        $testCommand = "dotnet test --filter `"$filter`" --logger console;verbosity=normal"
        if ($RunCoverage) {
            $testCommand += " /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura"
        }        if ($DetailedOutput) {
            $testCommand += " --verbosity detailed"
        }

        Write-ColorOutput "Command: $testCommand" -Color "Info"

        # Run the test
        $output = Invoke-Expression $testCommand 2>&1

        # Parse results
        $testResult = $output | Select-String "Passed.*Failed.*Skipped" | Select-Object -Last 1
        if ($testResult) {
            $resultText = $testResult.ToString()
            Write-ColorOutput "Result: $resultText" -Color "Info"

            # Extract numbers (basic parsing)
            if ($resultText -match "Passed:\s*(\d+)") { $passedTests += [int]$matches[1] }
            if ($resultText -match "Failed:\s*(\d+)") { $failedTests += [int]$matches[1] }
            $totalTests = $passedTests + $failedTests
        }

        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "Some tests failed in: $filter" -Color "Warning"
            Write-ColorOutput "Check the output above for details." -Color "Warning"
        }
        else {
            Write-ColorOutput "All tests passed in: $filter" -Color "Success"
        }

        Write-Host ""
    }

    # Phase summary
    Write-ColorOutput "Phase Summary:" -Color "Header"
    Write-ColorOutput "  Total Tests: $totalTests" -Color "Info"
    Write-ColorOutput "  Passed: $passedTests" -Color "Success"
    Write-ColorOutput "  Failed: $failedTests" -Color $(if ($failedTests -eq 0) { "Success" } else { "Warning" })

    return @{ Total = $totalTests; Passed = $passedTests; Failed = $failedTests }
}

function Show-ProjectStatus {
    Show-Header "BusBus Test Project Status"

    Write-ColorOutput "Project Structure:" -Color "Info"
    $testFiles = Get-ChildItem -Path "BusBus.Tests" -Recurse -Filter "*.cs" | Where-Object { $_.Name -like "*Tests.cs" }

    $categories = @{}
    foreach ($file in $testFiles) {
        $category = $file.Directory.Name
        if (-not $categories.ContainsKey($category)) {
            $categories[$category] = @()
        }
        $categories[$category] += $file.Name
    }

    foreach ($category in $categories.Keys | Sort-Object) {
        Write-ColorOutput "  $category/:" -Color "Header"
        foreach ($file in $categories[$category] | Sort-Object) {
            Write-ColorOutput "    - $file" -Color "Info"
        }
    }

    Write-Host ""
    Write-ColorOutput "Test Phases:" -Color "Info"
    Write-ColorOutput "  Phase 1: Core Infrastructure (DatabaseManager, AppDbContext, ResourceTracker)" -Color "Info"
    Write-ColorOutput "  Phase 2: Business Logic (Models, Services, AI)" -Color "Info"
    Write-ColorOutput "  Phase 3: UI and Integration (UI Components, Integration Tests)" -Color "Info"

    Write-Host ""
    Write-ColorOutput "Usage Examples:" -Color "Info"
    Write-ColorOutput "  .\run-tests-incrementally.ps1 -Phase phase1" -Color "Info"
    Write-ColorOutput "  .\run-tests-incrementally.ps1 -Phase phase2 -DetailedOutput" -Color "Info"
    Write-ColorOutput "  .\run-tests-incrementally.ps1 -Phase all -Coverage" -Color "Info"
}

# Main execution
Show-Header "BusBus Incremental Test Runner"

if (-not (Test-Prerequisites)) {
    exit 1
}

switch ($Phase) {
    "status" {
        Show-ProjectStatus
    }

    "phase1" {
        $filters = @(
            "Category=Unit&FullyQualifiedName~DatabaseManager",
            "Category=Unit&FullyQualifiedName~AppDbContext",
            "Category=Unit&FullyQualifiedName~ResourceTracker"
        )
        $result = Run-TestPhase "Phase 1" "Core Infrastructure" $filters $Coverage
    }

    "phase2" {
        $filters = @(
            "Category=Unit&FullyQualifiedName~ModelTests",
            "Category=Unit&FullyQualifiedName~DriverService",
            "Category=Unit&FullyQualifiedName~VehicleService",
            "Category=Unit&FullyQualifiedName~RouteService",
            "Category=Unit&FullyQualifiedName~StatisticsService",
            "Category=Unit&FullyQualifiedName~GrokService",
            "Category=Unit&FullyQualifiedName~AppSettings"
        )
        $result = Run-TestPhase "Phase 2" "Business Logic" $filters $Coverage
    }

    "phase3" {
        $filters = @(
            "Category=Unit&FullyQualifiedName~ThreadSafeUI",
            "Category=Unit&FullyQualifiedName~CustomFieldsManager",
            "Category=Unit&FullyQualifiedName~Dashboard",
            "Category=Integration"
        )
        $result = Run-TestPhase "Phase 3" "UI and Integration" $filters $Coverage
    }

    "all" {
        Write-ColorOutput "Running all test phases..." -Color "Info"

        # Run each phase
        $phase1 = Run-TestPhase "Phase 1" "Core Infrastructure" @(
            "Category=Unit&FullyQualifiedName~DatabaseManager",
            "Category=Unit&FullyQualifiedName~AppDbContext",
            "Category=Unit&FullyQualifiedName~ResourceTracker"
        ) $false

        $phase2 = Run-TestPhase "Phase 2" "Business Logic" @(
            "Category=Unit&FullyQualifiedName~ModelTests",
            "Category=Unit&FullyQualifiedName~DriverService",
            "Category=Unit&FullyQualifiedName~VehicleService",
            "Category=Unit&FullyQualifiedName~RouteService",
            "Category=Unit&FullyQualifiedName~StatisticsService",
            "Category=Unit&FullyQualifiedName~GrokService",
            "Category=Unit&FullyQualifiedName~AppSettings"
        ) $false

        $phase3 = Run-TestPhase "Phase 3" "UI and Integration" @(
            "Category=Unit&FullyQualifiedName~ThreadSafeUI",
            "Category=Unit&FullyQualifiedName~CustomFieldsManager",
            "Category=Unit&FullyQualifiedName~Dashboard",
            "Category=Integration"
        ) $Coverage

        # Overall summary
        Show-Header "Overall Test Summary"
        $totalTests = $phase1.Total + $phase2.Total + $phase3.Total
        $totalPassed = $phase1.Passed + $phase2.Passed + $phase3.Passed
        $totalFailed = $phase1.Failed + $phase2.Failed + $phase3.Failed

        Write-ColorOutput "Total Tests: $totalTests" -Color "Info"
        Write-ColorOutput "Total Passed: $totalPassed" -Color "Success"
        Write-ColorOutput "Total Failed: $totalFailed" -Color $(if ($totalFailed -eq 0) { "Success" } else { "Warning" })

        if ($totalTests -gt 0) {
            $successRate = [math]::Round(($totalPassed / $totalTests) * 100, 1)
            Write-ColorOutput "Success Rate: $successRate%" -Color $(if ($successRate -ge 80) { "Success" } else { "Warning" })
        }
    }
}

Write-ColorOutput "Test run complete!" -Color "Success"
