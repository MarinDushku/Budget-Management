// Theme Service Implementation
// File: Services/ThemeService.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace BudgetManagement.Services
{
    /// <summary>
    /// Implementation of theme service for managing application themes
    /// </summary>
    public class ThemeService : IThemeService
    {
        private readonly ISettingsService _settingsService;
        private bool _initialized = false;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public ThemeService(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _settingsService.SettingChanged += OnSettingChanged;
        }

        public string CurrentTheme => _settingsService.Theme;

        public bool IsDarkTheme => GetEffectiveTheme().Equals("Dark", StringComparison.OrdinalIgnoreCase);

        public bool IsAutoTheme => CurrentTheme.Equals("Auto", StringComparison.OrdinalIgnoreCase);

        public async Task InitializeAsync()
        {
            if (_initialized) return;

            // Apply the current theme
            ApplyTheme();
            _initialized = true;

            // If auto theme is enabled, listen for system theme changes
            if (IsAutoTheme)
            {
                ListenForSystemThemeChanges();
            }

            await Task.CompletedTask;
        }

        public async Task SetThemeAsync(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
                throw new ArgumentException("Theme name cannot be null or empty", nameof(themeName));

            var validThemes = new[] { "Light", "Dark", "Auto" };
            if (!validThemes.Contains(themeName, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Invalid theme name: {themeName}", nameof(themeName));

            var oldTheme = CurrentTheme;
            var oldIsDark = IsDarkTheme;

            _settingsService.Theme = themeName;
            await _settingsService.SaveSettingsAsync();

            // Apply the new theme
            ApplyTheme();

            // Setup or remove system theme listening
            if (IsAutoTheme)
            {
                ListenForSystemThemeChanges();
            }

            // Fire theme changed event
            var newIsDark = IsDarkTheme;
            if (oldIsDark != newIsDark || !oldTheme.Equals(CurrentTheme, StringComparison.OrdinalIgnoreCase))
            {
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, CurrentTheme, newIsDark));
            }
        }

        public async Task ToggleThemeAsync()
        {
            var newTheme = IsDarkTheme ? "Light" : "Dark";
            await SetThemeAsync(newTheme);
        }

        public string GetEffectiveTheme()
        {
            return CurrentTheme.ToLowerInvariant() switch
            {
                "auto" => DetectSystemTheme(),
                "dark" => "Dark",
                _ => "Light"
            };
        }

        public void ApplyTheme()
        {
            if (Application.Current?.Resources == null) return;

            try
            {
                var effectiveTheme = GetEffectiveTheme();
                var resources = Application.Current.Resources;

                // Remove existing theme dictionaries
                RemoveThemeResourceDictionaries(resources);

                // Apply the new theme resources
                ApplyThemeResourceDictionary(resources, effectiveTheme);
            }
            catch (Exception ex)
            {
                // Log error but don't crash - fall back to default theme
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private void OnSettingChanged(object? sender, SettingChangedEventArgs e)
        {
            if (e.SettingName.Equals(nameof(ISettingsService.Theme), StringComparison.OrdinalIgnoreCase))
            {
                if (_initialized)
                {
                    ApplyTheme();
                }
            }
        }

        private static string DetectSystemTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var appsUseLightTheme = key?.GetValue("AppsUseLightTheme");
                
                if (appsUseLightTheme is int value)
                {
                    return value == 0 ? "Dark" : "Light";
                }
            }
            catch
            {
                // Ignore registry errors
            }

            // Default to Light theme if detection fails
            return "Light";
        }

        private void ListenForSystemThemeChanges()
        {
            // Note: For full system theme change detection, you would typically use
            // SystemEvents.UserPreferenceChanged or WM_SETTINGCHANGE messages
            // For this implementation, we'll keep it simple and just detect on startup
        }

        private static void RemoveThemeResourceDictionaries(ResourceDictionary resources)
        {
            // Remove any existing theme resource dictionaries
            var themeDictionaries = resources.MergedDictionaries
                .Where(d => d.Source?.OriginalString?.Contains("ThemeResources") == true ||
                           d.Source?.OriginalString?.Contains("DarkTheme") == true ||
                           d.Source?.OriginalString?.Contains("LightTheme") == true)
                .ToList();

            foreach (var dict in themeDictionaries)
            {
                resources.MergedDictionaries.Remove(dict);
            }
            
            // Don't remove the base resource keys - just let them be overridden by the theme dictionary
        }

        private static void ApplyThemeResourceDictionary(ResourceDictionary resources, string theme)
        {
            try
            {
                // Create a resource dictionary for theme-specific colors
                var themeDict = new ResourceDictionary();

                if (theme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyDarkThemeResources(themeDict);
                }
                else
                {
                    ApplyLightThemeResources(themeDict);
                }

                // Add the theme dictionary resources directly to override base resources
                foreach (var key in themeDict.Keys)
                {
                    resources[key] = themeDict[key];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme resources: {ex.Message}");
            }
        }

        private static void ApplyDarkThemeResources(ResourceDictionary themeDict)
        {
            // Dark Theme Colors
            themeDict["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x1E));
            themeDict["SurfaceBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x30));
            themeDict["SurfaceElevatedBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3C, 0x3C, 0x3C));
            themeDict["BorderLightBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x48, 0x48, 0x48));
            themeDict["BorderMediumBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x5A, 0x5A, 0x5A));
            
            // Text Colors
            themeDict["PrimaryTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
            themeDict["SecondaryTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC));
            themeDict["DisabledTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x80, 0x80, 0x80));
            
            // Keep accent colors the same but slightly brighter for dark mode
            themeDict["PrimaryBlueBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x86, 0xF0));
            themeDict["AccentGreenBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x12, 0x8C, 0x12));
            themeDict["WarningOrangeBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x9C, 0x00));
            themeDict["DangerRedBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35));
            
            // Chart and Visualization Colors for Dark Mode
            themeDict["ChartBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x2A));
            themeDict["ChartGridBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x40, 0x40, 0x40));
            themeDict["ChartAxisBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x60, 0x60, 0x60));
            themeDict["ChartLabelBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE0, 0xE0, 0xE0));
            
            // Chart Series Colors (bright colors for dark background)
            themeDict["ChartSeries1Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x42, 0xA5, 0xF5)); // Light Blue
            themeDict["ChartSeries2Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0xBB, 0x6A)); // Light Green
            themeDict["ChartSeries3Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xA7, 0x26)); // Orange
            themeDict["ChartSeries4Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEF, 0x53, 0x50)); // Red
            themeDict["ChartSeries5Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xAB, 0x47, 0xBC)); // Purple
            themeDict["ChartSeries6Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x26, 0xC6, 0xDA)); // Cyan
            
            // Status-specific chart colors
            themeDict["ChartSuccessBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
            themeDict["ChartWarningBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x9C, 0x02));
            themeDict["ChartErrorBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF4, 0x43, 0x36));
            themeDict["ChartInfoBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x96, 0xF3));
            
            // Header Gradient for Dark Mode
            var headerGradient = new System.Windows.Media.LinearGradientBrush();
            headerGradient.StartPoint = new System.Windows.Point(0, 0);
            headerGradient.EndPoint = new System.Windows.Point(0, 1);
            headerGradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x00, 0x86, 0xF0), 0));
            headerGradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x12, 0x7A, 0xCE), 1));
            themeDict["HeaderGradientBrush"] = headerGradient;
        }

        private static void ApplyLightThemeResources(ResourceDictionary themeDict)
        {
            // Light Theme Colors (from ModernDesign.xaml)
            themeDict["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFA, 0xFA, 0xFA));
            themeDict["SurfaceBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
            themeDict["SurfaceElevatedBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF5, 0xF5, 0xF5));
            themeDict["BorderLightBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE1, 0xE1, 0xE1));
            themeDict["BorderMediumBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC4, 0xC4, 0xC4));
            
            // Text Colors
            themeDict["PrimaryTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x1F, 0x1F));
            themeDict["SecondaryTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x61, 0x61, 0x61));
            themeDict["DisabledTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x9E, 0x9E, 0x9E));
            
            // Accent Colors
            themeDict["PrimaryBlueBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4));
            themeDict["AccentGreenBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0x7C, 0x10));
            themeDict["WarningOrangeBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x8C, 0x00));
            themeDict["DangerRedBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD7, 0x35, 0x27));
            
            // Chart and Visualization Colors for Light Mode
            themeDict["ChartBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
            themeDict["ChartGridBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE0, 0xE0, 0xE0));
            themeDict["ChartAxisBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x9E, 0x9E, 0x9E));
            themeDict["ChartLabelBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x42, 0x42, 0x42));
            
            // Chart Series Colors (darker colors for light background)
            themeDict["ChartSeries1Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x19, 0x76, 0xD2)); // Blue
            themeDict["ChartSeries2Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0x8E, 0x3C)); // Green
            themeDict["ChartSeries3Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF5, 0x7C, 0x00)); // Orange
            themeDict["ChartSeries4Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD3, 0x2F, 0x2F)); // Red
            themeDict["ChartSeries5Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7B, 0x1F, 0xA2)); // Purple
            themeDict["ChartSeries6Brush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x96, 0x88)); // Teal
            
            // Status-specific chart colors
            themeDict["ChartSuccessBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32));
            themeDict["ChartWarningBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xED, 0x6C, 0x02));
            themeDict["ChartErrorBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));
            themeDict["ChartInfoBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x02, 0x79, 0xB9));
            
            // Header Gradient for Light Mode
            var headerGradient = new System.Windows.Media.LinearGradientBrush();
            headerGradient.StartPoint = new System.Windows.Point(0, 0);
            headerGradient.EndPoint = new System.Windows.Point(0, 1);
            headerGradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4), 0));
            headerGradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x10, 0x6E, 0xBE), 1));
            themeDict["HeaderGradientBrush"] = headerGradient;
        }
    }
}