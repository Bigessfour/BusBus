# Safety Measures Verification Script
# This script verifies that all critical safety measures are in place

Write-Host "🔒 Verifying Test Safety Measures..." -ForegroundColor Green

$allChecksPass = $true

# Check 1: coverlet.runsettings exists and has timeout
Write-Host "`nChecking coverlet.runsettings..." -ForegroundColor Yellow
if (Test-Path "coverlet.runsettings") {
    $content = Get-Content "coverlet.runsettings" -Raw
    if ($content -match "TestSessionTimeout.*300000" -and $content -match "TestTimeout.*30000") {
        Write-Host "✅ coverlet.runsettings has proper timeouts" -ForegroundColor Green
    }
    else {
        Write-Host "❌ coverlet.runsettings missing critical timeouts" -ForegroundColor Red
        $allChecksPass = $false
    }
}
else {
    Write-Host "❌ coverlet.runsettings not found" -ForegroundColor Red
    $allChecksPass = $false
}

# Check 2: GitHub workflow exists
Write-Host "`nChecking GitHub workflow..." -ForegroundColor Yellow
if (Test-Path ".github/workflows/safe-tests.yml") {
    $content = Get-Content ".github/workflows/safe-tests.yml" -Raw
    if ($content -match "timeout-minutes" -and $content -match "self-hosted") {
        Write-Host "✅ GitHub safe-tests workflow configured" -ForegroundColor Green
    }
    else {
        Write-Host "❌ GitHub workflow missing critical safety features" -ForegroundColor Red
        $allChecksPass = $false
    }
}
else {
    Write-Host "❌ GitHub workflow not found" -ForegroundColor Red
    $allChecksPass = $false
}

# Check 3: Safe runner script exists
Write-Host "`nChecking safe runner script..." -ForegroundColor Yellow
if (Test-Path "run-tests-safely.ps1") {
    $content = Get-Content "run-tests-safely.ps1" -Raw
    if ($content -match "GitHub Actions self-hosted runner" -and $content -match "TimeoutMinutes") {
        Write-Host "✅ Safe runner script configured" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Safe runner script missing safety features" -ForegroundColor Red
        $allChecksPass = $false
    }
}
else {
    Write-Host "❌ Safe runner script not found" -ForegroundColor Red
    $allChecksPass = $false
}

# Check 4: TestBase has timeout attributes
Write-Host "`nChecking TestBase safety measures..." -ForegroundColor Yellow
if (Test-Path "BusBus.Tests/TestBase.cs") {
    $content = Get-Content "BusBus.Tests/TestBase.cs" -Raw
    if ($content -match "\[Timeout\(30000\)\]" -and $content -match "CancellationTokenSource") {
        Write-Host "✅ TestBase has timeout protection" -ForegroundColor Green
    }
    else {
        Write-Host "❌ TestBase missing timeout attributes" -ForegroundColor Red
        $allChecksPass = $false
    }
}
else {
    Write-Host "❌ TestBase.cs not found" -ForegroundColor Red
    $allChecksPass = $false
}

# Check 5: Safety documentation exists
Write-Host "`nChecking safety documentation..." -ForegroundColor Yellow
if (Test-Path "TEST_SAFETY_MEASURES.md") {
    Write-Host "✅ Safety documentation exists" -ForegroundColor Green
}
else {
    Write-Host "❌ Safety documentation not found" -ForegroundColor Red
    $allChecksPass = $false
}

# Check 6: Build succeeds
Write-Host "`nChecking build status..." -ForegroundColor Yellow
$buildResult = dotnet build BusBus.sln --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Solution builds successfully" -ForegroundColor Green
}
else {
    Write-Host "❌ Build failed - safety measures may have syntax errors" -ForegroundColor Red
    $allChecksPass = $false
}

# Final result
Write-Host "`n" + "="*60 -ForegroundColor Cyan
if ($allChecksPass) {
    Write-Host "🔒 ALL SAFETY MEASURES VERIFIED AND LOCKED IN!" -ForegroundColor Green
    Write-Host "✅ Test infinite loop protection is ACTIVE" -ForegroundColor Green
    Write-Host "✅ Use '.\run-tests-safely.ps1' for safe test execution" -ForegroundColor Green
}
else {
    Write-Host "🚨 CRITICAL: Some safety measures are missing!" -ForegroundColor Red
    Write-Host "❌ Fix the above issues before running tests" -ForegroundColor Red
}
Write-Host "="*60 -ForegroundColor Cyan
