// Dashboard ViewModel - Modern MVVM with Vertical Slice Architecture
// File: Features/Dashboard/ViewModels/DashboardViewModel.cs

using System.Collections.ObjectModel;
using System.Windows.Input;
using BudgetManagement.Features.Dashboard.Queries;
using BudgetManagement.ViewModels;
using BudgetManagement.Shared.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;

namespace BudgetManagement.Features.Dashboard.ViewModels
{
    /// <summary>
    /// Modern Dashboard ViewModel using vertical slice architecture
    /// Demonstrates CQRS pattern, Result pattern, and proper separation of concerns
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DashboardViewModel> _logger;

        // Private fields for properties
        private DashboardSummary? _dashboardData;
        private DateTime _selectedPeriodStart;
        private DateTime _selectedPeriodEnd;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private string? _errorMessage;
        private int _bankStatementDay = 1;

        public DashboardViewModel(IMediator mediator, ILogger<DashboardViewModel> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize date range to current month
            var now = DateTime.Now;
            _selectedPeriodStart = new DateTime(now.Year, now.Month, 1);
            _selectedPeriodEnd = _selectedPeriodStart.AddMonths(1).AddDays(-1);

            // Initialize commands
            RefreshDashboardCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(RefreshDashboardAsync, CanRefreshDashboard);
            SetCurrentMonthCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(SetCurrentMonthAsync);
            SetCurrentYearCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(SetCurrentYearAsync);
            SetLast30DaysCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(SetLast30DaysAsync);

            // Initialize collections
            RecentIncomeEntries = new ObservableCollection<Models.Income>();
            RecentSpendingEntries = new ObservableCollection<Models.SpendingWithCategory>();
            BudgetTrendData = new ObservableCollection<WeeklyBudgetData>();
        }

        #region Properties

        /// <summary>
        /// Complete dashboard data
        /// </summary>
        public DashboardSummary? DashboardData
        {
            get => _dashboardData;
            private set => SetProperty(ref _dashboardData, value, OnDashboardDataChanged);
        }

        /// <summary>
        /// Start date for the selected period
        /// </summary>
        public DateTime SelectedPeriodStart
        {
            get => _selectedPeriodStart;
            set => SetProperty(ref _selectedPeriodStart, value, async () => await RefreshDashboardAsync());
        }

        /// <summary>
        /// End date for the selected period
        /// </summary>
        public DateTime SelectedPeriodEnd
        {
            get => _selectedPeriodEnd;
            set => SetProperty(ref _selectedPeriodEnd, value, async () => await RefreshDashboardAsync());
        }

        /// <summary>
        /// Bank statement day setting
        /// </summary>
        public int BankStatementDay
        {
            get => _bankStatementDay;
            set => SetProperty(ref _bankStatementDay, value, async () => await RefreshDashboardAsync());
        }

