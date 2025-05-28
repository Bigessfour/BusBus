#!/usr/bin/env pwsh

Write-Host "Creating Message Box Suppression Fix" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Get all UI files
$uiFiles = Get-ChildItem -Path "UI" -Recurse -Include "*.cs" | Where-Object { $_.FullName -notlike "*\Common\MessageBoxHelper.cs" }

Write-Host "Found $($uiFiles.Count) UI files to check for MessageBox calls" -ForegroundColor Yellow
Write-Host ""

# Create the modified file contents
foreach ($file in $uiFiles) {
    $content = Get-Content -Path $file.FullName -Raw

    # Skip files that don't have MessageBox.Show calls
    if ($content -notmatch "MessageBox\.Show") {
        Write-Host "  Skipping $($file.Name) - no MessageBox calls" -ForegroundColor Gray
        continue
    }

    # Ensure the file uses the BusBus.UI.Common namespace
    if ($content -notmatch "using BusBus\.UI\.Common;") {
        $content = $content -replace "using System\.Windows\.Forms;", "using System.Windows.Forms;`r`nusing BusBus.UI.Common;"
    }

    # Replace MessageBox.Show with MessageBoxHelper.Show
    $originalContent = $content
    $content = $content -replace "MessageBox\.Show\(", "MessageBoxHelper.Show("

    # Save the modified content back to the file
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content
        Write-Host "  ✅ Updated $($file.Name) - replaced MessageBox.Show calls" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️ No changes made to $($file.Name)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Creating Global Dialog Suppression in Tests" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

# Add global dialog suppression to all UI tests
$testFiles = Get-ChildItem -Path "BusBus.Tests\UI" -Recurse -Include "*Tests.cs"

foreach ($testFile in $testFiles) {
    $content = Get-Content -Path $testFile.FullName -Raw

    # Skip files that already have the suppression code
    if ($content -match "MessageBoxHelper\.SuppressAllDialogs = true") {
        Write-Host "  Skipping $($testFile.Name) - already has suppression" -ForegroundColor Gray
        continue
    }

    # Ensure the file uses the BusBus.UI.Common namespace
    if ($content -notmatch "using BusBus\.UI\.Common;") {
        $content = $content -replace "using System\.Windows\.Forms;", "using System.Windows.Forms;`r`nusing BusBus.UI.Common;"
    }

    # Add the setup code to suppress dialogs
    if ($content -match "\[SetUp\][\s\S]*?public[\s\S]*?(?:override )?.*?SetUp\(\)[\s\S]*?{") {
        $content = $content -replace "(\[SetUp\][\s\S]*?public[\s\S]*?(?:override )?.*?SetUp\(\)[\s\S]*?{)", "`$1`r`n            // Suppress all dialogs during testing`r`n            MessageBoxHelper.SuppressAllDialogs = true;"
    }

    # Add teardown code to reset the suppression
    if ($content -match "\[TearDown\][\s\S]*?public[\s\S]*?(?:override )?.*?TearDown\(\)[\s\S]*?{") {
        $content = $content -replace "(\[TearDown\][\s\S]*?public[\s\S]*?(?:override )?.*?TearDown\(\)[\s\S]*?{)", "`$1`r`n            // Reset dialog suppression`r`n            MessageBoxHelper.SuppressAllDialogs = false;"
    }

    # Save the modified content back to the file
    Set-Content -Path $testFile.FullName -Value $content
    Write-Host "  ✅ Updated $($testFile.Name) - added global dialog suppression" -ForegroundColor Green
}

Write-Host ""
Write-Host "Dialog suppression fix applied to all UI components and tests!" -ForegroundColor Green
Write-Host "Run your tests now without any dialogs interrupting them." -ForegroundColor Cyan
