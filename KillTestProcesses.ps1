# PowerShell script to forcefully kill hanging test processes
# Usage: .\KillTestProcesses.ps1

Write-Host "Checking for hanging .NET test processes..." -ForegroundColor Yellow

# Kill dotnet test processes
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
if ($dotnetProcesses) {
    Write-Host "Found $($dotnetProcesses.Count) dotnet processes. Killing..." -ForegroundColor Red
    $dotnetProcesses | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force
            Write-Host "Killed process ID: $($_.Id)" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to kill process ID: $($_.Id) - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Kill testhost processes
$testhostProcesses = Get-Process -Name "testhost*" -ErrorAction SilentlyContinue
if ($testhostProcesses) {
    Write-Host "Found $($testhostProcesses.Count) testhost processes. Killing..." -ForegroundColor Red
    $testhostProcesses | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force
            Write-Host "Killed testhost process ID: $($_.Id)" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to kill testhost process ID: $($_.Id) - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Kill any MSBuild processes
$msbuildProcesses = Get-Process -Name "MSBuild*" -ErrorAction SilentlyContinue
if ($msbuildProcesses) {
    Write-Host "Found $($msbuildProcesses.Count) MSBuild processes. Killing..." -ForegroundColor Red
    $msbuildProcesses | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force
            Write-Host "Killed MSBuild process ID: $($_.Id)" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to kill MSBuild process ID: $($_.Id) - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Kill any VBCSCompiler processes (Roslyn compiler)
$compilerProcesses = Get-Process -Name "VBCSCompiler" -ErrorAction SilentlyContinue
if ($compilerProcesses) {
    Write-Host "Found $($compilerProcesses.Count) VBCSCompiler processes. Killing..." -ForegroundColor Red
    $compilerProcesses | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force
            Write-Host "Killed VBCSCompiler process ID: $($_.Id)" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to kill VBCSCompiler process ID: $($_.Id) - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host "Process cleanup complete." -ForegroundColor Green
Write-Host "You can now safely run tests again." -ForegroundColor Cyan
