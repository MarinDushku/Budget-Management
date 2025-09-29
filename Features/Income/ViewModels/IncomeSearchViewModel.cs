// Income Search ViewModel - Advanced Search Interface with CQRS Integration
// File: Features/Income/ViewModels/IncomeSearchViewModel.cs

using BudgetManagement.Features.Income.Commands;
using BudgetManagement.Features.Income.Queries;
using BudgetManagement.Models;
using BudgetManagement.Services;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Infrastructure;
using BudgetManagement.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using WpfICommand = System.Windows.Input.ICommand;

namespace BudgetManagement.Features.Income.ViewModels
{
    /// <summary>
    /// ViewModel for advanced income search functionality with CQRS integration
    /// Provides comprehensive search and filtering capabilities for income entries
    /// </summary>
    public class IncomeSearchViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly IMediator _mediator;
        private readonly IDialogService _dialogService;
        private readonly ILogger<IncomeSearchViewModel> _logger;
        private readonly BudgetManagement.Services.IBudgetService _budgetService;

        // Search criteria properties
        private string? _descriptionFilter;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private decimal? _minAmount;
        private decimal? _maxAmount;
        private IncomeSortBy _sortBy = IncomeSortBy.Date;
        private SortDirection _sortDirection = SortDirection.Descending;

        // Search results and state
        private ObservableCollection<Models.Income> _searchResults = new();
        private int _totalResults;
        private decimal _totalAmount;
        private bool _hasSearched;
        private bool _isSearching;
        private bool _hasResults;
        private string? _errorMessage;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages;

        #endregion

        #region Constructors

