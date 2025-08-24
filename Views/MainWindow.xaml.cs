using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BudgetManagement.ViewModels;
using BudgetManagement.Services;
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
            InitializeComponent();
            
            // Set up senior-friendly window behavior
            InitializeSeniorFriendlyFeatures();
        }

        public MainWindow(MainViewModel viewModel) : this()
        {
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
            // Initialize the view model when the window loads
            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }

            // Set up language combobox
            SetupLanguageSelector();
            
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
        }

        private void SetupLanguageSelector()
        {
            try
            {
                var app = Application.Current as App;
                var localizationService = app?.GetService<ILocalizationService>();
                var settingsService = app?.GetService<ISettingsService>();

                if (localizationService != null && settingsService != null)
                {
                    // Set current selection based on current language
                    var currentLanguage = localizationService.CurrentLanguage;
                    foreach (ComboBoxItem item in LanguageComboBox.Items)
                    {
                        if (item.Tag?.ToString() == currentLanguage)
                        {
                            LanguageComboBox.SelectedItem = item;
                            break;
                        }
                    }

                    // Handle language changes
                    LanguageComboBox.SelectionChanged += async (s, e) =>
                    {
                        if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem &&
                            selectedItem.Tag?.ToString() is string languageCode)
                        {
                            localizationService.SetLanguage(languageCode);
                            settingsService.Language = languageCode;
                            await settingsService.SaveSettingsAsync();
                        }
                    };
                }
            }
            catch
            {
                // Ignore errors in language setup
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
            });
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

            // Animate content appearance
            AnimateContentTransition(SettingsContent);
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