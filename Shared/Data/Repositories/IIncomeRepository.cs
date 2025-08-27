// Income Repository Interface - Domain-Specific Data Access
// File: Shared/Data/Repositories/IIncomeRepository.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;

namespace BudgetManagement.Shared.Data.Repositories
{
    /// <summary>
    /// Repository interface for Income entity with domain-specific operations
    /// Extends the generic repository with income-specific queries and operations
    /// </summary>
    public interface IIncomeRepository : IRepository<Income>
    {
        /// <summary>
        /// Gets income entries within a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Income entries within the date range</returns>
        Task<Result<IEnumerable<Income>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the most recent income entries
        /// </summary>
        /// <param name="count">Number of entries to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Most recent income entries ordered by date descending</returns>
        Task<Result<IEnumerable<Income>>> GetMostRecentAsync(int count = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total income for a specific date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total income amount</returns>
        Task<Result<decimal>> GetTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets income entries by description pattern
        /// </summary>
        /// <param name="descriptionPattern">Description pattern to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Income entries matching the description pattern</returns>
        Task<Result<IEnumerable<Income>>> GetByDescriptionPatternAsync(string descriptionPattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets income entries by amount range
        /// </summary>
        /// <param name="minAmount">Minimum amount (inclusive)</param>
        /// <param name="maxAmount">Maximum amount (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Income entries within the amount range</returns>
        Task<Result<IEnumerable<Income>>> GetByAmountRangeAsync(decimal minAmount, decimal maxAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets monthly income totals for a specific year
        /// </summary>
        /// <param name="year">Year to get monthly totals for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Monthly income totals</returns>
        Task<Result<IEnumerable<MonthlyIncomeSummary>>> GetMonthlyTotalsAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets average daily income for a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Average daily income</returns>
        Task<Result<decimal>> GetAverageDailyIncomeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets income statistics for a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Income statistics</returns>
        Task<Result<IncomeStatistics>> GetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets income trend data for analytics
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="groupBy">Grouping period (daily, weekly, monthly)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Income trend data</returns>
        Task<Result<IEnumerable<IncomeTrendData>>> GetTrendDataAsync(
            DateTime startDate, 
            DateTime endDate, 
            TrendGrouping groupBy = TrendGrouping.Daily,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk delete income entries by date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of deleted entries</returns>
        Task<Result<int>> DeleteByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if there are any income entries for a specific date
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if entries exist for the date</returns>
        Task<Result<bool>> HasEntriesForDateAsync(DateTime date, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Monthly income summary data structure
    /// </summary>
    public class MonthlyIncomeSummary
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
    }

    /// <summary>
    /// Income statistics data structure
    /// </summary>
    public class IncomeStatistics
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
        public int DaysWithIncome { get; set; }
        public decimal AverageDailyIncome { get; set; }
        public IEnumerable<string> TopDescriptions { get; set; } = Enumerable.Empty<string>();
    }

    /// <summary>
    /// Income trend data for analytics
    /// </summary>
    public class IncomeTrendData
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalAmount { get; set; }
        public int EntryCount { get; set; }
        public decimal AverageAmount { get; set; }
        public TrendGrouping Grouping { get; set; }
        public string PeriodLabel => Grouping switch
        {
            TrendGrouping.Daily => PeriodStart.ToString("yyyy-MM-dd"),
            TrendGrouping.Weekly => $"Week of {PeriodStart:MMM dd}",
            TrendGrouping.Monthly => PeriodStart.ToString("MMM yyyy"),
            TrendGrouping.Yearly => PeriodStart.ToString("yyyy"),
            _ => PeriodStart.ToString("yyyy-MM-dd")
        };
    }

    /// <summary>
    /// Trend grouping options
    /// </summary>
    public enum TrendGrouping
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }
}