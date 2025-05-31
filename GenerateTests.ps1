param(
    [Parameter(Mandatory = $true)]
    [string]$TargetFilePath,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath = $null,

    [Parameter(Mandatory = $false)]
    [string]$TestCategory = "UnitTest",

    [Parameter(Mandatory = $false)]
    [int]$TestTimeout = 15000
)

# Ensure the target file exists
if (-not (Test-Path $TargetFilePath)) {
    Write-Error "Target file does not exist: $TargetFilePath"
    exit 1
}

# Read the file content
$fileContent = Get-Content $TargetFilePath -Raw

# Default output path if not provided
if (-not $OutputPath) {
    $fileName = [System.IO.Path]::GetFileNameWithoutExtension($TargetFilePath)
    $directory = [System.IO.Path]::GetDirectoryName($TargetFilePath)
    $testsDir = Join-Path -Path (Split-Path -Parent $MyInvocation.MyCommand.Path) -ChildPath "BusBus.Tests"

    # Create the namespace-based folder structure in the test project
    $namespaceMatch = [regex]::Match($fileContent, 'namespace\s+([^\s{;]+)')
    $namespace = "BusBus"
    if ($namespaceMatch.Success) {
        $namespace = $namespaceMatch.Groups[1].Value
    }

    # Convert namespace to folder path
    $namespacePath = $namespace -replace "BusBus\.", ""
    $namespacePath = $namespacePath -replace "\.", "\"

    $outputDir = Join-Path -Path $testsDir -ChildPath $namespacePath
    if (-not (Test-Path $outputDir)) {
        New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
    }

    $OutputPath = Join-Path -Path $outputDir -ChildPath "${fileName}Test.cs"
}
}

# Extract namespace
$namespaceMatch = [regex]::Match($fileContent, 'namespace\s+([^\s{;]+)')
$namespace = "BusBus"
if ($namespaceMatch.Success) {
    $namespace = $namespaceMatch.Groups[1].Value
}

# Extract class information
$classPattern = 'public\s+(abstract\s+)?(class|interface|record|struct)\s+([^\s:<]+)(?:<[^>]+>)?(?:\s*:\s*([^{]+))?'
$classMatches = [regex]::Matches($fileContent, $classPattern)

if ($classMatches.Count -eq 0) {
    Write-Warning "No public classes found in the file"
    exit 0
}

# Get the using statements
$usingPattern = 'using\s+([^;]+);'
$usingMatches = [regex]::Matches($fileContent, $usingPattern)
$usings = $usingMatches | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique

# Extract method information
$methodPattern = 'public\s+(virtual\s+|override\s+)?(async\s+)?(static\s+)?([^\s(<]+)\s+([^\s(<]+)(?:<[^>]+>)?\s*\(([^)]*)\)'
$methodMatches = [regex]::Matches($fileContent, $methodPattern)

# Extract property information
$propertyPattern = 'public\s+(virtual\s+|override\s+)?(static\s+)?([^\s{(<]+)\s+([^\s{(<]+)(?:<[^>]+>)?\s*{\s*get;'
$propertyMatches = [regex]::Matches($fileContent, $propertyPattern)

# Build the test class
$className = $classMatches[0].Groups[3].Value
$testClassName = "${className}Test"

$testFileContent = @"
using System;
using System.Threading;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
$(($usings | ForEach-Object { "using $_;" }) -join "`n")

namespace BusBus.Tests.$($namespace -replace "BusBus\.", "")
{
    [TestClass]
    public class $testClassName
    {
        private IServiceProvider? _serviceProvider;

        [TestInitialize]
        public void Setup()
        {
            // Setup service provider for testing
            var services = new ServiceCollection();

            // Register services needed for testing
            // TODO: Add appropriate service registrations for $className dependencies

            _serviceProvider = services.BuildServiceProvider();
        }

"@

# Generate test methods for each public method
$methodMatches | ForEach-Object {
    $returnType = $_.Groups[4].Value
    $methodName = $_.Groups[5].Value
    $parameters = $_.Groups[6].Value

    # Skip property accessor methods
    if ($methodName -match '^(get_|set_)') {
        return
    }

    $testFileContent += @"
        [TestMethod]
        [TestCategory("$TestCategory")]
        [Timeout($TestTimeout)]
        public void ${methodName}_ShouldWork_WhenCalled()
        {
            // Arrange
            var testCompleted = new ManualResetEventSlim(false);
            Exception? testException = null;

            var thread = new Thread(() => {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    // TODO: Get required services from scope

                    // Create instance of $className or mock its dependencies
                    // var instance = new $className(...);

                    // Act
                    // var result = instance.$methodName(...);

                    // Assert
                    // Assert.IsNotNull(result);
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!testCompleted.Wait(12000))
            {
                Assert.Fail("$methodName test timed out");
            }

            Assert.IsNull(testException, \$"$methodName test failed with exception: {testException}");
        }

"@
}

# Generate property test methods
$propertyMatches | ForEach-Object {
    $propertyType = $_.Groups[3].Value
    $propertyName = $_.Groups[4].Value

    $testFileContent += @"
        [TestMethod]
        [TestCategory("$TestCategory")]
        [Timeout($TestTimeout)]
        public void ${propertyName}_ShouldBeAccessible()
        {
            // Arrange
            var testCompleted = new ManualResetEventSlim(false);
            Exception? testException = null;

            var thread = new Thread(() => {
                try
                {
                    using var scope = _serviceProvider!.CreateScope();
                    // TODO: Get required services from scope

                    // Create instance of $className or mock its dependencies
                    // var instance = new $className(...);

                    // Act & Assert
                    // var value = instance.$propertyName;
                    // Assert.IsNotNull(value);
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    testCompleted.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!testCompleted.Wait(12000))
            {
                Assert.Fail("$propertyName property test timed out");
            }

            Assert.IsNull(testException, \$"$propertyName property test failed with exception: {testException}");
        }

"@
}

# Close the class and namespace
$testFileContent += @"
    }
}
"@

# Write the test file
$testFileContent | Out-File -FilePath $OutputPath -Encoding utf8

Write-Host "Test file generated at: $OutputPath"
Write-Host "Class: $className"
Write-Host "Methods: $($methodMatches.Count)"
Write-Host "Properties: $($propertyMatches.Count)"
