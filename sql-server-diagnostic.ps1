# SQL Server Express Diagnostic and Setup Script
Write-Host "=== SQL Server Express Diagnostic ===" -ForegroundColor Cyan
Write-Host "Checking SQL Server Express installation and configuration" -ForegroundColor Yellow
Write-Host ""

# Function to check if a service exists and its status
function Get-ServiceInfo {
    param($ServiceName)
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($service) {
            return @{
                Exists = $true
                Status = $service.Status
                StartType = $service.StartType
            }
        } else {
            return @{ Exists = $false }
        }
    } catch {
        return @{ Exists = $false }
    }
}

# Check for SQL Server services
Write-Host "1. Checking SQL Server Services:" -ForegroundColor Green
$sqlServices = @(
    "MSSQLSERVER",           # Default instance
    "MSSQL`$SQLEXPRESS",     # SQL Server Express default
    "MSSQL`$SQLEXPRESS01",   # The instance we're trying to use
    "SQLBrowser"             # SQL Server Browser
)

foreach ($serviceName in $sqlServices) {
    $serviceInfo = Get-ServiceInfo $serviceName
    if ($serviceInfo.Exists) {
        $statusColor = if ($serviceInfo.Status -eq "Running") { "Green" } else { "Yellow" }
        Write-Host "  ✓ $serviceName - Status: $($serviceInfo.Status), StartType: $($serviceInfo.StartType)" -ForegroundColor $statusColor
    } else {
        Write-Host "  ✗ $serviceName - Not found" -ForegroundColor Red
    }
}

Write-Host ""

# Check for SQL Server installations using registry
Write-Host "2. Checking SQL Server Installations:" -ForegroundColor Green
try {
    $sqlInstances = @()

    # Check registry for installed instances
    $regPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL"
    )

    foreach ($regPath in $regPaths) {
        if (Test-Path $regPath) {
            $instances = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
            if ($instances) {
                $instances.PSObject.Properties | Where-Object { $_.Name -ne "PSPath" -and $_.Name -ne "PSParentPath" -and $_.Name -ne "PSChildName" -and $_.Name -ne "PSDrive" -and $_.Name -ne "PSProvider" } | ForEach-Object {
                    $sqlInstances += @{
                        Name = $_.Name
                        Path = $_.Value
                        RegistryPath = $regPath
                    }
                }
            }
        }
    }

    if ($sqlInstances.Count -gt 0) {
        foreach ($instance in $sqlInstances) {
            Write-Host "  ✓ Found instance: $($instance.Name) at $($instance.Path)" -ForegroundColor Green
        }
    } else {
        Write-Host "  ✗ No SQL Server instances found in registry" -ForegroundColor Red
    }
} catch {
    Write-Host "  ⚠ Error checking registry: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Try to discover SQL Server instances using sqlcmd
Write-Host "3. Discovering SQL Server Instances:" -ForegroundColor Green
try {
    $discoveredInstances = & sqlcmd -L 2>&1
    if ($LASTEXITCODE -eq 0 -and $discoveredInstances) {
        Write-Host "  Available SQL Server instances:" -ForegroundColor Cyan
        $discoveredInstances | Where-Object { $_ -and $_.Trim() -ne "" } | ForEach-Object {
            Write-Host "    $_" -ForegroundColor White
        }
    } else {
        Write-Host "  ✗ No instances discovered via sqlcmd -L" -ForegroundColor Red
    }
} catch {
    Write-Host "  ⚠ Error discovering instances: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Test common connection strings
Write-Host "4. Testing Common Connection Strings:" -ForegroundColor Green
$connectionStrings = @(
    "(localdb)\MSSQLLocalDB",        # LocalDB default
    ".\SQLEXPRESS",                  # Local SQL Express default
    "localhost\SQLEXPRESS",          # Local SQL Express
    "$env:COMPUTERNAME\SQLEXPRESS",  # Computer name with SQL Express
    "$env:COMPUTERNAME\SQLEXPRESS01", # Computer name with SQLEXPRESS01
    "ST-LPTP9-23\SQLEXPRESS01"       # Current connection string
)

foreach ($connStr in $connectionStrings) {
    Write-Host "  Testing: $connStr" -ForegroundColor Gray
    try {
        $testResult = & sqlcmd -S $connStr -Q "SELECT @@VERSION" -h -1 -W 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "    ✓ SUCCESS - Connection works!" -ForegroundColor Green
            # Try to list databases
            $dbResult = & sqlcmd -S $connStr -Q "SELECT name FROM sys.databases WHERE name IN ('BusBusDB', 'master')" -h -1 -W 2>&1
            if ($dbResult) {
                Write-Host "    Available databases: $($dbResult -join ', ')" -ForegroundColor Cyan
            }
        } else {
            Write-Host "    ✗ Failed: $($testResult -join ' ')" -ForegroundColor Red
        }
    } catch {
        Write-Host "    ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# Recommendations
Write-Host "5. Recommendations:" -ForegroundColor Green

# Check if any SQL Server is running
$anySqlRunning = $false
foreach ($serviceName in $sqlServices) {
    $serviceInfo = Get-ServiceInfo $serviceName
    if ($serviceInfo.Exists -and $serviceInfo.Status -eq "Running") {
        $anySqlRunning = $true
        break
    }
}

if (-not $anySqlRunning) {
    Write-Host "  📋 No SQL Server services are running. Choose one of these options:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Option A - Use LocalDB (Recommended for development):" -ForegroundColor Cyan
    Write-Host "    1. Update connection string to: (localdb)\MSSQLLocalDB" -ForegroundColor White
    Write-Host "    2. LocalDB is likely already installed with Visual Studio" -ForegroundColor White
    Write-Host ""
    Write-Host "  Option B - Install/Start SQL Server Express:" -ForegroundColor Cyan
    Write-Host "    1. Download SQL Server Express from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads" -ForegroundColor White
    Write-Host "    2. Install with instance name SQLEXPRESS01" -ForegroundColor White
    Write-Host "    3. Or start existing SQL Server Express service" -ForegroundColor White
    Write-Host ""
    Write-Host "  Option C - Use existing SQL Server instance:" -ForegroundColor Cyan
    Write-Host "    1. Find a working connection string from the test results above" -ForegroundColor White
    Write-Host "    2. Update appsettings.json with the working connection string" -ForegroundColor White
} else {
    Write-Host "  📋 SQL Server is running. Update your connection string to use a working instance from the test results above." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan
