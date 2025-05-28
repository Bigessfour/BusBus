#!/usr/bin/env pwsh
# Script to convert NUnit attributes to MSTest attributes for .NET 8 compliance
# Following Microsoft recommendations from: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest

Write-Host "Converting BusBus.Tests from NUnit to MSTest for .NET 8 compliance..." -ForegroundColor Cyan
Write-Host "Following Microsoft's testing framework recommendations" -ForegroundColor Yellow

$testFiles = Get-ChildItem -Path "BusBus.Tests" -Recurse -Filter "*.cs" | Where-Object {
    $_.Name -notlike "*.Designer.cs" -and $_.Directory.Name -ne "bin" -and $_.Directory.Name -ne "obj"
}

Write-Host "Found $($testFiles.Count) test files to convert" -ForegroundColor Green

$conversions = @{
    # NUnit -> MSTest attribute mappings per Microsoft documentation
    '[TestFixture]'                     = '[TestClass]'
    '[Test]'                            = '[TestMethod]'
    '[SetUp]'                           = '[TestInitialize]'
    '[TearDown]'                        = '[TestCleanup]'
    '[OneTimeSetUp]'                    = '[ClassInitialize]'
    '[OneTimeTearDown]'                 = '[ClassCleanup]'
    '[Category('                        = '[TestCategory('
    'using NUnit.Framework;'            = 'using Microsoft.VisualStudio.TestTools.UnitTesting;'
    'using NUnit.Framework.Interfaces;' = '// Removed NUnit.Framework.Interfaces - not needed in MSTest'
    'TestContext.WriteLine'             = 'TestContext.WriteLine'
    # Remove NUnit-specific attributes that don't have MSTest equivalents
    '[Platform("Win")]'                 = '// [Platform("Win")] - Removed (MSTest doesn''t need platform attributes for Windows)'
    '[Apartment(ApartmentState.STA)]'   = '[STATestMethod] // MSTest STA support'
    '[Ignore]'                          = '[Ignore]'
    '[Description('                     = '// [Description( - Converted to TestMethod DisplayName'
}

$processedFiles = 0
$totalConversions = 0

foreach ($file in $testFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $originalContent = $content
    $fileConversions = 0

    foreach ($find in $conversions.Keys) {
        $replace = $conversions[$find]
        if ($content -match [regex]::Escape($find)) {
            $content = $content -replace [regex]::Escape($find), $replace
            $fileConversions++
        }
    }

    # Special handling for Description attributes - convert to DisplayName in TestMethod
    $content = $content -replace '\[TestMethod\]\s*\n\s*\/\/\s*\[Description\("([^"]+)"\)', '[TestMethod("$1")]'

    # Remove custom timeout attributes for now (MSTest has Timeout attribute)
    $content = $content -replace '\[TestTimeout\([^\]]+\)\]', '[Timeout(30000)] // 30 second timeout'

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $processedFiles++
        $totalConversions += $fileConversions
        Write-Host "✓ Converted $($file.Name) ($fileConversions changes)" -ForegroundColor Green
    }
}

Write-Host "`nConversion Summary:" -ForegroundColor Cyan
Write-Host "Files processed: $processedFiles" -ForegroundColor Yellow
Write-Host "Total conversions: $totalConversions" -ForegroundColor Yellow
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Remove custom TestTimeoutAttribute.cs file" -ForegroundColor White
Write-Host "2. Update using statements" -ForegroundColor White
Write-Host "3. Test the converted test suite" -ForegroundColor White
Write-Host "4. Verify compliance with Microsoft .NET 8 guidelines" -ForegroundColor White
