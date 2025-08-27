// Delete Spending Command Handler - Vertical Slice Architecture
// File: Features/Spending/Handlers/DeleteSpendingHandler.cs

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
    /// Handler for deleting spending entries with caching invalidation
    /// </summary>
    public class DeleteSpendingHandler : IRequestHandler<DeleteSpendingCommand, Result>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<DeleteSpendingHandler> _logger;

        public DeleteSpendingHandler(
            ISpendingRepository spendingRepository,
            ICategoryRepository categoryRepository,
            IBudgetCacheService cacheService,
            IApplicationLoggingService loggingService,
            ILogger<DeleteSpendingHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(DeleteSpendingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Deleting spending entry ID {SpendingId}", request.Id);

                // Get the existing spending entry first to capture info for logging
                var existingResult = await _spendingRepository.GetByIdAsync(request.Id, cancellationToken);
                if (existingResult.IsFailure)
                {
                    _logger.LogWarning("Spending entry not found for deletion: ID {SpendingId}", request.Id);
                    return Result.Failure(existingResult.Error!);
                }

                var existingSpending = existingResult.Value!;
                var affectedDate = existingSpending.Date;
                var affectedCategoryId = existingSpending.CategoryId;

                // Get category name for logging
                var categoryResult = await _categoryRepository.GetByIdAsync(affectedCategoryId, cancellationToken);
                var categoryName = categoryResult.IsSuccess ? categoryResult.Value!.Name : "Unknown";

                // Delete from repository
                var deleteResult = await _spendingRepository.DeleteAsync(request.Id, cancellationToken);
                if (deleteResult.IsFailure)
                {
                    _logger.LogError("Failed to delete spending entry ID {SpendingId}: {Error}", request.Id, deleteResult.Error);
                    return Result.Failure(deleteResult.Error!);
                }

                // Invalidate relevant caches
                await InvalidateRelatedCaches(affectedDate, affectedCategoryId, cancellationToken);

                // Log the business operation
                await _loggingService.LogSpendingDeletedAsync(
                    existingSpending.Id,
                    existingSpending.Amount,
                    existingSpending.Date,
                    existingSpending.Description,
                    categoryName,
                    cancellationToken);

                _logger.LogInformation("Successfully deleted spending entry ID {SpendingId}", request.Id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting spending entry ID {SpendingId}", request.Id);
                return Result.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to delete spending entry", 
                    new Dictionary<string, object>
                    {
                        ["Id"] = request.Id
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
                _logger.LogWarning(ex, "Failed to invalidate caches after deleting spending");
            }
        }
    }

    /// <summary>
    /// Handler for deleting spending entries by date range
    /// </summary>
    public class DeleteSpendingByDateRangeHandler : IRequestHandler<DeleteSpendingByDateRangeCommand, Result<int>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<DeleteSpendingByDateRangeHandler> _logger;

        public DeleteSpendingByDateRangeHandler(
            ISpendingRepository spendingRepository,
            IBudgetCacheService cacheService,
            IApplicationLoggingService loggingService,
            ILogger<DeleteSpendingByDateRangeHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<int>> Handle(DeleteSpendingByDateRangeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Deleting spending entries from {StartDate} to {EndDate}", request.StartDate, request.EndDate);

                // Delete from repository
                var deleteResult = await _spendingRepository.DeleteByDateRangeAsync(request.StartDate, request.EndDate, cancellationToken);
                if (deleteResult.IsFailure)
                {
                    _logger.LogError("Failed to delete spending entries by date range: {Error}", deleteResult.Error);
                    return Result<int>.Failure(deleteResult.Error!);
                }

                var deletedCount = deleteResult.Value;

                // Invalidate relevant caches
                await InvalidateRelatedCaches(request.StartDate, request.EndDate, cancellationToken);

                // Log the business operation
                await _loggingService.LogBulkSpendingDeletedAsync(
                    request.StartDate,
                    request.EndDate,
                    deletedCount,
                    cancellationToken);

                _logger.LogInformation("Successfully deleted {DeletedCount} spending entries from {StartDate} to {EndDate}", 
                    deletedCount, request.StartDate, request.EndDate);
                
                return Result<int>.Success(deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting spending entries by date range");
                return Result<int>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to delete spending entries by date range", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = request.StartDate,
                        ["EndDate"] = request.EndDate
                    }));
            }
        }

        private async Task InvalidateRelatedCaches(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            try
            {
                // Invalidate spending-specific caches for the entire range
                await _cacheService.InvalidateSpendingAsync(null, null, cancellationToken); // Invalidate all spending cache
                
                // Invalidate dashboard caches since totals changed
                await _cacheService.InvalidateDashboardSummariesAsync(cancellationToken);
                
                _logger.LogDebug("Invalidated caches for date range {StartDate} to {EndDate}", startDate, endDate);
            }
            catch (Exception ex)
            {
                // Don't fail the operation if cache invalidation fails
                _logger.LogWarning(ex, "Failed to invalidate caches after bulk deleting spending");
            }
        }
    }

    /// <summary>
    /// Handler for deleting spending entries by category
    /// </summary>
    public class DeleteSpendingByCategoryHandler : IRequestHandler<DeleteSpendingByCategoryCommand, Result<int>>
    {
        private readonly ISpendingRepository _spendingRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<DeleteSpendingByCategoryHandler> _logger;

        public DeleteSpendingByCategoryHandler(
            ISpendingRepository spendingRepository,
            ICategoryRepository categoryRepository,
            IBudgetCacheService cacheService,
            IApplicationLoggingService loggingService,
            ILogger<DeleteSpendingByCategoryHandler> logger)
        {
            _spendingRepository = spendingRepository ?? throw new ArgumentNullException(nameof(spendingRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<int>> Handle(DeleteSpendingByCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Deleting spending entries for category ID {CategoryId}", request.CategoryId);

                // Verify category exists
                var categoryResult = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (categoryResult.IsFailure)
                {
                    _logger.LogWarning("Category not found for bulk deletion: CategoryId={CategoryId}", request.CategoryId);
                    return Result<int>.Failure(Error.NotFound(
                        Error.Codes.NOT_FOUND, 
                        "Category not found",
                        new Dictionary<string, object> { ["CategoryId"] = request.CategoryId }));
                }

                var category = categoryResult.Value!;

                // Delete from repository
                var deleteResult = await _spendingRepository.DeleteByCategoryAsync(request.CategoryId, cancellationToken);
                if (deleteResult.IsFailure)
                {
                    _logger.LogError("Failed to delete spending entries by category: {Error}", deleteResult.Error);
                    return Result<int>.Failure(deleteResult.Error!);
                }

                var deletedCount = deleteResult.Value;

                // Invalidate relevant caches
                await InvalidateRelatedCaches(request.CategoryId, cancellationToken);

                // Log the business operation
                await _loggingService.LogBulkSpendingDeletedByCategoryAsync(
                    request.CategoryId,
                    category.Name,
                    deletedCount,
                    cancellationToken);

                _logger.LogInformation("Successfully deleted {DeletedCount} spending entries for category {CategoryName}", 
                    deletedCount, category.Name);
                
                return Result<int>.Success(deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting spending entries by category");
                return Result<int>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to delete spending entries by category", 
                    new Dictionary<string, object>
                    {
                        ["CategoryId"] = request.CategoryId
                    }));
            }
        }

        private async Task InvalidateRelatedCaches(int categoryId, CancellationToken cancellationToken)
        {
            try
            {
                // Invalidate spending-specific caches for this category
                await _cacheService.InvalidateSpendingAsync(null, categoryId, cancellationToken);
                
                // Invalidate dashboard caches since totals changed
                await _cacheService.InvalidateDashboardSummariesAsync(cancellationToken);
                
                _logger.LogDebug("Invalidated caches for category {CategoryId}", categoryId);
            }
            catch (Exception ex)
            {
                // Don't fail the operation if cache invalidation fails
                _logger.LogWarning(ex, "Failed to invalidate caches after bulk deleting spending by category");
            }
        }
    }
}