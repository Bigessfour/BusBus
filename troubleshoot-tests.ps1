# Test Troubleshooting Helper for BusBus Project
# This script helps identify and fix common test issues

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("config", "references", "build", "all")]
    [string]$Check = "all"
)

$Colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "Cyan"
    Header = "Magenta"
}

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Colors[$Color]
}

function Show-Header {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "=" * 50 -Color "Header"
    Write-ColorOutput "  $Title" -Color "Header"
    Write-ColorOutput "=" * 50 -Color "Header"
}

function Check-Configuration {
    Show-Header "Configuration Check"

    $issues = @()

    # Check for appsettings.json
    if (Test-Path "config/appsettings.json") {
        Write-ColorOutput "✅ Found config/appsettings.json" -Color "Success"
    } else {
        Write-ColorOutput "❌ Missing config/appsettings.json" -Color "Error"
        $issues += "Missing config/appsettings.json"
    }

    # Check for appsettings.Development.json
    if (Test-Path "config/appsettings.Development.json") {
        Write-ColorOutput "✅ Found config/appsettings.Development.json" -Color "Success"
    } else {
        Write-ColorOutput "❌ Missing config/appsettings.Development.json" -Color "Error"
        $issues += "Missing config/appsettings.Development.json"
    }

    # Check TestBase.cs configuration
    if (Test-Path "BusBus.Tests/TestBase.cs") {
        $testBaseContent = Get-Content "BusBus.Tests/TestBase.cs" -Raw
        if ($testBaseContent -like "*config/*") {
            Write-ColorOutput "✅ TestBase.cs references config directory" -Color "Success"
        } else {
            Write-ColorOutput "❌ TestBase.cs may have wrong config path" -Color "Error"
            $issues += "TestBase.cs configuration path issue"
        }
    }

    return $issues
}

function Check-ProjectReferences {
    Show-Header "Project References Check"

    $issues = @()

    # Check if test project exists
    if (Test-Path "BusBus.Tests/BusBus.Tests.csproj") {
        Write-ColorOutput "✅ Found BusBus.Tests.csproj" -Color "Success"

        # Check project reference
        $testProjContent = Get-Content "BusBus.Tests/BusBus.Tests.csproj" -Raw
        if ($testProjContent -like "*ProjectReference*BusBus.csproj*") {
            Write-ColorOutput "✅ Test project references main project" -Color "Success"
        } else {
            Write-ColorOutput "❌ Missing project reference to BusBus.csproj" -Color "Error"
            $issues += "Missing project reference"
        }

        # Check for required NuGet packages
        $requiredPackages = @("NUnit", "FluentAssertions", "Moq", "Microsoft.EntityFrameworkCore.InMemory")
        foreach ($package in $requiredPackages) {
            if ($testProjContent -like "*$package*") {
                Write-ColorOutput "✅ Found package: $package" -Color "Success"
            } else {
                Write-ColorOutput "❌ Missing package: $package" -Color "Warning"
                $issues += "Missing NuGet package: $package"
            }
        }
    } else {
        Write-ColorOutput "❌ BusBus.Tests.csproj not found" -Color "Error"
        $issues += "Test project file missing"
    }

    return $issues
}

function Check-BuildIssues {
    Show-Header "Build Issues Check"

    Write-ColorOutput "Building solution..." -Color "Info"
    $buildOutput = dotnet build BusBus.sln 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Solution builds successfully" -Color "Success"
        return @()
    } else {
        Write-ColorOutput "❌ Build failed" -Color "Error"
        Write-ColorOutput "Build errors:" -Color "Error"
        $buildOutput | Where-Object { $_ -like "*error*" } | ForEach-Object {
            Write-ColorOutput "  $_" -Color "Error"
        }
        return @("Build failed - see errors above")
    }
}

function Suggest-Fixes {
    param([string[]]$Issues)

    if ($Issues.Count -eq 0) {
        Write-ColorOutput "🎉 No issues found! Your test setup looks good." -Color "Success"
        return
    }

    Show-Header "Suggested Fixes"

    foreach ($issue in $Issues) {
        Write-ColorOutput "Issue: $issue" -Color "Warning"

        switch -Wildcard ($issue) {
            "*appsettings.json*" {
                Write-ColorOutput "  Fix: Copy appsettings.json to config/ directory" -Color "Info"
                Write-ColorOutput "  Command: Copy-Item appsettings.json config/" -Color "Info"
            }
            "*TestBase.cs*" {
                Write-ColorOutput "  Fix: Update TestBase.cs to use correct config path" -Color "Info"
                Write-ColorOutput "  Look for: Path.Combine(..., 'appsettings.json')" -Color "Info"
                Write-ColorOutput "  Should be: Path.Combine(..., 'config', 'appsettings.json')" -Color "Info"
            }
            "*project reference*" {
                Write-ColorOutput "  Fix: Add project reference in test project" -Color "Info"
                Write-ColorOutput "  Command: dotnet add BusBus.Tests reference BusBus.csproj" -Color "Info"
            }
            "*NuGet package*" {
                $packageName = ($issue -split ": ")[1]
                Write-ColorOutput "  Fix: Install missing NuGet package" -Color "Info"
                Write-ColorOutput "  Command: dotnet add BusBus.Tests package $packageName" -Color "Info"
            }
            "*Build failed*" {
                Write-ColorOutput "  Fix: Review build errors above and fix compilation issues" -Color "Info"
                Write-ColorOutput "  Tip: Run 'dotnet build --verbosity detailed' for more info" -Color "Info"
            }
        }
        Write-Host ""
    }
}

# Main execution
Write-ColorOutput "BusBus Test Troubleshooting Helper" -Color "Header"

$allIssues = @()

switch ($Check) {
    "config" {
        $allIssues += Check-Configuration
    }
    "references" {
        $allIssues += Check-ProjectReferences
    }
    "build" {
        $allIssues += Check-BuildIssues
    }
    "all" {
        $allIssues += Check-Configuration
        $allIssues += Check-ProjectReferences
        $allIssues += Check-BuildIssues
    }
}

Suggest-Fixes $allIssues

Write-Host ""
Write-ColorOutput "Run this script with different parameters:" -Color "Info"
Write-ColorOutput "  .\troubleshoot-tests.ps1 -Check config" -Color "Info"
Write-ColorOutput "  .\troubleshoot-tests.ps1 -Check references" -Color "Info"
Write-ColorOutput "  .\troubleshoot-tests.ps1 -Check build" -Color "Info"
