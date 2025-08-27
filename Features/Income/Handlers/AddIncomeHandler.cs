// Add Income Command Handler - Vertical Slice Architecture
// File: Features/Income/Handlers/AddIncomeHandler.cs

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
    /// Handler for adding new income entries with caching invalidation
    /// </summary>
    public class AddIncomeHandler : IRequestHandler<AddIncomeCommand, Result<Models.Income>>
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<AddIncomeHandler> _logger;

        public AddIncomeHandler(
            IIncomeRepository incomeRepository,
            IBudgetCacheService cacheService,
            IApplicationLoggingService loggingService,
            ILogger<AddIncomeHandler> logger)
        {
            _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Models.Income>> Handle(AddIncomeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Adding income entry: Amount={Amount}, Date={Date}, Description={Description}", 
                    request.Amount, request.Date, request.Description);

                // Create new income entry
                var income = new Models.Income
                {
                    Date = request.Date,
                    Amount = request.Amount,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add to repository
                var addResult = await _incomeRepository.AddAsync(income, cancellationToken);
                if (addResult.IsFailure)
                {
                    _logger.LogError("Failed to add income entry: {Error}", addResult.Error);
                    return Result<Models.Income>.Failure(addResult.Error!);
                }

                var addedIncome = addResult.Value!;

                // Invalidate relevant caches
                await InvalidateRelatedCaches(addedIncome.Date, cancellationToken);

                // Log the business operation
                await _loggingService.LogIncomeAddedAsync(
                    addedIncome.Id, 
                    addedIncome.Amount, 
                    addedIncome.Date, 
                    addedIncome.Description,
                    cancellationToken);

                _logger.LogInformation("Successfully added income entry with ID {IncomeId}", addedIncome.Id);
                return Result<Models.Income>.Success(addedIncome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding income entry");
                return Result<Models.Income>.Failure(Error.System(
                    Error.Codes.SYSTEM_ERROR, 
                    "Failed to add income entry", 
                    new Dictionary<string, object>
                    {
                        ["Amount"] = request.Amount,
                        ["Date"] = request.Date,
                        ["Description"] = request.Description
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
                _logger.LogWarning(ex, "Failed to invalidate caches after adding income");
            }
        }
    }
}