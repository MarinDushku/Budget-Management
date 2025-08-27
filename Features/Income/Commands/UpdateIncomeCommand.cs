// Update Income Command - Vertical Slice Architecture
// File: Features/Income/Commands/UpdateIncomeCommand.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using MediatR;

namespace BudgetManagement.Features.Income.Commands
{
    /// <summary>
    /// Command to update an existing income entry
    /// </summary>
    public record UpdateIncomeCommand(
        int Id,
        DateTime Date,
        decimal Amount,
        string Description
    ) : IRequest<Result<Models.Income>>;

    /// <summary>
    /// Data transfer object for updating income
    /// </summary>
    public class UpdateIncomeDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;

        public UpdateIncomeCommand ToCommand()
        {
            return new UpdateIncomeCommand(Id, Date, Amount, Description.Trim());
        }
    }
}