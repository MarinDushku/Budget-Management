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
    /// Enumeration for different time periods
    /// </summary>
    public enum TimePeriod
    {
        ThisWeek,
        TwoWeeks,
        ThisMonth,
        ThreeMonths,
        SixMonths,
        OneYear,
        FiveYears,
        AllTime
    }

    /// <summary>
    /// Main view model for the budget management application
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly IBudgetService _budgetService;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;

        // Private fields
        private BudgetSummary _budgetSummary = new();
        private BankStatementSummary _bankStatementSummary = new();
        private BudgetHealthMetrics _heroMetrics = new();
        private DateTime _selectedPeriodStart = DateTime.Now.AddYears(-10); // Default to far past for "All Time"
        private DateTime _selectedPeriodEnd = DateTime.Now;
        private TimePeriod _selectedTimePeriod = TimePeriod.AllTime;
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        // Collections
        public ObservableCollection<Income> IncomeEntries { get; } = new();
        public ObservableCollection<SpendingWithCategory> SpendingEntries { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        // Recent entries collections (for the activity feed)
        public ObservableCollection<Income> RecentIncomeEntries { get; } = new();
        public ObservableCollection<SpendingWithCategory> RecentSpendingEntries { get; } = new();

        // Budget trend data for analytics (legacy - keeping for compatibility)
        public ObservableCollection<WeeklyBudgetData> BudgetTrendData { get; } = new();
        
        // Daily budget balance data for tracking budget changes over time (legacy - keeping for compatibility)
        public ObservableCollection<DailyBudgetBalance> BudgetBalanceData { get; } = new();
        
        // Simplified analytics data (legacy - keeping for compatibility)
        public ObservableCollection<WeeklySpendingPattern> WeeklyPatterns { get; } = new();
        public ObservableCollection<BudgetInsight> BudgetInsights { get; } = new();

        // New actionable analytics data
        public ObservableCollection<MonthlyComparison> MonthlyComparisons { get; } = new();
        public ObservableCollection<CategoryTrend> CategoryTrends { get; } = new();
        public ObservableCollection<CategoryInsight> CategoryInsights { get; } = new();

        // Quick stats and other new analytics
        private QuickStats _quickStats = new();
        private SpendingVelocity _spendingVelocity = new();
        private BudgetPerformanceScore _budgetPerformanceScore = new();

        // Collection views for filtering and sorting
        public ICollectionView IncomeView { get; }
        public ICollectionView SpendingView { get; }

        // Properties
        public BudgetSummary BudgetSummary
        {
            get => _budgetSummary;
            set => SetProperty(ref _budgetSummary, value);
        }

        public BankStatementSummary BankStatementSummary
        {
            get => _bankStatementSummary;
            set => SetProperty(ref _bankStatementSummary, value);
        }

        public BudgetHealthMetrics HeroMetrics
        {
            get => _heroMetrics;
            set => SetProperty(ref _heroMetrics, value);
        }

        public QuickStats QuickStats
        {
            get => _quickStats;
            set => SetProperty(ref _quickStats, value);
        }

        public SpendingVelocity SpendingVelocity
        {
            get => _spendingVelocity;
            set => SetProperty(ref _spendingVelocity, value);
        }

        public BudgetPerformanceScore BudgetPerformanceScore
        {
            get => _budgetPerformanceScore;
            set => SetProperty(ref _budgetPerformanceScore, value);
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

        public TimePeriod SelectedTimePeriod
        {
            get => _selectedTimePeriod;
            set => SetProperty(ref _selectedTimePeriod, value, async () => await SetTimePeriodAsync(value));
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
        public ICommand ManageCategoriesCommand { get; }

        // Search ViewModels
        public BudgetManagement.Features.Income.ViewModels.IncomeSearchViewModel? IncomeSearchViewModel { get; private set; }
        public BudgetManagement.Features.Spending.ViewModels.SpendingSearchViewModel? SpendingSearchViewModel { get; private set; }

        public MainViewModel(
            IBudgetService budgetService, 
            IDialogService dialogService, 
            ISettingsService settingsService,
            BudgetManagement.Features.Income.ViewModels.IncomeSearchViewModel incomeSearchViewModel,
            BudgetManagement.Features.Spending.ViewModels.SpendingSearchViewModel spendingSearchViewModel)
        {
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            IncomeSearchViewModel = incomeSearchViewModel ?? throw new ArgumentNullException(nameof(incomeSearchViewModel));
            SpendingSearchViewModel = spendingSearchViewModel ?? throw new ArgumentNullException(nameof(spendingSearchViewModel));

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
            ManageCategoriesCommand = new RelayCommand(() => _ = ManageCategoriesAsync());
            
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

                // Initialize All Time period as default
                await SetTimePeriodAsync(TimePeriod.AllTime);

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
                System.Diagnostics.Debug.WriteLine($"RefreshDataAsync: Starting for period {SelectedPeriodStart:yyyy-MM-dd} to {SelectedPeriodEnd:yyyy-MM-dd}");
                
                IsLoading = true;
                StatusMessage = "Loading data...";

                // Load income entries
                var incomeEntries = await _budgetService.GetIncomeAsync(SelectedPeriodStart, SelectedPeriodEnd);
                System.Diagnostics.Debug.WriteLine($"RefreshDataAsync: Retrieved {incomeEntries?.Count() ?? 0} income entries");
                IncomeEntries.Clear();
                foreach (var income in incomeEntries)
                {
                    IncomeEntries.Add(income);
                }

                // Load spending entries
                var spendingEntries = await _budgetService.GetSpendingWithCategoryAsync(SelectedPeriodStart, SelectedPeriodEnd);
                System.Diagnostics.Debug.WriteLine($"RefreshDataAsync: Retrieved {spendingEntries?.Count() ?? 0} spending entries");
                SpendingEntries.Clear();
                foreach (var spending in spendingEntries)
                {
                    SpendingEntries.Add(spending);
                }

                // Calculate budget summary
                BudgetSummary = await _budgetService.GetBudgetSummaryAsync(SelectedPeriodStart, SelectedPeriodEnd);
                System.Diagnostics.Debug.WriteLine($"RefreshDataAsync: BudgetSummary - Income: ${BudgetSummary?.TotalIncome ?? 0}, Spending: ${BudgetSummary?.TotalSpending ?? 0}");

                // Calculate bank statement summary
                BankStatementSummary = await _budgetService.GetBankStatementSummaryAsync(_settingsService.BankStatementDay);
                System.Diagnostics.Debug.WriteLine($"RefreshDataAsync: BankStatementSummary - Income: ${BankStatementSummary?.TotalIncome ?? 0}, Spending: ${BankStatementSummary?.TotalSpending ?? 0}, Period: {BankStatementSummary?.PeriodDescription}");

                // Update recent entries (last 5 of each type)
                UpdateRecentEntries();

                // Update simplified analytics data (legacy - keeping for compatibility)
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Loading hero metrics...");
                await UpdateHeroMetricsAsync();
                
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Loading weekly patterns...");
                await UpdateWeeklyPatternsAsync();
                
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Loading budget insights...");
                await UpdateBudgetInsightsAsync();

                // Update new actionable analytics data
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Loading quick stats...");
                await UpdateQuickStatsAsync();
                
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Loading monthly comparisons...");
                await UpdateMonthlyComparisonsAsync();
                
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Loading category trends...");
                await UpdateCategoryTrendsAsync();
                
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Loading category insights...");
                await UpdateCategoryInsightsAsync();
                
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Loading spending velocity...");
                await UpdateSpendingVelocityAsync();
                
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Loading budget performance score...");
                await UpdateBudgetPerformanceScoreAsync();

                // Update legacy analytics (keeping for compatibility)
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Calling UpdateBudgetTrendDataAsync...");
                await UpdateBudgetTrendDataAsync();
                
                // Update daily budget balance data (last 30 days)
                System.Diagnostics.Debug.WriteLine("RefreshDataAsync: Calling UpdateBudgetBalanceDataAsync...");
                await UpdateBudgetBalanceDataAsync();

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
        private async void UpdateRecentEntries()
        {
            try
            {
                // Clear existing recent entries
                RecentIncomeEntries.Clear();
                RecentSpendingEntries.Clear();

                // Get data from the last 3 months for recent activity
                var threeMonthsAgo = DateTime.Now.AddMonths(-3);
                var now = DateTime.Now;

                // Get recent income entries (last 3 months, take 20)
                var allRecentIncome = await _budgetService.GetIncomeAsync(threeMonthsAgo, now);
                var recentIncome = allRecentIncome
                    .OrderByDescending(i => i.Date)
                    .ThenByDescending(i => i.CreatedAt)
                    .Take(20);

                foreach (var income in recentIncome)
                {
                    RecentIncomeEntries.Add(income);
                }

                // Get recent spending entries (last 3 months, take 30)
                var allRecentSpending = await _budgetService.GetSpendingWithCategoryAsync(threeMonthsAgo, now);
                var recentSpending = allRecentSpending
                    .OrderByDescending(s => s.Date)
                    .ThenByDescending(s => s.CreatedAt)
                    .Take(30);

                foreach (var spending in recentSpending)
                {
                    RecentSpendingEntries.Add(spending);
                }

                System.Diagnostics.Debug.WriteLine($"UpdateRecentEntries: Loaded {RecentIncomeEntries.Count} income and {RecentSpendingEntries.Count} spending entries from last 3 months");

                // Notify property changes for computed properties
                OnPropertyChanged(nameof(HasNoRecentEntries));
                OnPropertyChanged(nameof(TotalEntries));
                OnPropertyChanged(nameof(AverageDailySpending));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateRecentEntries: Error - {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the budget trend data for the last 10 weeks
        /// </summary>
        private async Task UpdateBudgetTrendDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UpdateBudgetTrendDataAsync: Starting...");
                
                BudgetTrendData.Clear();

                // Calculate 10 weeks back from today
                var endDate = DateTime.Now.Date;
                var startDate = endDate.AddDays(-70); // 10 weeks

                System.Diagnostics.Debug.WriteLine($"UpdateBudgetTrendDataAsync: Fetching data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                // Get data for the entire 10-week period
                var allIncome = await _budgetService.GetIncomeAsync(startDate, endDate);
                var allSpending = await _budgetService.GetSpendingWithCategoryAsync(startDate, endDate);

                System.Diagnostics.Debug.WriteLine($"UpdateBudgetTrendDataAsync: Retrieved {allIncome?.Count() ?? 0} income entries and {allSpending?.Count() ?? 0} spending entries");

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

                    var weeklyData = new WeeklyBudgetData
                    {
                        WeekStartDate = weekStart,
                        TotalIncome = weeklyIncome,
                        TotalSpending = weeklySpending
                    };
                    
                    BudgetTrendData.Add(weeklyData);
                    
                    System.Diagnostics.Debug.WriteLine($"UpdateBudgetTrendDataAsync: Week {weekOffset+1} ({weekStart:MM/dd} - {weekEnd:MM/dd}): Income=${weeklyIncome}, Spending=${weeklySpending}, Remaining=${weeklyData.RemainingBudget}");
                }
                
                System.Diagnostics.Debug.WriteLine($"UpdateBudgetTrendDataAsync: Added {BudgetTrendData.Count} weekly data points to BudgetTrendData collection");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateBudgetTrendDataAsync error: {ex}");
                StatusMessage = $"Error updating trend data: {ex.Message}";
            }
        }

        /// <summary>
        /// Updates the daily budget balance data for the last 30 days
        /// </summary>
        private async Task UpdateBudgetBalanceDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UpdateBudgetBalanceDataAsync: Starting...");
                
                BudgetBalanceData.Clear();

                // Calculate 30 days back from today
                var endDate = DateTime.Now.Date;
                var startDate = endDate.AddDays(-30);

                System.Diagnostics.Debug.WriteLine($"UpdateBudgetBalanceDataAsync: Fetching data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                // Get data for the entire 30-day period
                var allIncome = await _budgetService.GetIncomeAsync(startDate, endDate);
                var allSpending = await _budgetService.GetSpendingWithCategoryAsync(startDate, endDate);

                System.Diagnostics.Debug.WriteLine($"UpdateBudgetBalanceDataAsync: Retrieved {allIncome?.Count() ?? 0} income entries and {allSpending?.Count() ?? 0} spending entries");

                // Calculate daily balances and cumulative balance
                decimal cumulativeBalance = 0m;
                
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dailyIncome = allIncome
                        .Where(i => i.Date.Date == date.Date)
                        .Sum(i => i.Amount);

                    var dailySpending = allSpending
                        .Where(s => s.Date.Date == date.Date)
                        .Sum(s => s.Amount);

                    var dailyBalance = dailyIncome - dailySpending;
                    cumulativeBalance += dailyBalance;

                    var dailyData = new DailyBudgetBalance
                    {
                        Date = date,
                        DailyIncome = dailyIncome,
                        DailySpending = dailySpending,
                        CumulativeBalance = cumulativeBalance
                    };
                    
                    BudgetBalanceData.Add(dailyData);
                    
                    System.Diagnostics.Debug.WriteLine($"UpdateBudgetBalanceDataAsync: {date:MM/dd}: Income=${dailyIncome}, Spending=${dailySpending}, Daily=${dailyBalance}, Cumulative=${cumulativeBalance}");
                }
                
                System.Diagnostics.Debug.WriteLine($"UpdateBudgetBalanceDataAsync: Added {BudgetBalanceData.Count} daily data points to BudgetBalanceData collection");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateBudgetBalanceDataAsync error: {ex}");
                StatusMessage = $"Error updating balance data: {ex.Message}";
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
                    BankStatementSummary = await _budgetService.GetBankStatementSummaryAsync(_settingsService.BankStatementDay);
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
                    BankStatementSummary = await _budgetService.GetBankStatementSummaryAsync(_settingsService.BankStatementDay);
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

        private async void SetCurrentMonth()
        {
            var now = DateTime.Now;
            SelectedPeriodStart = new DateTime(now.Year, now.Month, 1);
            SelectedPeriodEnd = SelectedPeriodStart.AddMonths(1).AddDays(-1);
            await RefreshDataAsync(); // Refresh all data including analytics
        }

        private async void SetCurrentYear()
        {
            var now = DateTime.Now;
            SelectedPeriodStart = new DateTime(now.Year, 1, 1);
            SelectedPeriodEnd = new DateTime(now.Year, 12, 31);
            await RefreshDataAsync(); // Refresh all data including analytics
        }

        private async Task SetTimePeriodAsync(TimePeriod period)
        {
            var now = DateTime.Now;
            
            switch (period)
            {
                case TimePeriod.ThisWeek:
                    var startOfWeek = now.AddDays(-(int)now.DayOfWeek + 1); // Monday
                    _selectedPeriodStart = startOfWeek;
                    _selectedPeriodEnd = startOfWeek.AddDays(6); // Sunday
                    break;
                    
                case TimePeriod.TwoWeeks:
                    _selectedPeriodStart = now.AddDays(-14);
                    _selectedPeriodEnd = now;
                    break;
                    
                case TimePeriod.ThisMonth:
                    _selectedPeriodStart = new DateTime(now.Year, now.Month, 1);
                    _selectedPeriodEnd = _selectedPeriodStart.AddMonths(1).AddDays(-1);
                    break;
                    
                case TimePeriod.ThreeMonths:
                    _selectedPeriodStart = now.AddMonths(-3);
                    _selectedPeriodEnd = now;
                    break;
                    
                case TimePeriod.SixMonths:
                    _selectedPeriodStart = now.AddMonths(-6);
                    _selectedPeriodEnd = now;
                    break;
                    
                case TimePeriod.OneYear:
                    _selectedPeriodStart = now.AddYears(-1);
                    _selectedPeriodEnd = now;
                    break;
                    
                case TimePeriod.FiveYears:
                    _selectedPeriodStart = now.AddYears(-5);
                    _selectedPeriodEnd = now;
                    break;
                    
                case TimePeriod.AllTime:
                    var earliestDate = await _budgetService.GetEarliestEntryDateAsync();
                    _selectedPeriodStart = earliestDate ?? now.AddYears(-10);
                    _selectedPeriodEnd = now;
                    break;
            }
            
            OnPropertyChanged(nameof(SelectedPeriodStart));
            OnPropertyChanged(nameof(SelectedPeriodEnd));
            await RefreshDataAsync();
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

        private async Task ManageCategoriesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ManageCategoriesAsync: Starting...");
                StatusMessage = "🔄 Opening category management...";
                
                // Show the category management dialog using proper dialog service pattern
                System.Diagnostics.Debug.WriteLine("ManageCategoriesAsync: Creating CategoryManagementDialog...");
                var dialog = new BudgetManagement.Views.Dialogs.CategoryManagementDialog(
                    _budgetService, _dialogService)
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ShowInTaskbar = false
                };
                
                // Use Dispatcher.Invoke to ensure proper dialog handling
                System.Diagnostics.Debug.WriteLine("ManageCategoriesAsync: Showing dialog...");
                var result = await Application.Current.Dispatcher.InvokeAsync(() => dialog.ShowDialog());
                System.Diagnostics.Debug.WriteLine($"ManageCategoriesAsync: Dialog result: {result}");
                
                if (result == true)
                {
                    StatusMessage = "🔄 Refreshing categories...";
                    
                    // Refresh categories collection
                    var categories = await _budgetService.GetCategoriesAsync();
                    Categories.Clear();
                    foreach (var category in categories.OrderBy(c => c.DisplayOrder))
                    {
                        Categories.Add(category);
                    }
                    
                    StatusMessage = "✅ Categories updated successfully";
                }
                else
                {
                    StatusMessage = "❌ Category management canceled";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error managing categories: {ex.Message}";
                await _dialogService.ShowErrorAsync("Category Management Error", ex.Message);
            }
        }

        /// <summary>
        /// Updates the hero metrics for the simplified analytics dashboard
        /// </summary>
        private async Task UpdateHeroMetricsAsync()
        {
            try
            {
                HeroMetrics = await _budgetService.GetBudgetHealthMetricsAsync(SelectedPeriodStart, SelectedPeriodEnd);
                System.Diagnostics.Debug.WriteLine($"UpdateHeroMetricsAsync: Loaded hero metrics - Health: {HeroMetrics.HealthStatus}, Spending: ${HeroMetrics.MonthlySpending}, Remaining: ${HeroMetrics.BudgetRemaining}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateHeroMetricsAsync error: {ex}");
                HeroMetrics = new BudgetHealthMetrics { HealthStatus = "Critical" };
            }
        }

        /// <summary>
        /// Updates the weekly spending patterns for the simplified bar chart
        /// </summary>
        private async Task UpdateWeeklyPatternsAsync()
        {
            try
            {
                WeeklyPatterns.Clear();
                var patterns = await _budgetService.GetWeeklySpendingPatternsAsync(SelectedPeriodStart, SelectedPeriodEnd);
                foreach (var pattern in patterns)
                {
                    WeeklyPatterns.Add(pattern);
                }
                System.Diagnostics.Debug.WriteLine($"UpdateWeeklyPatternsAsync: Loaded {WeeklyPatterns.Count} weekly patterns");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateWeeklyPatternsAsync error: {ex}");
                WeeklyPatterns.Clear();
            }
        }

        /// <summary>
        /// Updates the budget insights for plain English analytics
        /// </summary>
        private async Task UpdateBudgetInsightsAsync()
        {
            try
            {
                BudgetInsights.Clear();
                var insights = await _budgetService.GenerateBudgetInsightsAsync(SelectedPeriodStart, SelectedPeriodEnd);
                foreach (var insight in insights)
                {
                    BudgetInsights.Add(insight);
                }
                System.Diagnostics.Debug.WriteLine($"UpdateBudgetInsightsAsync: Loaded {BudgetInsights.Count} insights");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateBudgetInsightsAsync error: {ex}");
                BudgetInsights.Clear();
            }
        }

        /// <summary>
        /// Updates the quick stats for the analytics dashboard
        /// </summary>
        private async Task UpdateQuickStatsAsync()
        {
            try
            {
                QuickStats = await _budgetService.GetQuickStatsAsync(SelectedPeriodStart, SelectedPeriodEnd);
                System.Diagnostics.Debug.WriteLine($"UpdateQuickStatsAsync: Loaded quick stats - Days left: {QuickStats.DaysLeft}, Daily budget: ${QuickStats.DailyBudgetRemaining}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateQuickStatsAsync error: {ex}");
                QuickStats = new QuickStats();
            }
        }

        /// <summary>
        /// Updates the monthly comparisons for the analytics dashboard
        /// </summary>
        private async Task UpdateMonthlyComparisonsAsync()
        {
            try
            {
                MonthlyComparisons.Clear();
                var comparisons = await _budgetService.GetMonthlyComparisonAsync(6); // Last 6 months
                foreach (var comparison in comparisons)
                {
                    MonthlyComparisons.Add(comparison);
                }
                System.Diagnostics.Debug.WriteLine($"UpdateMonthlyComparisonsAsync: Loaded {MonthlyComparisons.Count} monthly comparisons");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateMonthlyComparisonsAsync error: {ex}");
                MonthlyComparisons.Clear();
            }
        }

        /// <summary>
        /// Updates the category trends for the analytics dashboard
        /// </summary>
        private async Task UpdateCategoryTrendsAsync()
        {
            try
            {
                CategoryTrends.Clear();
                var trends = await _budgetService.GetCategoryTrendsAsync(3); // Last 3 months
                foreach (var trend in trends)
                {
                    CategoryTrends.Add(trend);
                }
                System.Diagnostics.Debug.WriteLine($"UpdateCategoryTrendsAsync: Loaded {CategoryTrends.Count} category trends");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCategoryTrendsAsync error: {ex}");
                CategoryTrends.Clear();
            }
        }

        /// <summary>
        /// Updates the category insights for the analytics dashboard
        /// </summary>
        private async Task UpdateCategoryInsightsAsync()
        {
            try
            {
                CategoryInsights.Clear();
                var insights = await _budgetService.GetCategoryInsightsAsync(SelectedPeriodStart, SelectedPeriodEnd);
                foreach (var insight in insights)
                {
                    CategoryInsights.Add(insight);
                }
                System.Diagnostics.Debug.WriteLine($"UpdateCategoryInsightsAsync: Loaded {CategoryInsights.Count} category insights");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCategoryInsightsAsync error: {ex}");
                CategoryInsights.Clear();
            }
        }

        /// <summary>
        /// Updates the spending velocity for the analytics dashboard
        /// </summary>
        private async Task UpdateSpendingVelocityAsync()
        {
            try
            {
                SpendingVelocity = await _budgetService.GetSpendingVelocityAsync(SelectedPeriodStart, SelectedPeriodEnd);
                System.Diagnostics.Debug.WriteLine($"UpdateSpendingVelocityAsync: Loaded spending velocity - Daily average: ${SpendingVelocity.DailySpendingAverage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSpendingVelocityAsync error: {ex}");
                SpendingVelocity = new SpendingVelocity();
            }
        }

        /// <summary>
        /// Updates the budget performance score for the analytics dashboard
        /// </summary>
        private async Task UpdateBudgetPerformanceScoreAsync()
        {
            try
            {
                BudgetPerformanceScore = await _budgetService.GetBudgetPerformanceScoreAsync(SelectedPeriodStart, SelectedPeriodEnd);
                System.Diagnostics.Debug.WriteLine($"UpdateBudgetPerformanceScoreAsync: Loaded performance score - Score: {BudgetPerformanceScore.OverallScore}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateBudgetPerformanceScoreAsync error: {ex}");
                BudgetPerformanceScore = new BudgetPerformanceScore();
            }
        }
    }
}