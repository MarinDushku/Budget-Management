// Dashboard Summary Query - Vertical Slice Implementation
// File: Features/Dashboard/Queries/GetDashboardSummaryQuery.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;

namespace BudgetManagement.Features.Dashboard.Queries
{
    /// <summary>
    /// Query to get dashboard summary information
    /// Part of Dashboard vertical slice
    /// </summary>
    public record GetDashboardSummaryQuery : BaseQuery<DashboardSummary>
    {
        /// <summary>
        /// Start date for the summary period
        /// </summary>
        public DateTime StartDate { get; init; }

        /// <summary>
        /// End date for the summary period
        /// </summary>
        public DateTime EndDate { get; init; }

        /// <summary>
        /// Bank statement day for bank statement summary
        /// </summary>
        public int BankStatementDay { get; init; } = 1;

        public GetDashboardSummaryQuery(DateTime startDate, DateTime endDate, int bankStatementDay = 1)
        {
            StartDate = startDate;
            EndDate = endDate;
            BankStatementDay = bankStatementDay;
        }

        /// <summary>
        /// Creates a query for the current month
        /// </summary>
        public static GetDashboardSummaryQuery ForCurrentMonth(int bankStatementDay = 1)
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            
            return new GetDashboardSummaryQuery(startOfMonth, endOfMonth, bankStatementDay);
        }

        /// <summary>
        /// Creates a query for the current year
        /// </summary>
        public static GetDashboardSummaryQuery ForCurrentYear(int bankStatementDay = 1)
        {
            var now = DateTime.Now;
            var startOfYear = new DateTime(now.Year, 1, 1);
            var endOfYear = new DateTime(now.Year, 12, 31);
            
            return new GetDashboardSummaryQuery(startOfYear, endOfYear, bankStatementDay);
        }

        /// <summary>
        /// Creates a query for the last N days
        /// </summary>
        public static GetDashboardSummaryQuery ForLastDays(int days, int bankStatementDay = 1)
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-days + 1);
            
            return new GetDashboardSummaryQuery(startDate, endDate, bankStatementDay);
        }
    }

    /// <summary>
    /// Dashboard summary data transfer object
    /// Contains all the information needed for the dashboard view
    /// </summary>
    public class DashboardSummary
    {
        /// <summary>
        /// Budget summary for the selected period
        /// </summary>
        public BudgetSummary BudgetSummary { get; set; } = new();

        /// <summary>
        /// Bank statement summary
        /// </summary>
        public BankStatementSummary BankStatementSummary { get; set; } = new();

        /// <summary>
        /// Recent income entries (last 5)
        /// </summary>
        public IReadOnlyList<Models.Income> RecentIncomeEntries { get; set; } = new List<Models.Income>();

        /// <summary>
        /// Recent spending entries (last 5)
        /// </summary>
        public IReadOnlyList<SpendingWithCategory> RecentSpendingEntries { get; set; } = new List<SpendingWithCategory>();

        /// <summary>
        /// Weekly budget trend data for analytics
        /// </summary>
        public IReadOnlyList<Models.WeeklyBudgetData> BudgetTrendData { get; set; } = new List<Models.WeeklyBudgetData>();

        /// <summary>
        /// Whether there are no recent entries
        /// </summary>
        public bool HasNoRecentEntries => !RecentIncomeEntries.Any() && !RecentSpendingEntries.Any();

        /// <summary>
        /// Total number of entries in the period
        /// </summary>
        public int TotalEntries => RecentIncomeEntries.Count + RecentSpendingEntries.Count;

        /// <summary>
        /// Average daily spending for the period
        /// </summary>
        public decimal AverageDailySpending { get; set; }

        /// <summary>
        /// Period description for display
        /// </summary>
        public string PeriodDescription { get; set; } = string.Empty;
    }
}