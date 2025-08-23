using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace BudgetManagement.Services
{
    /// <summary>
    /// Implementation of localization service with runtime language switching
    /// </summary>
    public class LocalizationService : ILocalizationService, INotifyPropertyChanged
    {
        private string _currentLanguage = "en"; // Default to English
        private ResourceDictionary? _currentResourceDictionary;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<string>? LanguageChanged;

        public string CurrentLanguage
        {
            get => _currentLanguage;
            private set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged();
                }
            }
        }

        public LocalizationService()
        {
            LoadLanguage("en"); // Load English by default
        }

        public void SetLanguage(string languageCode)
        {
            if (CurrentLanguage == languageCode)
                return;

            LoadLanguage(languageCode);
            CurrentLanguage = languageCode;
            LanguageChanged?.Invoke(this, languageCode);

            // Notify all bound properties to refresh
            OnPropertyChanged(string.Empty);
        }

        public string GetString(string key)
        {
            try
            {
                if (_currentResourceDictionary?.Contains(key) == true)
                {
                    return _currentResourceDictionary[key]?.ToString() ?? key;
                }
                return key; // Return key if not found
            }
            catch
            {
                return key; // Return key if error occurs
            }
        }

        private void LoadLanguage(string languageCode)
        {
            try
            {
                var resourceUri = new Uri($"/Resources/Strings.{languageCode}.xaml", UriKind.Relative);
                var resourceDictionary = new ResourceDictionary { Source = resourceUri };

                // Remove old language resources
                if (_currentResourceDictionary != null && Application.Current.Resources.MergedDictionaries.Contains(_currentResourceDictionary))
                {
                    Application.Current.Resources.MergedDictionaries.Remove(_currentResourceDictionary);
                }

                // Add new language resources
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
                _currentResourceDictionary = resourceDictionary;
            }
            catch (Exception)
            {
                // If loading fails, fall back to English
                if (languageCode != "en")
                {
                    LoadLanguage("en");
                    CurrentLanguage = "en";
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}