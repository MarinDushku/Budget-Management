using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BudgetManagement.Helpers
{
    /// <summary>
    /// Helper class for senior-friendly navigation and window management
    /// </summary>
    public static class NavigationHelper
    {
        /// <summary>
        /// Sets up senior-friendly window behavior
        /// </summary>
        /// <param name="window">The window to enhance</param>
        public static void SetupSeniorFriendlyWindow(Window window)
        {
            if (window == null) return;

            // Ensure minimum size for readability
            window.MinWidth = Math.Max(window.MinWidth, 800);
            window.MinHeight = Math.Max(window.MinHeight, 600);

            // Set appropriate font size
            if (window.FontSize < 14)
                window.FontSize = 16;

            // Ensure good contrast
            if (window.Background == null)
                window.Background = new SolidColorBrush(Colors.White);

            // Add keyboard shortcuts help
            window.KeyDown += Window_KeyDown;

            // Prevent accidental closing
            window.Closing += (sender, e) =>
            {
                var result = MessageBox.Show(
                    "Are you sure you want to close the Budget Management application?",
                    "Confirm Close",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
            };

            // Center on screen
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        /// <summary>
        /// Creates a help overlay showing keyboard shortcuts
        /// </summary>
        /// <param name="parentWindow">The parent window to show help for</param>
        public static void ShowKeyboardHelpOverlay(Window parentWindow)
        {
            var helpWindow = new Window
            {
                Title = "Keyboard Shortcuts - Budget Management",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = parentWindow,
                ResizeMode = ResizeMode.NoResize,
                FontSize = 16
            };

            var helpContent = new ScrollViewer
            {
                Padding = new Thickness(20),
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Keyboard Shortcuts",
                            FontSize = 20,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 0, 20),
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        CreateShortcutItem("F1", "Show this help"),
                        CreateShortcutItem("F5", "Refresh data"),
                        CreateShortcutItem("Ctrl + N", "Add new income"),
                        CreateShortcutItem("Ctrl + E", "Add new expense"),
                        CreateShortcutItem("Ctrl + S", "Save current entry"),
                        CreateShortcutItem("Escape", "Cancel current action"),
                        CreateShortcutItem("Enter", "Move to next field or confirm"),
                        CreateShortcutItem("Tab", "Move to next control"),
                        CreateShortcutItem("Shift + Tab", "Move to previous control"),
                        CreateShortcutItem("Arrow Keys", "Navigate in data grid"),
                        CreateShortcutItem("Page Up/Down", "Scroll data grid"),
                        CreateShortcutItem("Home/End", "Go to first/last item"),
                        CreateShortcutItem("Delete", "Delete selected item (with confirmation)"),
                        new TextBlock
                        {
                            Text = "Tip: All buttons and menus can also be accessed with the mouse.",
                            FontStyle = FontStyles.Italic,
                            Margin = new Thickness(0, 20, 0, 0),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "Close Help",
                            Width = 120,
                            Height = 40,
                            Margin = new Thickness(0, 20, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 16
                        }
                    }
                }
            };

            ((Button)((StackPanel)helpContent.Content).Children[^1]).Click += (s, e) => helpWindow.Close();

            helpWindow.Content = helpContent;
            helpWindow.ShowDialog();
        }

        /// <summary>
        /// Sets up focus management for a container control
        /// </summary>
        /// <param name="container">The container to manage focus for</param>
        public static void SetupFocusManagement(Panel container)
        {
            if (container == null) return;

            container.Loaded += (sender, e) =>
            {
                // Find the first focusable control and set focus
                var firstFocusable = FindFirstFocusableChild(container);
                firstFocusable?.Focus();
            };

            // Handle focus cycling
            container.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.F6)
                {
                    // Cycle through major sections
                    CycleFocusThroughSections(container);
                    e.Handled = true;
                }
            };
        }

        /// <summary>
        /// Creates visual focus indicators that are more visible for seniors
        /// </summary>
        /// <param name="control">The control to add focus indicators to</param>
        public static void AddSeniorFocusIndicators(Control control)
        {
            if (control == null) return;

            control.GotFocus += (sender, e) =>
            {
                control.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 255));
                control.BorderThickness = new Thickness(3);
                
                // Add glow effect
                control.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 120, 255),
                    Direction = 0,
                    ShadowDepth = 0,
                    BlurRadius = 10,
                    Opacity = 0.8
                };
            };

            control.LostFocus += (sender, e) =>
            {
                control.BorderBrush = new SolidColorBrush(Colors.Gray);
                control.BorderThickness = new Thickness(1);
                control.Effect = null;
            };

            // Add hover effects for better visibility
            control.MouseEnter += (sender, e) =>
            {
                if (!control.IsFocused)
                {
                    control.BorderBrush = new SolidColorBrush(Colors.LightBlue);
                    control.BorderThickness = new Thickness(2);
                }
            };

            control.MouseLeave += (sender, e) =>
            {
                if (!control.IsFocused)
                {
                    control.BorderBrush = new SolidColorBrush(Colors.Gray);
                    control.BorderThickness = new Thickness(1);
                }
            };
        }

        /// <summary>
        /// Creates a status announcement for screen readers
        /// </summary>
        /// <param name="message">The message to announce</param>
        /// <param name="parentElement">The parent element to attach the announcement to</param>
        public static void AnnounceToScreenReader(string message, FrameworkElement parentElement)
        {
            if (string.IsNullOrEmpty(message) || parentElement == null) return;

            // Create an invisible text block for screen reader announcements
            var announcement = new TextBlock
            {
                Text = message,
                Visibility = Visibility.Collapsed
            };

            // Add to parent temporarily
            if (parentElement is Panel panel)
            {
                panel.Children.Add(announcement);
                
                // Set up automation properties for announcement
                System.Windows.Automation.AutomationProperties.SetLiveSetting(announcement, 
                    System.Windows.Automation.AutomationLiveSetting.Assertive);
                
                // Remove after a delay
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (s, e) =>
                {
                    panel.Children.Remove(announcement);
                    timer.Stop();
                };
                timer.Start();
            }
        }

        /// <summary>
        /// Sets up breadcrumb navigation for complex forms
        /// </summary>
        /// <param name="container">The container for breadcrumbs</param>
        /// <param name="steps">The steps in the process</param>
        /// <param name="currentStep">The current step index</param>
        public static void SetupBreadcrumbNavigation(Panel container, string[] steps, int currentStep)
        {
            if (container == null || steps == null || steps.Length == 0) return;

            container.Children.Clear();

            for (int i = 0; i < steps.Length; i++)
            {
                var stepButton = new Button
                {
                    Content = $"{i + 1}. {steps[i]}",
                    Height = 40,
                    Margin = new Thickness(5, 0, 5, 0),
                    FontSize = 14,
                    IsEnabled = i <= currentStep
                };

                if (i == currentStep)
                {
                    stepButton.Background = new SolidColorBrush(Colors.DodgerBlue);
                    stepButton.Foreground = new SolidColorBrush(Colors.White);
                    stepButton.FontWeight = FontWeights.Bold;
                }
                else if (i < currentStep)
                {
                    stepButton.Background = new SolidColorBrush(Colors.LightGreen);
                }

                container.Children.Add(stepButton);

                // Add separator if not last item
                if (i < steps.Length - 1)
                {
                    container.Children.Add(new TextBlock
                    {
                        Text = " → ",
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(5, 0, 5, 0)
                    });
                }
            }
        }

        private static void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var window = sender as Window;
            if (window == null) return;

            switch (e.Key)
            {
                case Key.F1:
                    ShowKeyboardHelpOverlay(window);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    // Find and click Cancel button if available
                    var cancelButton = FindControlByName(window, "CancelButton") as Button;
                    cancelButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                    break;
            }
        }

        private static StackPanel CreateShortcutItem(string shortcut, string description)
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5),
                Children =
                {
                    new TextBlock
                    {
                        Text = shortcut,
                        Width = 120,
                        FontWeight = FontWeights.Bold,
                        FontFamily = new FontFamily("Consolas")
                    },
                    new TextBlock
                    {
                        Text = description,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            };
        }

        private static FrameworkElement? FindFirstFocusableChild(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is FrameworkElement element && element.Focusable && element.IsEnabled && element.Visibility == Visibility.Visible)
                {
                    return element;
                }

                var found = FindFirstFocusableChild(child);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void CycleFocusThroughSections(DependencyObject parent)
        {
            // Simple implementation - find next focusable control
            var current = Keyboard.FocusedElement as DependencyObject;
            if (current == null) return;

            var next = FindNextFocusableChild(parent, current);
            if (next is IInputElement inputElement)
            {
                inputElement.Focus();
            }
        }

        private static FrameworkElement? FindNextFocusableChild(DependencyObject parent, DependencyObject current)
        {
            bool foundCurrent = false;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child == current)
                {
                    foundCurrent = true;
                    continue;
                }

                if (foundCurrent && child is FrameworkElement element && element.Focusable && element.IsEnabled && element.Visibility == Visibility.Visible)
                {
                    return element;
                }

                var found = FindNextFocusableChild(child, current);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static FrameworkElement? FindControlByName(DependencyObject parent, string name)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is FrameworkElement element && element.Name == name)
                {
                    return element;
                }

                var found = FindControlByName(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}