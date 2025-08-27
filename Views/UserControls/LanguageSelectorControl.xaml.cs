// Language Selector Control - Runtime Language Switching
// File: Views/UserControls/LanguageSelectorControl.xaml.cs

using System;
using System.Windows;
using System.Windows.Controls;
using BudgetManagement.Shared.Infrastructure;

namespace BudgetManagement.Views.UserControls
{
    /// <summary>
    /// User control for selecting application language with real-time switching
    /// </summary>
    public partial class LanguageSelectorControl : UserControl
    {
        private ILanguageManager? _languageManager;
        private bool _isUpdatingSelection = false;

        public LanguageSelectorControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Try to get language manager from the application's service provider
            if (Application.Current is App app && app.Host != null)
            {
                _languageManager = app.Host.Services.GetService(typeof(ILanguageManager)) as ILanguageManager;
                InitializeLanguageSelector();
            }
        }

        private void InitializeLanguageSelector()
        {
            if (_languageManager == null) return;

            try
            {
                // Populate available languages
                LanguageComboBox.ItemsSource = _languageManager.AvailableLanguages;
                
                // Set current selection
                LanguageComboBox.SelectedItem = _languageManager.CurrentLanguage;
                
                // Subscribe to language changes
                _languageManager.LanguageChanged += OnLanguageChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing language selector: {ex.Message}");
            }
        }

        private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _isUpdatingSelection = true;
                try
                {
                    LanguageComboBox.SelectedItem = e.NewLanguage;
                }
                finally
                {
                    _isUpdatingSelection = false;
                }
            }));
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection || _languageManager == null) return;

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Language selectedLanguage)
            {
                try
                {
                    _languageManager.ChangeLanguage(selectedLanguage);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error changing language: {ex.Message}");
                    
                    // Revert selection on error
                    _isUpdatingSelection = true;
                    try
                    {
                        LanguageComboBox.SelectedItem = _languageManager.CurrentLanguage;
                    }
                    finally
                    {
                        _isUpdatingSelection = false;
                    }
                }
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from events to prevent memory leaks
            if (_languageManager != null)
            {
                _languageManager.LanguageChanged -= OnLanguageChanged;
            }
        }
    }
}