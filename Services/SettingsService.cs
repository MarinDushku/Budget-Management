// Settings Service Implementation
// File: Services/SettingsService.cs

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace BudgetManagement.Services
{
    /// <summary>
    /// Implementation of settings service for application configuration
    /// </summary>
    public class SettingsService : ISettingsService, INotifyPropertyChanged
    {
        private readonly string _settingsFilePath;
        private AppSettings _settings = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<SettingChangedEventArgs>? SettingChanged;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "BudgetManagement");
            Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "settings.json");
        }

        #region UI Settings

        public int FontSize
        {
            get => _settings.FontSize;
            set
            {
                if (_settings.FontSize != value)
                {
                    var oldValue = _settings.FontSize;
                    _settings.FontSize = value;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(FontSize), oldValue, value);
                }
            }
        }

        public string Theme
        {
            get => _settings.Theme;
            set
            {
                var newValue = value ?? "Light";
                if (_settings.Theme != newValue)
                {
                    var oldValue = _settings.Theme;
                    _settings.Theme = newValue;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(Theme), oldValue, newValue);
                }
            }
        }

        public string Language
        {
            get => _settings.Language;
            set
            {
                var newValue = value ?? "en";
                if (_settings.Language != newValue)
                {
                    var oldValue = _settings.Language;
                    _settings.Language = newValue;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(Language), oldValue, newValue);
                }
            }
        }

        public string CurrencySymbol
        {
            get => _settings.CurrencySymbol;
            set
            {
                var newValue = value ?? "$";
                if (_settings.CurrencySymbol != newValue)
                {
                    var oldValue = _settings.CurrencySymbol;
                    _settings.CurrencySymbol = newValue;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(CurrencySymbol), oldValue, newValue);
                }
            }
        }

        public string DateFormat
        {
            get => _settings.DateFormat;
            set
            {
                var newValue = value ?? "MM/dd/yyyy";
                if (_settings.DateFormat != newValue)
                {
                    var oldValue = _settings.DateFormat;
                    _settings.DateFormat = newValue;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(DateFormat), oldValue, newValue);
                }
            }
        }

        #endregion

        #region Application Settings

        public string DatabasePath
        {
            get => _settings.DatabasePath;
            set
            {
                var newValue = value ?? GetDefaultDatabasePath();
                if (_settings.DatabasePath != newValue)
                {
                    var oldValue = _settings.DatabasePath;
                    _settings.DatabasePath = newValue;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(DatabasePath), oldValue, newValue);
                }
            }
        }

        public bool AutoBackup
        {
            get => _settings.AutoBackup;
            set
            {
                if (_settings.AutoBackup != value)
                {
                    var oldValue = _settings.AutoBackup;
                    _settings.AutoBackup = value;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(AutoBackup), oldValue, value);
                }
            }
        }

        public int BackupRetentionDays
        {
            get => _settings.BackupRetentionDays;
            set
            {
                var newValue = Math.Max(1, value);
                if (_settings.BackupRetentionDays != newValue)
                {
                    var oldValue = _settings.BackupRetentionDays;
                    _settings.BackupRetentionDays = newValue;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(BackupRetentionDays), oldValue, newValue);
                }
            }
        }

        #endregion

        #region Window Settings

        public double WindowWidth
        {
            get => _settings.WindowWidth;
            set
            {
                var newValue = Math.Max(800, value);
                if (_settings.WindowWidth != newValue)
                {
                    var oldValue = _settings.WindowWidth;
                    _settings.WindowWidth = newValue;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(WindowWidth), oldValue, newValue);
                }
            }
        }

        public double WindowHeight
        {
            get => _settings.WindowHeight;
            set
            {
                var newValue = Math.Max(600, value);
                if (_settings.WindowHeight != newValue)
                {
                    var oldValue = _settings.WindowHeight;
                    _settings.WindowHeight = newValue;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(WindowHeight), oldValue, newValue);
                }
            }
        }

        public bool WindowMaximized
        {
            get => _settings.WindowMaximized;
            set
            {
                if (_settings.WindowMaximized != value)
                {
                    var oldValue = _settings.WindowMaximized;
                    _settings.WindowMaximized = value;
                    OnPropertyChanged();
                    NotifySettingChanged(nameof(WindowMaximized), oldValue, value);
                }
            }
        }

        #endregion

        #region Methods

        public async Task LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loadedSettings != null)
                    {
                        _settings = loadedSettings;
                        OnAllPropertiesChanged();
                    }
                }
                else
                {
                    // First run - initialize with defaults
                    ResetToDefaults();
                    await SaveSettingsAsync();
                }
            }
            catch (Exception)
            {
                // If loading fails, use defaults
                ResetToDefaults();
            }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(_settings, options);
                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch (Exception)
            {
                // Ignore save errors - settings will revert to previous state
            }
        }

        public void ResetToDefaults()
        {
            var oldSettings = _settings;
            _settings = new AppSettings();
            
            // Notify of all changes
            OnAllPropertiesChanged();
            
            // Fire individual setting changed events
            NotifySettingChanged(nameof(FontSize), oldSettings.FontSize, _settings.FontSize);
            NotifySettingChanged(nameof(Theme), oldSettings.Theme, _settings.Theme);
            NotifySettingChanged(nameof(CurrencySymbol), oldSettings.CurrencySymbol, _settings.CurrencySymbol);
            NotifySettingChanged(nameof(DateFormat), oldSettings.DateFormat, _settings.DateFormat);
            NotifySettingChanged(nameof(DatabasePath), oldSettings.DatabasePath, _settings.DatabasePath);
            NotifySettingChanged(nameof(AutoBackup), oldSettings.AutoBackup, _settings.AutoBackup);
            NotifySettingChanged(nameof(BackupRetentionDays), oldSettings.BackupRetentionDays, _settings.BackupRetentionDays);
            NotifySettingChanged(nameof(WindowWidth), oldSettings.WindowWidth, _settings.WindowWidth);
            NotifySettingChanged(nameof(WindowHeight), oldSettings.WindowHeight, _settings.WindowHeight);
            NotifySettingChanged(nameof(WindowMaximized), oldSettings.WindowMaximized, _settings.WindowMaximized);
        }

        #endregion

        #region Private Methods

        private static string GetDefaultDatabasePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "BudgetManagement");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "budget.db");
        }


        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnAllPropertiesChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private void NotifySettingChanged(string settingName, object? oldValue, object? newValue)
        {
            SettingChanged?.Invoke(this, new SettingChangedEventArgs(settingName, oldValue, newValue));
        }

        #endregion

        #region Internal Settings Class

        private class AppSettings
        {
            // UI Settings with senior-friendly defaults
            public int FontSize { get; set; } = 16; // Larger default font
            public string Theme { get; set; } = "Light";
            public string Language { get; set; } = "en"; // Default to English
            public string CurrencySymbol { get; set; } = "$";
            public string DateFormat { get; set; } = "MM/dd/yyyy";

            // Application Settings
            public string DatabasePath { get; set; } = GetDefaultDatabasePath();
            public bool AutoBackup { get; set; } = true;
            public int BackupRetentionDays { get; set; } = 30;

            // Window Settings - senior-friendly defaults
            public double WindowWidth { get; set; } = 1200; // Larger default window
            public double WindowHeight { get; set; } = 800;
            public bool WindowMaximized { get; set; } = false;
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for settings-related operations
    /// </summary>
    public static class SettingsExtensions
    {
        /// <summary>
        /// Gets senior-friendly font size based on current setting
        /// </summary>
        public static double GetScaledFontSize(this ISettingsService settings, double baseFontSize = 12)
        {
            var scale = settings.FontSize / 12.0; // Base scale from default font size
            return Math.Round(baseFontSize * scale, 1);
        }

        /// <summary>
        /// Gets senior-friendly control size scaling
        /// </summary>
        public static double GetControlScale(this ISettingsService settings)
        {
            return Math.Max(1.0, settings.FontSize / 12.0);
        }

        /// <summary>
        /// Determines if high contrast should be used based on theme
        /// </summary>
        public static bool UseHighContrast(this ISettingsService settings)
        {
            return settings.Theme.Contains("HighContrast", StringComparison.OrdinalIgnoreCase);
        }
    }
}