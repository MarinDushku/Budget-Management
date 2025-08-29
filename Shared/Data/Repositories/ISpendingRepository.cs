// Spending Repository Interface - Domain-Specific Data Access
// File: Shared/Data/Repositories/ISpendingRepository.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using BudgetManagement.Features.Spending.Queries;

namespace BudgetManagement.Shared.Data.Repositories
{
    /// <summary>
    /// Repository interface for Spending entity with domain-specific operations
    /// Extends the generic repository with spending-specific queries and operations
    /// </summary>
    public interface ISpendingRepository : IRepository<Spending>
    {
        /// <summary>
        /// Gets spending entries within a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Spending entries within the date range</returns>
        Task<Result<IEnumerable<Spending>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets spending entries with category information within a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Spending entries with category information</returns>
        Task<Result<IEnumerable<SpendingWithCategory>>> GetWithCategoryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the most recent spending entries
        /// </summary>
        /// <param name="count">Number of entries to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Most recent spending entries ordered by date descending</returns>
        Task<Result<IEnumerable<Spending>>> GetMostRecentAsync(int count = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the most recent spending entries with category information
        /// </summary>
        /// <param name="count">Number of entries to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Most recent spending entries with category information</returns>
        Task<Result<IEnumerable<SpendingWithCategory>>> GetMostRecentWithCategoryAsync(int count = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets spending entries by category ID
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <param name="startDate">Start date (optional)</param>
        /// <param name="endDate">End date (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Spending entries for the specified category</returns>
        Task<Result<IEnumerable<Spending>>> GetByCategoryAsync(int categoryId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total spending for a specific date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total spending amount</returns>
        Task<Result<decimal>> GetTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total spending by category for a date range
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total spending for the category</returns>
        Task<Result<decimal>> GetTotalByCategoryAsync(int categoryId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets spending entries by description pattern
        /// </summary>
        /// <param name="descriptionPattern">Description pattern to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Spending entries matching the description pattern</returns>
        Task<Result<IEnumerable<Spending>>> GetByDescriptionPatternAsync(string descriptionPattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets spending entries by amount range
        /// </summary>
        /// <param name="minAmount">Minimum amount (inclusive)</param>
        /// <param name="maxAmount">Maximum amount (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Spending entries within the amount range</returns>
        Task<Result<IEnumerable<Spending>>> GetByAmountRangeAsync(decimal minAmount, decimal maxAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets category spending summary for a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Spending summary by category</returns>
        Task<Result<IEnumerable<CategorySpendingSummary>>> GetCategorySpendingSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets monthly spending totals for a specific year
        /// </summary>
        /// <param name="year">Year to get monthly totals for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Monthly spending totals</returns>
        Task<Result<IEnumerable<MonthlySpendingSummary>>> GetMonthlyTotalsAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets average daily spending for a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Average daily spending</returns>
        Task<Result<decimal>> GetAverageDailySpendingAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets spending statistics for a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Spending statistics</returns>
        Task<Result<SpendingStatistics>> GetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets spending trend data for analytics
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="groupBy">Grouping period (daily, weekly, monthly)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Spending trend data</returns>
        Task<Result<IEnumerable<SpendingTrendData>>> GetTrendDataAsync(
            DateTime startDate, 
            DateTime endDate, 
            TrendGrouping groupBy = TrendGrouping.Daily,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets top spending entries by amount
        /// </summary>
        /// <param name="count">Number of top entries to retrieve</param>
        /// <param name="startDate">Start date (optional)</param>
        /// <param name="endDate">End date (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Top spending entries by amount</returns>
        Task<Result<IEnumerable<SpendingWithCategory>>> GetTopSpendingAsync(int count, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk delete spending entries by date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of deleted entries</returns>
        Task<Result<int>> DeleteByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk delete spending entries by category
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of deleted entries</returns>
        Task<Result<int>> DeleteByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if there are any spending entries for a specific date
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if entries exist for the date</returns>
        Task<Result<bool>> HasEntriesForDateAsync(DateTime date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs advanced spending search with multiple criteria and pagination
        /// </summary>
        /// <param name="descriptionPattern">Optional description pattern to search for</param>
        /// <param name="startDate">Optional start date filter (inclusive)</param>
        /// <param name="endDate">Optional end date filter (inclusive)</param>
        /// <param name="minAmount">Optional minimum amount filter (inclusive)</param>
        /// <param name="maxAmount">Optional maximum amount filter (inclusive)</param>
        /// <param name="categoryIds">Optional list of category IDs to filter by</param>
        /// <param name="skip">Number of records to skip for pagination</param>
        /// <param name="take">Number of records to take for pagination</param>
        /// <param name="sortBy">Field to sort by</param>
        /// <param name="sortDirection">Sort direction (ascending or descending)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Advanced search result with spending entries and pagination info</returns>
        Task<Result<AdvancedSpendingSearchResult>> GetAdvancedSearchAsync(
            string? descriptionPattern = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            List<int>? categoryIds = null,
            int skip = 0,
            int take = 50,
            SpendingSortBy sortBy = SpendingSortBy.Date,
            SortDirection sortDirection = SortDirection.Descending,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Category spending summary data structure
    /// </summary>
    public class CategorySpendingSummary
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryIcon { get; set; }
        public decimal TotalAmount { get; set; }
        public int EntryCount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal PercentageOfTotal { get; set; }
        public DateTime EarliestDate { get; set; }
        public DateTime LatestDate { get; set; }
    }

    /// <summary>
    /// Monthly spending summary data structure
    /// </summary>
    public class MonthlySpendingSummary
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalAmount { get; set; }
        public int EntryCount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");
        public DateTime StartDate => new DateTime(Year, Month, 1);
        public DateTime EndDate => new DateTime(Year, Month, DateTime.DaysInMonth(Year, Month));
        public IEnumerable<CategorySpendingSummary> CategoryBreakdown { get; set; } = Enumerable.Empty<CategorySpendingSummary>();
    }

    /// <summary>
    /// Spending statistics data structure
    /// </summary>
    public class SpendingStatistics
    {
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int TotalEntries { get; set; }
        public decimal StandardDeviation { get; set; }
        public decimal MedianAmount { get; set; }
        public DateTime EarliestDate { get; set; }
        public DateTime LatestDate { get; set; }
        public int DaysWithSpending { get; set; }
        public decimal AverageDailySpending { get; set; }
        public int UniqueCategoriesUsed { get; set; }
        public CategorySpendingSummary TopCategory { get; set; } = new();
        public IEnumerable<string> TopDescriptions { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<CategorySpendingSummary> CategoryBreakdown { get; set; } = Enumerable.Empty<CategorySpendingSummary>();
    }

    /// <summary>
    /// Spending trend data for analytics
    /// </summary>
    public class SpendingTrendData
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalAmount { get; set; }
        public int EntryCount { get; set; }
        public decimal AverageAmount { get; set; }
        public TrendGrouping Grouping { get; set; }
        public IEnumerable<CategorySpendingSummary> CategoryBreakdown { get; set; } = Enumerable.Empty<CategorySpendingSummary>();
        public string PeriodLabel => Grouping switch
        {
            TrendGrouping.Daily => PeriodStart.ToString("yyyy-MM-dd"),
            TrendGrouping.Weekly => $"Week of {PeriodStart:MMM dd}",
            TrendGrouping.Monthly => PeriodStart.ToString("MMM yyyy"),
            TrendGrouping.Yearly => PeriodStart.ToString("yyyy"),
            _ => PeriodStart.ToString("yyyy-MM-dd")
        };
    }
}