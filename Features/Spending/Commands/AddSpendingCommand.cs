// Add Spending Command - Vertical Slice Architecture
// File: Features/Spending/Commands/AddSpendingCommand.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using MediatR;

namespace BudgetManagement.Features.Spending.Commands
{
    /// <summary>
    /// Command to add a new spending entry
    /// </summary>
    public record AddSpendingCommand(
        DateTime Date,
        decimal Amount,
        string Description,
        int CategoryId
    ) : IRequest<Result<Models.Spending>>;

    /// <summary>
    /// Data transfer object for adding spending
    /// </summary>
    public class AddSpendingDto
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }

        public AddSpendingCommand ToCommand()
        {
            return new AddSpendingCommand(Date, Amount, Description.Trim(), CategoryId);
        }
    }
}