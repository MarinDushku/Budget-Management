// Add Income Command - Vertical Slice Architecture
// File: Features/Income/Commands/AddIncomeCommand.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using MediatR;

namespace BudgetManagement.Features.Income.Commands
{
    /// <summary>
    /// Command to add a new income entry
    /// </summary>
    public record AddIncomeCommand(
        DateTime Date,
        decimal Amount,
        string Description
    ) : IRequest<Result<Models.Income>>;

    /// <summary>
    /// Data transfer object for adding income
    /// </summary>
    public class AddIncomeDto
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;

        public AddIncomeCommand ToCommand()
        {
            return new AddIncomeCommand(Date, Amount, Description.Trim());
        }
    }
}