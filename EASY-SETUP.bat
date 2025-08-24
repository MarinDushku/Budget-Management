@echo off
title Budget Management - Easy Setup
color 0B

echo.
echo  ███████╗ Budget Management ███████╗
echo  ███████╗   Easy Setup      ███████╗
echo.
echo This will automatically create a desktop shortcut
echo with the Budget Management icon.
echo.
pause

echo Creating desktop shortcut...
echo.

REM Run PowerShell script to create shortcut
powershell -ExecutionPolicy Bypass -File "Setup-Desktop-Shortcut.ps1"

echo.
echo Setup complete!
pause