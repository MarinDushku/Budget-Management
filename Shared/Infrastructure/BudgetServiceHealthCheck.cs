// Budget Service Health Check - Infrastructure Component
// File: Shared/Infrastructure/BudgetServiceHealthCheck.cs

using BudgetManagement.Services;
using BudgetManagement.Shared.Infrastructure.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// Health check for the Budget Service to ensure database connectivity and functionality
    /// </summary>
    public class BudgetServiceHealthCheck : IHealthCheck
    {
        private readonly IBudgetService _budgetService;
        private readonly IBudgetCacheService? _cacheService;
        private readonly ILogger<BudgetServiceHealthCheck> _logger;

        public BudgetServiceHealthCheck(
            IBudgetService budgetService,
            ILogger<BudgetServiceHealthCheck> logger,
            IBudgetCacheService? cacheService = null)
        {
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            const string healthCheckName = "Budget Service";
            
            try
            {
                _logger.LogDebug("Starting health check for {HealthCheckName}", healthCheckName);

                // Check 1: Test database connection
                var connectionHealthy = await TestDatabaseConnectionAsync(cancellationToken);
                if (connectionHealthy.Status != HealthStatus.Healthy)
                {
                    return connectionHealthy;
                }

                // Check 2: Test basic database operations
                var operationsHealthy = await TestBasicOperationsAsync(cancellationToken);
                if (operationsHealthy.Status != HealthStatus.Healthy)
                {
                    return operationsHealthy;
                }

                // Check 3: Verify database schema integrity
                var schemaHealthy = await TestDatabaseSchemaAsync(cancellationToken);
                if (schemaHealthy.Status != HealthStatus.Healthy)
                {
                    return schemaHealthy;
                }

                // Check 4: Test cache service if available
                var cacheHealthy = await TestCacheServiceAsync(cancellationToken);
                if (cacheHealthy.Status != HealthStatus.Healthy)
                {
                    return cacheHealthy;
                }

                _logger.LogDebug("Health check passed for {HealthCheckName}", healthCheckName);

                var healthData = new Dictionary<string, object>
                {
                    ["database_connection"] = "OK",
                    ["basic_operations"] = "OK",
                    ["schema_integrity"] = "OK",
                    ["last_checked"] = DateTime.UtcNow
                };

                if (_cacheService != null)
                {
                    healthData["cache_service"] = "OK";
                }

                return HealthCheckResult.Healthy("Budget service is functioning correctly", healthData);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Health check for {HealthCheckName} was cancelled", healthCheckName);
                return HealthCheckResult.Unhealthy("Health check was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for {HealthCheckName}", healthCheckName);
                return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}", ex, new Dictionary<string, object>
                {
                    ["error_type"] = ex.GetType().Name,
                    ["error_message"] = ex.Message,
                    ["last_checked"] = DateTime.UtcNow
                });
            }
        }

        private async Task<HealthCheckResult> TestDatabaseConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var isConnected = await _budgetService.TestConnectionAsync();
                if (!isConnected)
                {
                    return HealthCheckResult.Unhealthy("Database connection test failed");
                }

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return HealthCheckResult.Unhealthy("Database connection error", ex);
            }
        }

        private async Task<HealthCheckResult> TestBasicOperationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Test reading data (should not throw even if no data exists)
                var testDate = DateTime.Now.Date;
                _ = await _budgetService.GetIncomeAsync(testDate, testDate);
                _ = await _budgetService.GetSpendingAsync(testDate, testDate);

                // Test getting categories (basic read operation)
                _ = await _budgetService.GetCategoriesAsync();

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Basic operations test failed");
                return HealthCheckResult.Unhealthy("Database basic operations failed", ex);
            }
        }

        private async Task<HealthCheckResult> TestDatabaseSchemaAsync(CancellationToken cancellationToken)
        {
            try
            {
                // This would ideally check if all expected tables exist and have correct schema
                // For now, we'll attempt to get a summary which exercises multiple tables
                var endDate = DateTime.Now.Date;
                var startDate = endDate.AddDays(-1);
                
                _ = await _budgetService.GetBudgetSummaryAsync(startDate, endDate);

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database schema test failed");
                
                // If it's a schema-related error, mark as degraded instead of unhealthy
                if (ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("schema", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("column", StringComparison.OrdinalIgnoreCase))
                {
                    return HealthCheckResult.Degraded("Database schema issues detected", ex);
                }

                return HealthCheckResult.Unhealthy("Database schema verification failed", ex);
            }
        }

        private async Task<HealthCheckResult> TestCacheServiceAsync(CancellationToken cancellationToken)
        {
            if (_cacheService == null)
            {
                return HealthCheckResult.Healthy(); // Cache is optional
            }

            try
            {
                // Test cache service functionality
                var statisticsResult = await _cacheService.GetStatisticsAsync(cancellationToken);
                if (statisticsResult.IsFailure)
                {
                    _logger.LogWarning("Cache service statistics check failed: {Error}", statisticsResult.Error);
                    return HealthCheckResult.Degraded("Cache service statistics unavailable", null, new Dictionary<string, object>
                    {
                        ["cache_statistics"] = "UNAVAILABLE",
                        ["error"] = statisticsResult.Error?.Message ?? "Unknown error"
                    });
                }

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache service test failed");
                return HealthCheckResult.Degraded("Cache service test failed", ex);
            }
        }
    }

    /// <summary>
    /// Extension methods for health check result
    /// </summary>
    public static class HealthCheckResultExtensions
    {
        /// <summary>
        /// Checks if the health check result indicates a healthy state
        /// </summary>
        public static bool IsHealthy(this HealthCheckResult result)
        {
            return result.Status == HealthStatus.Healthy;
        }

        /// <summary>
        /// Checks if the health check result indicates a degraded state
        /// </summary>
        public static bool IsDegraded(this HealthCheckResult result)
        {
            return result.Status == HealthStatus.Degraded;
        }

        /// <summary>
        /// Checks if the health check result indicates an unhealthy state
        /// </summary>
        public static bool IsUnhealthy(this HealthCheckResult result)
        {
            return result.Status == HealthStatus.Unhealthy;
        }
    }
}