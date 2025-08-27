// Income Validators - FluentValidation Rules
// File: Features/Income/Validators/IncomeValidators.cs

using BudgetManagement.Features.Income.Commands;
using BudgetManagement.Features.Income.Queries;
using BudgetManagement.Shared.Data.Repositories;
using FluentValidation;

namespace BudgetManagement.Features.Income.Validators
{
    /// <summary>
    /// Validator for AddIncomeCommand
    /// </summary>
    public class AddIncomeCommandValidator : AbstractValidator<AddIncomeCommand>
    {
        public AddIncomeCommandValidator()
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
        }
    }

    /// <summary>
    /// Validator for UpdateIncomeCommand
    /// </summary>
    public class UpdateIncomeCommandValidator : AbstractValidator<UpdateIncomeCommand>
    {
        public UpdateIncomeCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Income ID must be greater than 0");

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
        }
    }

    /// <summary>
    /// Validator for DeleteIncomeCommand
    /// </summary>
    public class DeleteIncomeCommandValidator : AbstractValidator<DeleteIncomeCommand>
    {
        public DeleteIncomeCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Income ID must be greater than 0");
        }
    }

    /// <summary>
    /// Validator for DeleteIncomeByDateRangeCommand
    /// </summary>
    public class DeleteIncomeByDateRangeCommandValidator : AbstractValidator<DeleteIncomeByDateRangeCommand>
    {
        public DeleteIncomeByDateRangeCommandValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required")
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Start date must be before or equal to end date");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required")
                .LessThanOrEqualTo(DateTime.Today)
                .WithMessage("End date cannot be in the future");

            RuleFor(x => x)
                .Must(x => (x.EndDate - x.StartDate).TotalDays <= 365)
                .WithMessage("Date range cannot exceed 365 days");
        }
    }

    /// <summary>
    /// Validator for GetIncomeByDateRangeQuery
    /// </summary>
    public class GetIncomeByDateRangeQueryValidator : AbstractValidator<GetIncomeByDateRangeQuery>
    {
        public GetIncomeByDateRangeQueryValidator()
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
    /// Validator for GetIncomeByIdQuery
    /// </summary>
    public class GetIncomeByIdQueryValidator : AbstractValidator<GetIncomeByIdQuery>
    {
        public GetIncomeByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Income ID must be greater than 0");
        }
    }

    /// <summary>
    /// Validator for GetRecentIncomeQuery
    /// </summary>
    public class GetRecentIncomeQueryValidator : AbstractValidator<GetRecentIncomeQuery>
    {
        public GetRecentIncomeQueryValidator()
        {
            RuleFor(x => x.Count)
                .GreaterThan(0)
                .WithMessage("Count must be greater than 0")
                .LessThanOrEqualTo(1000)
                .WithMessage("Count cannot exceed 1000");
        }
    }

    /// <summary>
    /// Validator for GetIncomeTotalQuery
    /// </summary>
    public class GetIncomeTotalQueryValidator : AbstractValidator<GetIncomeTotalQuery>
    {
        public GetIncomeTotalQueryValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required")
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Start date must be before or equal to end date");

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
    /// Validator for GetIncomeStatisticsQuery
    /// </summary>
    public class GetIncomeStatisticsQueryValidator : AbstractValidator<GetIncomeStatisticsQuery>
    {
        public GetIncomeStatisticsQueryValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required")
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Start date must be before or equal to end date");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required")
                .LessThanOrEqualTo(DateTime.Today.AddDays(1))
                .WithMessage("End date cannot be in the future");

            RuleFor(x => x)
                .Must(x => (x.EndDate - x.StartDate).TotalDays <= 1095) // 3 years
                .WithMessage("Date range cannot exceed 3 years for statistics");
        }
    }

    /// <summary>
    /// Validator for GetIncomeTrendQuery
    /// </summary>
    public class GetIncomeTrendQueryValidator : AbstractValidator<GetIncomeTrendQuery>
    {
        public GetIncomeTrendQueryValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required")
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Start date must be before or equal to end date");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required")
                .LessThanOrEqualTo(DateTime.Today.AddDays(1))
                .WithMessage("End date cannot be in the future");

            RuleFor(x => x.Grouping)
                .IsInEnum()
                .WithMessage("Invalid grouping value");

            RuleFor(x => x)
                .Must(x => ValidateDateRangeForGrouping(x.StartDate, x.EndDate, x.Grouping))
                .WithMessage("Date range is too large for the selected grouping");
        }

        private static bool ValidateDateRangeForGrouping(DateTime startDate, DateTime endDate, TrendGrouping grouping)
        {
            var daysDiff = (endDate - startDate).TotalDays;
            
            return grouping switch
            {
                TrendGrouping.Daily => daysDiff <= 365,       // Max 1 year for daily
                TrendGrouping.Weekly => daysDiff <= 730,      // Max 2 years for weekly  
                TrendGrouping.Monthly => daysDiff <= 1095,    // Max 3 years for monthly
                TrendGrouping.Yearly => daysDiff <= 3650,     // Max 10 years for yearly
                _ => false
            };
        }
    }

    /// <summary>
    /// Validator for GetMonthlyIncomeTotalsQuery
    /// </summary>
    public class GetMonthlyIncomeTotalsQueryValidator : AbstractValidator<GetMonthlyIncomeTotalsQuery>
    {
        public GetMonthlyIncomeTotalsQueryValidator()
        {
            RuleFor(x => x.Year)
                .GreaterThan(DateTime.Today.Year - 10)
                .WithMessage("Year cannot be more than 10 years in the past")
                .LessThanOrEqualTo(DateTime.Today.Year)
                .WithMessage("Year cannot be in the future");
        }
    }

    /// <summary>
    /// Validator for SearchIncomeByDescriptionQuery
    /// </summary>
    public class SearchIncomeByDescriptionQueryValidator : AbstractValidator<SearchIncomeByDescriptionQuery>
    {
        public SearchIncomeByDescriptionQueryValidator()
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
    /// Validator for GetIncomeByAmountRangeQuery
    /// </summary>
    public class GetIncomeByAmountRangeQueryValidator : AbstractValidator<GetIncomeByAmountRangeQuery>
    {
        public GetIncomeByAmountRangeQueryValidator()
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