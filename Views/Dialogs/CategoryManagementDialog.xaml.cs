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
using BudgetManagement.ViewModels;

namespace BudgetManagement.Views.Dialogs
{
    /// <summary>
    /// ViewModel for Category Management Dialog
    /// </summary>
    public class CategoryManagementViewModel : BaseViewModel
    {
        private ObservableCollection<Category> _categories = new();
        private bool _hasUnsavedChanges = false;
        private bool _isLoading = false;
        private string _statusMessage = "Ready";
        private bool _hasError = false;

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// Public method to trigger property change notifications
        /// </summary>
        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Sets a success status message
        /// </summary>
        public void SetSuccessMessage(string message)
        {
            StatusMessage = message;
            HasError = false;
        }

        /// <summary>
        /// Sets an error status message
        /// </summary>
        public void SetErrorMessage(string message)
        {
            StatusMessage = message;
            HasError = true;
        }
    }

    public partial class CategoryManagementDialog : Window
    {
        private readonly IBudgetService _budgetService;
        private readonly IDialogService _dialogService;
        private readonly CategoryManagementViewModel _viewModel;

        public ObservableCollection<Category> Categories => _viewModel.Categories;
        public bool HasUnsavedChanges => _viewModel.HasUnsavedChanges;

        public CategoryManagementDialog(IBudgetService budgetService, IDialogService dialogService)
        {
            InitializeComponent();
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _viewModel = new CategoryManagementViewModel();
            
            DataContext = _viewModel;
            
            // Load categories after the window is fully loaded
            Loaded += CategoryManagementDialog_Loaded;
        }

        private async void CategoryManagementDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                _viewModel.IsLoading = true;
                _viewModel.SetSuccessMessage("Loading categories...");
                System.Diagnostics.Debug.WriteLine("CategoryManagementDialog: Starting to load categories...");
                
                // Use Dispatcher.Invoke to ensure UI operations happen on the UI thread
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var categories = await _budgetService.GetAllCategoriesAsync(); // Get ALL categories including inactive
                    
                    System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: Retrieved {categories?.Count() ?? 0} categories from database");
                    
                    // Clear and reload categories using proper property notification
                    _viewModel.Categories.Clear();
                    
                    foreach (var category in categories.OrderBy(c => c.DisplayOrder))
                    {
                        _viewModel.Categories.Add(category);
                        System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: Added category - Id: {category.Id}, Name: {category.Name}, Active: {category.IsActive}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: Categories collection now has {_viewModel.Categories.Count} items");
                    
                    // Explicit check of ListBox binding
                    System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: ListBox DataContext: {CategoriesListBox.DataContext?.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: ListBox ItemsSource: {CategoriesListBox.ItemsSource?.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: ListBox Items.Count: {CategoriesListBox.Items.Count}");
                    
                    // Force binding refresh
                    var binding = BindingOperations.GetBinding(CategoriesListBox, ListBox.ItemsSourceProperty);
                    if (binding != null)
                    {
                        BindingOperations.ClearBinding(CategoriesListBox, ListBox.ItemsSourceProperty);
                        BindingOperations.SetBinding(CategoriesListBox, ListBox.ItemsSourceProperty, binding);
                        System.Diagnostics.Debug.WriteLine("CategoryManagementDialog: Refreshed ListBox binding");
                    }
                    
                    // Set success message
                    var count = _viewModel.Categories.Count;
                    if (count == 0)
                    {
                        _viewModel.SetSuccessMessage("No categories found. Add your first category above.");
                    }
                    else
                    {
                        _viewModel.SetSuccessMessage($"Successfully loaded {count} categories.");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CategoryManagementDialog: Exception loading categories: {ex}");
                _viewModel.SetErrorMessage($"Failed to load categories: {ex.Message}");
                await _dialogService.ShowErrorAsync("Load Error", 
                    $"Failed to load categories: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
            finally
            {
                _viewModel.IsLoading = false;
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
            if (_viewModel.Categories.Any(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase)))
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
                    DisplayOrder = _viewModel.Categories.Count + 1,
                    IsActive = true
                };

                var addedCategory = await _budgetService.AddCategoryAsync(newCategory);
                _viewModel.Categories.Add(addedCategory);
                
                NewCategoryNameTextBox.Clear();
                _viewModel.HasUnsavedChanges = true;
                
                _viewModel.SetSuccessMessage($"Category '{categoryName}' added successfully.");
                
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
            if (_viewModel.Categories.Any(c => c.Id != category.Id && 
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
                _viewModel.HasUnsavedChanges = true;
                
                // Refresh the display
                var index = _viewModel.Categories.IndexOf(category);
                _viewModel.Categories.RemoveAt(index);
                _viewModel.Categories.Insert(index, category);
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
                _viewModel.Categories.Remove(category);
                _viewModel.HasUnsavedChanges = true;
                
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
            
            var currentIndex = _viewModel.Categories.IndexOf(category);
            if (currentIndex <= 0) return;

            // Swap with previous item
            var previousCategory = _viewModel.Categories[currentIndex - 1];
            
            (category.DisplayOrder, previousCategory.DisplayOrder) = 
                (previousCategory.DisplayOrder, category.DisplayOrder);

            _viewModel.Categories.Move(currentIndex, currentIndex - 1);
            
            try
            {
                await _budgetService.UpdateCategoryAsync(category);
                await _budgetService.UpdateCategoryAsync(previousCategory);
                _viewModel.HasUnsavedChanges = true;
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
            
            var currentIndex = _viewModel.Categories.IndexOf(category);
            if (currentIndex >= _viewModel.Categories.Count - 1) return;

            // Swap with next item
            var nextCategory = _viewModel.Categories[currentIndex + 1];
            
            (category.DisplayOrder, nextCategory.DisplayOrder) = 
                (nextCategory.DisplayOrder, category.DisplayOrder);

            _viewModel.Categories.Move(currentIndex, currentIndex + 1);
            
            try
            {
                await _budgetService.UpdateCategoryAsync(category);
                await _budgetService.UpdateCategoryAsync(nextCategory);
                _viewModel.HasUnsavedChanges = true;
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
                _viewModel.HasUnsavedChanges = true;
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
            if (_viewModel.HasUnsavedChanges)
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
            for (int i = 0; i < _viewModel.Categories.Count; i++)
            {
                _viewModel.Categories[i].DisplayOrder = i + 1;
                await _budgetService.UpdateCategoryAsync(_viewModel.Categories[i]);
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
}