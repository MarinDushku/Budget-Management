// Localization Service - Enterprise Infrastructure Component
// File: Shared/Infrastructure/LocalizationService.cs

using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// Service for handling localization and resource string retrieval in enterprise components
    /// </summary>
    public interface IEnterpriseLocalizationService
    {
        /// <summary>
        /// Gets a localized string by resource key
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Localized string</returns>
        string GetString(string key, string? defaultValue = null);

        /// <summary>
        /// Gets a formatted localized string with parameters
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <param name="args">Format arguments</param>
        /// <returns>Formatted localized string</returns>
        string GetFormattedString(string key, params object[] args);

        /// <summary>
        /// Gets the current culture info
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        /// Gets the current language code
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// Sets the application language
        /// </summary>
        /// <param name="languageCode">Language code (e.g., "en", "sq")</param>
        void SetLanguage(string languageCode);
    }

    /// <summary>
    /// Implementation of localization service using WPF resource dictionaries
    /// </summary>
    public class EnterpriseLocalizationService : IEnterpriseLocalizationService
    {
        private const string DefaultLanguage = "en";
        private string _currentLanguage = DefaultLanguage;

        public CultureInfo CurrentCulture => CultureInfo.GetCultureInfo(_currentLanguage);

        public string CurrentLanguage => _currentLanguage;

        public string GetString(string key, string? defaultValue = null)
        {
            try
            {
                if (Application.Current?.Resources[key] is string resourceString)
                {
                    return resourceString;
                }

                // Fallback: try to find in merged dictionaries
                var mergedDictionaries = Application.Current?.Resources.MergedDictionaries;
                if (mergedDictionaries != null)
                {
                    foreach (ResourceDictionary mergedDict in mergedDictionaries)
                    {
                        if (mergedDict[key] is string mergedResourceString)
                        {
                            return mergedResourceString;
                        }
                    }
                }

                return defaultValue ?? key;
            }
            catch
            {
                return defaultValue ?? key;
            }
        }

        public string GetFormattedString(string key, params object[] args)
        {
            var template = GetString(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        public void SetLanguage(string languageCode)
        {
            try
            {
                _currentLanguage = languageCode.ToLowerInvariant();
                var culture = CultureInfo.GetCultureInfo(_currentLanguage);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                // Load appropriate resource dictionary
                LoadLanguageResources(languageCode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set language {languageCode}: {ex.Message}");
                // Fallback to default language
                if (_currentLanguage != DefaultLanguage)
                {
                    SetLanguage(DefaultLanguage);
                }
            }
        }

        private void LoadLanguageResources(string languageCode)
        {
            if (Application.Current == null) return;

            var resourceUri = languageCode.ToLowerInvariant() switch
            {
                "sq" => new Uri("/Resources/Strings.sq.xaml", UriKind.Relative),
                "en" => new Uri("/Resources/Strings.en.xaml", UriKind.Relative),
                _ => new Uri("/Resources/Strings.en.xaml", UriKind.Relative)
            };

            try
            {
                var resourceDict = new ResourceDictionary { Source = resourceUri };
                
                // Clear existing language resources and add new ones
                var existingLangDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.ToString().Contains("Strings.") == true);
                
                if (existingLangDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(existingLangDict);
                }
                
                Application.Current.Resources.MergedDictionaries.Add(resourceDict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load language resources for {languageCode}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Static helper for easy access to localization in enterprise components
    /// </summary>
    public static class LocalizationHelper
    {
        private static IEnterpriseLocalizationService? _service;

        /// <summary>
        /// Initializes the localization helper with a service instance
        /// </summary>
        /// <param name="service">Localization service instance</param>
        public static void Initialize(IEnterpriseLocalizationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets a localized string by key
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Localized string</returns>
        public static string GetString(string key, string? defaultValue = null)
        {
            return _service?.GetString(key, defaultValue) ?? defaultValue ?? key;
        }

        /// <summary>
        /// Gets a formatted localized string
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <param name="args">Format arguments</param>
        /// <returns>Formatted localized string</returns>
        public static string GetFormattedString(string key, params object[] args)
        {
            return _service?.GetFormattedString(key, args) ?? key;
        }

        /// <summary>
        /// Common validation messages in a structured way
        /// </summary>
        public static class ValidationMessages
        {
            public static string DateRequired => GetString("DateRequired", "Date is required");
            public static string DateCannotBeFuture => GetString("DateCannotBeFuture", "Date cannot be in the future");
            public static string DescriptionRequired => GetString("DescriptionRequired", "Description is required");
            public static string DescriptionInvalidCharacters => GetString("DescriptionInvalidCharacters", "Description contains invalid characters");
            public static string AmountMustBePositive => GetString("AmountMustBePositive", "Amount must be greater than zero");
            public static string AmountRequired => GetString("AmountRequired", "Amount is required");
            public static string CategoryIdRequired => GetString("CategoryIdRequired", "Category ID is required");
            public static string StartDateRequired => GetString("StartDateRequired", "Start date is required");
            public static string EndDateRequired => GetString("EndDateRequired", "End date is required");
            public static string StartDateMustBeBeforeEndDate => GetString("StartDateMustBeBeforeEndDate", "Start date must be before or equal to end date");
            public static string EndDateCannotBeFuture => GetString("EndDateCannotBeFuture", "End date cannot be in the future");
            public static string SearchPatternRequired => GetString("SearchPatternRequired", "Search pattern is required");
            public static string SearchPatternTooShort => GetString("SearchPatternTooShort", "Search pattern must be at least 2 characters");
            public static string DescriptionCannotBeEmpty => GetString("DescriptionCannotBeEmpty", "Description cannot be empty or contain only whitespace");
            public static string CategoryNameRequired => GetString("CategoryNameRequired", "Category name is required");
            public static string CategoryNameInvalidCharacters => GetString("CategoryNameInvalidCharacters", "Category name contains invalid characters");
            public static string IdCannotBeEmpty => GetString("IdCannotBeEmpty", "ID cannot be empty");
            public static string CommandCannotBeNull => GetString("CommandCannotBeNull", "Command cannot be null");
            public static string QueryCannotBeNull => GetString("QueryCannotBeNull", "Query cannot be null");
        }

        /// <summary>
        /// Common error messages in a structured way
        /// </summary>
        public static class ErrorMessages
        {
            public static string ErrorAddingIncome => GetString("ErrorAddingIncome", "Error adding income");
            public static string ErrorLoadingIncomes => GetString("ErrorLoadingIncomes", "Error loading incomes");
            public static string ErrorSearchingIncomes => GetString("ErrorSearchingIncomes", "Error searching incomes");
            public static string ErrorLoadingInitialData => GetString("ErrorLoadingInitialData", "Error loading initial data");
            public static string ErrorAddingSpending => GetString("ErrorAddingSpending", "Error adding spending");
            public static string ErrorLoadingSpendings => GetString("ErrorLoadingSpendings", "Error loading spendings");
            public static string ErrorSearchingSpendings => GetString("ErrorSearchingSpendings", "Error searching spendings");
            public static string ErrorLoadingCategories => GetString("ErrorLoadingCategories", "Error loading categories");
            public static string ErrorLoadingRecentSpendings => GetString("ErrorLoadingRecentSpendings", "Error loading recent spendings");
            public static string ErrorLoadingRecentIncomes => GetString("ErrorLoadingRecentIncomes", "Error loading recent incomes");
        }

        /// <summary>
        /// Common status messages in a structured way
        /// </summary>
        public static class StatusMessages
        {
            public static string LoadingIncomes => GetString("LoadingIncomes", "Loading incomes...");
            public static string LoadingSpendings => GetString("LoadingSpendings", "Loading spendings...");
            public static string LoadingCategories => GetString("LoadingCategories", "Loading categories...");
            public static string LoadingDashboard => GetString("LoadingDashboard", "Loading dashboard...");
            public static string LoadingAnalytics => GetString("LoadingAnalytics", "Loading analytics...");
            public static string ProcessingRequest => GetString("ProcessingRequest", "Processing request...");
            public static string SavingChanges => GetString("SavingChanges", "Saving changes...");
            public static string OperationCompleted => GetString("OperationCompleted", "Operation completed");
            public static string OperationFailed => GetString("OperationFailed", "Operation failed");
            public static string NoDataAvailable => GetString("NoDataAvailable", "No data available");
            public static string DataLoadedSuccessfully => GetString("DataLoadedSuccessfully", "Data loaded successfully");
        }
    }
}