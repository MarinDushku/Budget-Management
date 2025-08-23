using System;
using System.ComponentModel;

namespace BudgetManagement.Services
{
    /// <summary>
    /// Interface for localization service supporting runtime language switching
    /// </summary>
    public interface ILocalizationService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the current language code (e.g., "en", "sq")
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// Changes the application language at runtime
        /// </summary>
        /// <param name="languageCode">Language code (e.g., "en" for English, "sq" for Albanian)</param>
        void SetLanguage(string languageCode);

        /// <summary>
        /// Gets a localized string by key
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <returns>Localized string or key if not found</returns>
        string GetString(string key);

        /// <summary>
        /// Event fired when language changes
        /// </summary>
        event EventHandler<string> LanguageChanged;
    }
}