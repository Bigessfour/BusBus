
# PowerShell script to build, test, and generate coverage/CRAP report locally
# Now runs in a loop until stopped (Ctrl+C)

while ($true) {
    # Restore dependencies
    dotnet restore

    # Build the solution
    dotnet build --configuration Release

    # Run tests and collect coverage
    dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage" --results-directory ./CoverageReport

    # Copy coverage.cobertura.xml from the actual output location if found
    $coverageFile = Get-ChildItem -Path . -Recurse -Filter coverage.cobertura.xml | Select-Object -First 1
    if ($coverageFile) {
        Copy-Item $coverageFile.FullName -Destination ./CoverageReport/coverage.cobertura.xml -Force
    }

    # Install ReportGenerator if not already installed
    if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
        dotnet tool install -g dotnet-reportgenerator-globaltool
        $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
    }

    # Generate HTML coverage and CRAP report
    reportgenerator -reports:CoverageReport/**/coverage.cobertura.xml -targetdir:CoverageReport -reporttypes:Html

    Write-Host "Coverage and CRAP report generated at CoverageReport/index.html"

    # Open the coverage report in Chrome
    $indexPath = Join-Path $PWD "CoverageReport/index.html"
    if (Test-Path $indexPath) {
        Start-Process "chrome.exe" $indexPath
    }
    else {
        Write-Host "Could not find CoverageReport/index.html to open in Chrome."
    }

    # --- Find the method with the highest CRAP score, prioritizing lowest coverage if tied ---
    $htmlFiles = Get-ChildItem -Path ./CoverageReport -Filter *.html | Where-Object { $_.Name -ne 'index.html' }
    $bestTarget = $null
    foreach ($file in $htmlFiles) {
        $content = Get-Content $file.FullName -Raw
        # Regex: method name, coverage %, CRAP score
        $methodResults = [regex]::Matches($content, '<tr><td title="([^"]+)"><a [^>]+>([^<]+)</a></td><td>([0-9.]+)%?</td><td>([0-9]+)</td>')
        foreach ($m in $methodResults) {
            $method = $m.Groups[1].Value
            $coverage = [double]$m.Groups[3].Value
            $crap = [int]$m.Groups[4].Value
            if ($null -eq $bestTarget) {
                $bestTarget = @{ Method = $method; Coverage = $coverage; Crap = $crap; File = $file.Name }
            }
            elseif ($crap -gt $bestTarget.Crap -or ($crap -eq $bestTarget.Crap -and $coverage -lt $bestTarget.Coverage)) {
                $bestTarget = @{ Method = $method; Coverage = $coverage; Crap = $crap; File = $file.Name }
            }
        }
    }


    # --- Extract coverage percentage from index.html ---
    $indexHtml = Get-Content ./CoverageReport/index.html -Raw
    $coverageMatch = [regex]::Match($indexHtml, '<th>Line coverage:</th>\s*<td class="limit-width right" title="[^"]*">([0-9.]+)%?</td>')
    if ($coverageMatch.Success) {
        $coveragePercent = $coverageMatch.Groups[1].Value
    }
    else {
        $coveragePercent = "N/A"
    }

    $blue = "`e[34m"
    $reset = "`e[0m"
    if ($null -ne $bestTarget) {
        Write-Host ("${blue}Target for next test: $($bestTarget.Method) (CRAP: $($bestTarget.Crap), Coverage: $($bestTarget.Coverage)%) in $($bestTarget.File) | Project Coverage: $coveragePercent%${reset}")
        # Output a marker file for the agent to pick up
        Set-Content -Path "./CoverageReport/next_crap_target.txt" -Value "$($bestTarget.Method)|$($bestTarget.Crap)|$($bestTarget.Coverage)|$($bestTarget.File)"
        exit 0
    }
    else {
        Write-Host ("${blue}No methods with CRAP score found in HTML reports. | Project Coverage: $coveragePercent%${reset}")
        exit 1
    }
}


