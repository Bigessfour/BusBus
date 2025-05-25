@echo off
echo ====================================
echo BusBus Dark Theme Test Launcher
echo ====================================
echo.

echo Checking build status...
dotnet build BusBus.sln --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed! Please fix compilation errors first.
    pause
    exit /b 1
)

echo Build successful!
echo.

echo Running theme-specific tests...
dotnet test --filter "Category=Unit" --no-build --verbosity minimal --logger "console;verbosity=minimal"
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: Some theme tests failed!
    echo.
)

echo.
echo ====================================
echo DARK THEME TESTING INSTRUCTIONS
echo ====================================
echo.
echo The application will now launch. Please test:
echo 1. Default light theme appearance
echo 2. Click theme toggle button to switch to dark
echo 3. Verify dark theme colors:
echo    - Background: Dark gray (#121212)
echo    - Text: Soft white (#E0E0E0) 
echo    - Buttons: Professional blue (#42A5F5)
echo 4. Test theme switching multiple times
echo 5. Restart app to verify theme persistence
echo.
echo Press any key to launch BusBus application...
pause > nul

echo.
echo Launching BusBus Dashboard...
echo Press Ctrl+C in this window to stop the application
echo.

dotnet run --project BusBus.csproj
