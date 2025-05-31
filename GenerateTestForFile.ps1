# Quick Test Generator Script

# Parameters
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetFile,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath
)

function Get-ClassInfo {
    param (
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )

    if (-not (Test-Path $FilePath)) {
        Write-Error "File not found: $FilePath"
        return $null
    }

    $fileContent = Get-Content -Path $FilePath -Raw
    $className = $null
    $namespace = $null
    $methods = @()
    $properties = @()
    $baseClass = $null
    $interfaces = @()
    $usings = @()

    # Extract using statements
    $usingMatches = [regex]::Matches($fileContent, "using\s+([^;]+);")
    foreach ($match in $usingMatches) {
        $usings += $match.Groups[1].Value.Trim()
    }

    # Extract namespace
    $namespaceMatch = [regex]::Match($fileContent, "namespace\s+([^\s{]+)")
    if ($namespaceMatch.Success) {
        $namespace = $namespaceMatch.Groups[1].Value
    }

    # Extract class definition
    $classMatch = [regex]::Match($fileContent, "(?:public|internal|private)?\s+(?:abstract|static|sealed)?\s+class\s+(\w+)(?:\s*:\s*([^{]+))?")
    if ($classMatch.Success) {
        $className = $classMatch.Groups[1].Value
        # Extract base class and interfaces
        if ($classMatch.Groups.Count -gt 2 -and $classMatch.Groups[2].Success) {
            $inheritance = $classMatch.Groups[2].Value.Split(',') | ForEach-Object { $_.Trim() }
            if ($inheritance.Count -gt 0) {
                $baseClass = $inheritance[0]
                if ($inheritance.Count -gt 1) {
                    $interfaces = $inheritance[1..($inheritance.Count - 1)]
                }
            }
        }
    }

    # Extract methods
    $methodMatches = [regex]::Matches($fileContent, "(?:public|protected|internal|private)\s+(?:static|virtual|abstract|override)?\s+(?!class|interface|enum)(\w+(?:<[^>]+>)?)\s+(\w+)\s*\(([^)]*)\)")
    foreach ($match in $methodMatches) {
        $returnType = $match.Groups[1].Value
        $methodName = $match.Groups[2].Value
        $parameters = $match.Groups[3].Value

        # Skip properties
        if (-not ($methodName -match "get_|set_")) {
            $methods += [PSCustomObject]@{
                Name       = $methodName
                ReturnType = $returnType
                Parameters = $parameters
            }
        }
    }

    # Extract properties
    $propertyMatches = [regex]::Matches($fileContent, "(?:public|protected|internal|private)\s+(?:virtual|abstract|override)?\s+(\w+(?:<[^>]+>)?)\s+(\w+)\s*{\s*(?:get;|set;)")
    foreach ($match in $propertyMatches) {
        $propertyType = $match.Groups[1].Value
        $propertyName = $match.Groups[2].Value

        $properties += [PSCustomObject]@{
            Name = $propertyName
            Type = $propertyType
        }
    }

    return [PSCustomObject]@{
        FilePath   = $FilePath
        Namespace  = $namespace
        ClassName  = $className
        BaseClass  = $baseClass
        Interfaces = $interfaces
        Methods    = $methods
        Properties = $properties
        Usings     = $usings
    }
}

