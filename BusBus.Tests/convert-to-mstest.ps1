#!/usr/bin/env pwsh
param(
    [string]$TestDirectory = "."
)

Write-Host "Converting NUnit tests to MSTest in directory: $TestDirectory" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

# Find all C# test files
$testFiles = Get-ChildItem -Path $TestDirectory -Recurse -Filter "*.cs" | Where-Object {
    $_.Name -match "Test" -or $_.FullName -match "Test"
}

Write-Host "Found $($testFiles.Count) test files to process..." -ForegroundColor Yellow

$totalChanges = 0

foreach ($file in $testFiles) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor White

    $content = Get-Content -Path $file.FullName -Raw
    $originalContent = $content
    $fileChanges = 0

    # Replace using statements
    if ($content -match "using NUnit\.Framework;") {
        $content = $content -replace "using NUnit\.Framework;", "using Microsoft.VisualStudio.TestTools.UnitTesting;"
        $fileChanges++
        Write-Host "  - Updated using statement" -ForegroundColor Green
    }

    # Replace attributes
    $replacements = @{
        '\[TestFixture\]'              = '[TestClass]'
        '\[Test\]'                     = '[TestMethod]'
        '\[SetUp\]'                    = '[TestInitialize]'
        '\[TearDown\]'                 = '[TestCleanup]'
        '\[Category\("([^"]+)"\)\]'    = '[TestCategory("$1")]'
        '\[Category\(([^)]+)\)\]'      = '[TestCategory($1)]'
        '\[Description\("([^"]+)"\)\]' = '// Description: $1'
        '\[TestTimeout\((\d+)\)\]'     = '[Timeout($1)]'
        '\[Platform\([^)]+\)\]'        = '// Platform attribute removed (MSTest incompatible)'
        '\[Apartment\([^)]+\)\]'       = '// Apartment attribute removed (MSTest incompatible)'
    }

    foreach ($pattern in $replacements.Keys) {
        $replacement = $replacements[$pattern]
        if ($content -match $pattern) {
            $regexMatches = [regex]::Matches($content, $pattern)
            $content = $content -replace $pattern, $replacement
            $fileChanges += $regexMatches.Count
            Write-Host "  - Replaced $($regexMatches.Count) occurrences of $pattern" -ForegroundColor Green
        }
    }

    # Special handling for Assert methods that might differ
    if ($content -match "Assert\.That\(") {
        Write-Host "  - Warning: Found Assert.That() calls - manual review needed" -ForegroundColor Yellow
    }

    # Only write if changes were made
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  - Saved $fileChanges changes to $($file.Name)" -ForegroundColor Green
        $totalChanges += $fileChanges
    }
    else {
        Write-Host "  - No changes needed" -ForegroundColor Gray
    }
}

Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host "Conversion completed!" -ForegroundColor Green
Write-Host "Total files processed: $($testFiles.Count)" -ForegroundColor White
Write-Host "Total changes made: $totalChanges" -ForegroundColor White
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Build the test project to check for remaining errors" -ForegroundColor White
Write-Host "2. Manually review any Assert.That() calls that need conversion" -ForegroundColor White
Write-Host "3. Remove any remaining platform-specific attributes" -ForegroundColor White
Write-Host "4. Run tests to verify functionality" -ForegroundColor White
