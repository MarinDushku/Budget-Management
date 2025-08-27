// Get Income Queries - Vertical Slice Architecture
// File: Features/Income/Queries/GetIncomeQuery.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;
using MediatR;

namespace BudgetManagement.Features.Income.Queries
{
    /// <summary>
    /// Query to get income entries by date range
    /// </summary>
    public record GetIncomeByDateRangeQuery(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<IEnumerable<Models.Income>>>;

    /// <summary>
    /// Query to get a single income entry by ID
    /// </summary>
    public record GetIncomeByIdQuery(int Id) : IRequest<Result<Models.Income>>;

    /// <summary>
    /// Query to get the most recent income entries
    /// </summary>
    public record GetRecentIncomeQuery(int Count = 10) : IRequest<Result<IEnumerable<Models.Income>>>;

    /// <summary>
    /// Query to get income total for a date range
    /// </summary>
    public record GetIncomeTotalQuery(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<decimal>>;

    /// <summary>
    /// Query to get income statistics for a date range
    /// </summary>
    public record GetIncomeStatisticsQuery(
        DateTime StartDate,
        DateTime EndDate
    ) : IRequest<Result<IncomeStatistics>>;

    /// <summary>
    /// Query to get income trend data for analytics
    /// </summary>
    public record GetIncomeTrendQuery(
        DateTime StartDate,
        DateTime EndDate,
        TrendGrouping Grouping = TrendGrouping.Daily
    ) : IRequest<Result<IEnumerable<IncomeTrendData>>>;

    /// <summary>
    /// Query to get monthly income totals for a specific year
    /// </summary>
    public record GetMonthlyIncomeTotalsQuery(int Year) : IRequest<Result<IEnumerable<MonthlyIncomeSummary>>>;

    /// <summary>
    /// Query to search income by description pattern
    /// </summary>
    public record SearchIncomeByDescriptionQuery(string Pattern) : IRequest<Result<IEnumerable<Models.Income>>>;

    /// <summary>
    /// Query to get income by amount range
    /// </summary>
    public record GetIncomeByAmountRangeQuery(
        decimal MinAmount,
        decimal MaxAmount
    ) : IRequest<Result<IEnumerable<Models.Income>>>;
}