        /// <summary>
        /// Whether the dashboard is currently loading data
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value, () => RefreshDashboardCommand.NotifyCanExecuteChanged());
        }

        /// <summary>
        /// Status message for user feedback
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Error message if something went wrong
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Budget summary data
        /// </summary>
        public Models.BudgetSummary? BudgetSummary => DashboardData?.BudgetSummary;

        /// <summary>
        /// Bank statement summary data
        /// </summary>
        public Models.BankStatementSummary? BankStatementSummary => DashboardData?.BankStatementSummary;

        /// <summary>
        /// Whether there are no recent entries to display
        /// </summary>
        public bool HasNoRecentEntries => DashboardData?.HasNoRecentEntries ?? true;

        /// <summary>
        /// Total number of entries in the current period
        /// </summary>
        public int TotalEntries => DashboardData?.TotalEntries ?? 0;

        /// <summary>
        /// Average daily spending for the period
        /// </summary>
        public decimal AverageDailySpending => DashboardData?.AverageDailySpending ?? 0m;

        /// <summary>
        /// Description of the current period
        /// </summary>
        public string PeriodDescription => DashboardData?.PeriodDescription ?? string.Empty;

        #endregion

        #region Collections

        /// <summary>
        /// Recent income entries for the activity feed
        /// </summary>
        public ObservableCollection<Models.Income> RecentIncomeEntries { get; }

        /// <summary>
        /// Recent spending entries for the activity feed
        /// </summary>
        public ObservableCollection<Models.SpendingWithCategory> RecentSpendingEntries { get; }

        /// <summary>
        /// Budget trend data for analytics charts
        /// </summary>
        public ObservableCollection<WeeklyBudgetData> BudgetTrendData { get; }

        #endregion

        #region Commands

        /// <summary>
        /// Command to refresh dashboard data
        /// </summary>
        public IAsyncRelayCommand RefreshDashboardCommand { get; }

        /// <summary>
        /// Command to set period to current month
        /// </summary>
        public IAsyncRelayCommand SetCurrentMonthCommand { get; }

        /// <summary>
        /// Command to set period to current year
        /// </summary>
        public IAsyncRelayCommand SetCurrentYearCommand { get; }

        /// <summary>
        /// Command to set period to last 30 days
        /// </summary>
        public IAsyncRelayCommand SetLast30DaysCommand { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the dashboard by loading initial data
        /// </summary>
        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing dashboard view model");
            await RefreshDashboardAsync();
        }

        /// <summary>
        /// Updates the date range and refreshes data
        /// </summary>
        public async Task UpdateDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate == SelectedPeriodStart && endDate == SelectedPeriodEnd)
                return;

            _selectedPeriodStart = startDate;
            _selectedPeriodEnd = endDate;
            OnPropertyChanged(nameof(SelectedPeriodStart));
            OnPropertyChanged(nameof(SelectedPeriodEnd));

            await RefreshDashboardAsync();
        }

        #endregion

        #region Private Methods

        private async Task RefreshDashboardAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading dashboard data...";
                ErrorMessage = null;

                _logger.LogInformation("Refreshing dashboard data for period {StartDate} to {EndDate}", 
                    SelectedPeriodStart, SelectedPeriodEnd);

                // Create and send the query using MediatR
                var query = new GetDashboardSummaryQuery(SelectedPeriodStart, SelectedPeriodEnd, BankStatementDay);
                var result = await _mediator.Send(query);

                // Handle the result using the Result pattern
                if (result.IsSuccess)
                {
                    DashboardData = result.Value;
                    StatusMessage = "Dashboard updated successfully";
                    _logger.LogInformation("Dashboard data refreshed successfully");
                }
                else
                {
                    HandleDashboardLoadError(result.Error!);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while refreshing dashboard");
                HandleDashboardLoadError(Error.System(Error.Codes.SYSTEM_ERROR, 
                    "An unexpected error occurred while loading dashboard data"));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SetCurrentMonthAsync()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            
            await UpdateDateRangeAsync(startOfMonth, endOfMonth);
        }

        private async Task SetCurrentYearAsync()
        {
            var now = DateTime.Now;
            var startOfYear = new DateTime(now.Year, 1, 1);
            var endOfYear = new DateTime(now.Year, 12, 31);
            
            await UpdateDateRangeAsync(startOfYear, endOfYear);
        }

        private async Task SetLast30DaysAsync()
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-29); // -29 to include today = 30 days total
            
            await UpdateDateRangeAsync(startDate, endDate);
        }

        private bool CanRefreshDashboard()
        {
            return !IsLoading;
        }

        private void OnDashboardDataChanged()
        {
            if (DashboardData == null)
                return;

            // Update collections
            UpdateObservableCollection(RecentIncomeEntries, DashboardData.RecentIncomeEntries);
            UpdateObservableCollection(RecentSpendingEntries, DashboardData.RecentSpendingEntries);
            UpdateObservableCollection(BudgetTrendData, DashboardData.BudgetTrendData);

            // Notify property changes for computed properties
            OnPropertyChanged(nameof(BudgetSummary));
            OnPropertyChanged(nameof(BankStatementSummary));
            OnPropertyChanged(nameof(HasNoRecentEntries));
            OnPropertyChanged(nameof(TotalEntries));
            OnPropertyChanged(nameof(AverageDailySpending));
            OnPropertyChanged(nameof(PeriodDescription));
        }

        private void HandleDashboardLoadError(Error error)
        {
            _logger.LogError("Dashboard load error: {Error}", error);
            
            ErrorMessage = error.Type switch
            {
                ErrorType.Validation => $"Invalid input: {error.Message}",
                ErrorType.NotFound => "Some data could not be found. Please check your date range.",
                ErrorType.System => "A system error occurred while loading dashboard data. Please try again.",
                _ => "An error occurred while loading dashboard data."
            };

            StatusMessage = "Error loading dashboard data";
            
            // Clear existing data on error
            DashboardData = null;
        }

        private static void UpdateObservableCollection<T>(ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            collection.Clear();
            foreach (var item in newItems)
            {
                collection.Add(item);
            }
        }

        #endregion
    }
}