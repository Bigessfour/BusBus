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
        if ($classMatch.Groups.Count > 2 -and $classMatch.Groups[2].Success) {
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

function Get-TestCoverage {
    param (
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath
    )

    Write-Host "Analyzing test coverage for solution: $SolutionPath" -ForegroundColor Cyan

    # First, build the solution with coverage instrumentation
    dotnet test "$SolutionPath" --collect:"XPlat Code Coverage" --no-build

    # Find the coverage report
    $coverageFiles = Get-ChildItem -Path (Split-Path $SolutionPath -Parent) -Filter "coverage.cobertura.xml" -Recurse
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "No coverage files found. Running tests with coverage collection..."
        return $null
    }

    $latestCoverageFile = $coverageFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "Using coverage file: $($latestCoverageFile.FullName)" -ForegroundColor Green

    # Parse the coverage XML
    $coverageXml = [xml](Get-Content $latestCoverageFile.FullName)

    # Extract classes and their coverage
    $classes = @()
    foreach ($class in $coverageXml.coverage.classes.class) {
        $classInfo = [PSCustomObject]@{
            Name            = $class.name -replace ',.*$', ''
            File            = $class.filename
            LinesCovered    = [int]$class.summary.lines.covered
            LinesTotal      = [int]$class.summary.lines.total
            CoveragePercent = if ([int]$class.summary.lines.total -gt 0) {
                [math]::Round(([int]$class.summary.lines.covered / [int]$class.summary.lines.total) * 100, 2)
            }
            else {
                0
            }
        }
        $classes += $classInfo
    }

    # Sort by coverage (lowest first)
    $sortedClasses = $classes | Sort-Object CoveragePercent

    return $sortedClasses
}

function New-TestScaffold {
    param (
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$ClassInfo,

        [Parameter(Mandatory = $true)]
        [string]$TestsDirectory,

        [Parameter(Mandatory = $false)]
        [string]$TestNamespace = "BusBus.Tests"
    )

    $testClassName = "$($ClassInfo.ClassName)Test"
    $relativePath = ""

    if ($ClassInfo.Namespace -match "^BusBus\.(.+)$") {
        $relativePath = $matches[1].Replace(".", "\")
    }

    $testFilePath = Join-Path -Path $TestsDirectory -ChildPath "$relativePath\$testClassName.cs"
    $testDirectoryPath = Split-Path -Path $testFilePath -Parent

    # Create directory if it doesn't exist
    if (-not (Test-Path $testDirectoryPath)) {
        New-Item -ItemType Directory -Path $testDirectoryPath -Force | Out-Null
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

namespace $TestNamespace
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
    Set-Content -Path $testFilePath -Value $testClass

    Write-Host "Test scaffold created at: $testFilePath" -ForegroundColor Green
    return $testFilePath
}

function Invoke-TestGeneration {
    param (
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath,

        [Parameter(Mandatory = $false)]
        [int]$TopN = 5
    )

    $solutionDir = Split-Path -Path $SolutionPath -Parent
    $testProjectDir = Join-Path -Path $solutionDir -ChildPath "BusBus.Tests"

    # Get test coverage
    Write-Host "Analyzing test coverage..." -ForegroundColor Cyan
    $coverage = Get-TestCoverage -SolutionPath $SolutionPath

    if ($null -eq $coverage) {
        Write-Warning "Could not get test coverage. Will select files based on complexity instead."

        # Find complex files (this is a simple heuristic - number of methods)
        $sourceFiles = Get-ChildItem -Path $solutionDir -Filter "*.cs" -Recurse |
        Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\|\\TestResults\\|\\BusBus.Tests\\" }

        $fileComplexity = @()
        foreach ($file in $sourceFiles) {
            $classInfo = Get-ClassInfo -FilePath $file.FullName
            if ($null -ne $classInfo -and $null -ne $classInfo.Methods) {
                $fileComplexity += [PSCustomObject]@{
                    FilePath    = $file.FullName
                    ClassName   = $classInfo.ClassName
                    MethodCount = $classInfo.Methods.Count
                    ClassInfo   = $classInfo
                }
            }
        }

        $candidateFiles = $fileComplexity | Sort-Object MethodCount -Descending | Select-Object -First $TopN
    }
    else {
        # Select files with lowest coverage
        $candidateFiles = $coverage | Where-Object { $_.CoveragePercent -lt 80 } | Select-Object -First $TopN
    }

    Write-Host "Selected files for test generation:" -ForegroundColor Yellow
    $candidateFiles | Format-Table -Property Name, File, CoveragePercent -AutoSize

    # Generate test scaffolds
    foreach ($file in $candidateFiles) {
        $filePath = if ($null -ne $file.ClassInfo) { $file.FilePath } else { Join-Path -Path $solutionDir -ChildPath $file.File }

        if (Test-Path $filePath) {
            $classInfo = if ($null -ne $file.ClassInfo) { $file.ClassInfo } else { Get-ClassInfo -FilePath $filePath }

            if ($null -ne $classInfo -and $null -ne $classInfo.ClassName) {
                Write-Host "Generating test for $($classInfo.ClassName) from $filePath" -ForegroundColor Cyan
                $testFilePath = New-TestScaffold -ClassInfo $classInfo -TestsDirectory $testProjectDir

                Write-Host "Test scaffold generated at: $testFilePath" -ForegroundColor Green
                Write-Host ""
            }
            else {
                Write-Warning "Could not extract class information from $filePath"
            }
        }
        else {
            Write-Warning "File not found: $filePath"
        }
    }
}

# Example usage
# Invoke-TestGeneration -SolutionPath "C:\Users\steve.mckitrick\Desktop\BusBus\BusBus.sln"
