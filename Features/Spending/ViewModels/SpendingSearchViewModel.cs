// Spending Search ViewModel - Advanced Search Interface with CQRS Integration
// File: Features/Spending/ViewModels/SpendingSearchViewModel.cs

using BudgetManagement.Features.Spending.Commands;
using BudgetManagement.Features.Spending.Queries;
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

namespace BudgetManagement.Features.Spending.ViewModels
{
    /// <summary>
    /// ViewModel for advanced spending search functionality with CQRS integration
    /// Provides comprehensive search and filtering capabilities for spending entries including category filtering
    /// </summary>
    public class SpendingSearchViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly IMediator _mediator;
        private readonly IDialogService _dialogService;
        private readonly ILogger<SpendingSearchViewModel> _logger;
        private readonly BudgetManagement.Services.IBudgetService _budgetService;

        // Search criteria properties
        private string? _descriptionFilter;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private decimal? _minAmount;
        private decimal? _maxAmount;
        private ObservableCollection<SelectableCategory> _availableCategories = new();
        private int _selectedCategoriesCount = 0;
        private SpendingSortBy _sortBy = SpendingSortBy.Date;
        private SortDirection _sortDirection = SortDirection.Descending;

        // Search results and state
        private ObservableCollection<SpendingWithCategory> _searchResults = new();
        private int _totalResults;
        private decimal _totalAmount;
        private Dictionary<int, decimal> _categoryTotals = new();
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

