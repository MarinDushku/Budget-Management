// Get Income Query Handlers - Vertical Slice Architecture
// File: Features/Income/Handlers/GetIncomeHandler.cs

using BudgetManagement.Features.Income.Queries;
using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;
using BudgetManagement.Shared.Infrastructure.Caching;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Features.Income.Handlers
{
    /// <summary>
    /// Handler for getting income entries by date range with caching
    /// </summary>
    public class GetIncomeByDateRangeHandler : IRequestHandler<GetIncomeByDateRangeQuery, Result<IEnumerable<Models.Income>>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly ILogger<GetIncomeByDateRangeHandler> _logger;

        public GetIncomeByDateRangeHandler(
            IIncomeRepository incomeRepository,
            IBudgetCacheService cacheService,
            ILogger<GetIncomeByDateRangeHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<Models.Income>>> Handle(GetIncomeByDateRangeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting income entries from {StartDate} to {EndDate}", request.StartDate, request.EndDate);

                // Try cache first
                var cachedResult = await _cacheService.GetIncomeAsync(request.StartDate, request.EndDate, cancellationToken);
                if (cachedResult.IsSuccess && cachedResult.Value != null)
                {
                    _logger.LogDebug("Retrieved {Count} income entries from cache", cachedResult.Value.Count());
                    return Result<IEnumerable<Models.Income>>.Success(cachedResult.Value);
                }

                // Cache miss, get from repository
                var repositoryResult = await _incomeRepository.GetByDateRangeAsync(request.StartDate, request.EndDate, cancellationToken);
                if (repositoryResult.IsFailure)
                {
                    _logger.LogError("Failed to get income entries from repository: {Error}", repositoryResult.Error);
                    return repositoryResult;
                }

                var incomes = repositoryResult.Value!;
                
                // Cache the result
                await _cacheService.SetIncomeAsync(request.StartDate, request.EndDate, incomes, cancellationToken);

                _logger.LogDebug("Retrieved {Count} income entries from repository", incomes.Count());
                return Result<IEnumerable<Models.Income>>.Success(incomes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting income entries by date range");
                return Result<IEnumerable<Models.Income>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get income entries", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting a single income entry by ID
    /// </summary>
    public class GetIncomeByIdHandler : IRequestHandler<GetIncomeByIdQuery, Result<Models.Income>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly ILogger<GetIncomeByIdHandler> _logger;

        public GetIncomeByIdHandler(
            IIncomeRepository incomeRepository,
            ILogger<GetIncomeByIdHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Models.Income>> Handle(GetIncomeByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting income entry by ID {IncomeId}", request.Id);

                var result = await _incomeRepository.GetByIdAsync(request.Id, cancellationToken);
                if (result.IsFailure)
                {
                    _logger.LogWarning("Income entry not found: ID {IncomeId}", request.Id);
                }
                else
                {
                    _logger.LogDebug("Retrieved income entry ID {IncomeId}", request.Id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting income entry by ID {IncomeId}", request.Id);
                return Result<Models.Income>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get income entry", 
                    new Dictionary<string, object>
                    {
                        ["Id"] = request.Id
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting recent income entries
    /// </summary>
    public class GetRecentIncomeHandler : IRequestHandler<GetRecentIncomeQuery, Result<IEnumerable<Models.Income>>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly ILogger<GetRecentIncomeHandler> _logger;

        public GetRecentIncomeHandler(
            IIncomeRepository incomeRepository,
            ILogger<GetRecentIncomeHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<Models.Income>>> Handle(GetRecentIncomeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting {Count} most recent income entries", request.Count);

                var result = await _incomeRepository.GetMostRecentAsync(request.Count, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Retrieved {ActualCount} recent income entries", result.Value!.Count());
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting recent income entries");
                return Result<IEnumerable<Models.Income>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get recent income entries", 
                    new Dictionary<string, object>
                    {
                        ["Count"] = request.Count
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting income total for a date range
    /// </summary>
    public class GetIncomeTotalHandler : IRequestHandler<GetIncomeTotalQuery, Result<decimal>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly ILogger<GetIncomeTotalHandler> _logger;

        public GetIncomeTotalHandler(
            IIncomeRepository incomeRepository,
            ILogger<GetIncomeTotalHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<decimal>> Handle(GetIncomeTotalQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting income total from {StartDate} to {EndDate}", request.StartDate, request.EndDate);

                var result = await _incomeRepository.GetTotalAsync(request.StartDate, request.EndDate, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Income total: {Total:C}", result.Value);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting income total");
                return Result<decimal>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get income total", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting income statistics
    /// </summary>
    public class GetIncomeStatisticsHandler : IRequestHandler<GetIncomeStatisticsQuery, Result<IncomeStatistics>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly ILogger<GetIncomeStatisticsHandler> _logger;

        public GetIncomeStatisticsHandler(
            IIncomeRepository incomeRepository,
            ILogger<GetIncomeStatisticsHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IncomeStatistics>> Handle(GetIncomeStatisticsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting income statistics from {StartDate} to {EndDate}", request.StartDate, request.EndDate);

                var result = await _incomeRepository.GetStatisticsAsync(request.StartDate, request.EndDate, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Income statistics: {TotalEntries} entries, Total: {Total:C}, Average: {Average:C}", 
                        result.Value!.TotalEntries, result.Value.TotalAmount, result.Value.AverageAmount);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting income statistics");
                return Result<IncomeStatistics>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get income statistics", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting income trend data
    /// </summary>
    public class GetIncomeTrendHandler : IRequestHandler<GetIncomeTrendQuery, Result<IEnumerable<IncomeTrendData>>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly ILogger<GetIncomeTrendHandler> _logger;

        public GetIncomeTrendHandler(
            IIncomeRepository incomeRepository,
            ILogger<GetIncomeTrendHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<IncomeTrendData>>> Handle(GetIncomeTrendQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting income trend data from {StartDate} to {EndDate}, Grouping: {Grouping}", 
                    request.StartDate, request.EndDate, request.Grouping);

                var result = await _incomeRepository.GetTrendDataAsync(request.StartDate, request.EndDate, request.Grouping, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Retrieved {Count} trend data points", result.Value!.Count());
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting income trend data");
                return Result<IEnumerable<IncomeTrendData>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get income trend data", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate,
                        ["Grouping"] = request.Grouping.ToString()
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting monthly income totals
    /// </summary>
    public class GetMonthlyIncomeTotalsHandler : IRequestHandler<GetMonthlyIncomeTotalsQuery, Result<IEnumerable<MonthlyIncomeSummary>>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly ILogger<GetMonthlyIncomeTotalsHandler> _logger;

        public GetMonthlyIncomeTotalsHandler(
            IIncomeRepository incomeRepository,
            ILogger<GetMonthlyIncomeTotalsHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<MonthlyIncomeSummary>>> Handle(GetMonthlyIncomeTotalsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting monthly income totals for year {Year}", request.Year);

                var result = await _incomeRepository.GetMonthlyTotalsAsync(request.Year, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Retrieved {Count} monthly income summaries", result.Value!.Count());
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting monthly income totals");
                return Result<IEnumerable<MonthlyIncomeSummary>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get monthly income totals", 
                    new Dictionary<string, object>
                    {
                        ["Year"] = request.Year
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for searching income by description pattern
    /// </summary>
    public class SearchIncomeByDescriptionHandler : IRequestHandler<SearchIncomeByDescriptionQuery, Result<IEnumerable<Models.Income>>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly ILogger<SearchIncomeByDescriptionHandler> _logger;

        public SearchIncomeByDescriptionHandler(
            IIncomeRepository incomeRepository,
            ILogger<SearchIncomeByDescriptionHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<Models.Income>>> Handle(SearchIncomeByDescriptionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Searching income entries by description pattern: {Pattern}", request.Pattern);

                var result = await _incomeRepository.GetByDescriptionPatternAsync(request.Pattern, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Found {Count} income entries matching pattern", result.Value!.Count());
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error searching income by description");
                return Result<IEnumerable<Models.Income>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to search income entries", 
                    new Dictionary<string, object>
                    {
                        ["Pattern"] = request.Pattern
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting income by amount range
    /// </summary>
    public class GetIncomeByAmountRangeHandler : IRequestHandler<GetIncomeByAmountRangeQuery, Result<IEnumerable<Models.Income>>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly ILogger<GetIncomeByAmountRangeHandler> _logger;

        public GetIncomeByAmountRangeHandler(
            IIncomeRepository incomeRepository,
            ILogger<GetIncomeByAmountRangeHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<Models.Income>>> Handle(GetIncomeByAmountRangeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting income entries by amount range: {MinAmount:C} - {MaxAmount:C}", request.MinAmount, request.MaxAmount);

                var result = await _incomeRepository.GetByAmountRangeAsync(request.MinAmount, request.MaxAmount, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Found {Count} income entries in amount range", result.Value!.Count());
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting income by amount range");
                return Result<IEnumerable<Models.Income>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get income entries by amount range", 
                    new Dictionary<string, object>
                    {
                        ["MinAmount"] = request.MinAmount,
                        ["MaxAmount"] = request.MaxAmount
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for advanced income search with multiple criteria and pagination
    /// </summary>
    public class AdvancedIncomeSearchHandler : IRequestHandler<AdvancedIncomeSearchQuery, Result<AdvancedIncomeSearchResult>>
    {
        private readonly BudgetManagement.Services.IBudgetService _budgetService;
        private readonly ILogger<AdvancedIncomeSearchHandler> _logger;

        public AdvancedIncomeSearchHandler(
            BudgetManagement.Services.IBudgetService budgetService,
            ILogger<AdvancedIncomeSearchHandler> logger)
        {
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AdvancedIncomeSearchResult>> Handle(AdvancedIncomeSearchQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Executing advanced income search with criteria: Pattern='{Pattern}', StartDate={StartDate}, EndDate={EndDate}, MinAmount={MinAmount}, MaxAmount={MaxAmount}, Skip={Skip}, Take={Take}",
                    request.DescriptionPattern, request.StartDate, request.EndDate, request.MinAmount, request.MaxAmount, request.Skip, request.Take);

                var result = await _budgetService.AdvancedIncomeSearchAsync(
                    request.DescriptionPattern,
                    request.StartDate,
                    request.EndDate,
                    request.MinAmount,
                    request.MaxAmount,
                    request.Skip,
                    request.Take,
                    request.SortBy,
                    request.SortDirection);

                _logger.LogDebug("Advanced income search completed successfully. Found {TotalCount} total entries, returning {CurrentCount} entries",
                    result.TotalCount, result.Incomes.Count());

                return Result<AdvancedIncomeSearchResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during advanced income search");
                return Result<AdvancedIncomeSearchResult>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to execute advanced income search", 
                    new Dictionary<string, object>
                    {
                        ["DescriptionPattern"] = request.DescriptionPattern ?? "null",
                        ["StartDate"] = request.StartDate?.ToString("yyyy-MM-dd") ?? "null",
                        ["EndDate"] = request.EndDate?.ToString("yyyy-MM-dd") ?? "null",
                        ["MinAmount"] = request.MinAmount?.ToString() ?? "null",
                        ["MaxAmount"] = request.MaxAmount?.ToString() ?? "null",
                        ["Skip"] = request.Skip,
                        ["Take"] = request.Take,
                        ["SortBy"] = request.SortBy.ToString(),
                        ["SortDirection"] = request.SortDirection.ToString()
                    }));
            }
        }
    }
}