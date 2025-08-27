// Add Spending Command Handler - Vertical Slice Architecture
// File: Features/Spending/Handlers/AddSpendingHandler.cs

using BudgetManagement.Features.Spending.Commands;
using BudgetManagement.Models;
using BudgetManagement.Services;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;
using BudgetManagement.Shared.Infrastructure;
using BudgetManagement.Shared.Infrastructure.Caching;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Features.Spending.Handlers
{
    /// <summary>
    /// Handler for adding new spending entries with caching invalidation
    /// </summary>
    public class AddSpendingHandler : IRequestHandler<AddSpendingCommand, Result<Models.Spending>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<AddSpendingHandler> _logger;

        public AddSpendingHandler(
            ISpendingRepository spendingRepository,
            ICategoryRepository categoryRepository,
            IBudgetCacheService cacheService,
            IApplicationLoggingService loggingService,
            ILogger<AddSpendingHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Models.Spending>> Handle(AddSpendingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Adding spending entry: Amount={Amount}, Date={Date}, Description={Description}, CategoryId={CategoryId}", 
                    request.Amount, request.Date, request.Description, request.CategoryId);

                // Validate category exists
                var categoryResult = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (categoryResult.IsFailure)
                {
                    _logger.LogWarning("Category not found for spending entry: CategoryId={CategoryId}", request.CategoryId);
                    return Result<Models.Spending>.Failure(Error.NotFound(
                        Error.Codes.NOT_FOUND, 
                        "Category not found",
                        new Dictionary<string, object> { ["CategoryId"] = request.CategoryId }));
                }

                var category = categoryResult.Value!;

                // Create new spending entry
                var spending = new Models.Spending
                {
                    Date = request.Date,
                    Amount = request.Amount,
                    Description = request.Description,
                    CategoryId = request.CategoryId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add to repository
                var addResult = await _spendingRepository.AddAsync(spending, cancellationToken);
                if (addResult.IsFailure)
                {
                    _logger.LogError("Failed to add spending entry: {Error}", addResult.Error);
                    return Result<Models.Spending>.Failure(addResult.Error!);
                }

                var addedSpending = addResult.Value!;

                // Invalidate relevant caches
                await InvalidateRelatedCaches(addedSpending.Date, addedSpending.CategoryId, cancellationToken);

                // Log the business operation
                await _loggingService.LogSpendingAddedAsync(
                    addedSpending.Id,
                    addedSpending.Amount,
                    addedSpending.Date,
                    addedSpending.Description,
                    category.Name,
                    cancellationToken);

                _logger.LogInformation("Successfully added spending entry with ID {SpendingId}", addedSpending.Id);
                return Result<Models.Spending>.Success(addedSpending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding spending entry");
                return Result<Models.Spending>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to add spending entry", 
                    new Dictionary<string, object>
                    {
                        ["Amount"] = request.Amount,
                        ["Date"] = request.Date,
                        ["Description"] = request.Description,
                        ["CategoryId"] = request.CategoryId
                    }));
            }
        }

        private async Task InvalidateRelatedCaches(DateTime affectedDate, int categoryId, CancellationToken cancellationToken)
        {
            try
            {
                // Invalidate spending-specific caches
                await _cacheService.InvalidateSpendingAsync(affectedDate, categoryId, cancellationToken);
                
                // Invalidate dashboard caches since totals changed
                await _cacheService.InvalidateDashboardSummariesAsync(cancellationToken);
                
                _logger.LogDebug("Invalidated caches for date {AffectedDate} and category {CategoryId}", affectedDate, categoryId);
            }
            catch (Exception ex)
            {
                // Don't fail the operation if cache invalidation fails
                _logger.LogWarning(ex, "Failed to invalidate caches after adding spending");
            }
        }
    }
}