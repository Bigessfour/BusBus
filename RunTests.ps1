param(
    [Parameter(Mandatory = $false)]
    [string]$Category = "",

    [Parameter(Mandatory = $false)]
    [string]$TestName = "",

    [Parameter(Mandatory = $false)]
    [switch]$NoBuild = $false,

    [Parameter(Mandatory = $false)]
    [switch]$ListTests = $false,

    [Parameter(Mandatory = $false)]
    [switch]$ListCategories = $false,

    [Parameter(Mandatory = $false)]
    [int]$Timeout = 15,

    [Parameter(Mandatory = $false)]
    [string]$Project = "BusBus.Tests\BusBus.Tests.csproj"
)

$solutionDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProjectPath = Join-Path $solutionDir $Project

# Function to format the output
function Format-ColorOutput {
    param(
        [string]$Text,
        [string]$Color = "White"
    )
    Write-Host $Text -ForegroundColor $Color
}

# List all test categories
if ($ListCategories) {
    Format-ColorOutput "Listing all test categories..." "Cyan"
    # Use dotnet to get all tests
    $tests = dotnet test $testProjectPath --list-tests

    # Extract unique categories using regex
    $categoryRegex = "\[TestCategory\(""([^""]+)""\)\]"
    $categories = $tests | Select-String -Pattern $categoryRegex -AllMatches |
    ForEach-Object { $_.Matches } |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object | Get-Unique

    Format-ColorOutput "`nAvailable test categories:" "Green"
    $categories | ForEach-Object { Format-ColorOutput "  • $_" "Yellow" }
    exit 0
}

# List all tests
if ($ListTests) {
    $filter = ""
    if ($Category) {
        $filter = "TestCategory=$Category"
    }
    if ($TestName) {
        if ($filter) {
            $filter += "&"
        }
        $filter += "FullyQualifiedName~$TestName"
    }

    Format-ColorOutput "Listing tests..." "Cyan"
    if ($filter) {
        Format-ColorOutput "Filter: $filter" "Magenta"
        dotnet test $testProjectPath --list-tests --filter "$filter"
    }
    else {
        dotnet test $testProjectPath --list-tests
    }
    exit 0
}

# Build the filter string
$filter = ""
if ($Category) {
    $filter = "TestCategory=$Category"
}
if ($TestName) {
    if ($filter) {
        $filter += "&"
    }
    $filter += "FullyQualifiedName~$TestName"
}

# Build the command
$command = "dotnet test $testProjectPath"
if ($filter) {
    $command += " --filter `"$filter`""
}
if ($NoBuild) {
    $command += " --no-build"
}

# Add timeout handling using PowerShell
$timeoutMilliseconds = $Timeout * 1000

Format-ColorOutput "Running tests with $Timeout second timeout..." "Cyan"
if ($filter) {
    Format-ColorOutput "Filter: $filter" "Magenta"
}

# Create a job to run the test
$job = Start-Job -ScriptBlock {
    param($cmd)
    Invoke-Expression $cmd
} -ArgumentList $command

# Wait for completion or timeout
$completed = Wait-Job $job -Timeout $Timeout
if ($completed -eq $null) {
    Format-ColorOutput "`nTEST EXECUTION TIMED OUT AFTER $Timeout SECONDS!" "Red"
    Format-ColorOutput "Stopping test run..." "Yellow"
    Stop-Job $job
    Remove-Job $job -Force
    exit 1
}

# Get the results
$result = Receive-Job $job
Remove-Job $job

# Display results
$result | ForEach-Object {
    if ($_ -match "Failed") {
        Format-ColorOutput $_ "Red"
    }
    elseif ($_ -match "Passed") {
        Format-ColorOutput $_ "Green"
    }
    elseif ($_ -match "Skipped") {
        Format-ColorOutput $_ "Yellow"
    }
    else {
        Write-Host $_
    }
}

# Exit with the same code as the test run
if ($result -match "Failed") {
    exit 1
}
exit 0
