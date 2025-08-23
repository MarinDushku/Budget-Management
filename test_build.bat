@echo off
echo Building Budget Management Application...
dotnet build --configuration Debug
if %ERRORLEVEL% EQU 0 (
    echo Build successful! Starting application...
    dotnet run --configuration Debug
) else (
    echo Build failed. Please check the errors above.
    pause
)