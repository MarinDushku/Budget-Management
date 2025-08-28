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
        private const string DefaultLanguage = "sq";
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
                System.Diagnostics.Debug.WriteLine($"SetLanguage: Called with languageCode = '{languageCode}'");
                System.Diagnostics.Debug.WriteLine($"SetLanguage: Current language before change = '{_currentLanguage}'");
                
                _currentLanguage = languageCode.ToLowerInvariant();
                var culture = CultureInfo.GetCultureInfo(_currentLanguage);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                System.Diagnostics.Debug.WriteLine($"SetLanguage: Set culture to {culture.Name}");

                // Load appropriate resource dictionary
                LoadLanguageResources(languageCode);
                
                System.Diagnostics.Debug.WriteLine($"SetLanguage: Completed successfully for '{languageCode}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetLanguage: Failed to set language {languageCode}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SetLanguage: Stack trace: {ex.StackTrace}");
                // Fallback to default language
                if (_currentLanguage != DefaultLanguage)
                {
                    SetLanguage(DefaultLanguage);
                }
            }
        }

        private void LoadLanguageResources(string languageCode)
        {
            if (Application.Current == null) 
            {
                System.Diagnostics.Debug.WriteLine("LoadLanguageResources: Application.Current is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Loading resources for language '{languageCode}'");

            // Use same URI format as App.xaml (no leading slash) for consistency
            var resourceUri = languageCode.ToLowerInvariant() switch
            {
                "sq" => new Uri("Resources/Strings.sq.xaml", UriKind.Relative),
                "en" => new Uri("Resources/Strings.en.xaml", UriKind.Relative),
                _ => new Uri("Resources/Strings.en.xaml", UriKind.Relative)
            };

            System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Resource URI = {resourceUri}");

            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: About to create ResourceDictionary with URI: {resourceUri}");
                
                var resourceDict = new ResourceDictionary();
                System.Diagnostics.Debug.WriteLine("LoadLanguageResources: Created empty ResourceDictionary");
                
                System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Setting Source to: {resourceUri}");
                resourceDict.Source = resourceUri;
                System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Successfully set Source. ResourceDict Count: {resourceDict.Count}");
                
                // Test accessing a few specific keys immediately after loading
                System.Diagnostics.Debug.WriteLine("LoadLanguageResources: Testing resource access...");
                if (resourceDict.Contains("AppTitle"))
                {
                    var appTitle = resourceDict["AppTitle"]?.ToString();
                    System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Direct access - AppTitle = '{appTitle}'");
                }
                if (resourceDict.Contains("Albanian"))
                {
                    var albanian = resourceDict["Albanian"]?.ToString();
                    System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Direct access - Albanian = '{albanian}'");
                }
                
                // Clear existing language resources and add new ones
                // Match App.xaml format exactly: "Resources/Strings.*.xaml"
                var existingLangDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && 
                        (d.Source.ToString().Equals("Resources/Strings.en.xaml") || 
                         d.Source.ToString().Equals("Resources/Strings.sq.xaml") ||
                         d.Source.ToString().Contains("Resources/Strings.") ||
                         d.Source.ToString().Contains("Strings.en.xaml") ||
                         d.Source.ToString().Contains("Strings.sq.xaml")));
                
                if (existingLangDict != null)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Removing existing resource dictionary: {existingLangDict.Source}");
                    Application.Current.Resources.MergedDictionaries.Remove(existingLangDict);
                    System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Successfully removed. Remaining dictionaries: {Application.Current.Resources.MergedDictionaries.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LoadLanguageResources: No existing language dictionary found");
                    System.Diagnostics.Debug.WriteLine("LoadLanguageResources: Current merged dictionaries:");
                    for (int i = 0; i < Application.Current.Resources.MergedDictionaries.Count; i++)
                    {
                        var dict = Application.Current.Resources.MergedDictionaries[i];
                        System.Diagnostics.Debug.WriteLine($"  Dictionary {i}: Source = '{dict.Source}', Count = {dict.Count}");
                    }
                }
                
                Application.Current.Resources.MergedDictionaries.Add(resourceDict);
                System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Added new resource dictionary. Total merged dictionaries: {Application.Current.Resources.MergedDictionaries.Count}");
                
                // CRITICAL: Force complete WPF resource system refresh
                // This ensures DynamicResource bindings pick up the new values
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("LoadLanguageResources: Starting aggressive UI refresh");
                        
                        // Method 1: Force all windows to invalidate and update
                        foreach (Window window in Application.Current.Windows)
                        {
                            ForceRefreshAllDynamicResources(window);
                        }
                        
                        // Method 2: Force a second pass after a delay to catch any lazy-loaded elements
                        Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                window.InvalidateVisual();
                                window.UpdateLayout();
                            }
                        }));
                        
                        System.Diagnostics.Debug.WriteLine("LoadLanguageResources: Aggressive UI refresh completed");
                    }
                    catch (Exception refreshEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Error during resource refresh: {refreshEx.Message}");
                    }
                }));
                
                // COMPREHENSIVE RESOURCE VERIFICATION
                System.Diagnostics.Debug.WriteLine("LoadLanguageResources: === RESOURCE VERIFICATION START ===");
                
                // Test critical UI keys that should change language
                var testKeys = new[] { 
                    ("AppTitle", languageCode == "sq" ? "Menaxhuesi i Buxhetit" : "Budget Manager"),
                    ("Dashboard", languageCode == "sq" ? "Paneli Kryesor" : "Dashboard"),
                    ("English", languageCode == "sq" ? "Anglisht" : "English"),
                    ("Albanian", languageCode == "sq" ? "Shqip" : "Albanian"),
                    ("AddIncomeButton", languageCode == "sq" ? "Shto të Ardhura" : "Add Income"),
                    ("Language", languageCode == "sq" ? "Gjuha" : "Language")
                };
                
                bool allKeysCorrect = true;
                foreach (var (key, expectedValue) in testKeys)
                {
                    if (Application.Current.Resources.Contains(key))
                    {
                        var actualValue = Application.Current.Resources[key]?.ToString();
                        bool isCorrect = actualValue == expectedValue;
                        System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: {key} = '{actualValue}' (Expected: '{expectedValue}') ✓{(isCorrect ? "CORRECT" : "WRONG")}");
                        if (!isCorrect) allKeysCorrect = false;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: {key} = KEY NOT FOUND ✗");
                        allKeysCorrect = false;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Overall verification: {(allKeysCorrect ? "✓ ALL CORRECT" : "✗ SOME WRONG")}");
                System.Diagnostics.Debug.WriteLine("LoadLanguageResources: === RESOURCE VERIFICATION END ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Failed to load language resources for {languageCode}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LoadLanguageResources: Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Forces refresh of all DynamicResource bindings in a visual tree
        /// </summary>
        private static void ForceRefreshAllDynamicResources(DependencyObject parent)
        {
            try
            {
                if (parent == null) return;

                // Force refresh of this element
                if (parent is FrameworkElement element)
                {
                    // Multiple approaches to force DynamicResource refresh
                    element.InvalidateVisual();
                    element.UpdateLayout();
                    
                    // Force refresh of specific properties that commonly use DynamicResource
                    if (element is System.Windows.Controls.TextBlock textBlock)
                    {
                        var binding = textBlock.GetBindingExpression(System.Windows.Controls.TextBlock.TextProperty);
                        binding?.UpdateTarget();
                    }
                    else if (element is System.Windows.Controls.ContentControl contentControl)
                    {
                        var binding = contentControl.GetBindingExpression(System.Windows.Controls.ContentControl.ContentProperty);
                        binding?.UpdateTarget();
                    }
                    else if (element is System.Windows.Controls.Button button)
                    {
                        var binding = button.GetBindingExpression(System.Windows.Controls.ContentControl.ContentProperty);
                        binding?.UpdateTarget();
                    }
                }
                
                // Recursively refresh all children
                var childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                    ForceRefreshAllDynamicResources(child);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ForceRefreshAllDynamicResources: Error refreshing element: {ex.Message}");
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