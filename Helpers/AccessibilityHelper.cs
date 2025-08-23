using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BudgetManagement.Helpers
{
    /// <summary>
    /// Helper class for implementing senior-friendly accessibility features
    /// </summary>
    public static class AccessibilityHelper
    {
        /// <summary>
        /// Sets up accessibility properties for a control to be senior-friendly
        /// </summary>
        /// <param name="element">The UI element to enhance</param>
        /// <param name="name">Accessible name for screen readers</param>
        /// <param name="helpText">Help text for the control</param>
        /// <param name="role">The automation role of the control</param>
        public static void SetAccessibilityProperties(FrameworkElement element, string name, string helpText = "", ControlType? role = null)
        {
            if (element == null) return;

            // Set automation properties for screen readers
            AutomationProperties.SetName(element, name);
            
            if (!string.IsNullOrEmpty(helpText))
            {
                AutomationProperties.SetHelpText(element, helpText);
            }

            AutomationProperties.SetAutomationId(element, GenerateAutomationId(name));
            
            // Note: AutomationProperties.SetControlType doesn't exist in WPF
            // Control types are inferred automatically by WPF

            // Ensure the element is accessible
            AutomationProperties.SetIsRequiredForForm(element, false);
        }

        /// <summary>
        /// Enhances button accessibility for seniors
        /// </summary>
        /// <param name="button">The button to enhance</param>
        /// <param name="description">Description of what the button does</param>
        public static void EnhanceButtonAccessibility(Button button, string description)
        {
            if (button == null) return;

            SetAccessibilityProperties(button, button.Content?.ToString() ?? "Button", description, ControlType.Button);
            
            // Add keyboard support
            button.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Space)
                {
                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                }
            };

            // Add focus visual enhancement
            button.GotFocus += (sender, e) =>
            {
                button.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                button.BorderThickness = new Thickness(3);
            };

            button.LostFocus += (sender, e) =>
            {
                button.BorderBrush = new SolidColorBrush(Colors.Gray);
                button.BorderThickness = new Thickness(1);
            };
        }

        /// <summary>
        /// Enhances text input accessibility for seniors
        /// </summary>
        /// <param name="textBox">The text box to enhance</param>
        /// <param name="label">Associated label text</param>
        /// <param name="isRequired">Whether the field is required</param>
        public static void EnhanceTextBoxAccessibility(TextBox textBox, string label, bool isRequired = false)
        {
            if (textBox == null) return;

            string accessibleName = isRequired ? $"{label} (Required)" : label;
            SetAccessibilityProperties(textBox, accessibleName, $"Enter {label.ToLower()}", ControlType.Edit);
            
            AutomationProperties.SetIsRequiredForForm(textBox, isRequired);

            // Add validation feedback for seniors
            textBox.GotFocus += (sender, e) =>
            {
                textBox.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                textBox.BorderThickness = new Thickness(2);
            };

            textBox.LostFocus += (sender, e) =>
            {
                if (isRequired && string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    textBox.BorderThickness = new Thickness(2);
                    textBox.ToolTip = $"{label} is required";
                }
                else
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Gray);
                    textBox.BorderThickness = new Thickness(1);
                    textBox.ToolTip = null;
                }
            };
        }

        /// <summary>
        /// Enhances ComboBox accessibility for seniors
        /// </summary>
        /// <param name="comboBox">The combo box to enhance</param>
        /// <param name="label">Associated label text</param>
        /// <param name="isRequired">Whether the field is required</param>
        public static void EnhanceComboBoxAccessibility(ComboBox comboBox, string label, bool isRequired = false)
        {
            if (comboBox == null) return;

            string accessibleName = isRequired ? $"{label} (Required)" : label;
            SetAccessibilityProperties(comboBox, accessibleName, $"Select {label.ToLower()}", ControlType.ComboBox);
            
            AutomationProperties.SetIsRequiredForForm(comboBox, isRequired);

            // Enhanced keyboard navigation for seniors
            comboBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.F4 || (e.Key == Key.Down && e.KeyboardDevice.Modifiers == ModifierKeys.Alt))
                {
                    comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                    e.Handled = true;
                }
            };

            // Visual feedback
            comboBox.GotFocus += (sender, e) =>
            {
                comboBox.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                comboBox.BorderThickness = new Thickness(2);
            };

            comboBox.LostFocus += (sender, e) =>
            {
                if (isRequired && comboBox.SelectedItem == null)
                {
                    comboBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    comboBox.BorderThickness = new Thickness(2);
                    comboBox.ToolTip = $"{label} is required";
                }
                else
                {
                    comboBox.BorderBrush = new SolidColorBrush(Colors.Gray);
                    comboBox.BorderThickness = new Thickness(1);
                    comboBox.ToolTip = null;
                }
            };
        }

        /// <summary>
        /// Enhances DataGrid accessibility for seniors
        /// </summary>
        /// <param name="dataGrid">The data grid to enhance</param>
        /// <param name="description">Description of the grid content</param>
        public static void EnhanceDataGridAccessibility(DataGrid dataGrid, string description)
        {
            if (dataGrid == null) return;

            SetAccessibilityProperties(dataGrid, "Data Grid", description, ControlType.DataGrid);

            // Enhanced keyboard navigation
            dataGrid.KeyDown += (sender, e) =>
            {
                switch (e.Key)
                {
                    case Key.Home:
                        if (dataGrid.Items.Count > 0)
                        {
                            dataGrid.SelectedIndex = 0;
                            dataGrid.ScrollIntoView(dataGrid.Items[0]);
                            e.Handled = true;
                        }
                        break;
                    case Key.End:
                        if (dataGrid.Items.Count > 0)
                        {
                            dataGrid.SelectedIndex = dataGrid.Items.Count - 1;
                            dataGrid.ScrollIntoView(dataGrid.Items[dataGrid.Items.Count - 1]);
                            e.Handled = true;
                        }
                        break;
                    case Key.PageUp:
                        ScrollDataGrid(dataGrid, -10);
                        e.Handled = true;
                        break;
                    case Key.PageDown:
                        ScrollDataGrid(dataGrid, 10);
                        e.Handled = true;
                        break;
                }
            };
        }

        /// <summary>
        /// Creates keyboard navigation between controls for seniors
        /// </summary>
        /// <param name="controls">Array of controls in tab order</param>
        public static void SetupKeyboardNavigation(params FrameworkElement[] controls)
        {
            if (controls == null || controls.Length == 0) return;

            for (int i = 0; i < controls.Length; i++)
            {
                var control = controls[i];
                if (control == null) continue;

                if (control is Control wpfControl)
                {
                    wpfControl.TabIndex = i;
                    wpfControl.IsTabStop = true;
                }

                // Enhanced Enter key handling for seniors
                control.KeyDown += (sender, e) =>
                {
                    if (e.Key == Key.Enter && !(sender is Button))
                    {
                        // Move to next control
                        var currentIndex = Array.IndexOf(controls, sender);
                        if (currentIndex >= 0 && currentIndex < controls.Length - 1)
                        {
                            var nextControl = controls[currentIndex + 1];
                            nextControl?.Focus();
                            e.Handled = true;
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Adds visual and audio feedback for senior users
        /// </summary>
        /// <param name="element">Element to add feedback to</param>
        /// <param name="feedbackType">Type of feedback to provide</param>
        public static void AddSeniorFeedback(FrameworkElement element, SeniorFeedbackType feedbackType)
        {
            if (element == null) return;

            switch (feedbackType)
            {
                case SeniorFeedbackType.ButtonClick:
                    if (element is Button button)
                    {
                        button.Click += (sender, e) =>
                        {
                            // Visual feedback
                            var originalBackground = button.Background;
                            button.Background = new SolidColorBrush(Colors.LightGreen);
                            
                            // Reset after short delay
                            var timer = new System.Windows.Threading.DispatcherTimer
                            {
                                Interval = TimeSpan.FromMilliseconds(150)
                            };
                            timer.Tick += (s, args) =>
                            {
                                button.Background = originalBackground;
                                timer.Stop();
                            };
                            timer.Start();

                            // Audio feedback (system sound)
                            SystemSounds.Beep();
                        };
                    }
                    break;

                case SeniorFeedbackType.ValidationError:
                    element.Loaded += (sender, e) =>
                    {
                        // Add visual validation error feedback
                        if (element is Control control)
                        {
                            control.BorderBrush = new SolidColorBrush(Colors.Red);
                            control.BorderThickness = new Thickness(2);
                        }
                        
                        // Audio feedback for error
                        SystemSounds.Hand();
                    };
                    break;

                case SeniorFeedbackType.SuccessAction:
                    element.Loaded += (sender, e) =>
                    {
                        // Visual success feedback
                        if (element is Control control)
                        {
                            control.BorderBrush = new SolidColorBrush(Colors.Green);
                            control.BorderThickness = new Thickness(2);
                        }
                        
                        // Audio feedback for success
                        SystemSounds.Asterisk();
                    };
                    break;
            }
        }

        private static void ScrollDataGrid(DataGrid dataGrid, int itemsToScroll)
        {
            if (dataGrid.Items.Count == 0) return;

            int currentIndex = dataGrid.SelectedIndex;
            int newIndex = Math.Max(0, Math.Min(dataGrid.Items.Count - 1, currentIndex + itemsToScroll));
            
            dataGrid.SelectedIndex = newIndex;
            dataGrid.ScrollIntoView(dataGrid.Items[newIndex]);
        }

        private static string GenerateAutomationId(string name)
        {
            return name.Replace(" ", "").Replace("(", "").Replace(")", "") + "AutoId";
        }
    }

    /// <summary>
    /// Types of feedback that can be provided to senior users
    /// </summary>
    public enum SeniorFeedbackType
    {
        ButtonClick,
        ValidationError,
        SuccessAction,
        FocusChange,
        DataChange
    }

    /// <summary>
    /// Static class for system sounds to provide audio feedback
    /// </summary>
    public static class SystemSounds
    {
        public static void Beep()
        {
            try
            {
                System.Media.SystemSounds.Beep.Play();
            }
            catch
            {
                // Ignore if sound can't be played
            }
        }

        public static void Asterisk()
        {
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch
            {
                // Ignore if sound can't be played
            }
        }

        public static void Hand()
        {
            try
            {
                System.Media.SystemSounds.Hand.Play();
            }
            catch
            {
                // Ignore if sound can't be played
            }
        }

        public static void Question()
        {
            try
            {
                System.Media.SystemSounds.Question.Play();
            }
            catch
            {
                // Ignore if sound can't be played
            }
        }
    }
}