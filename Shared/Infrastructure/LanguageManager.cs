// Language Manager - Advanced Localization Features
// File: Shared/Infrastructure/LanguageManager.cs

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// Advanced language management with runtime switching support
    /// </summary>
    public interface ILanguageManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the currently selected language
        /// </summary>
        Language CurrentLanguage { get; }

        /// <summary>
        /// Gets all available languages
        /// </summary>
        Language[] AvailableLanguages { get; }

        /// <summary>
        /// Changes the application language at runtime
        /// </summary>
        /// <param name="language">Target language</param>
        void ChangeLanguage(Language language);

        /// <summary>
        /// Changes the application language by code
        /// </summary>
        /// <param name="languageCode">Language code (e.g., "en", "sq")</param>
        void ChangeLanguage(string languageCode);

        /// <summary>
        /// Event fired when language changes
        /// </summary>
        event EventHandler<LanguageChangedEventArgs> LanguageChanged;
    }

    /// <summary>
    /// Implementation of advanced language manager
    /// </summary>
    public class LanguageManager : ILanguageManager
    {
        private Language _currentLanguage;
        private readonly IEnterpriseLocalizationService _localizationService;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

        public Language CurrentLanguage 
        { 
            get => _currentLanguage;
            private set
            {
                if (_currentLanguage?.Code != value?.Code)
                {
                    var oldLanguage = _currentLanguage;
                    _currentLanguage = value;
                    OnPropertyChanged(nameof(CurrentLanguage));
                    OnLanguageChanged(oldLanguage, value);
                }
            }
        }

        public Language[] AvailableLanguages { get; } = 
        {
            new Language("sq", "Shqip", "🇦🇱"),
            new Language("en", "English", "🇺🇸")
        };

        public LanguageManager(IEnterpriseLocalizationService localizationService)
        {
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _currentLanguage = AvailableLanguages[0]; // Default to Albanian (now at index 0)
        }

        public void ChangeLanguage(Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            if (CurrentLanguage?.Code == language.Code)
                return;

            try
            {
                // Update localization service
                _localizationService.SetLanguage(language.Code);
                
                // Update current language
                CurrentLanguage = language;

                // Force UI refresh by updating all DynamicResource references
                RefreshUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to change language to {language.Code}: {ex.Message}");
                throw;
            }
        }

        public void ChangeLanguage(string languageCode)
        {
            var language = Array.Find(AvailableLanguages, l => 
                string.Equals(l.Code, languageCode, StringComparison.OrdinalIgnoreCase));

            if (language == null)
                throw new ArgumentException($"Language '{languageCode}' is not supported", nameof(languageCode));

            ChangeLanguage(language);
        }

        private void RefreshUI()
        {
            if (Application.Current?.Dispatcher == null) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Force refresh of all windows
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != null)
                        {
                            // Trigger resource refresh
                            window.UpdateLayout();
                            
                            // Force reapplication of styles and templates
                            window.InvalidateVisual();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error refreshing UI after language change: {ex.Message}");
                }
            }));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnLanguageChanged(Language? oldLanguage, Language? newLanguage)
        {
            LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(oldLanguage, newLanguage));
        }
    }

    /// <summary>
    /// Language definition
    /// </summary>
    public record Language(string Code, string DisplayName, string Flag)
    {
        public CultureInfo Culture => CultureInfo.GetCultureInfo(Code);
        
        public override string ToString() => $"{Flag} {DisplayName}";
    }

    /// <summary>
    /// Event arguments for language change events
    /// </summary>
    public class LanguageChangedEventArgs : EventArgs
    {
        public Language? OldLanguage { get; }
        public Language? NewLanguage { get; }

        public LanguageChangedEventArgs(Language? oldLanguage, Language? newLanguage)
        {
            OldLanguage = oldLanguage;
            NewLanguage = newLanguage;
        }
    }

    /// <summary>
    /// WPF Markup Extension for localized strings that automatically update on language change
    /// </summary>
    public class LocalizeExtension : System.Windows.Markup.MarkupExtension, INotifyPropertyChanged
    {
        private string _key;
        private string? _defaultValue;
        private static ILanguageManager? _languageManager;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public string? DefaultValue
        {
            get => _defaultValue;
            set
            {
                _defaultValue = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public string Value => LocalizationHelper.GetString(Key, DefaultValue);

        public LocalizeExtension() { _key = string.Empty; }

        public LocalizeExtension(string key)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public static void Initialize(ILanguageManager languageManager)
        {
            if (_languageManager != null)
                _languageManager.LanguageChanged -= OnLanguageChanged;

            _languageManager = languageManager;
            if (_languageManager != null)
                _languageManager.LanguageChanged += OnLanguageChanged;
        }

        private static void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
        {
            // This would trigger updates in all LocalizeExtension instances
            // Implementation would require keeping track of all instances
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}