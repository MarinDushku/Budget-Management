# Budget Management v1.0.0

A simple, senior-friendly Windows desktop application for personal budget management.

## Features

- **💰 Income Tracking**: Easy income entry with date, description, and amount
- **💸 Spending Management**: Track expenses across three categories (Family, Personal, Marini)
- **📊 Budget Summary**: Real-time overview of total income, spending, and remaining budget
- **🗄️ Local Storage**: All data stored locally in SQLite database - no internet required
- **👴 Senior-Friendly Design**: Large fonts, clear buttons, and intuitive interface
- **💾 Automatic Backups**: Built-in database backup system

## Installation

1. Download the latest release from the [Releases](https://github.com/MarinDushku/Budget-Management/releases) page
2. Extract the ZIP file to your desired location
3. Run `BudgetManagement.exe`

No installation required - it's a standalone application!

## How to Use

1. **Launch the app** - The main window shows your budget summary
2. **Add Income** - Click "➕ Add Income" to record salary, pension, etc.
3. **Add Spending** - Click "💸 Add Spending" to track expenses by category
4. **View Summary** - See your total income, spending, and remaining budget at a glance

## Categories

- **Family**: Groceries, utilities, household expenses
- **Personal**: Clothing, hobbies, entertainment
- **Marini**: Special category for Marini-related expenses

## Technical Details

- **Framework**: .NET 8 WPF
- **Database**: SQLite (local file)
- **Architecture**: MVVM pattern with dependency injection
- **Platform**: Windows 10/11

## Building from Source

1. Install .NET 8 SDK
2. Clone this repository
3. Run the following commands:

```bash
dotnet clean
dotnet publish -c Release --self-contained true -r win-x64 -o publish
cd publish
./BudgetManagement.exe
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Screenshots

The application features a clean, modern interface designed specifically for ease of use by seniors, with large buttons, clear text, and intuitive navigation.