# Deployment Guide - Getting the App to Grandpa's PC

## Option 1: Simple Folder Copy (Easiest)

### Requirements on Grandpa's PC:
- Windows 10 or newer
- No additional software needed (app is self-contained)

### Steps:
1. **Build the app** (on a Windows computer with .NET):
   ```
   dotnet publish -c Release --self-contained true -r win-x64 -o "BudgetApp"
   ```

2. **Copy the BudgetApp folder** to:
   - USB drive
   - Email as zip file
   - Google Drive/Dropbox

3. **On Grandpa's PC**:
   - Copy folder to `C:\Program Files\Budget Management\`
   - Create desktop shortcut to `BudgetManagement.exe`
   - Double-click to run!

## Option 2: Professional Installer

### Create Windows Installer:
1. Download **Inno Setup** (free Windows installer creator)
2. Use the script below to create `setup.exe`

### Installer Script (save as `setup.iss`):
```ini
[Setup]
AppName=Budget Management
AppVersion=1.0
DefaultDirName={pf}\Budget Management
DefaultGroupName=Budget Management
OutputDir=installer
OutputBaseFilename=BudgetManagement_Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Budget Management"; Filename: "{app}\BudgetManagement.exe"
Name: "{commondesktop}\Budget Management"; Filename: "{app}\BudgetManagement.exe"

[Run]
Filename: "{app}\BudgetManagement.exe"; Description: "Launch Budget Management"; Flags: postinstall nowait skipifsilent
```

## Option 3: Direct Development on Grandpa's PC

If his PC has internet, you could:
1. Install .NET 8 Runtime on his PC
2. Copy the source code folder
3. Build and run directly

## Recommended Approach

**For easiest deployment:**
1. Build on any Windows PC with .NET
2. Copy the `publish` folder to USB drive
3. On grandpa's PC: Copy to `C:\Budget Management\`
4. Create desktop shortcut
5. Give him the `USER_GUIDE.md` file

## Troubleshooting

**If app doesn't start:**
- Check Windows version (needs Windows 10+)
- Run as Administrator once
- Check antivirus isn't blocking it

**If data doesn't save:**
- Ensure folder has write permissions
- Run as Administrator once to create data folders

## Files to Include:
- ✅ BudgetManagement.exe (main app)
- ✅ All DLL files
- ✅ DatabaseSchema.sql
- ✅ USER_GUIDE.md
- ✅ Desktop shortcut

The app will automatically create its data folder in:
`C:\Users\[Grandpa's Name]\AppData\Roaming\BudgetManagement\`