using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BudgetManagement.Models;
using BudgetManagement.Services;

namespace BudgetManagement.Views.Dialogs
{
    public partial class CategoryManagementDialog : Window
    {
        private readonly IBudgetService _budgetService;
        private readonly IDialogService _dialogService;
        public ObservableCollection<Category> Categories { get; set; } = new();
        private bool _hasUnsavedChanges = false;

        public CategoryManagementDialog(IBudgetService budgetService, IDialogService dialogService)
        {
            InitializeComponent();
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            
            DataContext = this;
            LoadCategoriesAsync();
        }

        private async void LoadCategoriesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CategoryManagementDialog: Starting to load categories...");
                
                var categories = await _budgetService.GetAllCategoriesAsync(); // Get ALL categories including inactive
                
                System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: Retrieved {categories?.Count() ?? 0} categories from database");
                
                Categories.Clear();
                
                foreach (var category in categories.OrderBy(c => c.DisplayOrder))
                {
                    Categories.Add(category);
                    System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: Added category - Id: {category.Id}, Name: {category.Name}, Active: {category.IsActive}");
                }
                
                System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: Categories collection now has {Categories.Count} items");
                
                // Force UI update
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() => 
                {
                    System.Diagnostics.Debug.WriteLine("CategoryManagementDialog: UI thread update triggered");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: Exception loading categories: {ex}");
                await _dialogService.ShowErrorAsync("Load Error", 
                    $"Failed to load categories: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }

        private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var categoryName = NewCategoryNameTextBox.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                await _dialogService.ShowWarningAsync("Validation Error", 
                    "Please enter a category name.");
                NewCategoryNameTextBox.Focus();
                return;
            }

            if (categoryName.Length > 50)
            {
                await _dialogService.ShowWarningAsync("Validation Error", 
                    "Category name cannot exceed 50 characters.");
                NewCategoryNameTextBox.Focus();
                return;
            }

            // Check for duplicate names
            if (Categories.Any(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase)))
            {
                await _dialogService.ShowWarningAsync("Duplicate Category", 
                    "A category with this name already exists.");
                NewCategoryNameTextBox.Focus();
                return;
            }

