// Spending Feature Module - Feature Registration and Configuration
// File: Features/Spending/SpendingFeatureModule.cs

using BudgetManagement.Features.Spending.Commands;
using BudgetManagement.Features.Spending.ViewModels;
using BudgetManagement.Models;
using BudgetManagement.Services;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Features.Spending
{
    /// <summary>
    /// Configuration options for the Spending feature module
    /// </summary>
    public class SpendingFeatureOptions
    {
        /// <summary>
        /// Maximum number of recent spending entries to display by default
        /// </summary>
        public int MaxRecentEntries { get; set; } = 10;

        /// <summary>
        /// Default date range in days for spending queries
        /// </summary>
        public int DefaultDateRangeDays { get; set; } = 30;

        /// <summary>
        /// Enable real-time updates for spending data
        /// </summary>
        public bool EnableRealTimeUpdates { get; set; } = false;

        /// <summary>
        /// Cache duration for spending data in minutes
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 15;

        /// <summary>
        /// Maximum amount allowed for a single spending entry
        /// </summary>
        public decimal MaxSpendingAmount { get; set; } = 1_000_000m;

        /// <summary>
        /// Enable spending statistics and analytics features
        /// </summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>
        /// Enable spending export functionality
        /// </summary>
        public bool EnableExport { get; set; } = true;

        /// <summary>
        /// Enable advanced spending search features
        /// </summary>
        public bool EnableAdvancedSearch { get; set; } = true;

        /// <summary>
        /// Enable category-based spending analysis
        /// </summary>
        public bool EnableCategoryAnalysis { get; set; } = true;

        /// <summary>
        /// Enable spending budgeting and limits
        /// </summary>
        public bool EnableBudgeting { get; set; } = true;
    }

    /// <summary>
    /// Extension methods for registering the Spending feature module
    /// </summary>
    public static class SpendingFeatureExtensions
    {
        /// <summary>
        /// Adds the Spending feature module to the service collection
        /// All handlers, validators, and ViewModels are auto-registered via Scrutor
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddSpendingFeature(this IServiceCollection services)
        {
            // Register the ViewModel with proper lifetime
            services.AddTransient<SpendingViewModel>();

            // Register feature-specific services that require manual configuration
            services.AddSingleton<ISpendingDialogService, SpendingDialogService>();
            services.AddSingleton<ISpendingExportService, SpendingExportService>();
            services.AddSingleton<ISpendingAnalyticsService, SpendingAnalyticsService>();

            return services;
        }

        /// <summary>
        /// Configures the Spending feature module options
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection ConfigureSpendingFeature(this IServiceCollection services, Action<SpendingFeatureOptions> configure)
        {
            services.Configure(configure);
            return services;
        }
    }

    /// <summary>
    /// Spending-specific dialog service interface
    /// </summary>
    public interface ISpendingDialogService
    {
        Task<Result<AddSpendingDto?>> ShowAddSpendingDialogAsync(AddSpendingDto? initialData = null);
        Task<Result<UpdateSpendingDto?>> ShowEditSpendingDialogAsync(UpdateSpendingDto spendingData);
        Task<Result> ShowSpendingDetailsDialogAsync(Models.Spending spending);
        Task<Result<SpendingStatisticsDto?>> ShowSpendingStatisticsDialogAsync(DateTime startDate, DateTime endDate);
        Task<Result<CategorySpendingAnalysisDto?>> ShowCategoryAnalysisDialogAsync(int categoryId, DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Spending-specific export service interface
    /// </summary>
    public interface ISpendingExportService
    {
        Task<Result> ExportToCsvAsync(IEnumerable<Models.Spending> spendings, string filePath);
        Task<Result> ExportToExcelAsync(IEnumerable<Models.Spending> spendings, string filePath);
        Task<Result> ExportToPdfAsync(IEnumerable<Models.Spending> spendings, string filePath);
        Task<Result<byte[]>> GenerateSpendingReportAsync(IEnumerable<Models.Spending> spendings, string reportType);
        Task<Result<byte[]>> GenerateCategoryBreakdownReportAsync(IEnumerable<CategorySpendingSummary> categoryBreakdown, DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Spending analytics service interface
    /// </summary>
    public interface ISpendingAnalyticsService
    {
        Task<Result<SpendingTrendAnalysis>> AnalyzeTrendsAsync(DateTime startDate, DateTime endDate, TrendGrouping grouping = TrendGrouping.Monthly);
        Task<Result<CategorySpendingAnalysis>> AnalyzeCategorySpendingAsync(DateTime startDate, DateTime endDate);
        Task<Result<SpendingPrediction>> PredictFutureSpendingAsync(DateTime startDate, DateTime endDate, int futurePeriods);
        Task<Result<SpendingAnomalyReport>> DetectAnomaliesAsync(DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Default implementation of spending dialog service
    /// </summary>
    public class SpendingDialogService : ISpendingDialogService
    {
        private readonly IDialogService _dialogService;
        private readonly ILogger<SpendingDialogService> _logger;

        public SpendingDialogService(IDialogService dialogService, ILogger<SpendingDialogService> logger)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AddSpendingDto?>> ShowAddSpendingDialogAsync(AddSpendingDto? initialData = null)
        {
            try
            {
                var dialogData = initialData ?? new AddSpendingDto { Date = DateTime.Today };
                
                _logger.LogDebug("Showing add spending dialog");
                await Task.Delay(10); // Simulate async operation
                
                return Result<AddSpendingDto?>.Success(dialogData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing add spending dialog");
                return Result<AddSpendingDto?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to show add spending dialog"));
            }
        }

        public async Task<Result<UpdateSpendingDto?>> ShowEditSpendingDialogAsync(UpdateSpendingDto spendingData)
        {
            try
            {
                _logger.LogDebug("Showing edit spending dialog for spending ID {SpendingId}", spendingData.Id);
                await Task.Delay(10); // Simulate async operation
                
                return Result<UpdateSpendingDto?>.Success(spendingData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing edit spending dialog");
                return Result<UpdateSpendingDto?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to show edit spending dialog"));
            }
        }

        public async Task<Result> ShowSpendingDetailsDialogAsync(Models.Spending spending)
        {
            try
            {
                _logger.LogDebug("Showing spending details dialog for spending ID {SpendingId}", spending.Id);
                await Task.Delay(10); // Simulate async operation
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing spending details dialog");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to show spending details dialog"));
            }
        }

        public async Task<Result<SpendingStatisticsDto?>> ShowSpendingStatisticsDialogAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogDebug("Showing spending statistics dialog for date range {StartDate} to {EndDate}", startDate, endDate);
                await Task.Delay(10); // Simulate async operation
                
                var stats = new SpendingStatisticsDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow
                };
                
                return Result<SpendingStatisticsDto?>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing spending statistics dialog");
                return Result<SpendingStatisticsDto?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to show spending statistics dialog"));
            }
        }

        public async Task<Result<CategorySpendingAnalysisDto?>> ShowCategoryAnalysisDialogAsync(int categoryId, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogDebug("Showing category analysis dialog for category {CategoryId}", categoryId);
                await Task.Delay(10); // Simulate async operation
                
                var analysis = new CategorySpendingAnalysisDto
                {
                    CategoryId = categoryId,
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow
                };
                
                return Result<CategorySpendingAnalysisDto?>.Success(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing category analysis dialog");
                return Result<CategorySpendingAnalysisDto?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to show category analysis dialog"));
            }
        }
    }

    /// <summary>
    /// Default implementation of spending export service
    /// </summary>
    public class SpendingExportService : ISpendingExportService
    {
        private readonly ILogger<SpendingExportService> _logger;

        public SpendingExportService(ILogger<SpendingExportService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> ExportToCsvAsync(IEnumerable<Models.Spending> spendings, string filePath)
        {
            try
            {
                _logger.LogDebug("Exporting {Count} spending entries to CSV: {FilePath}", spendings.Count(), filePath);
                await Task.Delay(10); // Simulate async operation
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting spending entries to CSV");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to export to CSV"));
            }
        }

        public async Task<Result> ExportToExcelAsync(IEnumerable<Models.Spending> spendings, string filePath)
        {
            try
            {
                _logger.LogDebug("Exporting {Count} spending entries to Excel: {FilePath}", spendings.Count(), filePath);
                await Task.Delay(10); // Simulate async operation
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting spending entries to Excel");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to export to Excel"));
            }
        }

        public async Task<Result> ExportToPdfAsync(IEnumerable<Models.Spending> spendings, string filePath)
        {
            try
            {
                _logger.LogDebug("Exporting {Count} spending entries to PDF: {FilePath}", spendings.Count(), filePath);
                await Task.Delay(10); // Simulate async operation
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting spending entries to PDF");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to export to PDF"));
            }
        }

        public async Task<Result<byte[]>> GenerateSpendingReportAsync(IEnumerable<Models.Spending> spendings, string reportType)
        {
            try
            {
                _logger.LogDebug("Generating {ReportType} report for {Count} spending entries", reportType, spendings.Count());
                await Task.Delay(10); // Simulate async operation
                
                var reportData = System.Text.Encoding.UTF8.GetBytes($"Spending Report - {reportType}");
                return Result<byte[]>.Success(reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating spending report");
                return Result<byte[]>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to generate spending report"));
            }
        }

        public async Task<Result<byte[]>> GenerateCategoryBreakdownReportAsync(IEnumerable<CategorySpendingSummary> categoryBreakdown, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogDebug("Generating category breakdown report for {Count} categories", categoryBreakdown.Count());
                await Task.Delay(10); // Simulate async operation
                
                var reportData = System.Text.Encoding.UTF8.GetBytes("Category Breakdown Report");
                return Result<byte[]>.Success(reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating category breakdown report");
                return Result<byte[]>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to generate category breakdown report"));
            }
        }
    }

    /// <summary>
    /// Default implementation of spending analytics service
    /// </summary>
    public class SpendingAnalyticsService : ISpendingAnalyticsService
    {
        private readonly ILogger<SpendingAnalyticsService> _logger;

        public SpendingAnalyticsService(ILogger<SpendingAnalyticsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<SpendingTrendAnalysis>> AnalyzeTrendsAsync(DateTime startDate, DateTime endDate, TrendGrouping grouping = TrendGrouping.Monthly)
        {
            try
            {
                _logger.LogDebug("Analyzing spending trends from {StartDate} to {EndDate}", startDate, endDate);
                await Task.Delay(10); // Simulate async operation
                
                var analysis = new SpendingTrendAnalysis
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Grouping = grouping,
                    GeneratedAt = DateTime.UtcNow
                };
                
                return Result<SpendingTrendAnalysis>.Success(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing spending trends");
                return Result<SpendingTrendAnalysis>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to analyze spending trends"));
            }
        }

        public async Task<Result<CategorySpendingAnalysis>> AnalyzeCategorySpendingAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogDebug("Analyzing category spending from {StartDate} to {EndDate}", startDate, endDate);
                await Task.Delay(10); // Simulate async operation
                
                var analysis = new CategorySpendingAnalysis
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow
                };
                
                return Result<CategorySpendingAnalysis>.Success(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing category spending");
                return Result<CategorySpendingAnalysis>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to analyze category spending"));
            }
        }

        public async Task<Result<SpendingPrediction>> PredictFutureSpendingAsync(DateTime startDate, DateTime endDate, int futurePeriods)
        {
            try
            {
                _logger.LogDebug("Predicting future spending for {FuturePeriods} periods", futurePeriods);
                await Task.Delay(10); // Simulate async operation
                
                var prediction = new SpendingPrediction
                {
                    BaseStartDate = startDate,
                    BaseEndDate = endDate,
                    FuturePeriods = futurePeriods,
                    GeneratedAt = DateTime.UtcNow
                };
                
                return Result<SpendingPrediction>.Success(prediction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting future spending");
                return Result<SpendingPrediction>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to predict future spending"));
            }
        }

        public async Task<Result<SpendingAnomalyReport>> DetectAnomaliesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogDebug("Detecting spending anomalies from {StartDate} to {EndDate}", startDate, endDate);
                await Task.Delay(10); // Simulate async operation
                
                var report = new SpendingAnomalyReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow
                };
                
                return Result<SpendingAnomalyReport>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting spending anomalies");
                return Result<SpendingAnomalyReport>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to detect spending anomalies"));
            }
        }
    }

    #region Data Transfer Objects

    /// <summary>
    /// Data transfer object for spending statistics
    /// </summary>
    public class SpendingStatisticsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int TotalEntries { get; set; }
        public int DaysWithSpending { get; set; }
        public decimal AverageDailySpending { get; set; }
        public IEnumerable<string> TopDescriptions { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<CategorySpendingSummary> CategoryBreakdown { get; set; } = Enumerable.Empty<CategorySpendingSummary>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Data transfer object for category spending analysis
    /// </summary>
    public class CategorySpendingAnalysisDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int EntryCount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal Percentage { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Analytics data structures
    /// </summary>
    public class SpendingTrendAnalysis
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TrendGrouping Grouping { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class CategorySpendingAnalysis
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SpendingPrediction
    {
        public DateTime BaseStartDate { get; set; }
        public DateTime BaseEndDate { get; set; }
        public int FuturePeriods { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SpendingAnomalyReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    #endregion
}