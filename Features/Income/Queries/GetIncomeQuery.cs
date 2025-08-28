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

    /// <summary>
    /// Advanced income search query with multiple criteria
    /// </summary>
    public record AdvancedIncomeSearchQuery(
        string? DescriptionPattern = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        decimal? MinAmount = null,
        decimal? MaxAmount = null,
        int Skip = 0,
        int Take = 50,
        IncomeSortBy SortBy = IncomeSortBy.Date,
        SortDirection SortDirection = SortDirection.Descending
    ) : IRequest<Result<AdvancedIncomeSearchResult>>;

    /// <summary>
    /// Query to get all income entries with pagination
    /// </summary>
    public record GetAllIncomesQuery(
        int Skip = 0,
        int Take = 50,
        IncomeSortBy SortBy = IncomeSortBy.Date,
        SortDirection SortDirection = SortDirection.Descending
    ) : IRequest<Result<PaginatedIncomeResult>>;
}

namespace BudgetManagement.Features.Income.Queries
{
    /// <summary>
    /// Sort options for income queries
    /// </summary>
    public enum IncomeSortBy
    {
        Date,
        Amount,
        Description,
        CreatedAt
    }

    /// <summary>
    /// Sort direction
    /// </summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Result for advanced income search with pagination info
    /// </summary>
    public class AdvancedIncomeSearchResult
    {
        public IEnumerable<Models.Income> Incomes { get; set; } = [];
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// Result for paginated income queries
    /// </summary>
    public class PaginatedIncomeResult
    {
        public IEnumerable<Models.Income> Incomes { get; set; } = [];
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }
}