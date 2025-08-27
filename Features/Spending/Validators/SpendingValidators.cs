// Spending Validators - FluentValidation Rules
// File: Features/Spending/Validators/SpendingValidators.cs

using BudgetManagement.Features.Spending.Commands;
using BudgetManagement.Features.Spending.Queries;
using FluentValidation;

namespace BudgetManagement.Features.Spending.Validators
{
    /// <summary>
    /// Validator for AddSpendingCommand
    /// </summary>
    public class AddSpendingCommandValidator : AbstractValidator<AddSpendingCommand>
    {
        public AddSpendingCommandValidator()
        {
            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Date is required")
                .LessThanOrEqualTo(DateTime.Today.AddDays(1))
                .WithMessage("Date cannot be in the future")
                .GreaterThan(DateTime.Today.AddYears(-10))
                .WithMessage("Date cannot be more than 10 years in the past");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than 0")
                .LessThanOrEqualTo(1_000_000)
                .WithMessage("Amount cannot exceed $1,000,000");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters")
                .Matches(@"^[a-zA-Z0-9\s\-\.\,\!\?\(\)\[\]\:\;\'\""\&\%\$\#\@\*\+\=\_\|\\\/]*$")
                .WithMessage("Description contains invalid characters");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0)
                .WithMessage("Category ID must be greater than 0");
        }
    }

    /// <summary>
    /// Validator for UpdateSpendingCommand
    /// </summary>
    public class UpdateSpendingCommandValidator : AbstractValidator<UpdateSpendingCommand>
    {
        public UpdateSpendingCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Spending ID must be greater than 0");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Date is required")
                .LessThanOrEqualTo(DateTime.Today.AddDays(1))
                .WithMessage("Date cannot be in the future")
                .GreaterThan(DateTime.Today.AddYears(-10))
                .WithMessage("Date cannot be more than 10 years in the past");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than 0")
                .LessThanOrEqualTo(1_000_000)
                .WithMessage("Amount cannot exceed $1,000,000");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters")
                .Matches(@"^[a-zA-Z0-9\s\-\.\,\!\?\(\)\[\]\:\;\'\""\&\%\$\#\@\*\+\=\_\|\\\/]*$")
                .WithMessage("Description contains invalid characters");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0)
                .WithMessage("Category ID must be greater than 0");
        }
    }

    /// <summary>
    /// Validator for DeleteSpendingCommand
    /// </summary>
    public class DeleteSpendingCommandValidator : AbstractValidator<DeleteSpendingCommand>
    {
        public DeleteSpendingCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Spending ID must be greater than 0");
        }
    }

    /// <summary>
    /// Validator for GetSpendingByDateRangeQuery
    /// </summary>
    public class GetSpendingByDateRangeQueryValidator : AbstractValidator<GetSpendingByDateRangeQuery>
    {
        public GetSpendingByDateRangeQueryValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required")
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Start date must be before or equal to end date")
                .GreaterThan(DateTime.Today.AddYears(-10))
                .WithMessage("Start date cannot be more than 10 years in the past");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required")
                .LessThanOrEqualTo(DateTime.Today.AddDays(1))
                .WithMessage("End date cannot be in the future");

            RuleFor(x => x)
                .Must(x => (x.EndDate - x.StartDate).TotalDays <= 1095) // 3 years
                .WithMessage("Date range cannot exceed 3 years");
        }
    }

    /// <summary>
    /// Validator for GetSpendingByCategoryQuery
    /// </summary>
    public class GetSpendingByCategoryQueryValidator : AbstractValidator<GetSpendingByCategoryQuery>
    {
        public GetSpendingByCategoryQueryValidator()
        {
            RuleFor(x => x.CategoryId)
                .GreaterThan(0)
                .WithMessage("Category ID must be greater than 0");

            When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
            {
                RuleFor(x => x.StartDate!.Value)
                    .LessThanOrEqualTo(x => x.EndDate!.Value)
                    .WithMessage("Start date must be before or equal to end date");

                RuleFor(x => x)
                    .Must(x => x.StartDate.HasValue && x.EndDate.HasValue && 
                              (x.EndDate.Value - x.StartDate.Value).TotalDays <= 1095)
                    .WithMessage("Date range cannot exceed 3 years");
            });
        }
    }

    /// <summary>
    /// Validator for SearchSpendingByDescriptionQuery
    /// </summary>
    public class SearchSpendingByDescriptionQueryValidator : AbstractValidator<SearchSpendingByDescriptionQuery>
    {
        public SearchSpendingByDescriptionQueryValidator()
        {
            RuleFor(x => x.Pattern)
                .NotEmpty()
                .WithMessage("Search pattern is required")
                .MinimumLength(2)
                .WithMessage("Search pattern must be at least 2 characters")
                .MaximumLength(100)
                .WithMessage("Search pattern cannot exceed 100 characters");
        }
    }

    /// <summary>
    /// Validator for GetSpendingByAmountRangeQuery
    /// </summary>
    public class GetSpendingByAmountRangeQueryValidator : AbstractValidator<GetSpendingByAmountRangeQuery>
    {
        public GetSpendingByAmountRangeQueryValidator()
        {
            RuleFor(x => x.MinAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum amount must be greater than or equal to 0")
                .LessThanOrEqualTo(x => x.MaxAmount)
                .WithMessage("Minimum amount must be less than or equal to maximum amount");

            RuleFor(x => x.MaxAmount)
                .GreaterThan(0)
                .WithMessage("Maximum amount must be greater than 0")
                .LessThanOrEqualTo(10_000_000)
                .WithMessage("Maximum amount cannot exceed $10,000,000");
        }
    }
}