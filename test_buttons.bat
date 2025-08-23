@echo off
echo Testing Budget Management Application - Add Income/Spending Buttons
echo Building application...
dotnet build --configuration Debug
if %ERRORLEVEL% EQU 0 (
    echo Build successful! Starting application...
    echo Click on Add Income and Add Spending buttons to test functionality.
    dotnet run --configuration Debug
) else (
    echo Build failed. Please check the errors above.
    pause
)