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
}