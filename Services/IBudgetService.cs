// Service Interface for Budget Management Operations
// File: Services/IBudgetService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BudgetManagement.Models;

namespace BudgetManagement.Services
{
    /// <summary>
    /// Interface for budget management business logic and data operations
    /// </summary>
    public interface IBudgetService
    {
        // Income operations
        Task<IEnumerable<Income>> GetIncomeAsync(DateTime startDate, DateTime endDate);
        Task<Income> GetIncomeByIdAsync(int id);
        Task<Income> AddIncomeAsync(Income income);
        Task<Income> UpdateIncomeAsync(Income income);
        Task DeleteIncomeAsync(int id);

        // Spending operations
        Task<IEnumerable<Spending>> GetSpendingAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<SpendingWithCategory>> GetSpendingWithCategoryAsync(DateTime startDate, DateTime endDate);
        Task<Spending> GetSpendingByIdAsync(int id);
        Task<Spending> AddSpendingAsync(Spending spending);
        Task<Spending> UpdateSpendingAsync(Spending spending);
        Task DeleteSpendingAsync(int id);

        // Category operations
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<IEnumerable<Category>> GetAllCategoriesAsync(); // Gets all categories including inactive ones
        Task<Category> GetCategoryByIdAsync(int id);
        Task<Category> AddCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int id);

        // Summary and calculation operations
        Task<BudgetSummary> GetBudgetSummaryAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<MonthlySummary>> GetMonthlySummaryAsync(int year);
        Task<decimal> GetCategoryTotalAsync(int categoryId, DateTime startDate, DateTime endDate);

        // Data export operations
        Task ExportDataAsync(DateTime startDate, DateTime endDate, string? filePath = null);
        Task<string> ExportToCsvAsync(DateTime startDate, DateTime endDate);

        // Application settings
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task SetSettingAsync(string key, string value);

        // Database maintenance
        Task<bool> TestConnectionAsync();
        Task InitializeDatabaseAsync();
        Task BackupDatabaseAsync(string backupPath);
        Task RestoreDatabaseAsync(string backupPath);
    }

    /// <summary>
    /// Interface for dialog services to show UI dialogs
    /// </summary>
    public interface IDialogService
    {
        Task<Income?> ShowIncomeDialogAsync(Income income);
        Task<Spending?> ShowSpendingDialogAsync(Spending spending, List<Category> categories);
        Task<bool> ShowConfirmationAsync(string title, string message);
        Task ShowInformationAsync(string title, string message);
        Task ShowErrorAsync(string title, string message);
        Task ShowWarningAsync(string title, string message);
        Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "");
    }

    /// <summary>
    /// Interface for application settings service
    /// </summary>
    public interface ISettingsService
    {
        // UI Settings
        int FontSize { get; set; }
        string Theme { get; set; }
        string Language { get; set; }
        string CurrencySymbol { get; set; }
        string DateFormat { get; set; }

        // Application Settings
        string DatabasePath { get; set; }
        bool AutoBackup { get; set; }
        int BackupRetentionDays { get; set; }

        // Window Settings
        double WindowWidth { get; set; }
        double WindowHeight { get; set; }
        bool WindowMaximized { get; set; }

        // Methods
        Task LoadSettingsAsync();
        Task SaveSettingsAsync();
        void ResetToDefaults();
        event EventHandler<SettingChangedEventArgs> SettingChanged;
    }

    /// <summary>
    /// Event arguments for setting changed events
    /// </summary>
    public class SettingChangedEventArgs : EventArgs
    {
        public string SettingName { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }

        public SettingChangedEventArgs(string settingName, object? oldValue, object? newValue)
        {
            SettingName = settingName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}