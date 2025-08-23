# Budget Management Application - Project Structure

## Overview
This document outlines the complete project structure for a Windows desktop budget management application using WPF with MVVM pattern, designed with senior-friendly features.

## Directory Structure

```
BudgetManagement/
├── App.xaml                           # Application resources and startup
├── App.xaml.cs                        # Application entry point and DI setup
├── MainWindow.xaml                    # Main application window
├── MainWindow.xaml.cs                 # Main window code-behind
├── DatabaseSchema.sql                 # SQLite database schema
├── BudgetManagement.csproj            # Project file
│
├── Models/                            # Data models and entities
│   ├── DataModels.cs                  # Core data models (Income, Spending, Category, etc.)
│   ├── ValidationModels.cs            # Models with validation attributes
│   └── DTOs/                          # Data transfer objects
│       ├── BudgetSummaryDto.cs
│       ├── MonthlySummaryDto.cs
│       └── ExportDto.cs
│
├── ViewModels/                        # MVVM ViewModels
│   ├── BaseViewModel.cs               # Base ViewModel with INotifyPropertyChanged
│   ├── MainViewModel.cs               # Main window ViewModel
│   ├── IncomeDialogViewModel.cs       # Income entry dialog ViewModel
│   ├── SpendingDialogViewModel.cs     # Spending entry dialog ViewModel
│   ├── SettingsViewModel.cs           # Application settings ViewModel
│   └── Dialogs/
│       ├── CategoryManagerViewModel.cs
│       ├── ExportDialogViewModel.cs
│       └── BackupRestoreViewModel.cs
│
├── Views/                             # WPF Views and UserControls
│   ├── MainWindow.xaml                # Main application window
│   ├── MainWindow.xaml.cs
│   ├── Dialogs/
│   │   ├── IncomeDialog.xaml          # Income entry dialog
│   │   ├── IncomeDialog.xaml.cs
│   │   ├── SpendingDialog.xaml        # Spending entry dialog
│   │   ├── SpendingDialog.xaml.cs
│   │   ├── SettingsDialog.xaml        # Application settings dialog
│   │   ├── SettingsDialog.xaml.cs
│   │   ├── CategoryManagerDialog.xaml
│   │   ├── CategoryManagerDialog.xaml.cs
│   │   ├── ExportDialog.xaml
│   │   ├── ExportDialog.xaml.cs
│   │   ├── BackupRestoreDialog.xaml
│   │   └── BackupRestoreDialog.xaml.cs
│   │
│   ├── UserControls/
│   │   ├── BudgetSummaryControl.xaml  # Budget summary display
│   │   ├── BudgetSummaryControl.xaml.cs
│   │   ├── IncomeListControl.xaml     # Income entries list
│   │   ├── IncomeListControl.xaml.cs
│   │   ├── SpendingListControl.xaml   # Spending entries list
│   │   ├── SpendingListControl.xaml.cs
│   │   ├── DateRangeControl.xaml      # Date range selector
│   │   ├── DateRangeControl.xaml.cs
│   │   ├── SeniorFriendlyButton.xaml  # Large, accessible button
│   │   └── SeniorFriendlyButton.xaml.cs
│   │
│   └── Styles/
│       ├── ButtonStyles.xaml          # Button styling for senior users
│       ├── DataGridStyles.xaml        # Data grid styling
│       ├── DialogStyles.xaml          # Dialog styling
│       └── SeniorFriendlyTheme.xaml   # Senior-friendly theme
│
├── Services/                          # Business logic and data access
│   ├── IBudgetService.cs              # Budget service interface
│   ├── BudgetService.cs               # Budget service implementation
│   ├── IDialogService.cs              # Dialog service interface (in IBudgetService.cs)
│   ├── DialogService.cs               # Dialog service implementation
│   ├── ISettingsService.cs            # Settings service interface (in IBudgetService.cs)
│   ├── SettingsService.cs             # Settings service implementation
│   ├── IExportService.cs              # Export service interface
│   ├── ExportService.cs               # Export service implementation
│   └── Database/
│       ├── IDatabaseContext.cs        # Database context interface
│       ├── SqliteDatabaseContext.cs   # SQLite database context
│       ├── DatabaseMigrations.cs      # Database migration logic
│       └── Repositories/
│           ├── IRepository.cs         # Generic repository interface
│           ├── IncomeRepository.cs    # Income data repository
│           ├── SpendingRepository.cs  # Spending data repository
│           └── CategoryRepository.cs  # Category data repository
│
├── Converters/                        # WPF Value Converters
│   ├── CurrencyConverter.cs           # Currency formatting converter
│   ├── DateConverter.cs               # Date formatting converter
│   ├── BooleanToVisibilityConverter.cs
│   ├── ColorToBrushConverter.cs
│   └── NullToVisibilityConverter.cs
│
├── Behaviors/                         # WPF Behaviors
│   ├── SeniorFriendlyBehaviors.cs     # Accessibility behaviors
│   ├── DataGridBehaviors.cs           # Data grid enhancements
│   └── ValidationBehaviors.cs         # Input validation behaviors
│
├── Helpers/                           # Utility classes
│   ├── FileHelper.cs                  # File operations
│   ├── ValidationHelper.cs            # Input validation
│   ├── AccessibilityHelper.cs         # Senior-friendly features
│   ├── ExceptionHelper.cs             # Exception handling
│   └── ColorHelper.cs                 # Color/theme utilities
│
├── Resources/                         # Application resources
│   ├── Images/                        # Application icons and images
│   │   ├── app-icon.ico
│   │   ├── add-icon.png
│   │   ├── edit-icon.png
│   │   ├── delete-icon.png
│   │   └── export-icon.png
│   ├── Fonts/                         # Custom fonts for accessibility
│   └── Templates/                     # Export templates
│       ├── budget-report.html
│       └── monthly-summary.html
│
├── Migrations/                        # Database migrations
│   ├── Migration_001_Initial.sql
│   ├── Migration_002_AddIndexes.sql
│   └── Migration_003_AddViews.sql
│
├── Configuration/                     # Configuration files
│   ├── appsettings.json               # Application settings
│   ├── database.config                # Database configuration
│   └── accessibility.config           # Accessibility settings
│
└── Documentation/                     # Project documentation
    ├── Architecture.md                # Architecture overview
    ├── DatabaseDesign.md              # Database design documentation
    ├── UserGuide.md                   # User manual
    ├── DeveloperGuide.md              # Developer documentation
    └── AccessibilityFeatures.md       # Senior-friendly features guide
```

