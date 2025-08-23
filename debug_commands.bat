@echo off
echo ========================================
echo BUDGET MANAGEMENT - COMMAND DEBUG TEST
echo ========================================
echo.
echo This test will:
echo 1. Clean and build the application
echo 2. Run the application with debug output
echo 3. Monitor command execution
echo.
echo INSTRUCTIONS:
echo - Look at the status bar at the bottom of the window
echo - Click "Add Income" - you should see status messages like:
echo   "🔄 Add Income command triggered..."
echo - Click "Add Spending" - you should see:
echo   "🔄 Add Spending command triggered..."
echo.
echo If you don't see these messages, the commands aren't being executed.
echo.
pause

echo Cleaning previous build...
dotnet clean

echo Building with debug information...
dotnet build --configuration Debug --verbosity normal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Build successful! Starting application with debug output...
    echo.
    echo WATCH THE STATUS BAR for debug messages when you click buttons!
    echo.
    dotnet run --configuration Debug
) else (
    echo.
    echo ❌ Build failed. Check errors above.
    pause
)