function New-TestScaffold {
    param (
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$ClassInfo,

        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )

    $testClassName = "$($ClassInfo.ClassName)Test"
    $testNamespace = "BusBus.Tests"

    if ($ClassInfo.Namespace -match "^BusBus\.(.+)$") {
        $testNamespace = "BusBus.Tests.$($matches[1])"
    }

    # Create usings
    $usings = @(
        "using System;",
        "using System.Collections.Generic;",
        "using System.Linq;",
        "using Microsoft.VisualStudio.TestTools.UnitTesting;",
        "using Microsoft.Extensions.DependencyInjection;",
        "using Microsoft.Extensions.Logging;"
    )

    foreach ($using in $ClassInfo.Usings) {
        $usings += "using $using;"
    }

    $usings = $usings | Sort-Object -Unique
    $usingsText = $usings -join [Environment]::NewLine

    # Create test methods
    $testMethods = @()
    foreach ($method in $ClassInfo.Methods) {
        if ($method.Name -notmatch "^(get_|set_)") {
            $testMethodName = "$($method.Name)_Test"
            $testCategory = if ($ClassInfo.Namespace -match "\.([^\.]+)$") { $matches[1] } else { "Core" }

            $testMethod = @"
        [TestMethod]
        [TestCategory("$testCategory")]
        [Timeout(15000)]
        public void $testMethodName()
        {
            // Arrange
            // TODO: Set up your test objects and dependencies

            // Act
            // TODO: Call the method being tested

            // Assert
            // TODO: Verify the expected outcome
            Assert.IsTrue(true, "Replace with actual assertion");
        }
"@
            $testMethods += $testMethod
        }
    }

    $testMethodsText = $testMethods -join [Environment]::NewLine + [Environment]::NewLine

    # Create the test class
    $testClass = @"
#nullable enable
$usingsText

namespace $testNamespace
{
    [TestClass]
    public class $testClassName
    {
        private static IServiceProvider? _serviceProvider;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Setup DI container for tests
            var services = new ServiceCollection();

            // TODO: Register required services
            // services.AddSingleton<ILogger<$testClassName>>(Mock.Of<ILogger<$testClassName>>());

            _serviceProvider = services.BuildServiceProvider();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Clean up resources
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

$testMethodsText
    }
}
"@

    # Write the test class to file
    Set-Content -Path $OutputPath -Value $testClass

    Write-Host "Test scaffold created at: $OutputPath" -ForegroundColor Green
    return $OutputPath
}

# Main execution
try {
    # Get the full path
    $targetFilePath = Resolve-Path $TargetFile

    Write-Host "Analyzing file: $targetFilePath" -ForegroundColor Cyan

    # Extract class information
    $classInfo = Get-ClassInfo -FilePath $targetFilePath

    if ($null -eq $classInfo -or $null -eq $classInfo.ClassName) {
        Write-Error "Could not extract class information from the target file."
        exit 1
    }

    Write-Host "Found class: $($classInfo.ClassName)" -ForegroundColor Green
    Write-Host "Base class: $($classInfo.BaseClass)" -ForegroundColor Green

    # Display methods
    Write-Host "`nMethods:" -ForegroundColor Yellow
    $classInfo.Methods | Format-Table -Property Name, ReturnType, Parameters -AutoSize

    # Display properties
    Write-Host "`nProperties:" -ForegroundColor Yellow
    $classInfo.Properties | Format-Table -Property Name, Type -AutoSize

    # Determine output path if not provided
    if (-not $OutputPath) {
        $solutionDir = Split-Path -Path $targetFilePath -Parent
        while ($solutionDir -notmatch "\\BusBus$" -and $solutionDir.Length -gt 3) {
            $solutionDir = Split-Path -Path $solutionDir -Parent
        }

        if ($solutionDir -notmatch "\\BusBus$") {
            $solutionDir = "C:\Users\steve.mckitrick\Desktop\BusBus"
        }

        $relativePath = ""
        if ($classInfo.Namespace -match "^BusBus\.(.+)$") {
            $relativePath = $matches[1].Replace(".", "\")
        }

        $testProjectDir = Join-Path -Path $solutionDir -ChildPath "BusBus.Tests"
        $outputDir = Join-Path -Path $testProjectDir -ChildPath $relativePath

        # Create directory if it doesn't exist
        if (-not (Test-Path $outputDir)) {
            New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
        }

        $OutputPath = Join-Path -Path $outputDir -ChildPath "$($classInfo.ClassName)Test.cs"
    }

    # Generate test scaffold
    $testPath = New-TestScaffold -ClassInfo $classInfo -OutputPath $OutputPath

    Write-Host "`nTest scaffold generated successfully!" -ForegroundColor Green
    Write-Host "You can find it at: $testPath"

    Write-Host "`nRecommended test command:" -ForegroundColor Cyan
    Write-Host "dotnet test --filter FullyQualifiedName~$($classInfo.ClassName)Test --no-build" -ForegroundColor White
}
catch {
    Write-Error "An error occurred: $_"
    exit 1
}
