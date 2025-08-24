# Budget Management - Automatic Desktop Setup
# This script creates a desktop shortcut with custom icon automatically

Write-Host "Budget Management - Desktop Setup" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# Get current directory and desktop path
$currentDir = Get-Location
$desktopPath = [System.Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "Budget Management.lnk"

# Paths for files
$batchFile = Join-Path $currentDir "QuickStart.bat"
$iconFile = Join-Path $currentDir "Resources\MD Logo.ico"

Write-Host "Current directory: $currentDir"
Write-Host "Desktop path: $desktopPath"
Write-Host ""

# Check if required files exist
if (-not (Test-Path $batchFile)) {
    Write-Host "❌ ERROR: QuickStart.bat not found!" -ForegroundColor Red
    Write-Host "Make sure you're running this from the Budget Management folder" -ForegroundColor Yellow
    pause
    exit 1
}

if (-not (Test-Path $iconFile)) {
    Write-Host "⚠️  WARNING: Icon file not found at $iconFile" -ForegroundColor Yellow
    Write-Host "Shortcut will be created without custom icon" -ForegroundColor Yellow
    $iconFile = $null
}

Write-Host "✅ Creating desktop shortcut..." -ForegroundColor Green

try {
    # Create COM object for shortcut
    $WScriptShell = New-Object -ComObject WScript.Shell
    $shortcut = $WScriptShell.CreateShortcut($shortcutPath)
    
    # Set shortcut properties
    $shortcut.TargetPath = $batchFile
    $shortcut.WorkingDirectory = $currentDir
    $shortcut.Description = "Budget Management Application v1.1.0"
    $shortcut.WindowStyle = 1  # Normal window
    
    # Set custom icon if available
    if ($iconFile -and (Test-Path $iconFile)) {
        $shortcut.IconLocation = "$iconFile,0"
        Write-Host "✅ Custom icon applied: MD Logo.ico" -ForegroundColor Green
    }
    
    # Save the shortcut
    $shortcut.Save()
    
    Write-Host ""
    Write-Host "🎉 SUCCESS!" -ForegroundColor Green
    Write-Host "Desktop shortcut created: 'Budget Management'" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "You can now:" -ForegroundColor White
    Write-Host "• Double-click the desktop icon to run the app" -ForegroundColor Yellow
    Write-Host "• The app will build and launch automatically" -ForegroundColor Yellow
    Write-Host ""
    
    # Optional: Open desktop folder to show the shortcut
    $openDesktop = Read-Host "Open Desktop folder to see the shortcut? (y/n)"
    if ($openDesktop -eq 'y' -or $openDesktop -eq 'Y') {
        Start-Process $desktopPath
    }
    
} catch {
    Write-Host "❌ ERROR creating shortcut: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")