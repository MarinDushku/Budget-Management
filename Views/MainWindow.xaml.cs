using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BudgetManagement.ViewModels;
using BudgetManagement.Services;
using BudgetManagement.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManagement.Views
{
    /// <summary>
    /// Main window for the Budget Management application
    /// Designed with senior-friendly interface principles
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            System.Diagnostics.Debug.WriteLine("🏗️ MainWindow: DEFAULT constructor called (no ViewModel)");
            InitializeComponent();
            
            // Set up senior-friendly window behavior
            InitializeSeniorFriendlyFeatures();
        }

        public MainWindow(MainViewModel viewModel) : this()
        {
            System.Diagnostics.Debug.WriteLine("🏗️ MainWindow: DI constructor called (with ViewModel)");
            DataContext = viewModel;
        }

        private void InitializeSeniorFriendlyFeatures()
        {
            // Ensure the window is properly sized for seniors
            if (SystemParameters.PrimaryScreenWidth < 1400)
            {
                Width = SystemParameters.PrimaryScreenWidth * 0.9;
            }
            
            if (SystemParameters.PrimaryScreenHeight < 900)
            {
                Height = SystemParameters.PrimaryScreenHeight * 0.9;
            }

            // Center the window
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Enable high DPI awareness for better text clarity
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== MAINWINDOW WINDOW_LOADED START ===");
            
            // Initialize the view model when the window loads
            if (DataContext is MainViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Initializing ViewModel...");
                await viewModel.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("MainWindow: ViewModel initialization completed");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: No ViewModel found in DataContext");
            }

            // Set up language combobox
            System.Diagnostics.Debug.WriteLine("MainWindow: About to call SetupLanguageSelector()");
            SetupLanguageSelector();
            System.Diagnostics.Debug.WriteLine("MainWindow: SetupLanguageSelector() completed");
            
            // Initialize theme controls and service
            InitializeThemeControls();
            
            // Initialize budget settings controls
            InitializeBudgetSettingsControls();
            
            // Initialize theme service if available
            var app = Application.Current as App;
            var themeService = app?.GetService<IThemeService>();
            if (themeService != null)
            {
                await themeService.InitializeAsync();
            }
            
            System.Diagnostics.Debug.WriteLine("=== MAINWINDOW WINDOW_LOADED END ===");
        }

        private void SetupLanguageSelector()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SetupLanguageSelector: Starting language selector setup");
                
                // CRITICAL: Check if LanguageComboBox exists
                if (LanguageComboBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("SetupLanguageSelector: ⚠️ CRITICAL: LanguageComboBox is NULL!");
                    return;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SetupLanguageSelector: ✅ LanguageComboBox found successfully");
                }
                
                var app = Application.Current as App;
                var localizationService = app?.GetService<IEnterpriseLocalizationService>();
                var settingsService = app?.GetService<ISettingsService>();

                System.Diagnostics.Debug.WriteLine($"SetupLanguageSelector: App is null: {app == null}");
                System.Diagnostics.Debug.WriteLine($"SetupLanguageSelector: LocalizationService is null: {localizationService == null}");
                System.Diagnostics.Debug.WriteLine($"SetupLanguageSelector: SettingsService is null: {settingsService == null}");

                if (localizationService != null && settingsService != null)
                {
                    // Set current selection based on current language
                    var currentLanguage = localizationService.CurrentLanguage;
                    System.Diagnostics.Debug.WriteLine($"SetupLanguageSelector: Current language: {currentLanguage}");
                    System.Diagnostics.Debug.WriteLine($"SetupLanguageSelector: LanguageComboBox has {LanguageComboBox.Items.Count} items");
                    
                    foreach (ComboBoxItem item in LanguageComboBox.Items)
                    {
                        var tagValue = item.Tag?.ToString();
                        System.Diagnostics.Debug.WriteLine($"SetupLanguageSelector: ComboBoxItem - Content: {item.Content}, Tag: {tagValue}");
                        if (tagValue == currentLanguage)
                        {
                            LanguageComboBox.SelectedItem = item;
                            System.Diagnostics.Debug.WriteLine($"SetupLanguageSelector: Selected item with tag '{tagValue}'");
                            break;
                        }
                    }

                    // Handle language changes - ADD MULTIPLE EVENT TYPES FOR DEBUGGING
                    System.Diagnostics.Debug.WriteLine("SetupLanguageSelector: About to attach SelectionChanged event handler");
                    
                    LanguageComboBox.SelectionChanged += async (s, e) =>
                    {
                        System.Diagnostics.Debug.WriteLine("🔥 MainWindow: LanguageComboBox.SelectionChanged event triggered");
                        System.Diagnostics.Debug.WriteLine($"🔥 Event sender: {s?.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"🔥 AddedItems count: {e.AddedItems.Count}");
                        System.Diagnostics.Debug.WriteLine($"🔥 RemovedItems count: {e.RemovedItems.Count}");
                        
                        if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem &&
                            selectedItem.Tag?.ToString() is string languageCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"🔥 MainWindow: Selected language = '{languageCode}'");
                            System.Diagnostics.Debug.WriteLine($"🔥 MainWindow: Current localizationService is null: {localizationService == null}");
                            
                            if (localizationService != null)
                            {
                                localizationService.SetLanguage(languageCode);
                                settingsService.Language = languageCode;
                                await settingsService.SaveSettingsAsync();
                                
                                System.Diagnostics.Debug.WriteLine("🔥 MainWindow: About to call RefreshUIForLanguageChange()");
                                // Force UI refresh for language changes
                                RefreshUIForLanguageChange();
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("🔥 MainWindow: LocalizationService is null - cannot change language");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"🔥 MainWindow: Invalid selection - SelectedItem: {LanguageComboBox.SelectedItem}, Tag: {(LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag}");
                        }
                    };
                    
                    // ALSO ADD DROPDOWN OPENED/CLOSED EVENTS FOR DEBUGGING
                    LanguageComboBox.DropDownOpened += (s, e) => 
                    {
                        System.Diagnostics.Debug.WriteLine("🔥 MainWindow: LanguageComboBox dropdown OPENED");
                    };
                    
                    LanguageComboBox.DropDownClosed += (s, e) => 
                    {
                        System.Diagnostics.Debug.WriteLine("🔥 MainWindow: LanguageComboBox dropdown CLOSED");
                        System.Diagnostics.Debug.WriteLine($"🔥 Current selection after close: {(LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag}");
                    };
                    
                    System.Diagnostics.Debug.WriteLine("SetupLanguageSelector: Event handler attached successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SetupLanguageSelector: Cannot setup language selector - services are null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetupLanguageSelector: Error during setup: {ex.Message}");
            }
        }

        private void InitializeBudgetSettingsControls()
        {
            try
            {
                var app = Application.Current as App;
                var settingsService = app?.GetService<ISettingsService>();
                
                if (settingsService != null)
                {
                    // Set current bank statement day value
                    BankStatementDayTextBox.Text = settingsService.BankStatementDay.ToString();
                }
            }
            catch
            {
                // Use default value if initialization fails
                BankStatementDayTextBox.Text = "17";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Could add confirmation dialog here if needed
            // For seniors, it's often better to prevent accidental closes
        }

        // Keyboard shortcuts for senior accessibility
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel) return;

            // F5 - Refresh (common and intuitive)
            if (e.Key == System.Windows.Input.Key.F5)
            {
                if (viewModel.RefreshCommand.CanExecute(null))
                    viewModel.RefreshCommand.Execute(null);
                e.Handled = true;
            }
            
            // Ctrl+N - New Income (Add Income)
            if (e.Key == System.Windows.Input.Key.N && 
                (e.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                if (viewModel.AddIncomeCommand.CanExecute(null))
                    viewModel.AddIncomeCommand.Execute(null);
                e.Handled = true;
            }
            
            // Ctrl+E - New Expense (Add Spending)
            if (e.Key == System.Windows.Input.Key.E && 
                (e.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                if (viewModel.AddSpendingCommand.CanExecute(null))
                    viewModel.AddSpendingCommand.Execute(null);
                e.Handled = true;
            }
        }

        #region Accessibility Event Handlers

        private void HighContrastCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ApplyHighContrastTheme(true);
        }

        private void HighContrastCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyHighContrastTheme(false);
        }

        private void FontSizeSmall_Click(object sender, RoutedEventArgs e)
        {
            SetApplicationFontSize(14);
            UpdateFontSizeButtons("small");
        }

        private void FontSizeMedium_Click(object sender, RoutedEventArgs e)
        {
            SetApplicationFontSize(16);
            UpdateFontSizeButtons("medium");
        }

        private void FontSizeLarge_Click(object sender, RoutedEventArgs e)
        {
            SetApplicationFontSize(20);
            UpdateFontSizeButtons("large");
        }

        private void ApplyHighContrastTheme(bool highContrast)
        {
            var resources = Application.Current.Resources;
            
            if (highContrast)
            {
                // Load high contrast theme resource dictionary
                var highContrastDict = new ResourceDictionary();
                highContrastDict.Source = new Uri("/Views/Styles/HighContrastTheme.xaml", UriKind.Relative);
                
                // Add high contrast dictionary to application resources
                if (!resources.MergedDictionaries.Any(d => d.Source?.OriginalString?.Contains("HighContrastTheme") == true))
                {
                    resources.MergedDictionaries.Add(highContrastDict);
                }
                
                // Apply high contrast window styling
                this.Background = highContrastDict["HighContrast_MainBackgroundBrush"] as SolidColorBrush;
                this.Foreground = highContrastDict["HighContrast_PrimaryTextBrush"] as SolidColorBrush;
                
                // Update dynamic styles for better contrast
                UpdateControlStyles(highContrastDict, true);
            }
            else
            {
                // Remove high contrast theme
                var highContrastDict = resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString?.Contains("HighContrastTheme") == true);
                
                if (highContrastDict != null)
                {
                    resources.MergedDictionaries.Remove(highContrastDict);
                }
                
                // Restore normal window styling
                this.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                this.Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41));
                
                // Update control styles back to normal
                UpdateControlStyles(null, false);
            }
        }
        
        private void UpdateControlStyles(ResourceDictionary? highContrastDict, bool isHighContrast)
        {
            // Update all buttons in the window
            UpdateButtonStyles(this, isHighContrast, highContrastDict);
            
            // Update all text controls
            UpdateTextControlStyles(this, isHighContrast, highContrastDict);
            
            // Update all input controls
            UpdateInputControlStyles(this, isHighContrast, highContrastDict);
            
            // Update all panels and borders
            UpdatePanelStyles(this, isHighContrast, highContrastDict);
        }
        
        private void UpdateButtonStyles(DependencyObject parent, bool isHighContrast, ResourceDictionary? highContrastDict)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Button button)
                {
                    if (isHighContrast && highContrastDict != null)
                    {
                        // Determine button type and apply appropriate high contrast style
                        if (button.Name?.Contains("Success") == true || button.Content?.ToString()?.Contains("Save") == true)
                        {
                            button.Style = highContrastDict["HighContrastSuccessButton"] as Style;
                        }
                        else if (button.Name?.Contains("Danger") == true || button.Content?.ToString()?.Contains("Cancel") == true)
                        {
                            button.Style = highContrastDict["HighContrastDangerButton"] as Style;
                        }
                        else if (button.Name?.Contains("Secondary") == true)
                        {
                            button.Style = highContrastDict["HighContrastSecondaryButton"] as Style;
                        }
                        else
                        {
                            button.Style = highContrastDict["HighContrastPrimaryButton"] as Style;
                        }
                    }
                    else
                    {
                        // Reset to normal style (you may want to store original styles)
                        button.ClearValue(Button.StyleProperty);
                    }
                }
                else
                {
                    UpdateButtonStyles(child, isHighContrast, highContrastDict);
                }
            }
        }
        
        private void UpdateTextControlStyles(DependencyObject parent, bool isHighContrast, ResourceDictionary? highContrastDict)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is TextBlock textBlock)
                {
                    if (isHighContrast && highContrastDict != null)
                    {
                        // Apply high contrast text styling based on context
                        if (textBlock.FontWeight == FontWeights.Bold || textBlock.FontSize > 20)
                        {
                            textBlock.Style = highContrastDict["HighContrastHeaderText"] as Style;
                        }
                        else if (textBlock.FontSize > 14)
                        {
                            textBlock.Style = highContrastDict["HighContrastPrimaryText"] as Style;
                        }
                        else
                        {
                            textBlock.Style = highContrastDict["HighContrastSecondaryText"] as Style;
                        }
                    }
                    else
                    {
                        textBlock.ClearValue(TextBlock.StyleProperty);
                    }
                }
                else
                {
                    UpdateTextControlStyles(child, isHighContrast, highContrastDict);
                }
            }
        }
        
        private void UpdateInputControlStyles(DependencyObject parent, bool isHighContrast, ResourceDictionary? highContrastDict)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is TextBox textBox)
                {
                    if (isHighContrast && highContrastDict != null)
                    {
                        textBox.Style = highContrastDict["HighContrastTextBoxStyle"] as Style;
                    }
                    else
                    {
                        textBox.ClearValue(TextBox.StyleProperty);
                    }
                }
                else if (child is ComboBox comboBox)
                {
                    if (isHighContrast && highContrastDict != null)
                    {
                        comboBox.Style = highContrastDict["HighContrastComboBoxStyle"] as Style;
                    }
                    else
                    {
                        comboBox.ClearValue(ComboBox.StyleProperty);
                    }
                }
                else if (child is CheckBox checkBox)
                {
                    if (isHighContrast && highContrastDict != null)
                    {
                        checkBox.Style = highContrastDict["HighContrastCheckBoxStyle"] as Style;
                    }
                    else
                    {
                        checkBox.ClearValue(CheckBox.StyleProperty);
                    }
                }
                else
                {
                    UpdateInputControlStyles(child, isHighContrast, highContrastDict);
                }
            }
        }
        
        private void UpdatePanelStyles(DependencyObject parent, bool isHighContrast, ResourceDictionary? highContrastDict)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Border border && border.Name?.Contains("Panel") == true)
                {
                    if (isHighContrast && highContrastDict != null)
                    {
                        border.Style = highContrastDict["HighContrastSidePanel"] as Style;
                    }
                    else
                    {
                        border.ClearValue(Border.StyleProperty);
                    }
                }
                else if (child is ScrollViewer scrollViewer)
                {
                    if (isHighContrast && highContrastDict != null)
                    {
                        scrollViewer.Style = highContrastDict["HighContrastScrollViewer"] as Style;
                    }
                    else
                    {
                        scrollViewer.ClearValue(ScrollViewer.StyleProperty);
                    }
                }
                else
                {
                    UpdatePanelStyles(child, isHighContrast, highContrastDict);
                }
            }
        }

        private void SetApplicationFontSize(double fontSize)
        {
            var resources = Application.Current.Resources;
            
            // Update global font size resource
            if (resources.Contains("GlobalFontSize"))
                resources["GlobalFontSize"] = fontSize;
            else
                resources.Add("GlobalFontSize", fontSize);
            
            // Calculate relative font sizes based on the base font size
            var headerFontSize = fontSize * 1.75;     // 28px when base is 16px
            var largeFontSize = fontSize * 1.25;      // 20px when base is 16px
            var mediumFontSize = fontSize;            // 16px base
            var smallFontSize = fontSize * 0.875;     // 14px when base is 16px
            var tinyFontSize = fontSize * 0.75;       // 12px when base is 16px
            
            // Update all global font size resources
            UpdateOrAddResource(resources, "HeaderFontSize", headerFontSize);
            UpdateOrAddResource(resources, "LargeFontSize", largeFontSize);
            UpdateOrAddResource(resources, "MediumFontSize", mediumFontSize);
            UpdateOrAddResource(resources, "SmallFontSize", smallFontSize);
            UpdateOrAddResource(resources, "TinyFontSize", tinyFontSize);
            
            // Update main window font size
            this.FontSize = fontSize;
            
            // Recursively update all controls in the application
            UpdateAllControlFontSizes(this, fontSize);
            
            // Update all open dialogs if any
            foreach (Window window in Application.Current.Windows)
            {
                if (window != this && window.IsLoaded)
                {
                    UpdateAllControlFontSizes(window, fontSize);
                }
            }
            
            // Save font size preference
            try
            {
                var app = Application.Current as App;
                var settingsService = app?.GetService<ISettingsService>();
                if (settingsService != null)
                {
                    settingsService.FontSize = (int)fontSize;
                    _ = settingsService.SaveSettingsAsync(); // Fire and forget
                }
            }
            catch
            {
                // Ignore errors in settings save
            }
        }
        
        private void UpdateOrAddResource(ResourceDictionary resources, string key, object value)
        {
            if (resources.Contains(key))
                resources[key] = value;
            else
                resources.Add(key, value);
        }
        
        private void UpdateAllControlFontSizes(DependencyObject parent, double baseFontSize)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is FrameworkElement element)
                {
                    UpdateControlFontSize(element, baseFontSize);
                }
                
                // Recursively update child controls
                UpdateAllControlFontSizes(child, baseFontSize);
            }
        }
        
        private void UpdateControlFontSize(FrameworkElement control, double baseFontSize)
        {
            
            if (control is Button button)
            {
                // Buttons get slightly larger font for better readability
                if (button.Name?.Contains("FontSize") == true)
                {
                    // Font size buttons keep their relative sizes
                    if (button.Name.Contains("Small"))
                        button.FontSize = baseFontSize * 0.75;
                    else if (button.Name.Contains("Medium"))
                        button.FontSize = baseFontSize;
                    else if (button.Name.Contains("Large"))
                        button.FontSize = baseFontSize * 1.25;
                }
                else
                {
                    // Regular buttons
                    button.FontSize = baseFontSize * 1.0;
                }
            }
            else if (control is TextBlock textBlock)
            {
                // Headers and important text get larger fonts
                if (textBlock.FontWeight == FontWeights.Bold && textBlock.FontSize > baseFontSize * 1.5)
                {
                    textBlock.FontSize = baseFontSize * 1.75; // Headers
                }
                else if (textBlock.FontWeight == FontWeights.Bold)
                {
                    textBlock.FontSize = baseFontSize * 1.125; // Bold text slightly larger
                }
                else if (textBlock.FontSize < baseFontSize * 0.9)
                {
                    textBlock.FontSize = baseFontSize * 0.875; // Small text
                }
                else
                {
                    textBlock.FontSize = baseFontSize; // Normal text
                }
            }
            else if (control is TextBox textBox)
            {
                textBox.FontSize = baseFontSize;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.FontSize = baseFontSize;
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.FontSize = baseFontSize;
            }
            else if (control is Label label)
            {
                label.FontSize = baseFontSize;
            }
            else if (control is DatePicker datePicker)
            {
                datePicker.FontSize = baseFontSize;
            }
            else if (control is DataGrid dataGrid)
            {
                dataGrid.FontSize = baseFontSize * 0.9375; // Slightly smaller for data
            }
            else if (control is Control otherControl)
            {
                // Default case for other controls that derive from Control
                otherControl.FontSize = baseFontSize;
            }
            // If it's not a control with FontSize property, just skip it
        }

        private void UpdateFontSizeButtons(string activeSize)
        {
            // Reset all button backgrounds
            FontSizeSmallBtn.Background = SystemColors.ControlBrush;
            FontSizeMediumBtn.Background = SystemColors.ControlBrush;
            FontSizeLargeBtn.Background = SystemColors.ControlBrush;
            
            // Highlight the active button
            switch (activeSize)
            {
                case "small":
                    FontSizeSmallBtn.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    FontSizeSmallBtn.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case "medium":
                    FontSizeMediumBtn.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    FontSizeMediumBtn.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case "large":
                    FontSizeLargeBtn.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    FontSizeLargeBtn.Foreground = new SolidColorBrush(Colors.White);
                    break;
            }
        }

        #endregion

        #region Theme Management

        private async void DarkModeToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Add button press animation feedback
                if (sender is FrameworkElement element)
                    AnimateButtonPress(element);

                var app = Application.Current as App;
                var themeService = app?.GetService<IThemeService>();
                
                if (themeService != null)
                {
                    var newTheme = DarkModeToggle.IsChecked == true ? "Dark" : "Light";
                    await themeService.SetThemeAsync(newTheme);
                    
                    // Update the theme combobox to match
                    UpdateThemeComboBoxSelection(newTheme);
                }
            }
            catch
            {
                // Reset toggle if theme change fails
                DarkModeToggle.IsChecked = !DarkModeToggle.IsChecked;
            }
        }

        private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem &&
                    selectedItem.Tag?.ToString() is string themeName)
                {
                    var app = Application.Current as App;
                    var themeService = app?.GetService<IThemeService>();
                    
                    if (themeService != null)
                    {
                        await themeService.SetThemeAsync(themeName);
                        
                        // Update the toggle to match
                        UpdateDarkModeToggle(themeService.IsDarkTheme);
                    }
                }
            }
            catch
            {
                // Ignore errors in theme selection
            }
        }

        private void UpdateThemeComboBoxSelection(string themeName)
        {
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag?.ToString()?.Equals(themeName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void UpdateDarkModeToggle(bool isDarkTheme)
        {
            DarkModeToggle.IsChecked = isDarkTheme;
        }

        private void InitializeThemeControls()
        {
            try
            {
                var app = Application.Current as App;
                var themeService = app?.GetService<IThemeService>();
                
                if (themeService != null)
                {
                    // Set initial theme combobox selection
                    UpdateThemeComboBoxSelection(themeService.CurrentTheme);
                    
                    // Set initial toggle state
                    UpdateDarkModeToggle(themeService.IsDarkTheme);
                    
                    // Listen for theme changes
                    themeService.ThemeChanged += OnThemeChanged;
                }
            }
            catch
            {
                // Use default theme settings if initialization fails
            }
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            // Update UI controls when theme changes
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateThemeComboBoxSelection(e.NewTheme);
                UpdateDarkModeToggle(e.IsDarkTheme);
                
                // Force refresh of all open dialogs and windows
                RefreshAllWindowsForThemeChange();
            });
        }
        
        private void RefreshAllWindowsForThemeChange()
        {
            try
            {
                // Refresh all open windows to apply new theme resources
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.IsLoaded)
                    {
                        // Force invalidate visual to refresh all theme-bound resources
                        window.InvalidateVisual();
                        
                        // Force refresh of all child elements recursively
                        RefreshVisualTree(window);
                        
                        // Update any DataContext that might need theme refresh
                        if (window.DataContext is INotifyPropertyChanged notifyContext)
                        {
                            // The DataContext can handle any theme-specific updates
                            // This is useful for ViewModels that generate theme-aware colors
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing windows for theme change: {ex.Message}");
            }
        }
        
        private void RefreshVisualTree(DependencyObject parent)
        {
            try
            {
                if (parent == null) return;
                
                // Invalidate this element
                if (parent is FrameworkElement element)
                {
                    element.InvalidateVisual();
                    element.UpdateLayout();
                }
                
                // Recursively refresh all children
                var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    RefreshVisualTree(child);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing visual tree element: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces UI refresh when language changes to update all DynamicResource bindings
        /// </summary>
        private void RefreshUIForLanguageChange()
        {
            try
            {
                // More aggressive approach: Force complete resource refresh
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                {
                    try
                    {
                        // Force refresh of all open windows with a delay to ensure resources are loaded
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.IsLoaded)
                            {
                                // Force complete visual tree refresh
                                window.InvalidateVisual();
                                RefreshVisualTreeForLanguage(window);
                                window.UpdateLayout();
                            }
                        }
                        
                        // Additional pass to ensure everything is updated
                        Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window.IsLoaded)
                                {
                                    window.InvalidateVisual();
                                    window.UpdateLayout();
                                }
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in UI refresh pass: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing UI for language change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Recursively refreshes visual tree for language changes
        /// </summary>
        private void RefreshVisualTreeForLanguage(DependencyObject parent)
        {
            try
            {
                if (parent == null) return;
                
                // Force refresh of this element
                if (parent is FrameworkElement element)
                {
                    // Force refresh
                    element.InvalidateVisual();
                    element.UpdateLayout();
                    
                    // Special handling for TextBlocks and other text elements
                    if (element is TextBlock textBlock)
                    {
                        // Force text refresh by clearing and re-evaluating bindings
                        var expression = textBlock.GetBindingExpression(TextBlock.TextProperty);
                        expression?.UpdateTarget();
                    }
                    else if (element is ContentControl contentControl)
                    {
                        // Force content refresh
                        var expression = contentControl.GetBindingExpression(ContentControl.ContentProperty);
                        expression?.UpdateTarget();
                    }
                }
                
                // Recursively refresh all children
                var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    RefreshVisualTreeForLanguage(child);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing visual tree element for language: {ex.Message}");
            }
        }

        /// <summary>
        /// Test method to manually trigger language switching - can be called from debugger
        /// </summary>
        public void TestLanguageSwitch(string languageCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"TestLanguageSwitch: Manually switching to '{languageCode}'");
                
                var app = Application.Current as App;
                var localizationService = app?.GetService<IEnterpriseLocalizationService>();
                var settingsService = app?.GetService<ISettingsService>();
                
                if (localizationService != null && settingsService != null)
                {
                    System.Diagnostics.Debug.WriteLine($"TestLanguageSwitch: Current language before switch: {localizationService.CurrentLanguage}");
                    
                    localizationService.SetLanguage(languageCode);
                    settingsService.Language = languageCode;
                    _ = settingsService.SaveSettingsAsync(); // Fire and forget
                    
                    System.Diagnostics.Debug.WriteLine($"TestLanguageSwitch: Language set to: {localizationService.CurrentLanguage}");
                    
                    // Test if resources are actually available
                    TestCurrentResources();
                    
                    // Force UI refresh
                    RefreshUIForLanguageChange();
                    
                    System.Diagnostics.Debug.WriteLine("TestLanguageSwitch: UI refresh completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("TestLanguageSwitch: Services not available");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TestLanguageSwitch: Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test what resources are currently available
        /// </summary>
        public void TestCurrentResources()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TestCurrentResources: === CURRENT RESOURCE STATUS ===");
                
                // Test critical language-specific keys
                var testKeys = new[] { 
                    "AppTitle", "Dashboard", "AddIncomeButton", "English", "Albanian", "Language",
                    "MainSection", "ActionsSection", "SearchSection", "ToolsSection"
                };
                
                foreach (var key in testKeys)
                {
                    try
                    {
                        if (Application.Current.Resources.Contains(key))
                        {
                            var resource = Application.Current.Resources[key];
                            System.Diagnostics.Debug.WriteLine($"TestCurrentResources: ✓ {key} = '{resource}'");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"TestCurrentResources: ✗ {key} = KEY NOT FOUND");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"TestCurrentResources: ✗ {key} - ERROR: {ex.Message}");
                    }
                }
                
                // Check merged dictionaries with detailed info
                System.Diagnostics.Debug.WriteLine($"TestCurrentResources: Total merged dictionaries: {Application.Current.Resources.MergedDictionaries.Count}");
                
                for (int i = 0; i < Application.Current.Resources.MergedDictionaries.Count; i++)
                {
                    var dict = Application.Current.Resources.MergedDictionaries[i];
                    System.Diagnostics.Debug.WriteLine($"TestCurrentResources: Dictionary {i}:");
                    System.Diagnostics.Debug.WriteLine($"  - Source: '{dict.Source}'");
                    System.Diagnostics.Debug.WriteLine($"  - Count: {dict.Count} resources");
                    
                    // Sample a few keys from this dictionary
                    var sampleKeys = new[] { "English", "Albanian", "Dashboard" };
                    foreach (var sampleKey in sampleKeys)
                    {
                        if (dict.Contains(sampleKey))
                        {
                            System.Diagnostics.Debug.WriteLine($"  - Sample: {sampleKey} = '{dict[sampleKey]}'");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("TestCurrentResources: === END RESOURCE STATUS ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TestCurrentResources: Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Nuclear option: Force complete resource reload by restarting the app with new language
        /// </summary>
        public void ForceLanguageRestart(string languageCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ForceLanguageRestart: Nuclear option - restarting with language '{languageCode}'");
                
                // Save the language setting
                var app = Application.Current as App;
                var settingsService = app?.GetService<ISettingsService>();
                if (settingsService != null)
                {
                    settingsService.Language = languageCode;
                    _ = settingsService.SaveSettingsAsync();
                }
                
                // Show message and restart
                MessageBox.Show("Language will change after restart. The application will now restart.", 
                    "Language Change", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Restart the application
                System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ForceLanguageRestart: Error: {ex.Message}");
                MessageBox.Show($"Failed to restart application: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Alternative nuclear option: Manually inject Albanian resources without using LocalizationService
        /// </summary>
        public void DirectInjectAlbanianResources()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DirectInjectAlbanianResources: Bypassing LocalizationService completely");
                
                // Create Albanian resource dictionary directly
                var albanianUri = new Uri("Resources/Strings.sq.xaml", UriKind.Relative);
                var albanianDict = new ResourceDictionary { Source = albanianUri };
                
                // Remove ALL existing string dictionaries
                var toRemove = Application.Current.Resources.MergedDictionaries
                    .Where(d => d.Source != null && d.Source.ToString().Contains("Strings."))
                    .ToList();
                
                foreach (var dict in toRemove)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(dict);
                    System.Diagnostics.Debug.WriteLine($"DirectInjectAlbanianResources: Removed {dict.Source}");
                }
                
                // Add Albanian dictionary
                Application.Current.Resources.MergedDictionaries.Insert(0, albanianDict);
                System.Diagnostics.Debug.WriteLine("DirectInjectAlbanianResources: Added Albanian dictionary at index 0");
                
                // Force super-aggressive refresh
                foreach (Window window in Application.Current.Windows)
                {
                    window.InvalidateVisual();
                    window.UpdateLayout();
                    
                    // Force complete re-render
                    window.Visibility = Visibility.Hidden;
                    window.Visibility = Visibility.Visible;
                }
                
                System.Diagnostics.Debug.WriteLine("DirectInjectAlbanianResources: Completed direct injection");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DirectInjectAlbanianResources: Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test method to directly load Albanian resources and catch any parsing errors
        /// </summary>
        public void TestAlbanianResourceLoading()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TestAlbanianResourceLoading: Starting direct Albanian resource test");
                
                // Try to create Albanian ResourceDictionary directly
                var albanianUri = new Uri("Resources/Strings.sq.xaml", UriKind.Relative);
                System.Diagnostics.Debug.WriteLine($"TestAlbanianResourceLoading: Created URI: {albanianUri}");
                
                var albanianDict = new ResourceDictionary();
                System.Diagnostics.Debug.WriteLine("TestAlbanianResourceLoading: Created empty ResourceDictionary");
                
                // This is where the error likely occurs
                albanianDict.Source = albanianUri;
                System.Diagnostics.Debug.WriteLine($"TestAlbanianResourceLoading: Successfully loaded Albanian dictionary with {albanianDict.Count} resources");
                
                // Test key access
                if (albanianDict.Contains("AppTitle"))
                {
                    var appTitle = albanianDict["AppTitle"];
                    System.Diagnostics.Debug.WriteLine($"TestAlbanianResourceLoading: AppTitle = '{appTitle}'");
                }
                
                if (albanianDict.Contains("Albanian"))
                {
                    var albanian = albanianDict["Albanian"];
                    System.Diagnostics.Debug.WriteLine($"TestAlbanianResourceLoading: Albanian = '{albanian}'");
                }
                
                System.Diagnostics.Debug.WriteLine("TestAlbanianResourceLoading: ✅ SUCCESS - Albanian resources loaded without error");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TestAlbanianResourceLoading: ❌ ERROR - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"TestAlbanianResourceLoading: Stack Trace: {ex.StackTrace}");
                
                // Check for inner exception
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"TestAlbanianResourceLoading: Inner Exception - {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
            }
        }

        #endregion

        #region Budget Settings Event Handlers

        private void BankStatementDayTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numeric input
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            // Check if the resulting value would be valid (1-31)
            if (sender is TextBox textBox)
            {
                var currentText = textBox.Text;
                var resultText = currentText.Insert(textBox.SelectionStart, e.Text);
                
                if (int.TryParse(resultText, out int value))
                {
                    if (value < 1 || value > 31)
                    {
                        e.Handled = true;
                    }
                }
                else if (resultText.Length > 2)
                {
                    e.Handled = true;
                }
            }
        }

        private async void BankStatementDayTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            try
            {
                var app = Application.Current as App;
                var settingsService = app?.GetService<ISettingsService>();
                
                if (settingsService == null) return;

                // Validate and parse the input
                if (int.TryParse(textBox.Text, out int day) && day >= 1 && day <= 31)
                {
                    // Update the setting
                    settingsService.BankStatementDay = day;
                    await settingsService.SaveSettingsAsync();

                    // Refresh the bank statement summary if MainViewModel is available
                    if (DataContext is MainViewModel viewModel)
                    {
                        viewModel.BankStatementSummary = await app.GetRequiredService<IBudgetService>()
                            .GetBankStatementSummaryAsync(day);
                    }
                }
                else
                {
                    // Reset to current setting if invalid input
                    textBox.Text = settingsService.BankStatementDay.ToString();
                }
            }
            catch (Exception ex)
            {
                // Reset to default on error
                textBox.Text = "17";
                System.Diagnostics.Debug.WriteLine($"Error updating bank statement day: {ex.Message}");
            }
        }

        #endregion

        #region Modern Navigation

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            // Add button press animation feedback
            AnimateButtonPress(button);

            // Update navigation selection
            UpdateNavigationSelection(button.Name);

            // Switch content based on button name
            switch (button.Name)
            {
                case "DashboardButton":
                    ShowDashboardContent();
                    break;
                case "AnalyticsButton":
                    ShowAnalyticsContent();
                    break;
                case "SearchIncomeButton":
                    ShowSearchIncomeContent();
                    break;
                case "SearchSpendingButton":
                    ShowSearchSpendingContent();
                    break;
                case "ExportButton":
                    ShowExportContent();
                    break;
                case "SettingsButton":
                    ShowSettingsContent();
                    break;
            }
        }

        private void UpdateNavigationSelection(string? selectedButtonName)
        {
            // Reset all navigation buttons to normal style
            var navigationButtons = new[] 
            { 
                DashboardButton, 
                AnalyticsButton, 
                SearchIncomeButton,
                SearchSpendingButton,
                ExportButton, 
                SettingsButton 
            };

            foreach (var navButton in navigationButtons)
            {
                navButton.Style = (Style)FindResource("NavigationItemStyle");
            }

            // Apply selected style to active button
            var selectedButton = selectedButtonName switch
            {
                "DashboardButton" => DashboardButton,
                "AnalyticsButton" => AnalyticsButton,
                "SearchIncomeButton" => SearchIncomeButton,
                "SearchSpendingButton" => SearchSpendingButton,
                "ExportButton" => ExportButton,
                "SettingsButton" => SettingsButton,
                _ => DashboardButton
            };

            selectedButton.Style = (Style)FindResource("SelectedNavigationItemStyle");
        }

        private void ShowDashboardContent()
        {
            PageTitle.Text = "Dashboard";
            PageSubtitle.Text = "Overview of your budget and recent activity";

            // Show dashboard content, hide others
            DashboardContent.Visibility = Visibility.Visible;
            AnalyticsContent.Visibility = Visibility.Collapsed;
            ExportContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;
            SearchIncomeContent.Visibility = Visibility.Collapsed;
            SearchSpendingContent.Visibility = Visibility.Collapsed;

            // Animate content appearance
            AnimateContentTransition(DashboardContent);
        }

        private void ShowAnalyticsContent()
        {
            PageTitle.Text = "Analytics";
            PageSubtitle.Text = "Detailed insights into your spending patterns";

            // Show analytics content, hide others
            DashboardContent.Visibility = Visibility.Collapsed;
            AnalyticsContent.Visibility = Visibility.Visible;
            ExportContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;
            SearchIncomeContent.Visibility = Visibility.Collapsed;
            SearchSpendingContent.Visibility = Visibility.Collapsed;

            // Animate content appearance
            AnimateContentTransition(AnalyticsContent);
        }

        private void ShowExportContent()
        {
            PageTitle.Text = "Export & Tools";
            PageSubtitle.Text = "Export your data and manage categories";

            // Show export content, hide others
            DashboardContent.Visibility = Visibility.Collapsed;
            AnalyticsContent.Visibility = Visibility.Collapsed;
            ExportContent.Visibility = Visibility.Visible;
            SettingsContent.Visibility = Visibility.Collapsed;
            SearchIncomeContent.Visibility = Visibility.Collapsed;
            SearchSpendingContent.Visibility = Visibility.Collapsed;

            // Animate content appearance
            AnimateContentTransition(ExportContent);
        }

        private void ShowSettingsContent()
        {
            PageTitle.Text = "Settings";
            PageSubtitle.Text = "Customize your application preferences";

            // Show settings content, hide others
            DashboardContent.Visibility = Visibility.Collapsed;
            AnalyticsContent.Visibility = Visibility.Collapsed;
            ExportContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Visible;
            SearchIncomeContent.Visibility = Visibility.Collapsed;
            SearchSpendingContent.Visibility = Visibility.Collapsed;

            // Animate content appearance
            AnimateContentTransition(SettingsContent);
        }

        private void ShowSearchIncomeContent()
        {
            PageTitle.Text = "Search Income";
            PageSubtitle.Text = "Find and filter your income entries";

            // Show search income content, hide others
            DashboardContent.Visibility = Visibility.Collapsed;
            AnalyticsContent.Visibility = Visibility.Collapsed;
            ExportContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;
            SearchIncomeContent.Visibility = Visibility.Visible;
            SearchSpendingContent.Visibility = Visibility.Collapsed;

            // Animate content appearance
            AnimateContentTransition(SearchIncomeContent);
        }

        private void ShowSearchSpendingContent()
        {
            PageTitle.Text = "Search Spending";
            PageSubtitle.Text = "Find and filter your spending entries";

            // Show search spending content, hide others
            DashboardContent.Visibility = Visibility.Collapsed;
            AnalyticsContent.Visibility = Visibility.Collapsed;
            ExportContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;
            SearchIncomeContent.Visibility = Visibility.Collapsed;
            SearchSpendingContent.Visibility = Visibility.Visible;

            // Animate content appearance
            AnimateContentTransition(SearchSpendingContent);
        }

        private void AnimateContentTransition(FrameworkElement content)
        {
            try
            {
                // Enhanced page transition animation with scale and easing
                var storyboard = (Storyboard)FindResource("EnhancedPageTransitionAnimation");
                if (storyboard != null)
                {
                    // Ensure content has proper render transform
                    if (content.RenderTransform == null || content.RenderTransform is not ScaleTransform)
                    {
                        content.RenderTransform = new System.Windows.Media.ScaleTransform(1, 1);
                        content.RenderTransformOrigin = new Point(0.5, 0.5);
                    }
                    
                    Storyboard.SetTarget(storyboard, content);
                    storyboard.Begin();
                }
            }
            catch
            {
                // Fallback to basic animation if enhanced fails
                try
                {
                    var basicStoryboard = (Storyboard)FindResource("PageTransitionAnimation");
                    if (basicStoryboard != null)
                    {
                        Storyboard.SetTarget(basicStoryboard, content);
                        basicStoryboard.Begin();
                    }
                }
                catch
                {
                    // If all animations fail, continue without animation
                }
            }
        }

        private void AnimateButtonPress(FrameworkElement button)
        {
            try
            {
                var storyboard = (Storyboard)FindResource("ButtonPressAnimation");
                if (storyboard != null)
                {
                    // Ensure button has proper render transform
                    if (button.RenderTransform == null || button.RenderTransform is not System.Windows.Media.ScaleTransform)
                    {
                        button.RenderTransform = new System.Windows.Media.ScaleTransform(1, 1);
                        button.RenderTransformOrigin = new Point(0.5, 0.5);
                    }
                    
                    Storyboard.SetTarget(storyboard, button);
                    storyboard.Begin();
                }
            }
            catch
            {
                // If animation fails, continue without animation
            }
        }

        #endregion
    }
}