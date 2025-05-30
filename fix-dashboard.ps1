$dashboardPath = "c:\Users\steve.mckitrick\Desktop\BusBus\UI\Dashboard.cs"
$lines = Get-Content $dashboardPath -TotalCount 1190

# Add proper closing structure
$lines += "        }"
$lines += "        #endregion"
$lines += "    }"
$lines += "}"

# Write the clean file
$lines | Set-Content "$dashboardPath"

Write-Host "Dashboard.cs file fixed."