        public IncomeSearchViewModel(
            IMediator mediator,
            IDialogService dialogService,
            ILogger<IncomeSearchViewModel> logger,
            BudgetManagement.Services.IBudgetService budgetService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));

            // Initialize commands
            SearchCommand = new RelayCommand(async () => await ExecuteSearchAsync(), () => CanExecuteSearch());
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            SetDatePresetCommand = new RelayCommand<string>(async preset => await SetDatePresetAsync(preset));
            EditCommand = new RelayCommand<Models.Income>(async income => await EditIncomeAsync(income));
            DeleteCommand = new RelayCommand<Models.Income>(async income => await DeleteIncomeAsync(income));
            GoToPageCommand = new RelayCommand<int>(async page => await GoToPageAsync(page));
            GoToFirstPageCommand = new RelayCommand(async () => await GoToPageAsync(1));
            GoToPreviousPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage - 1));
            GoToNextPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage + 1));
            GoToLastPageCommand = new RelayCommand(async () => await GoToPageAsync(TotalPages));

            // Set default date range to last month
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
        }

        #endregion

        #region Public Properties - Search Criteria

        /// <summary>
        /// Description filter text for search
        /// </summary>
        public string? DescriptionFilter
        {
            get => _descriptionFilter;
            set => SetProperty(ref _descriptionFilter, value);
        }

        /// <summary>
        /// Start date filter for search
        /// </summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        /// <summary>
        /// End date filter for search
        /// </summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        /// <summary>
        /// Minimum amount filter for search
        /// </summary>
        public decimal? MinAmount
        {
            get => _minAmount;
            set => SetProperty(ref _minAmount, value);
        }

        /// <summary>
        /// Maximum amount filter for search
        /// </summary>
        public decimal? MaxAmount
        {
            get => _maxAmount;
            set => SetProperty(ref _maxAmount, value);
        }

        /// <summary>
        /// Sort field selection
        /// </summary>
        public IncomeSortBy SortBy
        {
            get => _sortBy;
            set => SetProperty(ref _sortBy, value);
        }

        /// <summary>
        /// Sort direction selection
        /// </summary>
        public SortDirection SortDirection
        {
            get => _sortDirection;
            set => SetProperty(ref _sortDirection, value);
        }

        #endregion

        #region Public Properties - Search Results

        /// <summary>
        /// Collection of search results
        /// </summary>
        public ObservableCollection<Models.Income> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        /// <summary>
        /// Total number of matching results
        /// </summary>
        public int TotalResults
        {
            get => _totalResults;
            set => SetProperty(ref _totalResults, value);
        }

        /// <summary>
        /// Total amount of matching results
        /// </summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        /// <summary>
        /// Indicates if a search has been performed
        /// </summary>
        public bool HasSearched
        {
            get => _hasSearched;
            set => SetProperty(ref _hasSearched, value);
        }

        /// <summary>
        /// Indicates if search is currently executing
        /// </summary>
        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        /// <summary>
        /// Indicates if search has results
        /// </summary>
        public bool HasResults
        {
            get => _hasResults;
            set => SetProperty(ref _hasResults, value);
        }

        /// <summary>
        /// Current error message
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        #endregion

        #region Public Properties - Pagination

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// Can navigate to previous page
        /// </summary>
        public bool CanGoToPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Can navigate to next page
        /// </summary>
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        #endregion

        #region Commands

        /// <summary>
        /// Command to execute search
        /// </summary>
        public WpfICommand SearchCommand { get; }

        /// <summary>
        /// Command to clear all filters
        /// </summary>
        public WpfICommand ClearFiltersCommand { get; }

        /// <summary>
        /// Command to set date presets
        /// </summary>
        public WpfICommand SetDatePresetCommand { get; }

        /// <summary>
        /// Command to edit income entry
        /// </summary>
        public WpfICommand EditCommand { get; }

        /// <summary>
        /// Command to delete income entry
        /// </summary>
        public WpfICommand DeleteCommand { get; }

        /// <summary>
        /// Command to go to specific page
        /// </summary>
        public WpfICommand GoToPageCommand { get; }

        /// <summary>
        /// Command to go to first page
        /// </summary>
        public WpfICommand GoToFirstPageCommand { get; }

        /// <summary>
        /// Command to go to previous page
        /// </summary>
        public WpfICommand GoToPreviousPageCommand { get; }

        /// <summary>
        /// Command to go to next page
        /// </summary>
        public WpfICommand GoToNextPageCommand { get; }

        /// <summary>
        /// Command to go to last page
        /// </summary>
        public WpfICommand GoToLastPageCommand { get; }

        #endregion

        #region Private Methods

        private bool CanExecuteSearch()
        {
            return !IsSearching && (
                !string.IsNullOrWhiteSpace(DescriptionFilter) ||
                StartDate.HasValue ||
                EndDate.HasValue ||
                MinAmount.HasValue ||
                MaxAmount.HasValue
            );
        }

        private async Task ExecuteSearchAsync()
        {
            try
            {
                IsSearching = true;
                ErrorMessage = null;

                _logger.LogDebug("Executing income search with criteria");

                var query = new AdvancedIncomeSearchQuery(
                    DescriptionPattern: string.IsNullOrWhiteSpace(DescriptionFilter) ? null : DescriptionFilter,
                    StartDate: StartDate,
                    EndDate: EndDate,
                    MinAmount: MinAmount,
                    MaxAmount: MaxAmount,
                    Skip: (CurrentPage - 1) * PageSize,
                    Take: PageSize,
                    SortBy: SortBy,
                    SortDirection: SortDirection
                );

                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Value != null)
                {
                    SearchResults.Clear();
                    foreach (var income in result.Value.Incomes)
                    {
                        SearchResults.Add(income);
                    }

                    TotalResults = result.Value.TotalCount;
                    TotalAmount = result.Value.TotalAmount;
                    TotalPages = (int)Math.Ceiling((double)TotalResults / PageSize);
                    HasResults = SearchResults.Any();
                    HasSearched = true;

                    _logger.LogInformation("Income search completed successfully. Found {Count} results", TotalResults);
                }
                else
                {
                    ErrorMessage = result.Error?.Message ?? "Search failed";
                    HasResults = false;
                    HasSearched = true;
                    _logger.LogWarning("Income search failed: {Error}", result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred during search";
                HasResults = false;
                HasSearched = true;
                _logger.LogError(ex, "Unexpected error during income search");
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void ClearFilters()
        {
            DescriptionFilter = null;
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
            MinAmount = null;
            MaxAmount = null;
            SortBy = IncomeSortBy.Date;
            SortDirection = SortDirection.Descending;
            CurrentPage = 1;
            ErrorMessage = null;
            
            SearchResults.Clear();
            TotalResults = 0;
            TotalAmount = 0;
            HasSearched = false;
            HasResults = false;
        }

        private async Task SetDatePresetAsync(string? preset)
        {
            var today = DateTime.Today;
            
            switch (preset)
            {
                case "ThisMonth":
                    StartDate = new DateTime(today.Year, today.Month, 1);
                    EndDate = today;
                    break;
                case "LastMonth":
                    var lastMonth = today.AddMonths(-1);
                    StartDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    EndDate = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                    break;
                case "Last3Months":
                    StartDate = today.AddMonths(-3);
                    EndDate = today;
                    break;
                default:
                    return;
            }

            // Auto-execute search if other criteria exist
            if (CanExecuteSearch())
            {
                await ExecuteSearchAsync();
            }
        }

        private async Task EditIncomeAsync(Models.Income? income)
        {
            if (income == null) return;

            try
            {
                _logger.LogDebug("Editing income entry {Id}", income.Id);

                var updatedIncome = await _dialogService.ShowIncomeDialogAsync(income);
                if (updatedIncome != null)
                {
                    await _budgetService.UpdateIncomeAsync(updatedIncome);
                    await ExecuteSearchAsync(); // Refresh search results
                    _logger.LogInformation("Income entry {Id} updated successfully from search", income.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing income entry {Id}", income.Id);
                ErrorMessage = "Failed to edit income entry";
                await _dialogService.ShowErrorAsync("Edit Income Error", ex.Message);
            }
        }

        private async Task DeleteIncomeAsync(Models.Income? income)
        {
            if (income == null) return;

            var confirmResult = await _dialogService.ShowConfirmationAsync(
                "Delete Income Entry",
                $"Are you sure you want to delete the income entry '{income.Description}' for {income.Amount:C}?");

            if (confirmResult)
            {
                try
                {
                    _logger.LogDebug("Deleting income entry {Id}", income.Id);
                    
                    await _budgetService.DeleteIncomeAsync(income.Id);
                    
                    // Refresh search results
                    await ExecuteSearchAsync();
                    _logger.LogInformation("Income entry {Id} deleted successfully from search", income.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting income entry {Id}", income.Id);
                    ErrorMessage = "Failed to delete income entry";
                    await _dialogService.ShowErrorAsync("Delete Error", ex.Message);
                }
            }
        }

        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages || page == CurrentPage) return;

            CurrentPage = page;
            await ExecuteSearchAsync();
        }

        #endregion
    }
}