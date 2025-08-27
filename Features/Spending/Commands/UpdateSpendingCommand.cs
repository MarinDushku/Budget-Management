// Update Spending Command - Vertical Slice Architecture
// File: Features/Spending/Commands/UpdateSpendingCommand.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using MediatR;

namespace BudgetManagement.Features.Spending.Commands
{
    /// <summary>
    /// Command to update an existing spending entry
    /// </summary>
    public record UpdateSpendingCommand(
        int Id,
        DateTime Date,
        decimal Amount,
        string Description,
        int CategoryId
    ) : IRequest<Result<Models.Spending>>;

    /// <summary>
    /// Data transfer object for updating spending
    /// </summary>
    public class UpdateSpendingDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }

        public UpdateSpendingCommand ToCommand()
        {
            return new UpdateSpendingCommand(Id, Date, Amount, Description.Trim(), CategoryId);
        }
    }
}