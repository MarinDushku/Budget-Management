// Delete Spending Command - Vertical Slice Architecture
// File: Features/Spending/Commands/DeleteSpendingCommand.cs

using BudgetManagement.Shared.Core;
using MediatR;

namespace BudgetManagement.Features.Spending.Commands
{
    /// <summary>
    /// Command to delete a spending entry by ID
    /// </summary>
    public record DeleteSpendingCommand(int Id) : IRequest<Result>;

    /// <summary>
    /// Command to delete multiple spending entries by date range
    /// </summary>
    public record DeleteSpendingByDateRangeCommand(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<int>>;

    /// <summary>
    /// Command to delete multiple spending entries by category
    /// </summary>
    public record DeleteSpendingByCategoryCommand(int CategoryId) : IRequest<Result<int>>;
}