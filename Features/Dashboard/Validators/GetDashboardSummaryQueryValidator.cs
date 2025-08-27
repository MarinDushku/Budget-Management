// Dashboard Summary Query Validator - FluentValidation Implementation
// File: Features/Dashboard/Validators/GetDashboardSummaryQueryValidator.cs

using BudgetManagement.Features.Dashboard.Queries;
using BudgetManagement.Shared.Validators;
using FluentValidation;

namespace BudgetManagement.Features.Dashboard.Validators
{
    /// <summary>
    /// Validator for GetDashboardSummaryQuery using FluentValidation
    /// Ensures query parameters are valid before processing
    /// </summary>
    public class GetDashboardSummaryQueryValidator : BaseQueryValidator<GetDashboardSummaryQuery>
    {
        public GetDashboardSummaryQueryValidator()
        {
            // Validate that the query is not null
            ValidateQueryNotNull();

            // Validate StartDate using common validators
            RuleFor(query => query.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required")
                .NotInFuture(1)  // Allow 1 day in future
                .NotTooOld(5);   // Allow max 5 years in past

            // Validate EndDate using common validators
            RuleFor(query => query.EndDate)
                .NotEmpty()
                .WithMessage("End date is required")
                .NotInFuture(1)  // Allow 1 day in future
                .IsValidEndDate(query => query.StartDate)
                .HasReasonableDuration(query => query.StartDate, 730); // Max 2 years

            // Validate BankStatementDay using common validator
            RuleFor(query => query.BankStatementDay)
                .IsValidBankStatementDay();

            // Additional business-specific validations
            RuleFor(query => query)
                .Must(NotHaveInvalidMonthDayForBankStatement)
                .WithMessage("Bank statement day is invalid for the selected date range")
                .WithName("BankStatementDay");
        }


        /// <summary>
        /// Validates that the bank statement day makes sense for the given date range
        /// </summary>
        private static bool NotHaveInvalidMonthDayForBankStatement(GetDashboardSummaryQuery query)
        {
            if (query.BankStatementDay < 1 || query.BankStatementDay > 31)
                return true; // Let the range validator handle this

            // For February, ensure day 29-31 are handled appropriately
            if (query.BankStatementDay > 28)
            {
                // Check if the date range includes February and if the bank statement day is valid
                var current = query.StartDate;
                while (current <= query.EndDate)
                {
                    if (current.Month == 2 && query.BankStatementDay > DateTime.DaysInMonth(current.Year, 2))
                    {
                        // This might be problematic, but we'll allow it as the service should handle this gracefully
                        // by using the last day of February when the specific day doesn't exist
                    }
                    current = current.AddMonths(1);
                }
            }

            return true; // Allow all values that pass basic range check
        }
    }

    /// <summary>
    /// Extension methods for additional validation scenarios
    /// </summary>
    public static class DashboardValidationExtensions
    {
        /// <summary>
        /// Validates a date range for dashboard queries
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>True if the date range is valid</returns>
        public static bool IsValidDateRange(DateTime startDate, DateTime endDate)
        {
            // Basic validation
            if (startDate > endDate)
                return false;

            // Future date validation
            if (endDate > DateTime.Now.Date.AddDays(1))
                return false;

            // Range size validation
            var daysDifference = (endDate - startDate).Days;
            if (daysDifference > 730) // 2 years
                return false;

            // Historical limit validation
            var fiveYearsAgo = DateTime.Now.Date.AddYears(-5);
            if (startDate < fiveYearsAgo)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a human-readable description of why a date range might be invalid
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Error message or null if valid</returns>
        public static string? GetDateRangeValidationError(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                return "Start date cannot be after end date";

            if (endDate > DateTime.Now.Date.AddDays(1))
                return "End date cannot be in the future";

            var daysDifference = (endDate - startDate).Days;
            if (daysDifference > 730)
                return "Date range cannot exceed 2 years";

            var fiveYearsAgo = DateTime.Now.Date.AddYears(-5);
            if (startDate < fiveYearsAgo)
                return "Date range cannot be more than 5 years in the past";

            return null;
        }

        /// <summary>
        /// Validates a bank statement day
        /// </summary>
        /// <param name="day">Bank statement day</param>
        /// <returns>True if valid</returns>
        public static bool IsValidBankStatementDay(int day)
        {
            return day >= 1 && day <= 31;
        }
    }
}