// Income ViewModel - MVVM Pattern with CQRS Integration
// File: Features/Income/ViewModels/IncomeViewModel.cs

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
using System.Windows.Input;

namespace BudgetManagement.Features.Income.ViewModels
{
    /// <summary>
    /// ViewModel for Income management with CQRS integration
    /// Provides comprehensive income operations for WPF views
    /// </summary>
    public class IncomeViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly IMediator _mediator;
        private readonly IDialogService _dialogService;
        private readonly ILogger<IncomeViewModel> _logger;

        private ObservableCollection<Models.Income> _incomes = new();
        private Models.Income? _selectedIncome;
        private DateTime _startDate = DateTime.Today.AddDays(-30);
        private DateTime _endDate = DateTime.Today;
        private decimal _totalIncome;
        private bool _isLoading;
        private string _searchText = string.Empty;
        private string? _errorMessage;

        #endregion

        #region Public Properties

        /// <summary>
        /// Collection of income entries for display
        /// </summary>
        public ObservableCollection<Models.Income> Incomes
        {
            get => _incomes;
            set => SetProperty(ref _incomes, value);
        }

        /// <summary>
        /// Currently selected income entry
        /// </summary>
        public Models.Income? SelectedIncome
        {
            get => _selectedIncome;
            set => SetProperty(ref _selectedIncome, value);
        }

        /// <summary>
        /// Start date for income filtering
        /// </summary>
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    LoadIncomesCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// End date for income filtering
        /// </summary>
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    LoadIncomesCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Total income amount for current date range
        /// </summary>
        public decimal TotalIncome
        {
            get => _totalIncome;
            set => SetProperty(ref _totalIncome, value);
        }

        /// <summary>
        /// Indicates if operations are in progress
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Search text for filtering incomes
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// Error message to display to user
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Indicates if there are any income entries
        /// </summary>
        public bool HasIncomes => Incomes.Any();

        /// <summary>
        /// Indicates if an income is selected
        /// </summary>
        public bool HasSelectedIncome => SelectedIncome != null;

        #endregion

        #region Commands

        public System.Windows.Input.ICommand LoadIncomesCommand { get; }
        public System.Windows.Input.ICommand AddIncomeCommand { get; }
        public System.Windows.Input.ICommand EditIncomeCommand { get; }
        public System.Windows.Input.ICommand DeleteIncomeCommand { get; }
        public System.Windows.Input.ICommand SearchIncomeCommand { get; }
        public System.Windows.Input.ICommand RefreshCommand { get; }
        public System.Windows.Input.ICommand ExportIncomesCommand { get; }

        #endregion

        #region Constructor

        public IncomeViewModel(
            IMediator mediator,
            IDialogService dialogService,
            ILogger<IncomeViewModel> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            LoadIncomesCommand = new RelayCommand(async () => await LoadIncomesAsync(), () => !IsLoading);
            AddIncomeCommand = new RelayCommand(async () => await AddIncomeAsync(), () => !IsLoading);
            EditIncomeCommand = new RelayCommand(async () => await EditIncomeAsync(), () => !IsLoading && HasSelectedIncome);
            DeleteIncomeCommand = new RelayCommand(async () => await DeleteIncomeAsync(), () => !IsLoading && HasSelectedIncome);
            SearchIncomeCommand = new RelayCommand(async () => await SearchIncomeAsync(), () => !IsLoading && !string.IsNullOrWhiteSpace(SearchText));
            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsLoading);
            ExportIncomesCommand = new RelayCommand(async () => await ExportIncomesAsync(), () => !IsLoading && HasIncomes);

            // Subscribe to property changes for command updates
            PropertyChanged += OnPropertyChanged;

            // Initialize with recent data
            _ = Task.Run(LoadIncomesAsync);
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Loads income entries for the current date range
        /// </summary>
        private async Task LoadIncomesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                _logger.LogDebug("Loading income entries from {StartDate} to {EndDate}", StartDate, EndDate);

