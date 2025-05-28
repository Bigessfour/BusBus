# Archived UI Files

This folder contains UI files that were part of earlier iterations of the BusBus application but are not currently in active use. These files were moved here to reduce conflicts and confusion with the active UI components.

## Files in this folder:

1. `Dashboard.cs.bak` - A backup of the Dashboard form
2. `SidePanel.cs` - An alternative implementation of the side panel
3. `SidePanel.Designer.cs` - Designer file for the alternative side panel
4. `SampleSidePanelForm.cs` - A sample side panel form, likely for testing/demonstration

## Why these files were archived:

The application had multiple iterations of UI components, resulting in conflicts when loading the Dashboard. By separating active and inactive files, we can focus on making the core components work correctly before potentially incorporating useful code from these archived files.

## Active UI Structure:

The active UI components follow this structure:
1. `Dashboard.cs` - Main container/shell form with header, side panel, content panel, and status bar
2. View files loaded into the content panel:
   - `DashboardView.cs` - Home view (when Dashboard is selected)
   - `RouteListView.cs` - Routes view
   - `DriverListView.cs` - Drivers view
   - `VehicleListView.cs` - Vehicles view
   - `ReportsView.cs` - Reports view
   - `SettingsView.cs` - Settings view
3. Support files:
   - `BaseView.cs` - Base class for all views
   - `ThemeManager.cs` - Manages application themes
   - `TextRenderingManager.cs` - Handles text rendering quality

## How to use these files:

If you need to reference code or functionality from these files, they are preserved here for reference. You can copy specific parts into the active files as needed, or restore them to the main UI folder if you want to switch to using them.

Date archived: May 28, 2025
