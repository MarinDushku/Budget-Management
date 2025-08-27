// Common Validators - Shared FluentValidation Rules
// File: Shared/Validators/CommonValidators.cs

using FluentValidation;
using BudgetManagement.Shared.Infrastructure;

namespace BudgetManagement.Shared.Validators
{
    /// <summary>
    /// Common validation rules that can be reused across different validators
    /// Provides consistent validation logic throughout the application
    /// </summary>
    public static class CommonValidators
    {
        /// <summary>
        /// Validates that a date is not in the future
        /// </summary>
        public static IRuleBuilderOptions<T, DateTime> NotInFuture<T>(this IRuleBuilder<T, DateTime> ruleBuilder, int allowedFutureDays = 0)
        {
            return ruleBuilder
                .LessThanOrEqualTo(DateTime.Now.Date.AddDays(allowedFutureDays))
                .WithMessage($"Date cannot be more than {allowedFutureDays} day(s) in the future");
        }

        /// <summary>
        /// Validates that a date is not too far in the past
        /// </summary>
        public static IRuleBuilderOptions<T, DateTime> NotTooOld<T>(this IRuleBuilder<T, DateTime> ruleBuilder, int maxYearsInPast = 10)
        {
            return ruleBuilder
                .GreaterThanOrEqualTo(DateTime.Now.Date.AddYears(-maxYearsInPast))
                .WithMessage($"Date cannot be more than {maxYearsInPast} years in the past");
        }

        /// <summary>
        /// Validates that an amount is positive
        /// </summary>
        public static IRuleBuilderOptions<T, decimal> IsPositiveAmount<T>(this IRuleBuilder<T, decimal> ruleBuilder)
        {
            return ruleBuilder
                .GreaterThan(0)
                .WithMessage(LocalizationHelper.ValidationMessages.AmountMustBePositive);
        }

        /// <summary>
        /// Validates that an amount is within reasonable bounds
        /// </summary>
        public static IRuleBuilderOptions<T, decimal> IsReasonableAmount<T>(this IRuleBuilder<T, decimal> ruleBuilder, decimal maxAmount = 1_000_000m)
        {
            return ruleBuilder
                .GreaterThan(0)
                .WithMessage(LocalizationHelper.ValidationMessages.AmountMustBePositive)
                .LessThanOrEqualTo(maxAmount)
                .WithMessage($"Amount cannot exceed {maxAmount:C}");
        }

        /// <summary>
        /// Validates that a string is a meaningful description (not just whitespace)
        /// </summary>
        public static IRuleBuilderOptions<T, string> IsMeaningfulText<T>(this IRuleBuilder<T, string> ruleBuilder, int minLength = 1, int maxLength = 500)
        {
            return ruleBuilder
                .NotEmpty()
                .WithMessage(LocalizationHelper.ValidationMessages.DescriptionRequired)
                .Length(minLength, maxLength)
                .WithMessage($"Description must be between {minLength} and {maxLength} characters")
                .Must(text => !string.IsNullOrWhiteSpace(text))
                .WithMessage(LocalizationHelper.ValidationMessages.DescriptionCannotBeEmpty);
        }

        /// <summary>
        /// Validates that a date range is valid (start <= end)
        /// </summary>
        public static IRuleBuilderOptions<T, DateTime> IsValidEndDate<T>(this IRuleBuilder<T, DateTime> ruleBuilder, Func<T, DateTime> startDateSelector)
        {
            return ruleBuilder
                .Must((obj, endDate) => endDate >= startDateSelector(obj))
                .WithMessage(LocalizationHelper.ValidationMessages.StartDateMustBeBeforeEndDate);
        }

        /// <summary>
        /// Validates that a date range duration is reasonable
        /// </summary>
        public static IRuleBuilderOptions<T, DateTime> HasReasonableDuration<T>(
            this IRuleBuilder<T, DateTime> ruleBuilder, 
            Func<T, DateTime> startDateSelector, 
            int maxDays = 365)
        {
            return ruleBuilder
                .Must((obj, endDate) =>
                {
                    var startDate = startDateSelector(obj);
                    var duration = (endDate - startDate).Days;
                    return duration <= maxDays;
                })
                .WithMessage($"Date range cannot exceed {maxDays} days");
        }

        /// <summary>
        /// Validates that a category name is valid
        /// </summary>
        public static IRuleBuilderOptions<T, string> IsValidCategoryName<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty()
                .WithMessage(LocalizationHelper.ValidationMessages.CategoryNameRequired)
                .Length(1, 100)
                .WithMessage("Category name must be between 1 and 100 characters")
                .Must(BeValidCategoryName)
                .WithMessage(LocalizationHelper.ValidationMessages.CategoryNameInvalidCharacters);
        }

        /// <summary>
        /// Validates that a bank statement day is valid
        /// </summary>
        public static IRuleBuilderOptions<T, int> IsValidBankStatementDay<T>(this IRuleBuilder<T, int> ruleBuilder)
        {
            return ruleBuilder
                .InclusiveBetween(1, 31)
                .WithMessage("Bank statement day must be between 1 and 31");
        }

        /// <summary>
        /// Validates that an ID is not empty (for Guid properties)
        /// </summary>
        public static IRuleBuilderOptions<T, Guid> IsNotEmpty<T>(this IRuleBuilder<T, Guid> ruleBuilder)
        {
            return ruleBuilder
                .NotEqual(Guid.Empty)
                .WithMessage(LocalizationHelper.ValidationMessages.IdCannotBeEmpty);
        }

        /// <summary>
        /// Custom validator for category names
        /// </summary>
        private static bool BeValidCategoryName(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return false;

            // Check for invalid characters (basic validation)
            var invalidChars = new[] { '<', '>', '|', '*', '?', '"', ':', '\\', '/' };
            return !categoryName.Any(c => invalidChars.Contains(c));
        }
    }

    /// <summary>
    /// Base validator class for commands that provides common validation rules
    /// </summary>
    /// <typeparam name="T">Command type</typeparam>
    public abstract class BaseCommandValidator<T> : AbstractValidator<T>
        where T : class
    {
        protected BaseCommandValidator()
        {
            // Add any common command validation rules here
            CascadeMode = CascadeMode.Stop; // Stop on first validation failure
        }

        /// <summary>
        /// Validates that the command is not null
        /// </summary>
        protected void ValidateCommandNotNull()
        {
            RuleFor(x => x)
                .NotNull()
                .WithMessage("Command cannot be null");
        }
    }

    /// <summary>
    /// Base validator class for queries that provides common validation rules
    /// </summary>
    /// <typeparam name="T">Query type</typeparam>
    public abstract class BaseQueryValidator<T> : AbstractValidator<T>
        where T : class
    {
        protected BaseQueryValidator()
        {
            // Add any common query validation rules here
            CascadeMode = CascadeMode.Stop; // Stop on first validation failure
        }

        /// <summary>
        /// Validates that the query is not null
        /// </summary>
        protected void ValidateQueryNotNull()
        {
            RuleFor(x => x)
                .NotNull()
                .WithMessage("Query cannot be null");
        }
    }
}