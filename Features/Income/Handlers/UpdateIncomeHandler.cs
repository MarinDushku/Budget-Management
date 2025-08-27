// Update Income Command Handler - Vertical Slice Architecture
// File: Features/Income/Handlers/UpdateIncomeHandler.cs

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
    /// Handler for updating existing income entries with caching invalidation
    /// </summary>
    public class UpdateIncomeHandler : IRequestHandler<UpdateIncomeCommand, Result<Models.Income>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<UpdateIncomeHandler> _logger;

        public UpdateIncomeHandler(
            IIncomeRepository incomeRepository,
            IBudgetCacheService cacheService,
            IApplicationLoggingService loggingService,
            ILogger<UpdateIncomeHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Models.Income>> Handle(UpdateIncomeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Updating income entry ID {IncomeId}: Amount={Amount}, Date={Date}, Description={Description}", 
                    request.Id, request.Amount, request.Date, request.Description);

                // Get the existing income entry
                var existingResult = await _incomeRepository.GetByIdAsync(request.Id, cancellationToken);
                if (existingResult.IsFailure)
                {
                    _logger.LogWarning("Income entry not found for update: ID {IncomeId}", request.Id);
                    return Result<Models.Income>.Failure(existingResult.Error!);
                }

                var existingIncome = existingResult.Value!;
                var originalDate = existingIncome.Date;

                // Update the income entry properties
                existingIncome.Date = request.Date;
                existingIncome.Amount = request.Amount;
                existingIncome.Description = request.Description;
                existingIncome.UpdatedAt = DateTime.UtcNow;

                // Update in repository
                var updateResult = await _incomeRepository.UpdateAsync(existingIncome, cancellationToken);
                if (updateResult.IsFailure)
                {
                    _logger.LogError("Failed to update income entry ID {IncomeId}: {Error}", request.Id, updateResult.Error);
                    return Result<Models.Income>.Failure(updateResult.Error!);
                }

                var updatedIncome = updateResult.Value!;

                // Invalidate relevant caches (both old and new dates if changed)
                await InvalidateRelatedCaches(originalDate, updatedIncome.Date, cancellationToken);

                // Log the business operation
                await _loggingService.LogIncomeUpdatedAsync(
                    updatedIncome.Id,
                    updatedIncome.Amount,
                    updatedIncome.Date,
                    updatedIncome.Description,
                    cancellationToken);

                _logger.LogInformation("Successfully updated income entry ID {IncomeId}", updatedIncome.Id);
                return Result<Models.Income>.Success(updatedIncome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating income entry ID {IncomeId}", request.Id);
                return Result<Models.Income>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to update income entry", 
                    new Dictionary<string, object>
                    {
                        ["Id"] = request.Id,
                        ["Amount"] = request.Amount,
                        ["Date"] = request.Date,
                        ["Description"] = request.Description
                    }));
            }
        }

        private async Task InvalidateRelatedCaches(DateTime originalDate, DateTime newDate, CancellationToken cancellationToken)
        {
            try
            {
                // Invalidate caches for both dates if they're different
                if (originalDate.Date != newDate.Date)
                {
                    await _cacheService.InvalidateIncomeAsync(originalDate, cancellationToken);
                    await _cacheService.InvalidateIncomeAsync(newDate, cancellationToken);
                }
                else
                {
                    await _cacheService.InvalidateIncomeAsync(newDate, cancellationToken);
                }
                
                // Invalidate dashboard caches since totals may have changed
                await _cacheService.InvalidateDashboardSummariesAsync(cancellationToken);
                
                _logger.LogDebug("Invalidated caches for dates {OriginalDate} and {NewDate}", originalDate, newDate);
            }
            catch (Exception ex)
            {
                // Don't fail the operation if cache invalidation fails
                _logger.LogWarning(ex, "Failed to invalidate caches after updating income");
            }
        }
    }
}