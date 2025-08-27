// Income Feature Module - Feature Registration and Configuration
// File: Features/Income/IncomeFeatureModule.cs

using BudgetManagement.Features.Income.Commands;
using BudgetManagement.Features.Income.ViewModels;
using BudgetManagement.Services;
using BudgetManagement.Shared.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Features.Income
{
    /// <summary>
    /// Configuration options for the Income feature module
    /// </summary>
    public class IncomeFeatureOptions
    {
        /// <summary>
        /// Maximum number of recent income entries to display by default
        /// </summary>
        public int MaxRecentEntries { get; set; } = 10;

        /// <summary>
        /// Default date range in days for income queries
        /// </summary>
        public int DefaultDateRangeDays { get; set; } = 30;

        /// <summary>
        /// Enable real-time updates for income data
        /// </summary>
        public bool EnableRealTimeUpdates { get; set; } = false;

        /// <summary>
        /// Cache duration for income data in minutes
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 15;

        /// <summary>
        /// Maximum amount allowed for a single income entry
        /// </summary>
        public decimal MaxIncomeAmount { get; set; } = 1_000_000m;

        /// <summary>
        /// Enable income statistics and analytics features
        /// </summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>
        /// Enable income export functionality
        /// </summary>
        public bool EnableExport { get; set; } = true;

        /// <summary>
        /// Enable advanced income search features
        /// </summary>
        public bool EnableAdvancedSearch { get; set; } = true;
    }

    /// <summary>
    /// Extension methods for registering the Income feature module
    /// </summary>
    public static class IncomeFeatureExtensions
    {
        /// <summary>
        /// Adds the Income feature module to the service collection
        /// All handlers, validators, and ViewModels are auto-registered via Scrutor
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddIncomeFeature(this IServiceCollection services)
        {
            // Register the ViewModel with proper lifetime
            services.AddTransient<IncomeViewModel>();

            // Register feature-specific services that require manual configuration
            services.AddSingleton<IIncomeDialogService, IncomeDialogService>();
            services.AddSingleton<IIncomeExportService, IncomeExportService>();

            return services;
        }

        /// <summary>
        /// Configures the Income feature module options
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection ConfigureIncomeFeature(this IServiceCollection services, Action<IncomeFeatureOptions> configure)
        {
            services.Configure(configure);
            return services;
        }
    }

    /// <summary>
    /// Income-specific dialog service interface
    /// </summary>
    public interface IIncomeDialogService
    {
        Task<Result<AddIncomeDto?>> ShowAddIncomeDialogAsync(AddIncomeDto? initialData = null);
        Task<Result<UpdateIncomeDto?>> ShowEditIncomeDialogAsync(UpdateIncomeDto incomeData);
        Task<Result> ShowIncomeDetailsDialogAsync(Models.Income income);
        Task<Result<IncomeStatisticsDto?>> ShowIncomeStatisticsDialogAsync(DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Income-specific export service interface
    /// </summary>
    public interface IIncomeExportService
    {
        Task<Result> ExportToCsvAsync(IEnumerable<Models.Income> incomes, string filePath);
        Task<Result> ExportToExcelAsync(IEnumerable<Models.Income> incomes, string filePath);
        Task<Result> ExportToPdfAsync(IEnumerable<Models.Income> incomes, string filePath);
        Task<Result<byte[]>> GenerateIncomeReportAsync(IEnumerable<Models.Income> incomes, string reportType);
    }

    /// <summary>
    /// Default implementation of income dialog service
    /// </summary>
    public class IncomeDialogService : IIncomeDialogService
    {
        private readonly IDialogService _dialogService;
        private readonly ILogger<IncomeDialogService> _logger;

        public IncomeDialogService(IDialogService dialogService, ILogger<IncomeDialogService> logger)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AddIncomeDto?>> ShowAddIncomeDialogAsync(AddIncomeDto? initialData = null)
        {
            try
            {
                var dialogData = initialData ?? new AddIncomeDto { Date = DateTime.Today };
                
                // This would show the actual income dialog
                // For now, we'll simulate the dialog result
                _logger.LogDebug("Showing add income dialog");
                
                // In a real implementation, this would show the WPF dialog
                // and return the actual user input
                await Task.Delay(10); // Simulate async operation
                
                return Result<AddIncomeDto?>.Success(dialogData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing add income dialog");
                return Result<AddIncomeDto?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to show add income dialog"));
            }
        }

        public async Task<Result<UpdateIncomeDto?>> ShowEditIncomeDialogAsync(UpdateIncomeDto incomeData)
        {
            try
            {
                _logger.LogDebug("Showing edit income dialog for income ID {IncomeId}", incomeData.Id);
                
                // In a real implementation, this would show the WPF dialog
                await Task.Delay(10); // Simulate async operation
                
                return Result<UpdateIncomeDto?>.Success(incomeData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing edit income dialog");
                return Result<UpdateIncomeDto?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to show edit income dialog"));
            }
        }

        public async Task<Result> ShowIncomeDetailsDialogAsync(Models.Income income)
        {
            try
            {
                _logger.LogDebug("Showing income details dialog for income ID {IncomeId}", income.Id);
                
                // In a real implementation, this would show the details dialog
                await Task.Delay(10); // Simulate async operation
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing income details dialog");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to show income details dialog"));
            }
        }

        public async Task<Result<IncomeStatisticsDto?>> ShowIncomeStatisticsDialogAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogDebug("Showing income statistics dialog for date range {StartDate} to {EndDate}", startDate, endDate);
                
                // In a real implementation, this would show the statistics dialog
                await Task.Delay(10); // Simulate async operation
                
                var stats = new IncomeStatisticsDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow
                };
                
                return Result<IncomeStatisticsDto?>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing income statistics dialog");
                return Result<IncomeStatisticsDto?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to show income statistics dialog"));
            }
        }
    }

    /// <summary>
    /// Default implementation of income export service
    /// </summary>
    public class IncomeExportService : IIncomeExportService
    {
        private readonly ILogger<IncomeExportService> _logger;

        public IncomeExportService(ILogger<IncomeExportService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> ExportToCsvAsync(IEnumerable<Models.Income> incomes, string filePath)
        {
            try
            {
                _logger.LogDebug("Exporting {Count} income entries to CSV: {FilePath}", incomes.Count(), filePath);
                
                // In a real implementation, this would generate CSV content
                await Task.Delay(10); // Simulate async operation
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting income entries to CSV");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to export to CSV"));
            }
        }

        public async Task<Result> ExportToExcelAsync(IEnumerable<Models.Income> incomes, string filePath)
        {
            try
            {
                _logger.LogDebug("Exporting {Count} income entries to Excel: {FilePath}", incomes.Count(), filePath);
                
                // In a real implementation, this would generate Excel content
                await Task.Delay(10); // Simulate async operation
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting income entries to Excel");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to export to Excel"));
            }
        }

        public async Task<Result> ExportToPdfAsync(IEnumerable<Models.Income> incomes, string filePath)
        {
            try
            {
                _logger.LogDebug("Exporting {Count} income entries to PDF: {FilePath}", incomes.Count(), filePath);
                
                // In a real implementation, this would generate PDF content
                await Task.Delay(10); // Simulate async operation
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting income entries to PDF");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to export to PDF"));
            }
        }

        public async Task<Result<byte[]>> GenerateIncomeReportAsync(IEnumerable<Models.Income> incomes, string reportType)
        {
            try
            {
                _logger.LogDebug("Generating {ReportType} report for {Count} income entries", reportType, incomes.Count());
                
                // In a real implementation, this would generate the report
                await Task.Delay(10); // Simulate async operation
                
                var reportData = System.Text.Encoding.UTF8.GetBytes($"Income Report - {reportType}");
                return Result<byte[]>.Success(reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating income report");
                return Result<byte[]>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to generate income report"));
            }
        }
    }

    /// <summary>
    /// Data transfer object for income statistics
    /// </summary>
    public class IncomeStatisticsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int TotalEntries { get; set; }
        public int DaysWithIncome { get; set; }
        public decimal AverageDailyIncome { get; set; }
        public IEnumerable<string> TopDescriptions { get; set; } = Enumerable.Empty<string>();
        public DateTime GeneratedAt { get; set; }
    }
}