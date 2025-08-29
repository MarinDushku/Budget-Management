// Data Models for Budget Management Application
// File: Models/DataModels.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetManagement.Models
{
    /// <summary>
    /// Base entity class with common properties
    /// </summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Income entry model
    /// </summary>
    public class Income : BaseEntity
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Spending category model
    /// </summary>
    public class Category : BaseEntity
    {
        [Required]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        
        [StringLength(50, ErrorMessage = "Icon name cannot exceed 50 characters")]
        public string Icon { get; set; } = string.Empty;
    }

    /// <summary>
    /// Spending entry model
    /// </summary>
    public class Spending : BaseEntity
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        // Navigation property
        public Category? Category { get; set; }
    }

    /// <summary>
    /// Application settings model
    /// </summary>
    public class AppSetting
    {
        [Required]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// View model for spending with category information
    /// </summary>
    public class SpendingWithCategory
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Summary model for budget calculations
    /// </summary>
    public class BudgetSummary
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal FamilySpending { get; set; }
        public decimal PersonalSpending { get; set; }
        public decimal MariniSpending { get; set; }
        public decimal RemainingBudget => TotalIncome - TotalSpending;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    /// <summary>
    /// Summary model for bank statement period budget calculations
    /// </summary>
    public class BankStatementSummary
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal RemainingBudget => TotalIncome - TotalSpending;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int StatementDay { get; set; }
        
        /// <summary>
        /// Display-friendly period description (e.g., "All Time - Aug 17")
        /// </summary>
        public string PeriodDescription => $"All Time - {PeriodEnd:MMM dd}";
    }

    /// <summary>
    /// Monthly summary model
    /// </summary>
    public class MonthlySummary
    {
        public string Month { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// Weekly budget data for trend analysis and chart display
    /// </summary>
    public class WeeklyBudgetData
    {
        public DateTime WeekStartDate { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal RemainingBudget => TotalIncome - TotalSpending;
        
        /// <summary>
        /// Display-friendly week label for charts
        /// </summary>
        public string WeekLabel => WeekStartDate.ToString("MMM dd", System.Globalization.CultureInfo.InvariantCulture);
        
        /// <summary>
        /// Week end date (6 days after start date)
        /// </summary>
        public DateTime WeekEndDate => WeekStartDate.AddDays(6);
    }
}