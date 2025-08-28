// Get Spending Queries - Vertical Slice Architecture
// File: Features/Spending/Queries/GetSpendingQuery.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;
using MediatR;

namespace BudgetManagement.Features.Spending.Queries
{
    /// <summary>
    /// Query to get spending entries by date range
    /// </summary>
    public record GetSpendingByDateRangeQuery(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<IEnumerable<Models.Spending>>>;

    /// <summary>
    /// Query to get spending entries with category information by date range
    /// </summary>
    public record GetSpendingWithCategoryByDateRangeQuery(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<IEnumerable<SpendingWithCategory>>>;

    /// <summary>
    /// Query to get a single spending entry by ID
    /// </summary>
    public record GetSpendingByIdQuery(int Id) : IRequest<Result<Models.Spending>>;

    /// <summary>
    /// Query to get the most recent spending entries
    /// </summary>
    public record GetRecentSpendingQuery(int Count = 10) : IRequest<Result<IEnumerable<Models.Spending>>>;

    /// <summary>
    /// Query to get spending total for a date range
    /// </summary>
    public record GetSpendingTotalQuery(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<decimal>>;

    /// <summary>
    /// Query to get spending total by category for a date range
    /// </summary>
    public record GetSpendingTotalByCategoryQuery(
        DateTime StartDate,
        DateTime EndDate,
        int CategoryId
    ) : IRequest<Result<decimal>>;

    /// <summary>
    /// Query to get spending statistics for a date range
    /// </summary>
    public record GetSpendingStatisticsQuery(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<SpendingStatistics>>;

    /// <summary>
    /// Query to get spending trend data for analytics
    /// </summary>
    public record GetSpendingTrendQuery(
        DateTime StartDate,
        DateTime EndDate,
        TrendGrouping Grouping = TrendGrouping.Daily
    ) : IRequest<Result<IEnumerable<SpendingTrendData>>>;

    /// <summary>
    /// Query to get monthly spending totals for a specific year
    /// </summary>
    public record GetMonthlySpendingTotalsQuery(int Year) : IRequest<Result<IEnumerable<MonthlySpendingSummary>>>;

    /// <summary>
    /// Query to search spending by description pattern
    /// </summary>
    public record SearchSpendingByDescriptionQuery(string Pattern) : IRequest<Result<IEnumerable<Models.Spending>>>;

    /// <summary>
    /// Query to get spending by amount range
    /// </summary>
    public record GetSpendingByAmountRangeQuery(
        decimal MinAmount,
        decimal MaxAmount
    ) : IRequest<Result<IEnumerable<Models.Spending>>>;

    /// <summary>
    /// Query to get spending by category
    /// </summary>
    public record GetSpendingByCategoryQuery(
        int CategoryId,
        DateTime? StartDate = null,
        DateTime? EndDate = null
    ) : IRequest<Result<IEnumerable<Models.Spending>>>;

    /// <summary>
    /// Query to get spending category breakdown
    /// </summary>
    public record GetSpendingCategoryBreakdownQuery(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<IEnumerable<CategorySpendingSummary>>>;

    /// <summary>
    /// Advanced spending search query with multiple criteria
    /// </summary>
    public record AdvancedSpendingSearchQuery(
        string? DescriptionPattern = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        decimal? MinAmount = null,
        decimal? MaxAmount = null,
        List<int>? CategoryIds = null,
        int Skip = 0,
        int Take = 50,
        SpendingSortBy SortBy = SpendingSortBy.Date,
        SortDirection SortDirection = SortDirection.Descending
    ) : IRequest<Result<AdvancedSpendingSearchResult>>;

    /// <summary>
    /// Query to get all spending entries with pagination
    /// </summary>
    public record GetAllSpendingsQuery(
        int Skip = 0,
        int Take = 50,
        SpendingSortBy SortBy = SpendingSortBy.Date,
        SortDirection SortDirection = SortDirection.Descending
    ) : IRequest<Result<PaginatedSpendingResult>>;
}

namespace BudgetManagement.Features.Spending.Queries
{
    /// <summary>
    /// Sort options for spending queries
    /// </summary>
    public enum SpendingSortBy
    {
        Date,
        Amount,
        Description,
        Category,
        CreatedAt
    }

    /// <summary>
    /// Sort direction (reusing from Income namespace)
    /// </summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Result for advanced spending search with pagination info
    /// </summary>
    public class AdvancedSpendingSearchResult
    {
        public IEnumerable<SpendingWithCategory> Spendings { get; set; } = [];
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool HasMore { get; set; }
        public Dictionary<int, decimal> CategoryTotals { get; set; } = [];
    }

    /// <summary>
    /// Result for paginated spending queries
    /// </summary>
    public class PaginatedSpendingResult
    {
        public IEnumerable<SpendingWithCategory> Spendings { get; set; } = [];
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }
}