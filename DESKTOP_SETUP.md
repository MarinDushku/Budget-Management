# Desktop Setup Instructions

## Method 1: Quick Desktop Shortcut (Recommended)

1. **Download the app:**
   ```powershell
   git clone https://github.com/MarinDushku/Budget-Management.git
   cd Budget-Management
   ```

2. **Right-click on `RunBudgetApp.bat`** → **"Send to"** → **"Desktop (create shortcut)"**

3. **Customize the desktop shortcut:**
   - Right-click the shortcut on desktop → **"Properties"**
   - Click **"Change Icon"** → **"Browse"** → Select `Resources/MD Logo.ico`
   - Change name to: **"Budget Management"**
   - Click **"OK"**

4. **Double-click the desktop icon** to run the app!

---

## Method 2: Direct Executable Shortcut (After First Build)

1. **First, run the build once:**
   ```powershell
   dotnet clean
   dotnet publish -c Release --self-contained true -r win-x64 -o publish
   ```

2. **Create shortcut to the executable:**
   - Navigate to the `publish` folder
   - Right-click `BudgetManagement.exe` → **"Send to"** → **"Desktop (create shortcut)"**
   - Rename to: **"Budget Management"**

3. **Set custom icon:**
   - Right-click shortcut → **"Properties"** → **"Change Icon"**
   - Browse to: `Budget-Management/Resources/MD Logo.ico`

---

## Method 3: Professional Installer (Advanced)

Create a Windows installer using the batch file:

1. **Create installer shortcut:**
   - Copy `RunBudgetApp.bat` to Desktop
   - Rename to: **"Install & Run Budget Management.bat"**
   - Right-click → **Properties** → **Change Icon** → Browse to ico file

---

## What Each Method Does:

- **Method 1**: Always rebuilds and runs (good for development/updates)
- **Method 2**: Runs directly from built executable (fastest startup)
- **Method 3**: Creates installer-like experience

## Recommended for Grandpa: Method 1
- Single icon on desktop
- Always runs the latest version
- Handles updates automatically
- Professional appearance with custom icon