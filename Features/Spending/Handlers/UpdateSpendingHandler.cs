// Update Spending Command Handler - Vertical Slice Architecture
// File: Features/Spending/Handlers/UpdateSpendingHandler.cs

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
    /// Handler for updating existing spending entries with caching invalidation
    /// </summary>
    public class UpdateSpendingHandler : IRequestHandler<UpdateSpendingCommand, Result<Models.Spending>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<UpdateSpendingHandler> _logger;

        public UpdateSpendingHandler(
            ISpendingRepository spendingRepository,
            ICategoryRepository categoryRepository,
            IBudgetCacheService cacheService,
            IApplicationLoggingService loggingService,
            ILogger<UpdateSpendingHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Models.Spending>> Handle(UpdateSpendingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Updating spending entry ID {SpendingId}: Amount={Amount}, Date={Date}, Description={Description}, CategoryId={CategoryId}", 
                    request.Id, request.Amount, request.Date, request.Description, request.CategoryId);

                // Get the existing spending entry
                var existingResult = await _spendingRepository.GetByIdAsync(request.Id, cancellationToken);
                if (existingResult.IsFailure)
                {
                    _logger.LogWarning("Spending entry not found for update: ID {SpendingId}", request.Id);
                    return Result<Models.Spending>.Failure(existingResult.Error!);
                }

                var existingSpending = existingResult.Value!;
                var originalDate = existingSpending.Date;
                var originalCategoryId = existingSpending.CategoryId;

                // Validate new category exists if it's different
                if (request.CategoryId != originalCategoryId)
                {
                    var categoryResult = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                    if (categoryResult.IsFailure)
                    {
                        _logger.LogWarning("Category not found for spending update: CategoryId={CategoryId}", request.CategoryId);
                        return Result<Models.Spending>.Failure(Error.NotFound(
                            Error.Codes.NOT_FOUND, 
                            "Category not found",
                            new Dictionary<string, object> { ["CategoryId"] = request.CategoryId }));
                    }
                }

                // Get category name for logging
                var categoryForLogging = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                var categoryName = categoryForLogging.IsSuccess ? categoryForLogging.Value!.Name : "Unknown";

                // Update the spending entry properties
                existingSpending.Date = request.Date;
                existingSpending.Amount = request.Amount;
                existingSpending.Description = request.Description;
                existingSpending.CategoryId = request.CategoryId;
                existingSpending.UpdatedAt = DateTime.UtcNow;

                // Update in repository
                var updateResult = await _spendingRepository.UpdateAsync(existingSpending, cancellationToken);
                if (updateResult.IsFailure)
                {
                    _logger.LogError("Failed to update spending entry ID {SpendingId}: {Error}", request.Id, updateResult.Error);
                    return Result<Models.Spending>.Failure(updateResult.Error!);
                }

                var updatedSpending = updateResult.Value!;

                // Invalidate relevant caches (both old and new dates/categories if changed)
                await InvalidateRelatedCaches(originalDate, updatedSpending.Date, originalCategoryId, updatedSpending.CategoryId, cancellationToken);

                // Log the business operation
                await _loggingService.LogSpendingUpdatedAsync(
                    updatedSpending.Id,
                    updatedSpending.Amount,
                    updatedSpending.Date,
                    updatedSpending.Description,
                    categoryName,
                    cancellationToken);

                _logger.LogInformation("Successfully updated spending entry ID {SpendingId}", updatedSpending.Id);
                return Result<Models.Spending>.Success(updatedSpending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating spending entry ID {SpendingId}", request.Id);
                return Result<Models.Spending>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to update spending entry", 
                    new Dictionary<string, object>
                    {
                        ["Id"] = request.Id,
                        ["Amount"] = request.Amount,
                        ["Date"] = request.Date,
                        ["Description"] = request.Description,
                        ["CategoryId"] = request.CategoryId
                    }));
            }
        }

        private async Task InvalidateRelatedCaches(DateTime originalDate, DateTime newDate, int originalCategoryId, int newCategoryId, CancellationToken cancellationToken)
        {
            try
            {
                // Invalidate caches for both dates if they're different
                if (originalDate.Date != newDate.Date)
                {
                    await _cacheService.InvalidateSpendingAsync(originalDate, originalCategoryId, cancellationToken);
                    await _cacheService.InvalidateSpendingAsync(newDate, newCategoryId, cancellationToken);
                }
                else
                {
                    await _cacheService.InvalidateSpendingAsync(newDate, newCategoryId, cancellationToken);
                }

                // Also invalidate for original category if it changed
                if (originalCategoryId != newCategoryId)
                {
                    await _cacheService.InvalidateSpendingAsync(originalDate, originalCategoryId, cancellationToken);
                }
                
                // Invalidate dashboard caches since totals may have changed
                await _cacheService.InvalidateDashboardSummariesAsync(cancellationToken);
                
                _logger.LogDebug("Invalidated caches for dates {OriginalDate}->{NewDate} and categories {OriginalCategoryId}->{NewCategoryId}", 
                    originalDate, newDate, originalCategoryId, newCategoryId);
            }
            catch (Exception ex)
            {
                // Don't fail the operation if cache invalidation fails
                _logger.LogWarning(ex, "Failed to invalidate caches after updating spending");
            }
        }
    }
}