            try
            {
                var newCategory = new Category
                {
                    Name = categoryName,
                    DisplayOrder = Categories.Count + 1,
                    IsActive = true
                };

                var addedCategory = await _budgetService.AddCategoryAsync(newCategory);
                Categories.Add(addedCategory);
                
                NewCategoryNameTextBox.Clear();
                _hasUnsavedChanges = true;
                
                await _dialogService.ShowInformationAsync("Success", 
                    $"Category '{categoryName}' has been added successfully.");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Add Error", 
                    $"Failed to add category: {ex.Message}");
            }
        }

        private void NewCategoryNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddCategoryButton_Click(sender, e);
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).CommandParameter is not Category category) return;

            var result = await _dialogService.ShowInputDialogAsync(
                "Edit Category", 
                "Enter new category name:", 
                category.Name);

            if (string.IsNullOrWhiteSpace(result)) return;

            if (result.Length > 50)
            {
                await _dialogService.ShowWarningAsync("Validation Error", 
                    "Category name cannot exceed 50 characters.");
                return;
            }

            // Check for duplicates (excluding current category)
            if (Categories.Any(c => c.Id != category.Id && 
                             string.Equals(c.Name, result, StringComparison.OrdinalIgnoreCase)))
            {
                await _dialogService.ShowWarningAsync("Duplicate Category", 
                    "A category with this name already exists.");
                return;
            }

            try
            {
                category.Name = result;
                await _budgetService.UpdateCategoryAsync(category);
                _hasUnsavedChanges = true;
                
                // Refresh the display
                var index = Categories.IndexOf(category);
                Categories.RemoveAt(index);
                Categories.Insert(index, category);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Update Error", 
                    $"Failed to update category: {ex.Message}");
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).CommandParameter is not Category category) return;

            // Check if category is being used
            var isInUse = await IsCategoryInUseAsync(category.Id);
            
            string message;
            if (isInUse)
            {
                message = $"The category '{category.Name}' is currently being used by existing spending entries. " +
                         "Deleting it will affect those entries.\n\n" +
                         "Are you sure you want to delete this category?";
            }
            else
            {
                message = $"Are you sure you want to delete the category '{category.Name}'?";
            }

            var confirmed = await _dialogService.ShowConfirmationAsync("Delete Category", message);
            if (!confirmed) return;

            try
            {
                await _budgetService.DeleteCategoryAsync(category.Id);
                Categories.Remove(category);
                _hasUnsavedChanges = true;
                
                // Reorder remaining categories
                await ReorderCategoriesAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Delete Error", 
                    $"Failed to delete category: {ex.Message}");
            }
        }

        private async void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).CommandParameter is not Category category) return;
            
            var currentIndex = Categories.IndexOf(category);
            if (currentIndex <= 0) return;

            // Swap with previous item
            var previousCategory = Categories[currentIndex - 1];
            
            (category.DisplayOrder, previousCategory.DisplayOrder) = 
                (previousCategory.DisplayOrder, category.DisplayOrder);

            Categories.Move(currentIndex, currentIndex - 1);
            
            try
            {
                await _budgetService.UpdateCategoryAsync(category);
                await _budgetService.UpdateCategoryAsync(previousCategory);
                _hasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Reorder Error", 
                    $"Failed to reorder categories: {ex.Message}");
                // Reload to restore original order
                LoadCategoriesAsync();
            }
        }

        private async void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).CommandParameter is not Category category) return;
            
            var currentIndex = Categories.IndexOf(category);
            if (currentIndex >= Categories.Count - 1) return;

            // Swap with next item
            var nextCategory = Categories[currentIndex + 1];
            
            (category.DisplayOrder, nextCategory.DisplayOrder) = 
                (nextCategory.DisplayOrder, category.DisplayOrder);

            Categories.Move(currentIndex, currentIndex + 1);
            
            try
            {
                await _budgetService.UpdateCategoryAsync(category);
                await _budgetService.UpdateCategoryAsync(nextCategory);
                _hasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Reorder Error", 
                    $"Failed to reorder categories: {ex.Message}");
                // Reload to restore original order
                LoadCategoriesAsync();
            }
        }

        private async void ResetToDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync("Reset Categories", 
                "This will restore the default categories (Family, Personal, Marini) and may affect " +
                "existing spending entries.\n\n" +
                "Are you sure you want to continue?");
                
            if (!confirmed) return;

            try
            {
                // This is a complex operation - you might want to implement this in the service
                await ResetToDefaultCategoriesAsync();
                LoadCategoriesAsync();
                _hasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Reset Error", 
                    $"Failed to reset categories: {ex.Message}");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var saveChanges = await _dialogService.ShowConfirmationAsync("Unsaved Changes", 
                    "You have unsaved changes. Do you want to save them before closing?");
                    
                if (saveChanges)
                {
                    DialogResult = true;
                    Close();
                    return;
                }
            }
            
            DialogResult = false;
            Close();
        }

        private async Task<bool> IsCategoryInUseAsync(int categoryId)
        {
            try
            {
                // Check if any spending entries use this category
                var spending = await _budgetService.GetSpendingAsync(DateTime.MinValue, DateTime.MaxValue);
                return spending.Any(s => s.CategoryId == categoryId);
            }
            catch
            {
                return true; // Assume it's in use if we can't check
            }
        }

        private async Task ReorderCategoriesAsync()
        {
            for (int i = 0; i < Categories.Count; i++)
            {
                Categories[i].DisplayOrder = i + 1;
                await _budgetService.UpdateCategoryAsync(Categories[i]);
            }
        }

        private async Task ResetToDefaultCategoriesAsync()
        {
            // Get existing categories
            var existingCategories = await _budgetService.GetCategoriesAsync();
            
            // Define default categories
            var defaultCategories = new[]
            {
                new { Name = "Family", Order = 1 },
                new { Name = "Personal", Order = 2 },
                new { Name = "Marini", Order = 3 }
            };

            // Update or create default categories
            foreach (var defaultCat in defaultCategories)
            {
                var existing = existingCategories.FirstOrDefault(c => 
                    string.Equals(c.Name, defaultCat.Name, StringComparison.OrdinalIgnoreCase));
                
                if (existing != null)
                {
                    existing.DisplayOrder = defaultCat.Order;
                    existing.IsActive = true;
                    await _budgetService.UpdateCategoryAsync(existing);
                }
                else
                {
                    var newCategory = new Category
                    {
                        Name = defaultCat.Name,
                        DisplayOrder = defaultCat.Order,
                        IsActive = true
                    };
                    await _budgetService.AddCategoryAsync(newCategory);
                }
            }

            // Deactivate non-default categories instead of deleting them
            var nonDefaultCategories = existingCategories.Where(c => 
                !defaultCategories.Any(d => string.Equals(d.Name, c.Name, StringComparison.OrdinalIgnoreCase)));
                
            foreach (var category in nonDefaultCategories)
            {
                category.IsActive = false;
                category.DisplayOrder = 999; // Move to end
                await _budgetService.UpdateCategoryAsync(category);
            }
        }
    }

    // Converter for displaying category status
    public class BooleanToStatusConverter : IValueConverter
    {
        public static readonly BooleanToStatusConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Active" : "Inactive";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}