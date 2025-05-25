@echo off
echo BusBus .NET Core Debugging Helper
echo ================================
echo.

:: Check if .NET Core is installed
where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET Core SDK not found in PATH!
    echo Please ensure .NET Core SDK is installed and in your PATH.
    exit /b 1
)

echo .NET Core SDK detected:
dotnet --version
echo.

:: Set environment variables for debugging
set DOTNET_ENVIRONMENT=Development
set LOGGING__LOGLEVEL__DEFAULT=Debug
set LOGGING__LOGLEVEL__MICROSOFT=Information
set LOGGING__LOGLEVEL__MICROSOFT.ENTITYFRAMEWORKCORE=Information

echo Debug environment variables set:
echo DOTNET_ENVIRONMENT=%DOTNET_ENVIRONMENT%
echo LOGGING__LOGLEVEL__DEFAULT=%LOGGING__LOGLEVEL__DEFAULT%
echo.

:: Command options menu
echo Available debug commands:
echo.
echo 1. Build solution with diagnostics
echo 2. Clean and rebuild solution
echo 3. Run application with enhanced debugging
echo 4. Run application with debug console
echo 5. Test database connection
echo 6. Show thread and cross-thread analysis
echo 7. Show current environment info
echo 8. Exit
echo.

:menu
set /p choice=Enter your choice (1-8):

if "%choice%"=="1" (
    echo Building solution with diagnostic logging...
    dotnet build BusBus.sln --verbosity diagnostic /flp:logfile=build.log;verbosity=diagnostic
    echo Build log saved to build.log
    goto menu
)

if "%choice%"=="2" (
    echo Cleaning and rebuilding solution...
    dotnet clean BusBus.sln
    dotnet build BusBus.sln
    goto menu
)

if "%choice%"=="3" (
    echo Running application with enhanced debugging...
    echo If app crashes, check the output for exceptions.
    dotnet run --project BusBus.csproj
    goto menu
)

if "%choice%"=="4" (
    echo Running application with debug console...
    dotnet run --project BusBus.csproj -- --debug-console
    goto menu
)

if "%choice%"=="5" (
    echo Testing database connection...
    dotnet run --project BusBus.csproj -- --test-db-connection
    goto menu
)

if "%choice%"=="6" (
    echo Analyzing threading patterns...
    powershell -Command "Write-Host 'Thread and Cross-Thread Analysis' -ForegroundColor Cyan; Write-Host '===============================' -ForegroundColor Cyan; Write-Host 'Scanning code for UI thread patterns...' -ForegroundColor Yellow; $uiFiles = Get-ChildItem -Path 'UI' -Filter '*.cs' -Recurse; Write-Host \"Found $($uiFiles.Count) UI files to analyze\" -ForegroundColor Green; $potentialIssues = @(); foreach ($file in $uiFiles) { $content = Get-Content $file.FullName; $lineNum = 1; foreach ($line in $content) { if ($line -match 'Thread|Task|Async|await|BeginInvoke|Invoke|ThreadPool|BackgroundWorker') { $potentialIssues += [PSCustomObject]@{ File = $file.Name; Line = $lineNum; Content = $line.Trim() }; } $lineNum++; } }; Write-Host \"Found $($potentialIssues.Count) potential threading patterns to review:\" -ForegroundColor Yellow; $potentialIssues | Format-Table File, Line, Content -AutoSize -Wrap"
    goto menu
)

if "%choice%"=="7" (
    echo Current environment information:
    echo.
    echo Operating System:
    systeminfo | findstr /B /C:"OS Name" /C:"OS Version"
    echo.
    echo .NET Information:
    dotnet --info
    echo.
    echo SQL Server Status:
    powershell -Command "Get-Service -Name 'MSSQL$SQLEXPRESS','SQLEXPRESS' -ErrorAction SilentlyContinue | Select-Object Name, Status, StartType | Format-Table"
    echo.
    goto menu
)

if "%choice%"=="8" (
    echo Exiting...
    exit /b 0
)

echo Invalid choice. Please try again.
goto menu
