// Spending ViewModel - MVVM Integration for Spending Feature
// File: Features/Spending/ViewModels/SpendingViewModel.cs

using BudgetManagement.Features.Spending.Commands;
using BudgetManagement.Features.Spending.Queries;
using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;
using BudgetManagement.ViewModels;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace BudgetManagement.Features.Spending.ViewModels
{
    /// <summary>
    /// ViewModel for the Spending feature with CQRS integration
    /// </summary>
    public class SpendingViewModel : BaseViewModel
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SpendingViewModel> _logger;
        private readonly ICategoryRepository _categoryRepository;

        public SpendingViewModel(
            IMediator mediator,
            ILogger<SpendingViewModel> logger,
            ICategoryRepository categoryRepository)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));

            // Initialize collections
            Spendings = new ObservableCollection<Models.Spending>();
            Categories = new ObservableCollection<Category>();
            RecentSpendings = new ObservableCollection<Models.Spending>();

            // Initialize commands
            AddSpendingCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(ExecuteAddSpendingAsync, CanExecuteAddSpending);
            UpdateSpendingCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<Models.Spending>(ExecuteUpdateSpendingAsync, CanExecuteUpdateSpending);
            DeleteSpendingCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<Models.Spending>(ExecuteDeleteSpendingAsync, CanExecuteDeleteSpending);
            LoadSpendingsCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(ExecuteLoadSpendingsAsync);
            RefreshCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(ExecuteRefreshAsync);
            SearchCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(ExecuteSearchAsync, () => CanSearch);

            // Set default values
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
            NewSpendingDate = DateTime.Today;
            NewSpendingAmount = 0m;
            NewSpendingDescription = string.Empty;
            SearchPattern = string.Empty;

            // Load initial data
            _ = Task.Run(async () => await LoadInitialDataAsync());
        }

        #region Properties

        private ObservableCollection<Models.Spending> _spendings;
        public ObservableCollection<Models.Spending> Spendings
        {
            get => _spendings;
            set => SetProperty(ref _spendings, value);
        }

        private ObservableCollection<Category> _categories;
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private ObservableCollection<Models.Spending> _recentSpendings;
        public ObservableCollection<Models.Spending> RecentSpendings
        {
            get => _recentSpendings;
            set => SetProperty(ref _recentSpendings, value);
        }

        private Models.Spending? _selectedSpending;
        public Models.Spending? SelectedSpending
        {
            get => _selectedSpending;
            set => SetProperty(ref _selectedSpending, value);
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                SetProperty(ref _startDate, value);
                OnPropertyChanged(nameof(CanLoadSpendings));
            }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                SetProperty(ref _endDate, value);
                OnPropertyChanged(nameof(CanLoadSpendings));
            }
        }

        private DateTime _newSpendingDate;
        public DateTime NewSpendingDate
        {
            get => _newSpendingDate;
            set => SetProperty(ref _newSpendingDate, value);
        }

        private decimal _newSpendingAmount;
        public decimal NewSpendingAmount
        {
            get => _newSpendingAmount;
            set
            {
                SetProperty(ref _newSpendingAmount, value);
                OnPropertyChanged(nameof(CanAddSpending));
            }
        }

        private string _newSpendingDescription;
        public string NewSpendingDescription
        {
            get => _newSpendingDescription;
            set
            {
                SetProperty(ref _newSpendingDescription, value);
                OnPropertyChanged(nameof(CanAddSpending));
            }
        }

        private Category? _newSpendingCategory;
        public Category? NewSpendingCategory
        {
            get => _newSpendingCategory;
            set
            {
                SetProperty(ref _newSpendingCategory, value);
                OnPropertyChanged(nameof(CanAddSpending));
            }
        }

        private string _searchPattern;
        public string SearchPattern
        {
            get => _searchPattern;
            set
            {
                SetProperty(ref _searchPattern, value);
                OnPropertyChanged(nameof(CanSearch));
            }
        }

        private decimal _totalSpending;
        public decimal TotalSpending
        {
            get => _totalSpending;
            set => SetProperty(ref _totalSpending, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand AddSpendingCommand { get; }
        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<Models.Spending> UpdateSpendingCommand { get; }
        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<Models.Spending> DeleteSpendingCommand { get; }
        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand LoadSpendingsCommand { get; }
        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand RefreshCommand { get; }
        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand SearchCommand { get; }

        #endregion

        #region Command Implementations

        private async Task ExecuteAddSpendingAsync()
        {
            if (!CanAddSpending) return;

            try
            {
                IsLoading = true;

                var command = new AddSpendingCommand(
                    NewSpendingDate,
                    NewSpendingAmount,
                    NewSpendingDescription,
                    NewSpendingCategory!.Id
                );

                var result = await _mediator.Send(command);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Spending added successfully: {Description}, Amount: {Amount:C}", 
                        NewSpendingDescription, NewSpendingAmount);
                    
                    await ExecuteRefreshAsync();
                    ClearNewSpendingForm();
                }
                else
                {
                    _logger.LogWarning("Failed to add spending: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding spending");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteUpdateSpendingAsync(Models.Spending? spending)
        {
            if (spending == null || !CanExecuteUpdateSpending(spending)) return;

            try
            {
                IsLoading = true;

                var command = new UpdateSpendingCommand(
                    spending.Id,
                    spending.Date,
                    spending.Amount,
                    spending.Description,
                    spending.CategoryId
                );

                var result = await _mediator.Send(command);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Spending updated successfully: ID {Id}", spending.Id);
                    await ExecuteRefreshAsync();
                }
                else
                {
                    _logger.LogWarning("Failed to update spending: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating spending ID {Id}", spending.Id);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteDeleteSpendingAsync(Models.Spending? spending)
        {
            if (spending == null || !CanExecuteDeleteSpending(spending)) return;

            try
            {
                IsLoading = true;

                var command = new DeleteSpendingCommand(spending.Id);
                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Spending deleted successfully: ID {Id}", spending.Id);
                    await ExecuteRefreshAsync();
                }
                else
                {
                    _logger.LogWarning("Failed to delete spending: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting spending ID {Id}", spending.Id);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteLoadSpendingsAsync()
        {
            if (!CanLoadSpendings) return;

            try
            {
                IsLoading = true;

                var query = new GetSpendingByDateRangeQuery(StartDate, EndDate);
                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Value != null)
                {
                    var spendingsList = result.Value.ToList();
                    Spendings.Clear();
                    foreach (var spending in spendingsList)
                    {
                        Spendings.Add(spending);
                    }

                    // Calculate total
                    TotalSpending = spendingsList.Sum(s => s.Amount);

                    _logger.LogDebug("Loaded {Count} spending entries", spendingsList.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to load spendings: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading spendings");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteRefreshAsync()
        {
            await LoadCategoriesAsync();
            await ExecuteLoadSpendingsAsync();
            await LoadRecentSpendingsAsync();
        }

        private async Task ExecuteSearchAsync()
        {
            if (!CanSearch) return;

            try
            {
                IsLoading = true;

                var query = new SearchSpendingByDescriptionQuery(SearchPattern);
                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Value != null)
                {
                    var spendingsList = result.Value.ToList();
                    Spendings.Clear();
                    foreach (var spending in spendingsList)
                    {
                        Spendings.Add(spending);
                    }

                    TotalSpending = spendingsList.Sum(s => s.Amount);

                    _logger.LogDebug("Found {Count} spending entries matching pattern '{Pattern}'", 
                        spendingsList.Count, SearchPattern);
                }
                else
                {
                    _logger.LogWarning("Failed to search spendings: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching spendings");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Command Can Execute

        private bool CanExecuteAddSpending()
        {
            return !IsLoading && CanAddSpending;
        }

        private bool CanExecuteUpdateSpending(Models.Spending? spending)
        {
            return !IsLoading && spending != null;
        }

        private bool CanExecuteDeleteSpending(Models.Spending? spending)
        {
            return !IsLoading && spending != null;
        }

        private bool CanSearch => !IsLoading && !string.IsNullOrWhiteSpace(SearchPattern) && SearchPattern.Length >= 2;

        public bool CanAddSpending => 
            !IsLoading &&
            NewSpendingAmount > 0 && 
            !string.IsNullOrWhiteSpace(NewSpendingDescription) && 
            NewSpendingCategory != null;

        public bool CanLoadSpendings => 
            !IsLoading && StartDate <= EndDate && (EndDate - StartDate).TotalDays <= 1095; // 3 years max

        #endregion

        #region Helper Methods

        private async Task LoadInitialDataAsync()
        {
            try
            {
                await LoadCategoriesAsync();
                await LoadRecentSpendingsAsync();
                await ExecuteLoadSpendingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading initial data");
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var result = await _categoryRepository.GetAllAsync();
                if (result.IsSuccess && result.Value != null)
                {
                    Categories.Clear();
                    foreach (var category in result.Value)
                    {
                        Categories.Add(category);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
            }
        }

        private async Task LoadRecentSpendingsAsync()
        {
            try
            {
                var query = new GetRecentSpendingQuery(10);
                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Value != null)
                {
                    RecentSpendings.Clear();
                    foreach (var spending in result.Value)
                    {
                        RecentSpendings.Add(spending);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent spendings");
            }
        }

        private void ClearNewSpendingForm()
        {
            NewSpendingDate = DateTime.Today;
            NewSpendingAmount = 0m;
            NewSpendingDescription = string.Empty;
            NewSpendingCategory = null;
        }

        #endregion
    }
}