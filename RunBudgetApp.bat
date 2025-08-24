@echo off
echo Budget Management App - Starting...
echo.

REM Kill any existing instances
taskkill /F /IM "BudgetManagement.exe" 2>nul

REM Clean and build the application
echo Cleaning previous build...
dotnet clean >nul 2>&1

echo Building application...
dotnet publish -c Release --self-contained true -r win-x64 -o publish >nul 2>&1

if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed! Make sure .NET SDK is installed.
    pause
    exit /b 1
)

echo Starting Budget Management...
cd publish
start "" "BudgetManagement.exe"

REM Optional: Keep window open for 3 seconds to show status
timeout /t 3 /nobreak >nul
exit