// Theme Service Interface
// File: Services/IThemeService.cs

using System;
using System.Threading.Tasks;

namespace BudgetManagement.Services
{
    /// <summary>
    /// Interface for theme management and switching functionality
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Gets the current theme name (Light, Dark, Auto)
        /// </summary>
        string CurrentTheme { get; }

        /// <summary>
        /// Gets whether the current effective theme is dark
        /// </summary>
        bool IsDarkTheme { get; }

        /// <summary>
        /// Gets whether auto theme switching is enabled
        /// </summary>
        bool IsAutoTheme { get; }

        /// <summary>
        /// Event fired when the theme changes
        /// </summary>
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// Sets the application theme
        /// </summary>
        /// <param name="themeName">Theme name: Light, Dark, or Auto</param>
        Task SetThemeAsync(string themeName);

        /// <summary>
        /// Toggles between light and dark theme
        /// </summary>
        Task ToggleThemeAsync();

        /// <summary>
        /// Applies the current theme to the application
        /// </summary>
        void ApplyTheme();

        /// <summary>
        /// Gets the effective theme based on auto-detection if needed
        /// </summary>
        string GetEffectiveTheme();

        /// <summary>
        /// Initializes the theme service and applies the saved theme
        /// </summary>
        Task InitializeAsync();
    }

    /// <summary>
    /// Event arguments for theme changed events
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public string OldTheme { get; }
        public string NewTheme { get; }
        public bool IsDarkTheme { get; }

        public ThemeChangedEventArgs(string oldTheme, string newTheme, bool isDarkTheme)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
            IsDarkTheme = isDarkTheme;
        }
    }
}