// Budget Cache Service - Domain-Specific Caching Implementation
// File: Shared/Infrastructure/Caching/BudgetCacheService.cs

using BudgetManagement.Features.Dashboard.Queries;
using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Shared.Infrastructure.Caching
{
    /// <summary>
    /// Domain-specific cache service for Budget Management application
    /// Provides high-level caching operations for budget-related data
    /// </summary>
    public interface IBudgetCacheService
    {
        // Dashboard Caching
        Task<Result<DashboardSummary?>> GetDashboardSummaryAsync(DateTime startDate, DateTime endDate, int bankStatementDay, CancellationToken cancellationToken = default);
        Task<Result> SetDashboardSummaryAsync(DateTime startDate, DateTime endDate, int bankStatementDay, DashboardSummary summary, CancellationToken cancellationToken = default);
        Task<Result> InvalidateDashboardSummariesAsync(CancellationToken cancellationToken = default);

        // Income Caching
        Task<Result<IEnumerable<Income>?>> GetIncomeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<Result> SetIncomeAsync(DateTime startDate, DateTime endDate, IEnumerable<Income> incomes, CancellationToken cancellationToken = default);
        Task<Result> InvalidateIncomeAsync(DateTime? affectedDate = null, CancellationToken cancellationToken = default);

        // Spending Caching
        Task<Result<IEnumerable<Spending>?>> GetSpendingAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<SpendingWithCategory>?>> GetSpendingWithCategoryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<Result> SetSpendingAsync(DateTime startDate, DateTime endDate, IEnumerable<Spending> spendings, CancellationToken cancellationToken = default);
        Task<Result> SetSpendingWithCategoryAsync(DateTime startDate, DateTime endDate, IEnumerable<SpendingWithCategory> spendings, CancellationToken cancellationToken = default);
        Task<Result> InvalidateSpendingAsync(DateTime? affectedDate = null, int? categoryId = null, CancellationToken cancellationToken = default);

        // Category Caching
        Task<Result<IEnumerable<Category>?>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<Result> SetCategoriesAsync(IEnumerable<Category> categories, bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<Result> InvalidateCategoriesAsync(CancellationToken cancellationToken = default);

        // Summary Caching
        Task<Result<BudgetSummary?>> GetBudgetSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<Result> SetBudgetSummaryAsync(DateTime startDate, DateTime endDate, BudgetSummary summary, CancellationToken cancellationToken = default);
        Task<Result<BankStatementSummary?>> GetBankStatementSummaryAsync(int statementDay, CancellationToken cancellationToken = default);
        Task<Result> SetBankStatementSummaryAsync(int statementDay, BankStatementSummary summary, CancellationToken cancellationToken = default);

        // Cache Management
        Task<Result> WarmupAsync(CancellationToken cancellationToken = default);
        Task<Result> InvalidateAllAsync(CancellationToken cancellationToken = default);
        Task<Result<BudgetCacheStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of domain-specific budget cache service
    /// </summary>
    public class BudgetCacheService : IBudgetCacheService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<BudgetCacheService> _logger;
        private readonly ICacheKeyBuilder _keyBuilder;

        // Cache configuration constants
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShortExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan LongExpiration = TimeSpan.FromHours(1);
        private static readonly TimeSpan CategoryExpiration = TimeSpan.FromMinutes(30);

        // Cache key prefixes
        private const string DashboardPrefix = "dashboard";
        private const string IncomePrefix = "income";
        private const string SpendingPrefix = "spending";
        private const string SpendingWithCategoryPrefix = "spending-category";
        private const string CategoryPrefix = "categories";
        private const string BudgetSummaryPrefix = "budget-summary";
        private const string BankStatementPrefix = "bank-statement";

        public BudgetCacheService(
            ICacheService cacheService,
            ILogger<BudgetCacheService> logger,
            ICacheKeyBuilder? keyBuilder = null)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keyBuilder = keyBuilder ?? new DefaultCacheKeyBuilder();
        }

        #region Dashboard Caching

        public async Task<Result<DashboardSummary?>> GetDashboardSummaryAsync(DateTime startDate, DateTime endDate, int bankStatementDay, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(DashboardPrefix, FormatDate(startDate), FormatDate(endDate), bankStatementDay);
            var result = await _cacheService.GetAsync<DashboardSummary>(key, cancellationToken);
            
            if (result.IsSuccess && result.Value != null)
            {
                _logger.LogDebug("Dashboard summary cache hit for {StartDate}-{EndDate}", startDate, endDate);
            }

            return result;
        }

        public async Task<Result> SetDashboardSummaryAsync(DateTime startDate, DateTime endDate, int bankStatementDay, DashboardSummary summary, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(DashboardPrefix, FormatDate(startDate), FormatDate(endDate), bankStatementDay);
            var result = await _cacheService.SetAsync(key, summary, DefaultExpiration, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Cached dashboard summary for {StartDate}-{EndDate}", startDate, endDate);
            }

            return result;
        }

        public async Task<Result> InvalidateDashboardSummariesAsync(CancellationToken cancellationToken = default)
        {
            var pattern = _keyBuilder.BuildKey(DashboardPrefix, "*");
            var result = await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Invalidated all dashboard summaries");
            }

            return result;
        }

        #endregion

        #region Income Caching

        public async Task<Result<IEnumerable<Income>?>> GetIncomeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(IncomePrefix, FormatDate(startDate), FormatDate(endDate));
            var result = await _cacheService.GetAsync<List<Income>>(key, cancellationToken);
            
            if (result.IsSuccess && result.Value != null)
            {
                _logger.LogDebug("Income cache hit for {StartDate}-{EndDate}", startDate, endDate);
                return Result<IEnumerable<Income>?>.Success(result.Value);
            }

            return Result<IEnumerable<Income>?>.Success(null);
        }

        public async Task<Result> SetIncomeAsync(DateTime startDate, DateTime endDate, IEnumerable<Income> incomes, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(IncomePrefix, FormatDate(startDate), FormatDate(endDate));
            var incomeList = incomes.ToList();
            var result = await _cacheService.SetAsync(key, incomeList, DefaultExpiration, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Cached {Count} income entries for {StartDate}-{EndDate}", incomeList.Count, startDate, endDate);
            }

            return result;
        }

        public async Task<Result> InvalidateIncomeAsync(DateTime? affectedDate = null, CancellationToken cancellationToken = default)
        {
            var pattern = affectedDate.HasValue
                ? _keyBuilder.BuildKey(IncomePrefix, "*", FormatDate(affectedDate.Value), "*")
                : _keyBuilder.BuildKey(IncomePrefix, "*");

            var result = await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Invalidated income cache entries {AffectedDate}", affectedDate?.ToString("yyyy-MM-dd") ?? "all");
            }

            return result;
        }

        #endregion

        #region Spending Caching

        public async Task<Result<IEnumerable<Spending>?>> GetSpendingAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(SpendingPrefix, FormatDate(startDate), FormatDate(endDate));
            var result = await _cacheService.GetAsync<List<Spending>>(key, cancellationToken);
            
            if (result.IsSuccess && result.Value != null)
            {
                _logger.LogDebug("Spending cache hit for {StartDate}-{EndDate}", startDate, endDate);
                return Result<IEnumerable<Spending>?>.Success(result.Value);
            }

            return Result<IEnumerable<Spending>?>.Success(null);
        }

        public async Task<Result<IEnumerable<SpendingWithCategory>?>> GetSpendingWithCategoryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(SpendingWithCategoryPrefix, FormatDate(startDate), FormatDate(endDate));
            var result = await _cacheService.GetAsync<List<SpendingWithCategory>>(key, cancellationToken);
            
            if (result.IsSuccess && result.Value != null)
            {
                _logger.LogDebug("Spending with category cache hit for {StartDate}-{EndDate}", startDate, endDate);
                return Result<IEnumerable<SpendingWithCategory>?>.Success(result.Value);
            }

            return Result<IEnumerable<SpendingWithCategory>?>.Success(null);
        }

        public async Task<Result> SetSpendingAsync(DateTime startDate, DateTime endDate, IEnumerable<Spending> spendings, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(SpendingPrefix, FormatDate(startDate), FormatDate(endDate));
            var spendingList = spendings.ToList();
            var result = await _cacheService.SetAsync(key, spendingList, DefaultExpiration, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Cached {Count} spending entries for {StartDate}-{EndDate}", spendingList.Count, startDate, endDate);
            }

            return result;
        }

        public async Task<Result> SetSpendingWithCategoryAsync(DateTime startDate, DateTime endDate, IEnumerable<SpendingWithCategory> spendings, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(SpendingWithCategoryPrefix, FormatDate(startDate), FormatDate(endDate));
            var spendingList = spendings.ToList();
            var result = await _cacheService.SetAsync(key, spendingList, DefaultExpiration, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Cached {Count} spending with category entries for {StartDate}-{EndDate}", spendingList.Count, startDate, endDate);
            }

            return result;
        }

        public async Task<Result> InvalidateSpendingAsync(DateTime? affectedDate = null, int? categoryId = null, CancellationToken cancellationToken = default)
        {
            var patterns = new List<string>();

            if (affectedDate.HasValue)
            {
                patterns.Add(_keyBuilder.BuildKey(SpendingPrefix, "*", FormatDate(affectedDate.Value), "*"));
                patterns.Add(_keyBuilder.BuildKey(SpendingWithCategoryPrefix, "*", FormatDate(affectedDate.Value), "*"));
            }
            else
            {
                patterns.Add(_keyBuilder.BuildKey(SpendingPrefix, "*"));
                patterns.Add(_keyBuilder.BuildKey(SpendingWithCategoryPrefix, "*"));
            }

            var results = new List<Result>();
            foreach (var pattern in patterns)
            {
                results.Add(await _cacheService.RemoveByPatternAsync(pattern, cancellationToken));
            }

            var success = results.All(r => r.IsSuccess);
            if (success)
            {
                var context = affectedDate?.ToString("yyyy-MM-dd") ?? "all";
                if (categoryId.HasValue) context += $", category: {categoryId.Value}";
                _logger.LogInformation("Invalidated spending cache entries for {Context}", context);
            }

            return success ? Result.Success() : results.First(r => r.IsFailure);
        }

        #endregion

        #region Category Caching

        public async Task<Result<IEnumerable<Category>?>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(CategoryPrefix, includeInactive ? "all" : "active");
            var result = await _cacheService.GetAsync<List<Category>>(key, cancellationToken);
            
            if (result.IsSuccess && result.Value != null)
            {
                _logger.LogDebug("Categories cache hit (includeInactive: {IncludeInactive})", includeInactive);
                return Result<IEnumerable<Category>?>.Success(result.Value);
            }

            return Result<IEnumerable<Category>?>.Success(null);
        }

        public async Task<Result> SetCategoriesAsync(IEnumerable<Category> categories, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(CategoryPrefix, includeInactive ? "all" : "active");
            var categoryList = categories.ToList();
            var result = await _cacheService.SetAsync(key, categoryList, CategoryExpiration, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Cached {Count} categories (includeInactive: {IncludeInactive})", categoryList.Count, includeInactive);
            }

            return result;
        }

        public async Task<Result> InvalidateCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var pattern = _keyBuilder.BuildKey(CategoryPrefix, "*");
            var result = await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Invalidated all category cache entries");
            }

            return result;
        }

        #endregion

        #region Summary Caching

        public async Task<Result<BudgetSummary?>> GetBudgetSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(BudgetSummaryPrefix, FormatDate(startDate), FormatDate(endDate));
            var result = await _cacheService.GetAsync<BudgetSummary>(key, cancellationToken);
            
            if (result.IsSuccess && result.Value != null)
            {
                _logger.LogDebug("Budget summary cache hit for {StartDate}-{EndDate}", startDate, endDate);
            }

            return result;
        }

        public async Task<Result> SetBudgetSummaryAsync(DateTime startDate, DateTime endDate, BudgetSummary summary, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(BudgetSummaryPrefix, FormatDate(startDate), FormatDate(endDate));
            var result = await _cacheService.SetAsync(key, summary, ShortExpiration, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Cached budget summary for {StartDate}-{EndDate}", startDate, endDate);
            }

            return result;
        }

        public async Task<Result<BankStatementSummary?>> GetBankStatementSummaryAsync(int statementDay, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(BankStatementPrefix, statementDay);
            var result = await _cacheService.GetAsync<BankStatementSummary>(key, cancellationToken);
            
            if (result.IsSuccess && result.Value != null)
            {
                _logger.LogDebug("Bank statement summary cache hit for day {StatementDay}", statementDay);
            }

            return result;
        }

        public async Task<Result> SetBankStatementSummaryAsync(int statementDay, BankStatementSummary summary, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(BankStatementPrefix, statementDay);
            var result = await _cacheService.SetAsync(key, summary, LongExpiration, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Cached bank statement summary for day {StatementDay}", statementDay);
            }

            return result;
        }

        #endregion

        #region Cache Management

        public async Task<Result> WarmupAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting budget cache warmup");
            
            try
            {
                // This would typically pre-load frequently accessed data
                // For now, we'll just log that warmup was requested
                _logger.LogInformation("Budget cache warmup completed - no specific warmup data configured");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during budget cache warmup");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache warmup failed"));
            }
        }

        public async Task<Result> InvalidateAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Invalidating all budget cache entries");

            var patterns = new[]
            {
                _keyBuilder.BuildKey(DashboardPrefix, "*"),
                _keyBuilder.BuildKey(IncomePrefix, "*"),
                _keyBuilder.BuildKey(SpendingPrefix, "*"),
                _keyBuilder.BuildKey(SpendingWithCategoryPrefix, "*"),
                _keyBuilder.BuildKey(CategoryPrefix, "*"),
                _keyBuilder.BuildKey(BudgetSummaryPrefix, "*"),
                _keyBuilder.BuildKey(BankStatementPrefix, "*")
            };

            var results = new List<Result>();
            foreach (var pattern in patterns)
            {
                results.Add(await _cacheService.RemoveByPatternAsync(pattern, cancellationToken));
            }

            var success = results.All(r => r.IsSuccess);
            if (success)
            {
                _logger.LogInformation("Successfully invalidated all budget cache entries");
            }
            else
            {
                _logger.LogError("Failed to invalidate some budget cache entries");
            }

            return success ? Result.Success() : results.First(r => r.IsFailure);
        }

        public async Task<Result<BudgetCacheStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var generalStatsResult = await _cacheService.GetStatisticsAsync(cancellationToken);
            if (generalStatsResult.IsFailure)
            {
                return Result<BudgetCacheStatistics>.Failure(generalStatsResult.Error!);
            }

            var generalStats = generalStatsResult.Value!;
            var budgetStats = new BudgetCacheStatistics
            {
                GeneralStatistics = generalStats,
                DomainSpecificStats = new Dictionary<string, object>
                {
                    ["DashboardCacheEntries"] = await GetCacheCountByPrefix(DashboardPrefix),
                    ["IncomeCacheEntries"] = await GetCacheCountByPrefix(IncomePrefix),
                    ["SpendingCacheEntries"] = await GetCacheCountByPrefix(SpendingPrefix),
                    ["CategoryCacheEntries"] = await GetCacheCountByPrefix(CategoryPrefix),
                    ["SummaryCacheEntries"] = await GetCacheCountByPrefix(BudgetSummaryPrefix) + await GetCacheCountByPrefix(BankStatementPrefix)
                }
            };

            return Result<BudgetCacheStatistics>.Success(budgetStats);
        }

        private async Task<int> GetCacheCountByPrefix(string prefix)
        {
            try
            {
                var pattern = _keyBuilder.BuildKey(prefix, "*");
                var keysResult = await _cacheService.GetKeysAsync(pattern);
                return keysResult.IsSuccess ? keysResult.Value!.Count() : 0;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Helper Methods

        private static string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        #endregion
    }

    /// <summary>
    /// Budget-specific cache statistics
    /// </summary>
    public class BudgetCacheStatistics
    {
        public CacheStatistics GeneralStatistics { get; set; } = new();
        public Dictionary<string, object> DomainSpecificStats { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Extensions for easier budget cache registration and usage
    /// </summary>
    public static class BudgetCacheExtensions
    {
        /// <summary>
        /// Adds budget cache services to the DI container
        /// </summary>
        public static IServiceCollection AddBudgetCaching(this IServiceCollection services)
        {
            services.AddScoped<ICacheKeyBuilder, DefaultCacheKeyBuilder>();
            services.AddScoped<ICacheService, MemoryCacheService>();
            services.AddScoped<IBudgetCacheService, BudgetCacheService>();
            return services;
        }

        /// <summary>
        /// Configures memory cache options for budget application
        /// </summary>
        public static IServiceCollection ConfigureBudgetMemoryCache(this IServiceCollection services, Action<MemoryCacheOptions>? configure = null)
        {
            services.Configure<MemoryCacheOptions>(options =>
            {
                // Default budget-specific cache settings
                options.SizeLimit = 1000; // Max 1000 cache entries
                options.CompactionPercentage = 0.2; // Remove 20% when limit reached
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
                
                // Apply custom configuration
                configure?.Invoke(options);
            });

            return services;
        }
    }
}