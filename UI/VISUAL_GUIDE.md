# BusBus UI Visual Guide

This document helps you understand what you're seeing when the BusBus application runs.

## How to Run with Visual Debugging

1. Use one of these methods:
   - Run `.\run-visual-debug.bat`
   - Run `dotnet run --project BusBus.csproj -- --visual-debug`
   - Run `.\build-and-run.ps1` and follow the prompts

## What You Should See

When the application runs with visual debugging enabled, you'll see colored borders around the main UI components:

1. **MAIN HEADER** (Blue border) - The top header from Dashboard.cs
2. **NAVIGATION** (Green border) - The side panel with navigation buttons
3. **CONTENT AREA** (Orange border) - The main content area where views are loaded

## UI Structure

The application follows a shell/content pattern:

```
+--------------------------------------------------+
|                    Dashboard                     |  <- Main container (Dashboard.cs)
|                                                  |
| +----------------------------------------------+ |
| |               MAIN HEADER                    | |  <- Blue border
| +----------------------------------------------+ |
| |                                              | |
| | +---------------+ +------------------------+ | |
| | |               | |                        | | |
| | |  NAVIGATION   | |    CONTENT AREA        | | |
| | |  (Side Panel) | |                        | | |
| | |               | | +--------------------+ | | |
| | | * Dashboard   | | |   DashboardView    | | | |  <- Initially loaded inside
| | | * Routes      | | |                    | | | |     the content area
| | | * Drivers     | | |                    | | | |
| | | * Vehicles    | | |                    | | | |
| | | * Reports     | | |                    | | | |
| | | * Settings    | | |                    | | | |
| | |               | | |                    | | | |
| | |               | | +--------------------+ | | |
| | |               | |                        | | |
| | +---------------+ +------------------------+ | |
| |                                              | |
| +----------------------------------------------+ |
| |               Status Strip                   | |  <- At the bottom
| +----------------------------------------------+ |
+--------------------------------------------------+
```

## Navigation Process

1. When you click a navigation button in the side panel:
   - The current view is deactivated
   - The new view is loaded into the CONTENT AREA
   - The MAIN HEADER and NAVIGATION stay the same

2. For example, clicking "Routes" will:
   - Replace DashboardView with RouteListView in the CONTENT AREA
   - Keep the rest of the UI the same

## How This Helps You

By seeing the UI structure visually highlighted, you can:
- Understand which code controls which part of the UI
- See how navigation works without changing the outer shell
- Identify any UI layout issues more easily

This visual approach helps you learn the application structure without diving into code details.