                var query = new GetIncomeByDateRangeQuery(StartDate, EndDate);
                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    Incomes.Clear();
                    foreach (var income in result.Value!.OrderByDescending(i => i.Date).ThenByDescending(i => i.CreatedAt))
                    {
                        Incomes.Add(income);
                    }

                    // Update total
                    await UpdateTotalAsync();

                    _logger.LogDebug("Loaded {Count} income entries", Incomes.Count);
                }
                else
                {
                    ErrorMessage = LocalizationHelper.ErrorMessages.ErrorLoadingIncomes;
                    _logger.LogError("Failed to load income entries: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = LocalizationHelper.StatusMessages.OperationFailed;
                _logger.LogError(ex, "Unexpected error loading income entries");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasIncomes));
            }
        }

        /// <summary>
        /// Opens dialog to add a new income entry
        /// </summary>
        private async Task AddIncomeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var addIncomeDto = new AddIncomeDto { Date = DateTime.Today };
                
                var result = await _dialogService.ShowAddIncomeDialogAsync(addIncomeDto);
                if (result.IsSuccess && result.Value != null)
                {
                    var command = result.Value.ToCommand();
                    var addResult = await _mediator.Send(command);

                    if (addResult.IsSuccess)
                    {
                        await LoadIncomesAsync(); // Refresh the list
                        _logger.LogInformation("Successfully added income entry: {Description}, {Amount:C}", 
                            command.Description, command.Amount);
                    }
                    else
                    {
                        ErrorMessage = LocalizationHelper.ErrorMessages.ErrorAddingIncome;
                        _logger.LogError("Failed to add income entry: {Error}", addResult.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = LocalizationHelper.StatusMessages.OperationFailed;
                _logger.LogError(ex, "Unexpected error adding income entry");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Opens dialog to edit the selected income entry
        /// </summary>
        private async Task EditIncomeAsync()
        {
            if (SelectedIncome == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var updateIncomeDto = new UpdateIncomeDto
                {
                    Id = SelectedIncome.Id,
                    Date = SelectedIncome.Date,
                    Amount = SelectedIncome.Amount,
                    Description = SelectedIncome.Description
                };

                var result = await _dialogService.ShowEditIncomeDialogAsync(updateIncomeDto);
                if (result.IsSuccess && result.Value != null)
                {
                    var command = result.Value.ToCommand();
                    var updateResult = await _mediator.Send(command);

                    if (updateResult.IsSuccess)
                    {
                        await LoadIncomesAsync(); // Refresh the list
                        _logger.LogInformation("Successfully updated income entry ID {IncomeId}", SelectedIncome.Id);
                    }
                    else
                    {
                        ErrorMessage = LocalizationHelper.ErrorMessages.ErrorLoadingIncomes;
                        _logger.LogError("Failed to update income entry: {Error}", updateResult.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = LocalizationHelper.StatusMessages.OperationFailed;
                _logger.LogError(ex, "Unexpected error editing income entry");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deletes the selected income entry after confirmation
        /// </summary>
        private async Task DeleteIncomeAsync()
        {
            if (SelectedIncome == null) return;

            try
            {
                var confirmMessage = $"Are you sure you want to delete the income entry '{SelectedIncome.Description}' for {SelectedIncome.Amount:C}?";

                var confirmed = await _dialogService.ShowConfirmationAsync(
                    LocalizationHelper.GetString("DeleteIncomeTitle", "Delete Income Entry"),
                    confirmMessage);

                if (!confirmed) return;

                IsLoading = true;
                ErrorMessage = null;

                var command = new DeleteIncomeCommand(SelectedIncome.Id);
                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    var deletedIncome = SelectedIncome;
                    SelectedIncome = null;
                    await LoadIncomesAsync(); // Refresh the list
                    
                    _logger.LogInformation("Successfully deleted income entry ID {IncomeId}", deletedIncome.Id);
                }
                else
                {
                    ErrorMessage = LocalizationHelper.ErrorMessages.ErrorLoadingIncomes;
                    _logger.LogError("Failed to delete income entry: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = LocalizationHelper.StatusMessages.OperationFailed;
                _logger.LogError(ex, "Unexpected error deleting income entry");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Searches income entries by description
        /// </summary>
        private async Task SearchIncomeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadIncomesAsync();
                    return;
                }

                _logger.LogDebug("Searching income entries by description: {SearchText}", SearchText);

                var query = new SearchIncomeByDescriptionQuery(SearchText.Trim());
                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    Incomes.Clear();
                    foreach (var income in result.Value!.OrderByDescending(i => i.Date).ThenByDescending(i => i.CreatedAt))
                    {
                        Incomes.Add(income);
                    }

                    TotalIncome = Incomes.Sum(i => i.Amount);
                    _logger.LogDebug("Found {Count} income entries matching search", Incomes.Count);
                }
                else
                {
                    ErrorMessage = LocalizationHelper.StatusMessages.OperationFailed;
                    _logger.LogError("Failed to search income entries: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = LocalizationHelper.StatusMessages.OperationFailed;
                _logger.LogError(ex, "Unexpected error searching income entries");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasIncomes));
            }
        }

        /// <summary>
        /// Refreshes the income list and totals
        /// </summary>
        private async Task RefreshAsync()
        {
            await LoadIncomesAsync();
        }

        /// <summary>
        /// Exports income entries to a file
        /// </summary>
        private async Task ExportIncomesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var result = await _dialogService.ShowExportIncomeDialogAsync();
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully exported {Count} income entries", Incomes.Count);
                }
                else if (result.Error != null)
                {
                    ErrorMessage = LocalizationHelper.StatusMessages.OperationFailed;
                    _logger.LogError("Failed to export income entries: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = LocalizationHelper.StatusMessages.OperationFailed;
                _logger.LogError(ex, "Unexpected error exporting income entries");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Updates the total income for the current date range
        /// </summary>
        private async Task UpdateTotalAsync()
        {
            try
            {
                var query = new GetIncomeTotalQuery(StartDate, EndDate);
                var result = await _mediator.Send(query);
                
                if (result.IsSuccess)
                {
                    TotalIncome = result.Value;
                }
                else
                {
                    _logger.LogWarning("Failed to get income total: {Error}", result.Error);
                    TotalIncome = Incomes.Sum(i => i.Amount); // Fallback calculation
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating income total, using fallback calculation");
                TotalIncome = Incomes.Sum(i => i.Amount); // Fallback calculation
            }
        }

        /// <summary>
        /// Handles property changes to update command states
        /// </summary>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IsLoading):
                case nameof(HasSelectedIncome):
                case nameof(HasIncomes):
                case nameof(SearchText):
                    CommandManager.InvalidateRequerySuggested();
                    break;
            }
        }

        #endregion

        #region Statistics and Analytics Properties

        /// <summary>
        /// Average income amount for current entries
        /// </summary>
        public decimal AverageIncome => HasIncomes ? Incomes.Average(i => i.Amount) : 0;

        /// <summary>
        /// Minimum income amount for current entries
        /// </summary>
        public decimal MinIncome => HasIncomes ? Incomes.Min(i => i.Amount) : 0;

        /// <summary>
        /// Maximum income amount for current entries
        /// </summary>
        public decimal MaxIncome => HasIncomes ? Incomes.Max(i => i.Amount) : 0;

        /// <summary>
        /// Number of days with income in the current range
        /// </summary>
        public int DaysWithIncome => Incomes.Select(i => i.Date.Date).Distinct().Count();

        /// <summary>
        /// Average daily income for the current date range
        /// </summary>
        public decimal AverageDailyIncome
        {
            get
            {
                var totalDays = Math.Max(1, (EndDate - StartDate).Days + 1);
                return TotalIncome / totalDays;
            }
        }

        #endregion
    }
}