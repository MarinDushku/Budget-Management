// Get Spending Query Handlers - Vertical Slice Architecture
// File: Features/Spending/Handlers/GetSpendingHandler.cs

using BudgetManagement.Features.Spending.Queries;
using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;
using BudgetManagement.Shared.Infrastructure.Caching;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Features.Spending.Handlers
{
    /// <summary>
    /// Handler for getting spending entries by date range with caching
    /// </summary>
    public class GetSpendingByDateRangeHandler : IRequestHandler<GetSpendingByDateRangeQuery, Result<IEnumerable<Models.Spending>>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly ILogger<GetSpendingByDateRangeHandler> _logger;

        public GetSpendingByDateRangeHandler(
            ISpendingRepository spendingRepository,
            IBudgetCacheService cacheService,
            ILogger<GetSpendingByDateRangeHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<Models.Spending>>> Handle(GetSpendingByDateRangeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting spending entries from {StartDate} to {EndDate}", request.StartDate, request.EndDate);

                // Try cache first
                var cachedResult = await _cacheService.GetSpendingAsync(request.StartDate, request.EndDate, cancellationToken);
                if (cachedResult.IsSuccess && cachedResult.Value != null)
                {
                    _logger.LogDebug("Retrieved {Count} spending entries from cache", cachedResult.Value.Count());
                    return Result<IEnumerable<Models.Spending>>.Success(cachedResult.Value);
                }

                // Cache miss, get from repository
                var repositoryResult = await _spendingRepository.GetByDateRangeAsync(request.StartDate, request.EndDate, cancellationToken);
                if (repositoryResult.IsFailure)
                {
                    _logger.LogError("Failed to get spending entries from repository: {Error}", repositoryResult.Error);
                    return repositoryResult;
                }

                var spendings = repositoryResult.Value!;
                
                // Cache the result
                await _cacheService.SetSpendingAsync(request.StartDate, request.EndDate, spendings, cancellationToken);

                _logger.LogDebug("Retrieved {Count} spending entries from repository", spendings.Count());
                return Result<IEnumerable<Models.Spending>>.Success(spendings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting spending entries by date range");
                return Result<IEnumerable<Models.Spending>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get spending entries", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting spending entries with category information by date range
    /// </summary>
    public class GetSpendingWithCategoryByDateRangeHandler : IRequestHandler<GetSpendingWithCategoryByDateRangeQuery, Result<IEnumerable<SpendingWithCategory>>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly ILogger<GetSpendingWithCategoryByDateRangeHandler> _logger;

        public GetSpendingWithCategoryByDateRangeHandler(
            ISpendingRepository spendingRepository,
            IBudgetCacheService cacheService,
            ILogger<GetSpendingWithCategoryByDateRangeHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<SpendingWithCategory>>> Handle(GetSpendingWithCategoryByDateRangeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting spending with category from {StartDate} to {EndDate}", request.StartDate, request.EndDate);

                // Try cache first
                var cachedResult = await _cacheService.GetSpendingWithCategoryAsync(request.StartDate, request.EndDate, cancellationToken);
                if (cachedResult.IsSuccess && cachedResult.Value != null)
                {
                    _logger.LogDebug("Retrieved {Count} spending with category entries from cache", cachedResult.Value.Count());
                    return Result<IEnumerable<SpendingWithCategory>>.Success(cachedResult.Value);
                }

                // Cache miss, get from repository
                var repositoryResult = await _spendingRepository.GetWithCategoryAsync(request.StartDate, request.EndDate, cancellationToken);
                if (repositoryResult.IsFailure)
                {
                    _logger.LogError("Failed to get spending with category from repository: {Error}", repositoryResult.Error);
                    return repositoryResult;
                }

                var spendingsWithCategory = repositoryResult.Value!;
                
                // Cache the result
                await _cacheService.SetSpendingWithCategoryAsync(request.StartDate, request.EndDate, spendingsWithCategory, cancellationToken);

                _logger.LogDebug("Retrieved {Count} spending with category entries from repository", spendingsWithCategory.Count());
                return Result<IEnumerable<SpendingWithCategory>>.Success(spendingsWithCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting spending with category by date range");
                return Result<IEnumerable<SpendingWithCategory>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get spending with category entries", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting a single spending entry by ID
    /// </summary>
    public class GetSpendingByIdHandler : IRequestHandler<GetSpendingByIdQuery, Result<Models.Spending>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly ILogger<GetSpendingByIdHandler> _logger;

        public GetSpendingByIdHandler(
            ISpendingRepository spendingRepository,
            ILogger<GetSpendingByIdHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Models.Spending>> Handle(GetSpendingByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting spending entry by ID {SpendingId}", request.Id);

                var result = await _spendingRepository.GetByIdAsync(request.Id, cancellationToken);
                if (result.IsFailure)
                {
                    _logger.LogWarning("Spending entry not found: ID {SpendingId}", request.Id);
                }
                else
                {
                    _logger.LogDebug("Retrieved spending entry ID {SpendingId}", request.Id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting spending entry by ID {SpendingId}", request.Id);
                return Result<Models.Spending>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get spending entry", 
                    new Dictionary<string, object>
                    {
                        ["Id"] = request.Id
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting recent spending entries
    /// </summary>
    public class GetRecentSpendingHandler : IRequestHandler<GetRecentSpendingQuery, Result<IEnumerable<Models.Spending>>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly ILogger<GetRecentSpendingHandler> _logger;

        public GetRecentSpendingHandler(
            ISpendingRepository spendingRepository,
            ILogger<GetRecentSpendingHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<Models.Spending>>> Handle(GetRecentSpendingQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting {Count} most recent spending entries", request.Count);

                var result = await _spendingRepository.GetMostRecentAsync(request.Count, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Retrieved {ActualCount} recent spending entries", result.Value!.Count());
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting recent spending entries");
                return Result<IEnumerable<Models.Spending>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get recent spending entries", 
                    new Dictionary<string, object>
                    {
                        ["Count"] = request.Count
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting spending total for a date range
    /// </summary>
    public class GetSpendingTotalHandler : IRequestHandler<GetSpendingTotalQuery, Result<decimal>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly ILogger<GetSpendingTotalHandler> _logger;

        public GetSpendingTotalHandler(
            ISpendingRepository spendingRepository,
            ILogger<GetSpendingTotalHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<decimal>> Handle(GetSpendingTotalQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting spending total from {StartDate} to {EndDate}", request.StartDate, request.EndDate);

                var result = await _spendingRepository.GetTotalAsync(request.StartDate, request.EndDate, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Spending total: {Total:C}", result.Value);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting spending total");
                return Result<decimal>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get spending total", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting spending total by category for a date range
    /// </summary>
    public class GetSpendingTotalByCategoryHandler : IRequestHandler<GetSpendingTotalByCategoryQuery, Result<decimal>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly ILogger<GetSpendingTotalByCategoryHandler> _logger;

        public GetSpendingTotalByCategoryHandler(
            ISpendingRepository spendingRepository,
            ILogger<GetSpendingTotalByCategoryHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<decimal>> Handle(GetSpendingTotalByCategoryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting spending total for category {CategoryId} from {StartDate} to {EndDate}", 
                    request.CategoryId, request.StartDate, request.EndDate);

                var result = await _spendingRepository.GetTotalByCategoryAsync(request.CategoryId, request.StartDate, request.EndDate, cancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Spending total for category {CategoryId}: {Total:C}", request.CategoryId, result.Value);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting spending total by category");
                return Result<decimal>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get spending total by category", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate,
                        ["CategoryId"] = request.CategoryId
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for getting spending by category
    /// </summary>
    public class GetSpendingByCategoryHandler : IRequestHandler<GetSpendingByCategoryQuery, Result<IEnumerable<Models.Spending>>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly ILogger<GetSpendingByCategoryHandler> _logger;

        public GetSpendingByCategoryHandler(
            ISpendingRepository spendingRepository,
            ILogger<GetSpendingByCategoryHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<Models.Spending>>> Handle(GetSpendingByCategoryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting spending entries for category {CategoryId}", request.CategoryId);

                var result = await _spendingRepository.GetByCategoryAsync(
                    request.CategoryId, 
                    request.StartDate, 
                    request.EndDate, 
                    cancellationToken);
                
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Retrieved {Count} spending entries for category {CategoryId}", result.Value!.Count(), request.CategoryId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting spending entries by category");
                return Result<IEnumerable<Models.Spending>>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to get spending entries by category", 
                    new Dictionary<string, object>
                    {
                        ["CategoryId"] = request.CategoryId,
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate
                    }));
            }
        }
    }

    /// <summary>
    /// Handler for advanced spending search with multiple criteria and pagination
    /// </summary>
    public class AdvancedSpendingSearchHandler : IRequestHandler<AdvancedSpendingSearchQuery, Result<AdvancedSpendingSearchResult>>
    {
        private readonly BudgetManagement.Services.IBudgetService _budgetService;
        private readonly ILogger<AdvancedSpendingSearchHandler> _logger;

        public AdvancedSpendingSearchHandler(
            BudgetManagement.Services.IBudgetService budgetService,
            ILogger<AdvancedSpendingSearchHandler> logger)
        {
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AdvancedSpendingSearchResult>> Handle(AdvancedSpendingSearchQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Executing advanced spending search with criteria: Pattern='{Pattern}', StartDate={StartDate}, EndDate={EndDate}, MinAmount={MinAmount}, MaxAmount={MaxAmount}, CategoryIds=[{CategoryIds}], Skip={Skip}, Take={Take}",
                    request.DescriptionPattern, request.StartDate, request.EndDate, request.MinAmount, request.MaxAmount, 
                    request.CategoryIds != null ? string.Join(",", request.CategoryIds) : "null", request.Skip, request.Take);

                var result = await _budgetService.AdvancedSpendingSearchAsync(
                    request.DescriptionPattern,
                    request.StartDate,
                    request.EndDate,
                    request.MinAmount,
                    request.MaxAmount,
                    request.CategoryIds,
                    request.Skip,
                    request.Take,
                    request.SortBy,
                    request.SortDirection);

                _logger.LogDebug("Advanced spending search completed successfully. Found {TotalCount} total entries, returning {CurrentCount} entries, TotalAmount={TotalAmount}",
                    result.TotalCount, result.Spendings.Count(), result.TotalAmount);

                return Result<AdvancedSpendingSearchResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during advanced spending search");
                return Result<AdvancedSpendingSearchResult>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to execute advanced spending search", 
                    new Dictionary<string, object>
                    {
                        ["DescriptionPattern"] = request.DescriptionPattern ?? "null",
                        ["StartDate"] = request.StartDate?.ToString("yyyy-MM-dd") ?? "null",
                        ["EndDate"] = request.EndDate?.ToString("yyyy-MM-dd") ?? "null",
                        ["MinAmount"] = request.MinAmount?.ToString() ?? "null",
                        ["MaxAmount"] = request.MaxAmount?.ToString() ?? "null",
                        ["CategoryIds"] = request.CategoryIds != null ? string.Join(",", request.CategoryIds) : "null",
                        ["Skip"] = request.Skip,
                        ["Take"] = request.Take,
                        ["SortBy"] = request.SortBy.ToString(),
                        ["SortDirection"] = request.SortDirection.ToString()
                    }));
            }
        }
    }
}