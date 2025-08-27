// Delete Income Command - Vertical Slice Architecture
// File: Features/Income/Commands/DeleteIncomeCommand.cs

using BudgetManagement.Shared.Core;
using MediatR;

namespace BudgetManagement.Features.Income.Commands
{
    /// <summary>
    /// Command to delete an income entry by ID
    /// </summary>
    public record DeleteIncomeCommand(int Id) : IRequest<Result>;

    /// <summary>
    /// Command to delete multiple income entries by date range
    /// </summary>
    public record DeleteIncomeByDateRangeCommand(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<int>>;
}