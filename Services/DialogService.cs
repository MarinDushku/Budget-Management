using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using BudgetManagement.Models;
using BudgetManagement.Views.Dialogs;

namespace BudgetManagement.Services
{
    /// <summary>
    /// Implementation of dialog service with senior-friendly features
    /// </summary>
    public class DialogService : IDialogService
    {
        private Window? GetActiveWindow()
        {
            try
            {
                // Always prioritize MainWindow for dialog ownership to prevent app closing
                if (Application.Current?.MainWindow != null && 
                    Application.Current.MainWindow.IsLoaded &&
                    Application.Current.MainWindow.IsVisible)
                {
                    System.Diagnostics.Debug.WriteLine($"DialogService: Using MainWindow as owner - Loaded: {Application.Current.MainWindow.IsLoaded}");
                    return Application.Current.MainWindow;
                }
                    
                // Fallback: try any loaded, visible window
                if (Application.Current?.Windows != null && Application.Current.Windows.Count > 0)
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.IsLoaded && window.IsVisible && window.ShowInTaskbar)
                        {
                            System.Diagnostics.Debug.WriteLine($"DialogService: Using fallback window: {window.GetType().Name}");
                            return window;
                        }
                    }
                    
                    // Last resort: return any loaded window
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.IsLoaded)
                        {
                            System.Diagnostics.Debug.WriteLine($"DialogService: Using last resort window: {window.GetType().Name}");
                            return window;
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("DialogService: No suitable owner window found");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DialogService: Error finding owner window: {ex.Message}");
                return null;
            }
        }

        public async Task<Income?> ShowIncomeDialogAsync(Income income)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IncomeDialog? dialog = null;
                try
                {
                    System.Diagnostics.Debug.WriteLine("DialogService: Creating IncomeDialog...");
                    dialog = new IncomeDialog(income);
                    
                    // Get owner window and handle null case
                    var owner = GetActiveWindow();
                    if (owner != null)
                    {
                        dialog.Owner = owner;
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        System.Diagnostics.Debug.WriteLine($"DialogService: Dialog owner set to: {owner.GetType().Name}");
                    }
                    else
                    {
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        System.Diagnostics.Debug.WriteLine("DialogService: No owner found, centering on screen");
                    }

                    // Critical: Ensure dialog doesn't close parent
                    dialog.ShowInTaskbar = false;
                    dialog.Topmost = false;
                    
                    // Prevent dialog close from propagating to main window
                    dialog.Closing += (s, e) => {
                        System.Diagnostics.Debug.WriteLine("DialogService: IncomeDialog closing event");
                    };

                    System.Diagnostics.Debug.WriteLine("DialogService: Showing IncomeDialog...");
                    var result = dialog.ShowDialog();
                    System.Diagnostics.Debug.WriteLine($"DialogService: IncomeDialog result: {result}, Income: {dialog.Income != null}");
                    return result == true ? dialog.Income : null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DialogService: Exception creating/showing IncomeDialog: {ex}");
                    return null;
                }
            });
        }

        public async Task<Spending?> ShowSpendingDialogAsync(Spending spending, List<Category> categories)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("DialogService: Creating SpendingDialog...");
                    var dialog = new SpendingDialog(spending, categories)
                    {
                        Owner = GetActiveWindow()
                    };

                    // Senior-friendly dialog positioning and behavior
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    dialog.ShowInTaskbar = false;
                    dialog.Topmost = false;

                    System.Diagnostics.Debug.WriteLine("DialogService: Showing SpendingDialog...");
                    var result = dialog.ShowDialog();
                    System.Diagnostics.Debug.WriteLine($"DialogService: SpendingDialog result: {result}, Spending: {dialog.Spending != null}");
                    return result == true ? dialog.Spending : null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DialogService: Exception creating/showing SpendingDialog: {ex}");
                    return null;
                }
            });
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(
                        GetActiveWindow(),
                        message,
                        title,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No); // Default to No for safety

                    return result == MessageBoxResult.Yes;
                });
            });
        }

        public async Task ShowErrorAsync(string title, string message)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        GetActiveWindow(),
                        message,
                        title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            });
        }

        public async Task ShowInformationAsync(string title, string message)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        GetActiveWindow(),
                        message,
                        title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
            });
        }

        public async Task ShowWarningAsync(string title, string message)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        GetActiveWindow(),
                        message,
                        title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            });
        }

        public async Task<string?> ShowSaveFileDialogAsync(string defaultFileName, string filter)
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new SaveFileDialog
                    {
                        FileName = defaultFileName,
                        Filter = filter,
                        DefaultExt = GetDefaultExtension(filter),
                        AddExtension = true,
                        CheckPathExists = true,
                        OverwritePrompt = true,
                        Title = "Save Budget Data",
                        InitialDirectory = GetDefaultSaveLocation()
                    };

                    // Senior-friendly: Set larger font and better visibility
                    dialog.CustomPlaces.Add(new FileDialogCustomPlace(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
                    dialog.CustomPlaces.Add(new FileDialogCustomPlace(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));

                    bool? result = dialog.ShowDialog(GetActiveWindow());
                    return result == true ? dialog.FileName : null;
                });
            });
        }

        public async Task<string?> ShowOpenFileDialogAsync(string filter)
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new OpenFileDialog
                    {
                        Filter = filter,
                        Multiselect = false,
                        CheckFileExists = true,
                        CheckPathExists = true,
                        Title = "Open Budget Data",
                        InitialDirectory = GetDefaultSaveLocation()
                    };

                    // Senior-friendly: Add common locations
                    dialog.CustomPlaces.Add(new FileDialogCustomPlace(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
                    dialog.CustomPlaces.Add(new FileDialogCustomPlace(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));

                    bool? result = dialog.ShowDialog(GetActiveWindow());
                    return result == true ? dialog.FileName : null;
                });
            });
        }

        public async Task<string?> ShowFolderBrowserDialogAsync(string description)
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    // For now, fallback to save dialog for folder selection
                    var dialog = new SaveFileDialog
                    {
                        Title = description,
                        FileName = "SelectFolder",
                        DefaultExt = "",
                        Filter = "Folder Selection|*.*",
                        CheckPathExists = false,
                        CheckFileExists = false,
                        CreatePrompt = false
                    };

                    bool? result = dialog.ShowDialog(GetActiveWindow());
                    return result == true ? System.IO.Path.GetDirectoryName(dialog.FileName) : null;
                });
            });
        }

        private string GetDefaultExtension(string filter)
        {
            // Extract the first extension from the filter
            if (string.IsNullOrEmpty(filter)) return ".txt";

            var parts = filter.Split('|');
            if (parts.Length < 2) return ".txt";

            var extensions = parts[1].Split(';');
            if (extensions.Length == 0) return ".txt";

            var firstExt = extensions[0].Trim();
            if (firstExt.StartsWith("*"))
                return firstExt.Substring(1);

            return ".txt";
        }

        private string GetDefaultSaveLocation()
        {
            // Senior-friendly: Default to Documents folder with a Budget subfolder
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string budgetPath = Path.Combine(documentsPath, "Budget Management");

            try
            {
                if (!Directory.Exists(budgetPath))
                {
                    Directory.CreateDirectory(budgetPath);
                }
                return budgetPath;
            }
            catch
            {
                // Fall back to Documents folder if we can't create the subfolder
                return documentsPath;
            }
        }

        /// <summary>
        /// Shows a custom confirmation dialog with larger buttons and text for seniors
        /// </summary>
        public async Task<bool> ShowSeniorFriendlyConfirmationAsync(string title, string message, string yesText = "Yes", string noText = "No")
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    // For future enhancement: create a custom dialog with larger buttons
                    // For now, use the standard MessageBox
                    var result = MessageBox.Show(
                        GetActiveWindow(),
                        message,
                        title,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No);

                    return result == MessageBoxResult.Yes;
                });
            });
        }

        /// <summary>
        /// Shows an input dialog for getting text from the user
        /// </summary>
        public async Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "")
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    // Simple input dialog using a custom window
                    var inputDialog = new Views.Dialogs.SimpleInputDialog(title, message, defaultValue)
                    {
                        Owner = GetActiveWindow()
                    };
                    
                    var result = inputDialog.ShowDialog();
                    return result == true ? inputDialog.InputText : null;
                });
            });
        }

        /// <summary>
        /// Shows a message with multiple options for complex decisions
        /// </summary>
        public async Task<string?> ShowOptionsDialogAsync(string title, string message, params string[] options)
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    // For future enhancement: create a custom options dialog
                    // For now, use a simple approach with multiple MessageBox calls
                    string optionsText = string.Join("\n", options.Select((opt, idx) => $"{idx + 1}. {opt}"));
                    string fullMessage = $"{message}\n\nOptions:\n{optionsText}\n\nClick OK to see individual options, or Cancel to abort.";

                    var result = MessageBox.Show(GetActiveWindow(), fullMessage, title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    
                    if (result != MessageBoxResult.OK)
                        return null;

                    // Show options one by one (senior-friendly approach)
                    foreach (var option in options)
                    {
                        var optionResult = MessageBox.Show(
                            GetActiveWindow(),
                            $"Do you want to: {option}?",
                            title,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (optionResult == MessageBoxResult.Yes)
                            return option;
                    }

                    return null;
                });
            });
        }
    }
}