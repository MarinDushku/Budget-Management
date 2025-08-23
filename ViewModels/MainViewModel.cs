// Main ViewModel for the Budget Management Application
// File: ViewModels/MainViewModel.cs

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using BudgetManagement.Models;
using BudgetManagement.Services;

namespace BudgetManagement.ViewModels
{
    /// <summary>
    /// Main view model for the budget management application
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly IBudgetService _budgetService;
        private readonly IDialogService _dialogService;

        // Private fields
        private BudgetSummary _budgetSummary = new();
        private DateTime _selectedPeriodStart = DateTime.Now.AddMonths(-1);
        private DateTime _selectedPeriodEnd = DateTime.Now;
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        // Collections
        public ObservableCollection<Income> IncomeEntries { get; } = new();
        public ObservableCollection<SpendingWithCategory> SpendingEntries { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        // Collection views for filtering and sorting
        public ICollectionView IncomeView { get; }
        public ICollectionView SpendingView { get; }

        // Properties
        public BudgetSummary BudgetSummary
        {
            get => _budgetSummary;
            set => SetProperty(ref _budgetSummary, value);
        }

        public DateTime SelectedPeriodStart
        {
            get => _selectedPeriodStart;
            set => SetProperty(ref _selectedPeriodStart, value, async () => await RefreshDataAsync());
        }

        public DateTime SelectedPeriodEnd
        {
            get => _selectedPeriodEnd;
            set => SetProperty(ref _selectedPeriodEnd, value, async () => await RefreshDataAsync());
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

        // Commands
        public ICommand AddIncomeCommand { get; }
        public ICommand AddSpendingCommand { get; }
        public ICommand EditIncomeCommand { get; }
        public ICommand EditSpendingCommand { get; }
        public ICommand DeleteIncomeCommand { get; }
        public ICommand DeleteSpendingCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SetCurrentMonthCommand { get; }
        public ICommand SetCurrentYearCommand { get; }
        public ICommand ExportDataCommand { get; }

        public MainViewModel(IBudgetService budgetService, IDialogService dialogService)
        {
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Initialize collection views
            IncomeView = CollectionViewSource.GetDefaultView(IncomeEntries);
            SpendingView = CollectionViewSource.GetDefaultView(SpendingEntries);

            // Set up sorting (most recent first)
            IncomeView.SortDescriptions.Add(new SortDescription(nameof(Income.Date), ListSortDirection.Descending));
            SpendingView.SortDescriptions.Add(new SortDescription(nameof(SpendingWithCategory.Date), ListSortDirection.Descending));

            // Initialize commands
            AddIncomeCommand = new RelayCommand(async () => await AddIncomeAsync());
            AddSpendingCommand = new RelayCommand(async () => await AddSpendingAsync());
            EditIncomeCommand = new RelayCommand<Income>(async income => await EditIncomeAsync(income), income => income != null);
            EditSpendingCommand = new RelayCommand<SpendingWithCategory>(async spending => await EditSpendingAsync(spending), spending => spending != null);
            DeleteIncomeCommand = new RelayCommand<Income>(async income => await DeleteIncomeAsync(income), income => income != null);
            DeleteSpendingCommand = new RelayCommand<SpendingWithCategory>(async spending => await DeleteSpendingAsync(spending), spending => spending != null);
            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
            SetCurrentMonthCommand = new RelayCommand(SetCurrentMonth);
            SetCurrentYearCommand = new RelayCommand(SetCurrentYear);
            ExportDataCommand = new RelayCommand(async () => await ExportDataAsync());
        }

        /// <summary>
        /// Initializes the view model and loads initial data
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Initializing application...";

                // Load categories first
                var categories = await _budgetService.GetCategoriesAsync();
                Categories.Clear();
                foreach (var category in categories.OrderBy(c => c.DisplayOrder))
                {
                    Categories.Add(category);
                }

                // Load data for the selected period
                await RefreshDataAsync();

                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error initializing: {ex.Message}";
                await _dialogService.ShowErrorAsync("Initialization Error", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Refreshes all data for the current period
        /// </summary>
        private async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading data...";

                // Load income entries
                var incomeEntries = await _budgetService.GetIncomeAsync(SelectedPeriodStart, SelectedPeriodEnd);
                IncomeEntries.Clear();
                foreach (var income in incomeEntries)
                {
                    IncomeEntries.Add(income);
                }

                // Load spending entries
                var spendingEntries = await _budgetService.GetSpendingWithCategoryAsync(SelectedPeriodStart, SelectedPeriodEnd);
                SpendingEntries.Clear();
                foreach (var spending in spendingEntries)
                {
                    SpendingEntries.Add(spending);
                }

                // Calculate budget summary
                BudgetSummary = await _budgetService.GetBudgetSummaryAsync(SelectedPeriodStart, SelectedPeriodEnd);

                StatusMessage = $"Loaded {IncomeEntries.Count} income and {SpendingEntries.Count} spending entries";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
                await _dialogService.ShowErrorAsync("Data Loading Error", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Command implementations
        private async Task AddIncomeAsync()
        {
            try
            {
                // Ensure app is fully loaded before showing dialog
                if (IsLoading)
                {
                    StatusMessage = "Please wait for the application to finish loading...";
                    return;
                }

                // Give UI a moment to settle if just started
                await Task.Delay(100);
                
                var income = await _dialogService.ShowIncomeDialogAsync(new Income { Date = DateTime.Today });
                if (income != null)
                {
                    await _budgetService.AddIncomeAsync(income);
                    await RefreshDataAsync();
                    StatusMessage = "Income entry added successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding income: {ex.Message}";
                await _dialogService.ShowErrorAsync("Add Income Error", ex.Message);
            }
        }

        private async Task AddSpendingAsync()
        {
            try
            {
                // Ensure app is fully loaded before showing dialog
                if (IsLoading)
                {
                    StatusMessage = "Please wait for the application to finish loading...";
                    return;
                }

                // Give UI a moment to settle if just started
                await Task.Delay(100);
                
                var spending = await _dialogService.ShowSpendingDialogAsync(new Spending { Date = DateTime.Today }, Categories.ToList());
                if (spending != null)
                {
                    await _budgetService.AddSpendingAsync(spending);
                    await RefreshDataAsync();
                    StatusMessage = "Spending entry added successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding spending: {ex.Message}";
                await _dialogService.ShowErrorAsync("Add Spending Error", ex.Message);
            }
        }

        private async Task EditIncomeAsync(Income? income)
        {
            if (income == null) return;

            try
            {
                var updatedIncome = await _dialogService.ShowIncomeDialogAsync(income);
                if (updatedIncome != null)
                {
                    await _budgetService.UpdateIncomeAsync(updatedIncome);
                    await RefreshDataAsync();
                    StatusMessage = "Income entry updated successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating income: {ex.Message}";
                await _dialogService.ShowErrorAsync("Edit Income Error", ex.Message);
            }
        }

        private async Task EditSpendingAsync(SpendingWithCategory? spendingWithCategory)
        {
            if (spendingWithCategory == null) return;

            try
            {
                // Convert to Spending model for editing
                var spending = new Spending
                {
                    Id = spendingWithCategory.Id,
                    Date = spendingWithCategory.Date,
                    Amount = spendingWithCategory.Amount,
                    Description = spendingWithCategory.Description,
                    CategoryId = spendingWithCategory.CategoryId
                };

                var updatedSpending = await _dialogService.ShowSpendingDialogAsync(spending, Categories.ToList());
                if (updatedSpending != null)
                {
                    await _budgetService.UpdateSpendingAsync(updatedSpending);
                    await RefreshDataAsync();
                    StatusMessage = "Spending entry updated successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating spending: {ex.Message}";
                await _dialogService.ShowErrorAsync("Edit Spending Error", ex.Message);
            }
        }

        private async Task DeleteIncomeAsync(Income? income)
        {
            if (income == null) return;

            var result = await _dialogService.ShowConfirmationAsync(
                "Delete Income Entry",
                $"Are you sure you want to delete the income entry '{income.Description}' for {income.Amount:C}?");

            if (result)
            {
                try
                {
                    await _budgetService.DeleteIncomeAsync(income.Id);
                    await RefreshDataAsync();
                    StatusMessage = "Income entry deleted successfully";
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Delete Error", ex.Message);
                }
            }
        }

        private async Task DeleteSpendingAsync(SpendingWithCategory? spending)
        {
            if (spending == null) return;

            var result = await _dialogService.ShowConfirmationAsync(
                "Delete Spending Entry",
                $"Are you sure you want to delete the spending entry '{spending.Description}' for {spending.Amount:C}?");

            if (result)
            {
                try
                {
                    await _budgetService.DeleteSpendingAsync(spending.Id);
                    await RefreshDataAsync();
                    StatusMessage = "Spending entry deleted successfully";
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Delete Error", ex.Message);
                }
            }
        }

        private void SetCurrentMonth()
        {
            var now = DateTime.Now;
            SelectedPeriodStart = new DateTime(now.Year, now.Month, 1);
            SelectedPeriodEnd = SelectedPeriodStart.AddMonths(1).AddDays(-1);
        }

        private void SetCurrentYear()
        {
            var now = DateTime.Now;
            SelectedPeriodStart = new DateTime(now.Year, 1, 1);
            SelectedPeriodEnd = new DateTime(now.Year, 12, 31);
        }

        private async Task ExportDataAsync()
        {
            try
            {
                StatusMessage = "Exporting data...";
                await _budgetService.ExportDataAsync(SelectedPeriodStart, SelectedPeriodEnd);
                StatusMessage = "Data exported successfully";
                await _dialogService.ShowInformationAsync("Export Complete", "Data has been exported successfully");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
                await _dialogService.ShowErrorAsync("Export Error", ex.Message);
            }
        }
    }
}