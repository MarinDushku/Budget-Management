// Dashboard Summary Query Handler - Vertical Slice Implementation
// File: Features/Dashboard/Handlers/GetDashboardSummaryHandler.cs

using BudgetManagement.Features.Dashboard.Queries;
using BudgetManagement.Services;
using BudgetManagement.Shared.Core;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Features.Dashboard.Handlers
{
    /// <summary>
    /// Handler for GetDashboardSummaryQuery
    /// Implements the business logic for gathering dashboard summary data
    /// </summary>
    public class GetDashboardSummaryHandler : BaseQueryHandler<GetDashboardSummaryQuery, DashboardSummary>
    {
        private readonly IBudgetService _budgetService;
        private readonly ILogger<GetDashboardSummaryHandler> _logger;

        public GetDashboardSummaryHandler(
            IBudgetService budgetService,
            ILogger<GetDashboardSummaryHandler> logger)
        {
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<DashboardSummary>> Handle(
            GetDashboardSummaryQuery request, 
            CancellationToken cancellationToken)
        {
            try
            {
                LogQueryStart(request);

                // Validate the query
                var validationResult = ValidateQuery(request);
                if (validationResult.IsFailure)
                {
                    LogQueryComplete(request, validationResult);
                    return Result<DashboardSummary>.Failure(validationResult.Error!);
                }

                // Validate date range
                var dateValidation = ValidateDateRange(request.StartDate, request.EndDate);
                if (dateValidation.IsFailure)
                {
                    LogQueryComplete(request, dateValidation);
                    return Result<DashboardSummary>.Failure(dateValidation.Error!);
                }

                _logger.LogInformation("Fetching dashboard summary for period {StartDate} to {EndDate}", 
                    request.StartDate, request.EndDate);

                // Gather all required data in parallel for better performance
                var tasks = new List<Task>
                {
                    GetBudgetSummaryAsync(request.StartDate, request.EndDate),
                    GetBankStatementSummaryAsync(request.BankStatementDay),
                    GetRecentIncomeEntriesAsync(),
                    GetRecentSpendingEntriesAsync(),
                    GetBudgetTrendDataAsync(request.StartDate, request.EndDate)
                };

                await Task.WhenAll(tasks);

                // Extract results from completed tasks
                var budgetSummaryTask = (Task<Result<Models.BudgetSummary>>)tasks[0];
                var bankStatementSummaryTask = (Task<Result<Models.BankStatementSummary>>)tasks[1];
                var recentIncomeTask = (Task<Result<IEnumerable<Models.Income>>>)tasks[2];
                var recentSpendingTask = (Task<Result<IEnumerable<Models.SpendingWithCategory>>>)tasks[3];
                var trendDataTask = (Task<Result<IEnumerable<Models.WeeklyBudgetData>>>)tasks[4];

                // Check if any operation failed
                var results = new IResult[] { 
                    budgetSummaryTask.Result, 
                    bankStatementSummaryTask.Result,
                    recentIncomeTask.Result,
                    recentSpendingTask.Result,
                    trendDataTask.Result
                };

                var failedResults = results.Where(r => r.IsFailure).ToList();
                if (failedResults.Any())
                {
                    var firstError = failedResults.First().Error!;
                    _logger.LogError("Failed to fetch dashboard data: {Error}", firstError);
                    LogQueryComplete(request, Result.Failure(firstError));
                    return Result<DashboardSummary>.Failure(firstError);
                }

                // Build the dashboard summary
                var dashboardSummary = new DashboardSummary
                {
                    BudgetSummary = budgetSummaryTask.Result.Value!,
                    BankStatementSummary = bankStatementSummaryTask.Result.Value!,
                    RecentIncomeEntries = recentIncomeTask.Result.Value!.ToList(),
                    RecentSpendingEntries = recentSpendingTask.Result.Value!.ToList(),
                    BudgetTrendData = trendDataTask.Result.Value!.ToList(),
                    AverageDailySpending = CalculateAverageDailySpending(
                        recentSpendingTask.Result.Value!, 
                        request.StartDate, 
                        request.EndDate),
                    PeriodDescription = FormatPeriodDescription(request.StartDate, request.EndDate)
                };

                _logger.LogInformation("Successfully fetched dashboard summary with {IncomeCount} income entries and {SpendingCount} spending entries",
                    dashboardSummary.RecentIncomeEntries.Count,
                    dashboardSummary.RecentSpendingEntries.Count);

                var result = Result<DashboardSummary>.Success(dashboardSummary);
                LogQueryComplete(request, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching dashboard summary");
                var error = Error.System(Error.Codes.SYSTEM_ERROR, 
                    "An unexpected error occurred while fetching dashboard summary");
                LogQueryComplete(request, Result.Failure(error));
                return Result<DashboardSummary>.Failure(error);
            }
        }

        private async Task<Result<Models.BudgetSummary>> GetBudgetSummaryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var budgetSummary = await _budgetService.GetBudgetSummaryAsync(startDate, endDate);
                return Result<Models.BudgetSummary>.Success(budgetSummary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching budget summary");
                return Result<Models.BudgetSummary>.Failure(
                    Error.System(Error.Codes.SYSTEM_ERROR, "Failed to fetch budget summary"));
            }
        }

        private async Task<Result<Models.BankStatementSummary>> GetBankStatementSummaryAsync(int bankStatementDay)
        {
            try
            {
                var bankStatementSummary = await _budgetService.GetBankStatementSummaryAsync(bankStatementDay);
                return Result<Models.BankStatementSummary>.Success(bankStatementSummary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bank statement summary");
                return Result<Models.BankStatementSummary>.Failure(
                    Error.System(Error.Codes.SYSTEM_ERROR, "Failed to fetch bank statement summary"));
            }
        }

        private async Task<Result<IEnumerable<Models.Income>>> GetRecentIncomeEntriesAsync()
        {
            try
            {
                var threeMonthsAgo = DateTime.Now.AddMonths(-3);
                var now = DateTime.Now;
                var incomeEntries = await _budgetService.GetIncomeAsync(threeMonthsAgo, now);
                var recentEntries = incomeEntries.OrderByDescending(i => i.Date).Take(20).ToList();
                return Result<IEnumerable<Models.Income>>.Success(recentEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent income entries");
                return Result<IEnumerable<Models.Income>>.Failure(
                    Error.System(Error.Codes.SYSTEM_ERROR, "Failed to fetch recent income entries"));
            }
        }

        private async Task<Result<IEnumerable<Models.SpendingWithCategory>>> GetRecentSpendingEntriesAsync()
        {
            try
            {
                var threeMonthsAgo = DateTime.Now.AddMonths(-3);
                var now = DateTime.Now;
                var spendingEntries = await _budgetService.GetSpendingWithCategoryAsync(threeMonthsAgo, now);
                var recentEntries = spendingEntries.OrderByDescending(s => s.Date).Take(30).ToList();
                return Result<IEnumerable<Models.SpendingWithCategory>>.Success(recentEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent spending entries");
                return Result<IEnumerable<Models.SpendingWithCategory>>.Failure(
                    Error.System(Error.Codes.SYSTEM_ERROR, "Failed to fetch recent spending entries"));
            }
        }

        private async Task<Result<IEnumerable<Models.WeeklyBudgetData>>> GetBudgetTrendDataAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var trendData = new List<Models.WeeklyBudgetData>();
                var current = startDate;

                while (current <= endDate)
                {
                    var weekEnd = current.AddDays(6);
                    if (weekEnd > endDate) weekEnd = endDate;

                    var weekIncomeEntries = await _budgetService.GetIncomeAsync(current, weekEnd);
                    var weekSpendingEntries = await _budgetService.GetSpendingAsync(current, weekEnd);

                    var weekData = new Models.WeeklyBudgetData
                    {
                        WeekStartDate = current,
                        TotalIncome = weekIncomeEntries.Sum(i => i.Amount),
                        TotalSpending = weekSpendingEntries.Sum(s => s.Amount)
                    };

                    trendData.Add(weekData);
                    current = current.AddDays(7);
                }

                return Result<IEnumerable<Models.WeeklyBudgetData>>.Success(trendData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching budget trend data");
                return Result<IEnumerable<Models.WeeklyBudgetData>>.Failure(
                    Error.System(Error.Codes.SYSTEM_ERROR, "Failed to fetch budget trend data"));
            }
        }

        private static decimal CalculateAverageDailySpending(
            IEnumerable<Models.SpendingWithCategory> spendingEntries, 
            DateTime startDate, 
            DateTime endDate)
        {
            var totalSpending = spendingEntries.Sum(s => s.Amount);
            var daysDiff = Math.Max(1, (endDate - startDate).Days + 1);
            return totalSpending / daysDiff;
        }

        private static string FormatPeriodDescription(DateTime startDate, DateTime endDate)
        {
            if (startDate.Year == endDate.Year && startDate.Month == endDate.Month)
            {
                return $"{startDate:MMMM yyyy}";
            }
            
            if (startDate.Year == endDate.Year)
            {
                return $"{startDate:MMM} - {endDate:MMM yyyy}";
            }
            
            return $"{startDate:MMM yyyy} - {endDate:MMM yyyy}";
        }

        private static Result ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                return Result.Failure(Error.Validation(Error.Codes.INVALID_DATE, 
                    "Start date cannot be after end date"));
            }

            if (endDate > DateTime.Now.AddDays(1))
            {
                return Result.Failure(Error.Validation(Error.Codes.INVALID_DATE, 
                    "End date cannot be in the future"));
            }

            var daysDifference = (endDate - startDate).Days;
            if (daysDifference > 365)
            {
                return Result.Failure(Error.Validation(Error.Codes.INVALID_DATE, 
                    "Date range cannot exceed 365 days"));
            }

            return Result.Success();
        }
    }
}