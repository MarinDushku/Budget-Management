@echo off
echo ======================================
echo BUDGET MANAGEMENT - DIALOG FIX TEST
echo ======================================
echo.
echo FIXED ISSUES:
echo ✅ Removed missing StaticResource references from dialogs
echo ✅ Added proper button styles directly in XAML
echo ✅ Fixed DatePicker, TextBox, and ComboBox styling
echo ✅ Added debug logging to DialogService
echo.
echo WHAT TO EXPECT:
echo 1. Click "Add Income" - Income dialog should now open
echo 2. Click "Add Spending" - Spending dialog should now open
echo 3. Status bar will show: "🔄 Opening Add Income/Spending dialog..."
echo 4. If dialogs still don't open, check debug output
echo.
pause

echo Cleaning and building...
dotnet clean
dotnet build --configuration Debug

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Build successful! Testing dialog functionality...
    echo.
    echo CLICK THE ADD INCOME AND ADD SPENDING BUTTONS NOW!
    echo.
    dotnet run --configuration Debug
) else (
    echo.
    echo ❌ Build failed. Check errors above.
    pause
)