        public SpendingSearchViewModel(
            IMediator mediator,
            IDialogService dialogService,
            ILogger<SpendingSearchViewModel> logger,
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
            EditCommand = new RelayCommand<SpendingWithCategory>(async spending => await EditSpendingAsync(spending));
            DeleteCommand = new RelayCommand<SpendingWithCategory>(async spending => await DeleteSpendingAsync(spending));
            GoToPageCommand = new RelayCommand<int>(async page => await GoToPageAsync(page));
            GoToFirstPageCommand = new RelayCommand(async () => await GoToPageAsync(1));
            GoToPreviousPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage - 1));
            GoToNextPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage + 1));
            GoToLastPageCommand = new RelayCommand(async () => await GoToPageAsync(TotalPages));
            ToggleCategorySelectionCommand = new RelayCommand<SelectableCategory>(ToggleCategorySelection);
            SelectAllCategoriesCommand = new RelayCommand(SelectAllCategories);
            ClearCategoriesCommand = new RelayCommand(ClearCategorySelection);

            // Set default date range to last month
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;

            // Load categories
            _ = Task.Run(LoadCategoriesAsync);
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
        /// Available categories for filtering with selection state
        /// </summary>
        public ObservableCollection<SelectableCategory> AvailableCategories
        {
            get => _availableCategories;
            set => SetProperty(ref _availableCategories, value);
        }

        /// <summary>
        /// Number of selected categories
        /// </summary>
        public int SelectedCategoriesCount
        {
            get => _selectedCategoriesCount;
            set
            {
                if (SetProperty(ref _selectedCategoriesCount, value))
                {
                    OnPropertyChanged(nameof(CategorySelectionSummary));
                    OnPropertyChanged(nameof(AllCategoriesSelected));
                }
            }
        }

        /// <summary>
        /// Summary text for category selection
        /// </summary>
        public string CategorySelectionSummary
        {
            get
            {
                if (AvailableCategories.Count == 0) return "No categories";
                if (SelectedCategoriesCount == 0) return "All categories";
                if (SelectedCategoriesCount == AvailableCategories.Count) return "All categories";
                return $"{SelectedCategoriesCount} of {AvailableCategories.Count} categories";
            }
        }

        /// <summary>
        /// Indicates if all categories are selected (or none, which means all)
        /// </summary>
        public bool AllCategoriesSelected => SelectedCategoriesCount == 0 || SelectedCategoriesCount == AvailableCategories.Count;

        /// <summary>
        /// Sort field selection
        /// </summary>
        public SpendingSortBy SortBy
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
        public ObservableCollection<SpendingWithCategory> SearchResults
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
            set
            {
                if (SetProperty(ref _totalAmount, value))
                {
                    OnPropertyChanged(nameof(FormattedTotalAmount));
                }
            }
        }

        /// <summary>
        /// Formatted total amount with currency symbol for display
        /// </summary>
        public string FormattedTotalAmount => $"€{TotalAmount:F2}";

        /// <summary>
        /// Category-wise totals from search results
        /// </summary>
        public Dictionary<int, decimal> CategoryTotals
        {
            get => _categoryTotals;
            set => SetProperty(ref _categoryTotals, value);
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
        /// Command to edit spending entry
        /// </summary>
        public WpfICommand EditCommand { get; }

        /// <summary>
        /// Command to delete spending entry
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

        /// <summary>
        /// Command to toggle category selection
        /// </summary>
        public WpfICommand ToggleCategorySelectionCommand { get; }

        /// <summary>
        /// Command to select all categories
        /// </summary>
        public WpfICommand SelectAllCategoriesCommand { get; }

        /// <summary>
        /// Command to clear category selection
        /// </summary>
        public WpfICommand ClearCategoriesCommand { get; }

        #endregion

        #region Private Methods

        private bool CanExecuteSearch()
        {
            return !IsSearching && (
                !string.IsNullOrWhiteSpace(DescriptionFilter) ||
                StartDate.HasValue ||
                EndDate.HasValue ||
                MinAmount.HasValue ||
                MaxAmount.HasValue ||
                AvailableCategories.Any(c => c.IsSelected)
            );
        }

        private async Task ExecuteSearchAsync()
        {
            try
            {
                IsSearching = true;
                ErrorMessage = null;

                _logger.LogDebug("Executing spending search with criteria");

                var selectedCategories = AvailableCategories.Where(c => c.IsSelected).ToList();
                var categoryIds = selectedCategories.Any() ? selectedCategories.Select(c => c.Id).ToList() : null;

                var query = new AdvancedSpendingSearchQuery(
                    DescriptionPattern: string.IsNullOrWhiteSpace(DescriptionFilter) ? null : DescriptionFilter,
                    StartDate: StartDate,
                    EndDate: EndDate,
                    MinAmount: MinAmount,
                    MaxAmount: MaxAmount,
                    CategoryIds: categoryIds,
                    Skip: (CurrentPage - 1) * PageSize,
                    Take: PageSize,
                    SortBy: SortBy,
                    SortDirection: SortDirection
                );

                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Value != null)
                {
                    SearchResults.Clear();
                    foreach (var spending in result.Value.Spendings)
                    {
                        SearchResults.Add(spending);
                    }

                    TotalResults = result.Value.TotalCount;
                    TotalAmount = result.Value.TotalAmount;
                    CategoryTotals = result.Value.CategoryTotals;
                    TotalPages = (int)Math.Ceiling((double)TotalResults / PageSize);
                    HasResults = SearchResults.Any();
                    HasSearched = true;

                    _logger.LogInformation("Spending search completed successfully. Found {Count} results", TotalResults);
                }
                else
                {
                    ErrorMessage = result.Error?.Message ?? "Search failed";
                    HasResults = false;
                    HasSearched = true;
                    _logger.LogWarning("Spending search failed: {Error}", result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred during search";
                HasResults = false;
                HasSearched = true;
                _logger.LogError(ex, "Unexpected error during spending search");
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
            foreach (var category in AvailableCategories)
            {
                category.IsSelected = false;
            }
            UpdateSelectedCategoriesCount();
            SortBy = SpendingSortBy.Date;
            SortDirection = SortDirection.Descending;
            CurrentPage = 1;
            ErrorMessage = null;
            
            SearchResults.Clear();
            TotalResults = 0;
            TotalAmount = 0;
            CategoryTotals.Clear();
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

        private async Task LoadCategoriesAsync()
        {
            try
            {
                _logger.LogDebug("Loading categories for spending search");
                
                // Load actual categories from the database using BudgetService
                var categories = await _budgetService.GetCategoriesAsync();
                
                AvailableCategories.Clear();
                foreach (var category in categories)
                {
                    var selectableCategory = new SelectableCategory(category, false);
                    selectableCategory.PropertyChanged += OnCategorySelectionChanged;
                    AvailableCategories.Add(selectableCategory);
                }
                
                UpdateSelectedCategoriesCount();
                _logger.LogDebug("Loaded {Count} real categories for spending search", AvailableCategories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories for spending search");
            }
        }

        private void ToggleCategorySelection(SelectableCategory? category)
        {
            if (category == null) return;
            category.IsSelected = !category.IsSelected;
        }

        private void SelectAllCategories()
        {
            foreach (var category in AvailableCategories)
            {
                category.IsSelected = true;
            }
        }

        private void ClearCategorySelection()
        {
            foreach (var category in AvailableCategories)
            {
                category.IsSelected = false;
            }
        }

        private void OnCategorySelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableCategory.IsSelected))
            {
                UpdateSelectedCategoriesCount();
                // Refresh the SearchCommand's CanExecute
                (SearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void UpdateSelectedCategoriesCount()
        {
            SelectedCategoriesCount = AvailableCategories.Count(c => c.IsSelected);
        }

        private async Task EditSpendingAsync(SpendingWithCategory? spending)
        {
            if (spending == null) return;

            try
            {
                _logger.LogDebug("Editing spending entry {Id}", spending.Id);

                // Convert to Spending model for editing
                var spendingModel = new Models.Spending
                {
                    Id = spending.Id,
                    Date = spending.Date,
                    Amount = spending.Amount,
                    Description = spending.Description,
                    CategoryId = spending.CategoryId
                };

                var updatedSpending = await _dialogService.ShowSpendingDialogAsync(spendingModel, AvailableCategories.Select(sc => sc.Category).ToList());
                if (updatedSpending != null)
                {
                    await _budgetService.UpdateSpendingAsync(updatedSpending);
                    await ExecuteSearchAsync(); // Refresh search results
                    _logger.LogInformation("Spending entry {Id} updated successfully from search", spending.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing spending entry {Id}", spending.Id);
                ErrorMessage = "Failed to edit spending entry";
                await _dialogService.ShowErrorAsync("Edit Spending Error", ex.Message);
            }
        }

        private async Task DeleteSpendingAsync(SpendingWithCategory? spending)
        {
            if (spending == null) return;

            var confirmResult = await _dialogService.ShowConfirmationAsync(
                "Delete Spending Entry",
                $"Are you sure you want to delete the spending entry '{spending.Description}' for {spending.Amount:C}?");

            if (confirmResult)
            {
                try
                {
                    _logger.LogDebug("Deleting spending entry {Id}", spending.Id);
                    
                    await _budgetService.DeleteSpendingAsync(spending.Id);
                    
                    // Refresh search results
                    await ExecuteSearchAsync();
                    _logger.LogInformation("Spending entry {Id} deleted successfully from search", spending.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting spending entry {Id}", spending.Id);
                    ErrorMessage = "Failed to delete spending entry";
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