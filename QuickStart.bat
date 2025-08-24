@echo off
title Budget Management - Quick Start
color 0A

echo.
echo  ███████╗ Budget Management App ███████╗
echo  ███████╗     Quick Start      ███████╗
echo.

REM Check if we're in the right directory
if not exist "BudgetManagement.csproj" (
    echo ERROR: Please run this from the Budget Management folder
    echo.
    pause
    exit /b 1
)

echo [1/4] Stopping any running instances...
taskkill /F /IM "BudgetManagement.exe" 2>nul

echo [2/4] Cleaning project...
dotnet clean >nul 2>&1

echo [3/4] Building application (this may take a moment)...
dotnet publish -c Release --self-contained true -r win-x64 -o publish

if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ BUILD FAILED!
    echo Make sure .NET SDK is installed
    echo.
    pause
    exit /b 1
)

echo [4/4] Starting Budget Management...
echo.
echo ✅ Ready! The app should open shortly.
echo.

cd publish
start "" "BudgetManagement.exe"

REM Auto-close after 2 seconds
timeout /t 2 /nobreak >nul
exit