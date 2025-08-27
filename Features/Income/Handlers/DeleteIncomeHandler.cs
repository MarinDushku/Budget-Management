// Delete Income Command Handler - Vertical Slice Architecture
// File: Features/Income/Handlers/DeleteIncomeHandler.cs

using BudgetManagement.Features.Income.Commands;
using BudgetManagement.Models;
using BudgetManagement.Services;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;
using BudgetManagement.Shared.Infrastructure;
using BudgetManagement.Shared.Infrastructure.Caching;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Features.Income.Handlers
{
    /// <summary>
    /// Handler for deleting income entries with caching invalidation
    /// </summary>
    public class DeleteIncomeHandler : IRequestHandler<DeleteIncomeCommand, Result>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<DeleteIncomeHandler> _logger;

        public DeleteIncomeHandler(
            IIncomeRepository incomeRepository,
            IBudgetCacheService cacheService,
            IApplicationLoggingService loggingService,
            ILogger<DeleteIncomeHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(DeleteIncomeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Deleting income entry ID {IncomeId}", request.Id);

                // Get the existing income entry first to capture info for logging
                var existingResult = await _incomeRepository.GetByIdAsync(request.Id, cancellationToken);
                if (existingResult.IsFailure)
                {
                    _logger.LogWarning("Income entry not found for deletion: ID {IncomeId}", request.Id);
                    return Result.Failure(existingResult.Error!);
                }

                var existingIncome = existingResult.Value!;
                var affectedDate = existingIncome.Date;

                // Delete from repository
                var deleteResult = await _incomeRepository.DeleteAsync(request.Id, cancellationToken);
                if (deleteResult.IsFailure)
                {
                    _logger.LogError("Failed to delete income entry ID {IncomeId}: {Error}", request.Id, deleteResult.Error);
                    return Result.Failure(deleteResult.Error!);
                }

                // Invalidate relevant caches
                await InvalidateRelatedCaches(affectedDate, cancellationToken);

                // Log the business operation
                await _loggingService.LogIncomeDeletedAsync(
                    existingIncome.Id,
                    existingIncome.Amount,
                    existingIncome.Date,
                    existingIncome.Description,
                    cancellationToken);

                _logger.LogInformation("Successfully deleted income entry ID {IncomeId}", request.Id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting income entry ID {IncomeId}", request.Id);
                return Result.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to delete income entry", 
                    new Dictionary<string, object>
                    {
                        ["Id"] = request.Id
                    }));
            }
        }

        private async Task InvalidateRelatedCaches(DateTime affectedDate, CancellationToken cancellationToken)
        {
            try
            {
                // Invalidate income-specific caches
                await _cacheService.InvalidateIncomeAsync(affectedDate, cancellationToken);
                
                // Invalidate dashboard caches since totals changed
                await _cacheService.InvalidateDashboardSummariesAsync(cancellationToken);
                
                _logger.LogDebug("Invalidated caches for date {AffectedDate}", affectedDate);
            }
            catch (Exception ex)
            {
                // Don't fail the operation if cache invalidation fails
                _logger.LogWarning(ex, "Failed to invalidate caches after deleting income");
            }
        }
    }

    /// <summary>
    /// Handler for deleting income entries by date range
    /// </summary>
    public class DeleteIncomeByDateRangeHandler : IRequestHandler<DeleteIncomeByDateRangeCommand, Result<int>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<DeleteIncomeByDateRangeHandler> _logger;

        public DeleteIncomeByDateRangeHandler(
            IIncomeRepository incomeRepository,
            IBudgetCacheService cacheService,
            IApplicationLoggingService loggingService,
            ILogger<DeleteIncomeByDateRangeHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<int>> Handle(DeleteIncomeByDateRangeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Deleting income entries from {StartDate} to {EndDate}", request.StartDate, request.EndDate);

                // Delete from repository
                var deleteResult = await _incomeRepository.DeleteByDateRangeAsync(request.StartDate, request.EndDate, cancellationToken);
                if (deleteResult.IsFailure)
                {
                    _logger.LogError("Failed to delete income entries by date range: {Error}", deleteResult.Error);
                    return Result<int>.Failure(deleteResult.Error!);
                }

                var deletedCount = deleteResult.Value;

                // Invalidate relevant caches
                await InvalidateRelatedCaches(request.StartDate, request.EndDate, cancellationToken);

                // Log the business operation
                await _loggingService.LogBulkIncomeDeletedAsync(
                    request.StartDate,
                    request.EndDate,
                    deletedCount,
                    cancellationToken);

                _logger.LogInformation("Successfully deleted {DeletedCount} income entries from {StartDate} to {EndDate}", 
                    deletedCount, request.StartDate, request.EndDate);
                
                return Result<int>.Success(deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting income entries by date range");
                return Result<int>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to delete income entries by date range", 
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
                // Invalidate income-specific caches for the entire range
                await _cacheService.InvalidateIncomeAsync(null, cancellationToken); // Invalidate all income cache
                
                // Invalidate dashboard caches since totals changed
                await _cacheService.InvalidateDashboardSummariesAsync(cancellationToken);
                
                _logger.LogDebug("Invalidated caches for date range {StartDate} to {EndDate}", startDate, endDate);
            }
            catch (Exception ex)
            {
                // Don't fail the operation if cache invalidation fails
                _logger.LogWarning(ex, "Failed to invalidate caches after bulk deleting income");
            }
        }
    }
}