## Key Architectural Decisions

### 1. MVVM Pattern Implementation
- **ViewModels**: Handle all business logic and data binding
- **Views**: Pure UI with minimal code-behind
- **Models**: Data structures and validation
- **Services**: Business logic and data access

### 2. Dependency Injection
- Microsoft.Extensions.DependencyInjection for IoC container
- Service registration in App.xaml.cs
- Constructor injection throughout the application

### 3. Database Design
- SQLite for local storage (no server required)
- Entity Framework Core or raw SQL depending on complexity
- Repository pattern for data access
- Database migrations for version control

### 4. Senior-Friendly Features
- Large fonts and controls by default
- High contrast themes
- Simple navigation patterns
- Clear visual hierarchy
- Minimal cognitive load

### 5. Data Binding Strategy
- ObservableCollection for lists
- INotifyPropertyChanged for property updates
- ICommand for user actions
- Validation attributes for data validation

## Required NuGet Packages

```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="7.0.1" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="7.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="7.0.0" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="7.0.13" />
<PackageReference Include="System.Text.Json" Version="7.0.3" />
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.39" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
```

## Development Guidelines

### 1. Code Organization
- One class per file
- Consistent naming conventions
- Comprehensive XML documentation
- Separation of concerns

### 2. Error Handling
- Try-catch blocks in service methods
- User-friendly error messages
- Logging for debugging
- Graceful degradation

### 3. Performance Considerations
- Async/await for database operations
- Virtualization for large data sets
- Efficient data binding
- Memory management

### 4. Accessibility
- Keyboard navigation support
- Screen reader compatibility
- High contrast mode support
- Customizable font sizes

### 5. Testing Strategy
- Unit tests for ViewModels and Services
- Integration tests for database operations
- UI automation tests for critical paths
- Manual accessibility testing

## Senior-Friendly Design Principles

### 1. Visual Design
- **Font Size**: Minimum 14pt, default 16pt, scalable to 24pt
- **Contrast**: High contrast ratios (4.5:1 minimum)
- **Colors**: Limited color palette, avoid red/green combinations
- **Spacing**: Generous padding and margins

### 2. Interaction Design
- **Click Targets**: Minimum 44x44 pixels
- **Navigation**: Simple, linear flow
- **Feedback**: Clear visual and auditory feedback
- **Error Messages**: Plain language, helpful suggestions

### 3. Cognitive Load
- **Simplicity**: One primary action per screen
- **Consistency**: Predictable interface patterns
- **Memory**: Minimal requirement to remember information
- **Help**: Context-sensitive assistance

This architecture provides a solid foundation for a maintainable, scalable, and accessible budget management application specifically designed for senior users.