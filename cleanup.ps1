# Remove duplicate view files that are causing conflicts
Remove-Item -Path .\UI\Views\PlaceholderViews.cs -Force -ErrorAction SilentlyContinue
Remove-Item -Path .\UI\Views\DashboardView.cs -Force -ErrorAction SilentlyContinue
Remove-Item -Path .\UI\Views\RouteListView.cs -Force -ErrorAction SilentlyContinue
Remove-Item -Path .\UI\DashboardView.cs -Force -ErrorAction SilentlyContinue
Remove-Item -Path .\UI\ReportsView.cs -Force -ErrorAction SilentlyContinue
Remove-Item -Path .\UI\SettingsView.cs -Force -ErrorAction SilentlyContinue
Remove-Item -Path .\UI\RouteListView.cs -Force -ErrorAction SilentlyContinue
Remove-Item -Path .\UI\VehicleListView.cs -Force -ErrorAction SilentlyContinue
Remove-Item -Path .\Services\MaintenanceService.cs -Force -ErrorAction SilentlyContinue
Remove-Item -Path .\Services\VehicleService.cs -Force -ErrorAction SilentlyContinue

Write-Host "Cleaned up duplicate files"
