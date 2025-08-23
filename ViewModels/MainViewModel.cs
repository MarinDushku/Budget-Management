// Main ViewModel for the Budget Management Application
// File: ViewModels/MainViewModel.cs

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        // Recent entries collections (for the activity feed)
        public ObservableCollection<Income> RecentIncomeEntries { get; } = new();
        public ObservableCollection<SpendingWithCategory> RecentSpendingEntries { get; } = new();

        // Budget trend data for analytics
        public ObservableCollection<BudgetManagement.Views.UserControls.WeeklyBudgetData> BudgetTrendData { get; } = new();

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

        public bool HasNoRecentEntries => RecentIncomeEntries.Count == 0 && RecentSpendingEntries.Count == 0;

        public int TotalEntries => IncomeEntries.Count + SpendingEntries.Count;

        public decimal AverageDailySpending
        {
            get
            {
                if (SpendingEntries.Count == 0) return 0m;

                var daysDiff = Math.Max(1, (SelectedPeriodEnd - SelectedPeriodStart).Days + 1);
                return SpendingEntries.Sum(s => s.Amount) / daysDiff;
            }
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

            // DEBUG: Log constructor call
            System.Diagnostics.Debug.WriteLine("MainViewModel constructor called - initializing...");

            // Initialize collection views
            IncomeView = CollectionViewSource.GetDefaultView(IncomeEntries);
            SpendingView = CollectionViewSource.GetDefaultView(SpendingEntries);

            // Set up sorting (most recent first)
            IncomeView.SortDescriptions.Add(new SortDescription(nameof(Income.Date), ListSortDirection.Descending));
            SpendingView.SortDescriptions.Add(new SortDescription(nameof(SpendingWithCategory.Date), ListSortDirection.Descending));

            // Initialize commands - using fire-and-forget pattern for async methods
            AddIncomeCommand = new RelayCommand(() => _ = AddIncomeAsync());
            AddSpendingCommand = new RelayCommand(() => _ = AddSpendingAsync());
            EditIncomeCommand = new RelayCommand<Income>(income => _ = EditIncomeAsync(income), income => income != null);
            EditSpendingCommand = new RelayCommand<SpendingWithCategory>(spending => _ = EditSpendingAsync(spending), spending => spending != null);
            DeleteIncomeCommand = new RelayCommand<Income>(income => _ = DeleteIncomeAsync(income), income => income != null);
            DeleteSpendingCommand = new RelayCommand<SpendingWithCategory>(spending => _ = DeleteSpendingAsync(spending), spending => spending != null);
            RefreshCommand = new RelayCommand(() => _ = RefreshDataAsync());
            SetCurrentMonthCommand = new RelayCommand(SetCurrentMonth);
            SetCurrentYearCommand = new RelayCommand(SetCurrentYear);
            ExportDataCommand = new RelayCommand(() => _ = ExportDataAsync());
            
            // DEBUG: Confirm commands were created
            System.Diagnostics.Debug.WriteLine($"MainViewModel commands created - AddIncomeCommand: {AddIncomeCommand != null}, AddSpendingCommand: {AddSpendingCommand != null}");
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

                // Update recent entries (last 5 of each type)
                UpdateRecentEntries();

                // Update budget trend data (last 10 weeks)
                await UpdateBudgetTrendDataAsync();

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

        /// <summary>
        /// Updates the recent entries collections for the activity feed
        /// </summary>
        private void UpdateRecentEntries()
        {
            // Clear existing recent entries
            RecentIncomeEntries.Clear();
            RecentSpendingEntries.Clear();

            // Get the 5 most recent income entries
            var recentIncome = IncomeEntries
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.CreatedAt)
                .Take(5);

            foreach (var income in recentIncome)
            {
                RecentIncomeEntries.Add(income);
            }

            // Get the 5 most recent spending entries
            var recentSpending = SpendingEntries
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.CreatedAt)
                .Take(5);

            foreach (var spending in recentSpending)
            {
                RecentSpendingEntries.Add(spending);
            }

            // Notify property changes for computed properties
            OnPropertyChanged(nameof(HasNoRecentEntries));
            OnPropertyChanged(nameof(TotalEntries));
            OnPropertyChanged(nameof(AverageDailySpending));
        }

        /// <summary>
        /// Updates the budget trend data for the last 10 weeks
        /// </summary>
        private async Task UpdateBudgetTrendDataAsync()
        {
            try
            {
                BudgetTrendData.Clear();

                // Calculate 10 weeks back from today
                var endDate = DateTime.Now.Date;
                var startDate = endDate.AddDays(-70); // 10 weeks

                // Get data for the entire 10-week period
                var allIncome = await _budgetService.GetIncomeAsync(startDate, endDate);
                var allSpending = await _budgetService.GetSpendingWithCategoryAsync(startDate, endDate);

                // Group by week and calculate weekly totals
                for (int weekOffset = 0; weekOffset < 10; weekOffset++)
                {
                    var weekStart = endDate.AddDays(-(9 - weekOffset) * 7);
                    var weekEnd = weekStart.AddDays(6);

                    var weeklyIncome = allIncome
                        .Where(i => i.Date >= weekStart && i.Date <= weekEnd)
                        .Sum(i => i.Amount);

                    var weeklySpending = allSpending
                        .Where(s => s.Date >= weekStart && s.Date <= weekEnd)
                        .Sum(s => s.Amount);

                    BudgetTrendData.Add(new BudgetManagement.Views.UserControls.WeeklyBudgetData
                    {
                        WeekStartDate = weekStart,
                        TotalIncome = weeklyIncome,
                        TotalSpending = weeklySpending
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating trend data: {ex.Message}";
            }
        }

        // Command implementations
        private async Task AddIncomeAsync()
        {
            try
            {
                // DEBUG: Show that command is being executed
                StatusMessage = "🔄 Add Income command triggered...";
                
                // Ensure app is fully loaded before showing dialog
                if (IsLoading)
                {
                    StatusMessage = "Please wait for the application to finish loading...";
                    return;
                }
                
                StatusMessage = "🔄 Opening Add Income dialog...";
                var income = await _dialogService.ShowIncomeDialogAsync(new Income { Date = DateTime.Today });
                if (income != null)
                {
                    StatusMessage = "🔄 Saving income entry...";
                    await _budgetService.AddIncomeAsync(income);
                    
                    // Optimize: Add the new entry directly instead of full refresh
                    IncomeEntries.Insert(0, income); // Add at top (most recent first)
                    UpdateRecentEntries();
                    
                    // Update summary only
                    BudgetSummary = await _budgetService.GetBudgetSummaryAsync(SelectedPeriodStart, SelectedPeriodEnd);
                    OnPropertyChanged(nameof(AverageDailySpending));
                    OnPropertyChanged(nameof(TotalEntries));
                    
                    StatusMessage = "✅ " + (Application.Current.Resources["IncomeAddedSuccess"]?.ToString() ?? "Income entry added successfully");
                }
                else
                {
                    StatusMessage = "❌ Add Income dialog was canceled or returned null";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error adding income: {ex.Message}";
                await _dialogService.ShowErrorAsync("Add Income Error", ex.Message);
            }
        }

        private async Task AddSpendingAsync()
        {
            try
            {
                // DEBUG: Show that command is being executed
                StatusMessage = "🔄 Add Spending command triggered...";
                
                // Ensure app is fully loaded before showing dialog
                if (IsLoading)
                {
                    StatusMessage = "Please wait for the application to finish loading...";
                    return;
                }
                
                StatusMessage = "🔄 Opening Add Spending dialog...";
                var spending = await _dialogService.ShowSpendingDialogAsync(new Spending { Date = DateTime.Today }, Categories.ToList());
                if (spending != null)
                {
                    StatusMessage = "🔄 Saving spending entry...";
                    await _budgetService.AddSpendingAsync(spending);
                    
                    // Optimize: Convert to SpendingWithCategory and add directly
                    var category = Categories.FirstOrDefault(c => c.Id == spending.CategoryId);
                    var spendingWithCategory = new SpendingWithCategory
                    {
                        Id = spending.Id,
                        Date = spending.Date,
                        Amount = spending.Amount,
                        Description = spending.Description,
                        CategoryId = spending.CategoryId,
                        CategoryName = category?.Name ?? "Unknown",
                        CreatedAt = spending.CreatedAt,
                        UpdatedAt = spending.UpdatedAt
                    };
                    
                    SpendingEntries.Insert(0, spendingWithCategory); // Add at top
                    UpdateRecentEntries();
                    
                    // Update summary and trends
                    BudgetSummary = await _budgetService.GetBudgetSummaryAsync(SelectedPeriodStart, SelectedPeriodEnd);
                    OnPropertyChanged(nameof(AverageDailySpending));
                    OnPropertyChanged(nameof(TotalEntries));
                    
                    StatusMessage = "✅ " + (Application.Current.Resources["SpendingAddedSuccess"]?.ToString() ?? "Spending entry added successfully");
                }
                else
                {
                    StatusMessage = "❌ Add Spending dialog was canceled or returned null";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error adding spending: {ex.Message}";
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