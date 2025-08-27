// Application Logging Service - High-Level Logging Abstraction
// File: Shared/Infrastructure/ApplicationLoggingService.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// High-level application logging service that provides domain-specific logging methods
    /// Built on top of Serilog structured logging for consistency across the application
    /// </summary>
    public interface IApplicationLoggingService
    {
        // User Activity Logging
        void LogUserLogin(string userId, string method = "Unknown", Dictionary<string, object>? context = null);
        void LogUserLogout(string userId, Dictionary<string, object>? context = null);
        void LogUserAction(string userId, string action, string feature, Dictionary<string, object>? parameters = null);
        
        // Business Operations Logging
        void LogIncomeAdded(string userId, decimal amount, string description, Dictionary<string, object>? metadata = null);
        void LogSpendingAdded(string userId, decimal amount, string categoryName, string description, Dictionary<string, object>? metadata = null);
        void LogCategoryCreated(string userId, string categoryName, Dictionary<string, object>? metadata = null);
        void LogCategoryDeleted(string userId, string categoryName, Dictionary<string, object>? metadata = null);
        
        // Async Business Operations Logging
        Task LogIncomeAddedAsync(int incomeId, decimal amount, DateTime date, string description, CancellationToken cancellationToken = default);
        Task LogIncomeUpdatedAsync(int incomeId, decimal amount, DateTime date, string description, CancellationToken cancellationToken = default);
        Task LogIncomeDeletedAsync(int incomeId, decimal amount, DateTime date, string description, CancellationToken cancellationToken = default);
        Task LogBulkIncomeDeletedAsync(DateTime startDate, DateTime endDate, int deletedCount, CancellationToken cancellationToken = default);
        
        Task LogSpendingAddedAsync(int spendingId, decimal amount, DateTime date, string description, string categoryName, CancellationToken cancellationToken = default);
        Task LogSpendingUpdatedAsync(int spendingId, decimal amount, DateTime date, string description, string categoryName, CancellationToken cancellationToken = default);
        Task LogSpendingDeletedAsync(int spendingId, decimal amount, DateTime date, string description, string categoryName, CancellationToken cancellationToken = default);
        Task LogBulkSpendingDeletedAsync(DateTime startDate, DateTime endDate, int deletedCount, CancellationToken cancellationToken = default);
        Task LogBulkSpendingDeletedByCategoryAsync(int categoryId, string categoryName, int deletedCount, CancellationToken cancellationToken = default);
        
        // System Operations Logging  
        void LogDatabaseOperation(string operation, string tableName, long durationMs, bool success, Dictionary<string, object>? context = null);
        void LogExportOperation(string userId, string format, string filePath, bool success, Dictionary<string, object>? metadata = null);
        void LogImportOperation(string userId, string format, int recordsProcessed, int errors, Dictionary<string, object>? metadata = null);
        
        // Performance Logging
        void LogSlowOperation(string operationName, long durationMs, Dictionary<string, object>? context = null);
        void LogMemoryUsage(string component, long memoryBytes, Dictionary<string, object>? context = null);
        
        // Error and Exception Logging
        void LogBusinessError(string operation, string errorMessage, Dictionary<string, object>? context = null);
        void LogSystemError(Exception exception, string operation, string? userId = null, Dictionary<string, object>? context = null);
        void LogValidationError(string operation, IEnumerable<string> validationErrors, Dictionary<string, object>? context = null);
        
        // Security and Audit Logging
        void LogSecurityEvent(string eventType, string? userId, bool success, Dictionary<string, object>? details = null);
        void LogDataAccess(string userId, string dataType, string action, string recordId, Dictionary<string, object>? context = null);
        void LogConfigurationChange(string userId, string setting, object? oldValue, object? newValue, Dictionary<string, object>? context = null);
    }

    /// <summary>
    /// Implementation of application logging service using structured logging
    /// </summary>
    public class ApplicationLoggingService : IApplicationLoggingService
    {
        private readonly ILogger<ApplicationLoggingService> _logger;

        public ApplicationLoggingService(ILogger<ApplicationLoggingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region User Activity Logging

        public void LogUserLogin(string userId, string method = "Unknown", Dictionary<string, object>? context = null)
        {
            _logger.LogInformation("User {UserId} logged in using method {Method} with context {@Context}", 
                userId, method, context ?? new());
        }

        public void LogUserLogout(string userId, Dictionary<string, object>? context = null)
        {
            _logger.LogInformation("User {UserId} logged out with context {@Context}", 
                userId, context ?? new());
        }

        public void LogUserAction(string userId, string action, string feature, Dictionary<string, object>? parameters = null)
        {
            _logger.LogInformation("User {UserId} performed {Action} in {Feature} with parameters {@Parameters}", 
                userId, action, feature, parameters ?? new());
        }

        #endregion

        #region Business Operations Logging

        public void LogIncomeAdded(string userId, decimal amount, string description, Dictionary<string, object>? metadata = null)
        {
            using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
            
            _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                "CREATE", "Income", Guid.NewGuid().ToString(), userId ?? "System", 
                new Dictionary<string, object>
                {
                    ["Amount"] = amount,
                    ["Description"] = description,
                    ["Metadata"] = metadata ?? new()
                });

            _logger.LogInformation("Business Operation: Income added by {UserId} - Amount: {Amount:C}, Description: {Description} {@Metadata}",
                userId, amount, description, metadata ?? new());
        }

        public void LogSpendingAdded(string userId, decimal amount, string categoryName, string description, Dictionary<string, object>? metadata = null)
        {
            using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
            
            _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                "CREATE", "Spending", Guid.NewGuid().ToString(), userId ?? "System", 
                new Dictionary<string, object>
                {
                    ["Amount"] = amount,
                    ["Category"] = categoryName,
                    ["Description"] = description,
                    ["Metadata"] = metadata ?? new()
                });

            _logger.LogInformation("Business Operation: Spending added by {UserId} - Amount: {Amount:C}, Category: {Category}, Description: {Description} {@Metadata}",
                userId, amount, categoryName, description, metadata ?? new());
        }

        public void LogCategoryCreated(string userId, string categoryName, Dictionary<string, object>? metadata = null)
        {
            _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                "CREATE", "Category", categoryName, userId ?? "System", 
                new Dictionary<string, object>
                {
                    ["CategoryName"] = categoryName,
                    ["Metadata"] = metadata ?? new()
                });
        }

        public void LogCategoryDeleted(string userId, string categoryName, Dictionary<string, object>? metadata = null)
        {
            _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                "DELETE", "Category", categoryName, userId ?? "System", 
                new Dictionary<string, object>
                {
                    ["CategoryName"] = categoryName,
                    ["Metadata"] = metadata ?? new()
                });
        }

        // Async Business Operations Logging
        
        public async Task LogIncomeAddedAsync(int incomeId, decimal amount, DateTime date, string description, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
                
                _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    "CREATE", "Income", incomeId.ToString(), "System", 
                    new Dictionary<string, object>
                    {
                        ["IncomeId"] = incomeId,
                        ["Amount"] = amount,
                        ["Date"] = date,
                        ["Description"] = description
                    });

                _logger.LogInformation("Business Operation: Income entry {IncomeId} added - Amount: {Amount:C}, Date: {Date:yyyy-MM-dd}, Description: {Description}",
                    incomeId, amount, date, description);
            }, cancellationToken);
        }
        
        public async Task LogIncomeUpdatedAsync(int incomeId, decimal amount, DateTime date, string description, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
                
                _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    "UPDATE", "Income", incomeId.ToString(), "System", 
                    new Dictionary<string, object>
                    {
                        ["IncomeId"] = incomeId,
                        ["Amount"] = amount,
                        ["Date"] = date,
                        ["Description"] = description
                    });

                _logger.LogInformation("Business Operation: Income entry {IncomeId} updated - Amount: {Amount:C}, Date: {Date:yyyy-MM-dd}, Description: {Description}",
                    incomeId, amount, date, description);
            }, cancellationToken);
        }
        
        public async Task LogIncomeDeletedAsync(int incomeId, decimal amount, DateTime date, string description, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
                
                _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    "DELETE", "Income", incomeId.ToString(), "System", 
                    new Dictionary<string, object>
                    {
                        ["IncomeId"] = incomeId,
                        ["Amount"] = amount,
                        ["Date"] = date,
                        ["Description"] = description
                    });

                _logger.LogInformation("Business Operation: Income entry {IncomeId} deleted - Amount: {Amount:C}, Date: {Date:yyyy-MM-dd}, Description: {Description}",
                    incomeId, amount, date, description);
            }, cancellationToken);
        }
        
        public async Task LogBulkIncomeDeletedAsync(DateTime startDate, DateTime endDate, int deletedCount, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
                
                _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    "BULK_DELETE", "Income", $"{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}", "System", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = startDate,
                        ["EndDate"] = endDate,
                        ["DeletedCount"] = deletedCount
                    });

                _logger.LogInformation("Business Operation: Bulk deleted {DeletedCount} income entries from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
                    deletedCount, startDate, endDate);
            }, cancellationToken);
        }
        
        public async Task LogSpendingAddedAsync(int spendingId, decimal amount, DateTime date, string description, string categoryName, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
                
                _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    "CREATE", "Spending", spendingId.ToString(), "System", 
                    new Dictionary<string, object>
                    {
                        ["SpendingId"] = spendingId,
                        ["Amount"] = amount,
                        ["Date"] = date,
                        ["Description"] = description,
                        ["CategoryName"] = categoryName
                    });

                _logger.LogInformation("Business Operation: Spending entry {SpendingId} added - Amount: {Amount:C}, Date: {Date:yyyy-MM-dd}, Category: {CategoryName}, Description: {Description}",
                    spendingId, amount, date, categoryName, description);
            }, cancellationToken);
        }
        
        public async Task LogSpendingUpdatedAsync(int spendingId, decimal amount, DateTime date, string description, string categoryName, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
                
                _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    "UPDATE", "Spending", spendingId.ToString(), "System", 
                    new Dictionary<string, object>
                    {
                        ["SpendingId"] = spendingId,
                        ["Amount"] = amount,
                        ["Date"] = date,
                        ["Description"] = description,
                        ["CategoryName"] = categoryName
                    });

                _logger.LogInformation("Business Operation: Spending entry {SpendingId} updated - Amount: {Amount:C}, Date: {Date:yyyy-MM-dd}, Category: {CategoryName}, Description: {Description}",
                    spendingId, amount, date, categoryName, description);
            }, cancellationToken);
        }
        
        public async Task LogSpendingDeletedAsync(int spendingId, decimal amount, DateTime date, string description, string categoryName, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
                
                _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    "DELETE", "Spending", spendingId.ToString(), "System", 
                    new Dictionary<string, object>
                    {
                        ["SpendingId"] = spendingId,
                        ["Amount"] = amount,
                        ["Date"] = date,
                        ["Description"] = description,
                        ["CategoryName"] = categoryName
                    });

                _logger.LogInformation("Business Operation: Spending entry {SpendingId} deleted - Amount: {Amount:C}, Date: {Date:yyyy-MM-dd}, Category: {CategoryName}, Description: {Description}",
                    spendingId, amount, date, categoryName, description);
            }, cancellationToken);
        }
        
        public async Task LogBulkSpendingDeletedAsync(DateTime startDate, DateTime endDate, int deletedCount, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
                
                _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    "BULK_DELETE", "Spending", $"{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}", "System", 
                    new Dictionary<string, object>
                    {
                        ["StartDate"] = startDate,
                        ["EndDate"] = endDate,
                        ["DeletedCount"] = deletedCount
                    });

                _logger.LogInformation("Business Operation: Bulk deleted {DeletedCount} spending entries from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
                    deletedCount, startDate, endDate);
            }, cancellationToken);
        }
        
        public async Task LogBulkSpendingDeletedByCategoryAsync(int categoryId, string categoryName, int deletedCount, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                using var correlationContext = CorrelationIdManager.BeginCorrelationContext();
                
                _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    "BULK_DELETE", "Spending", $"category_{categoryId}", "System", 
                    new Dictionary<string, object>
                    {
                        ["CategoryId"] = categoryId,
                        ["CategoryName"] = categoryName,
                        ["DeletedCount"] = deletedCount
                    });

                _logger.LogInformation("Business Operation: Bulk deleted {DeletedCount} spending entries for category {CategoryName}",
                    deletedCount, categoryName);
            }, cancellationToken);
        }

        #endregion

        #region System Operations Logging

        public void LogDatabaseOperation(string operation, string tableName, long durationMs, bool success, Dictionary<string, object>? context = null)
        {
            var logLevel = success ? LogLevel.Debug : LogLevel.Warning;
            
            _logger.Log(logLevel, "Database Operation: {Operation} on {TableName} completed in {DurationMs}ms (Success: {Success}) {@Context}",
                operation, tableName, durationMs, success, context ?? new());

            // Also log as performance metric
            if (success)
            {
                _logger.LogInformation("Performance: {Operation} completed in {ElapsedMs}ms {UserId} {@Metrics}", 
                    $"Database.{operation}.{tableName}", durationMs, "System", 
                    new Dictionary<string, object>
                    {
                        ["TableName"] = tableName,
                        ["Operation"] = operation,
                        ["Success"] = success
                    });
            }
        }

        public void LogExportOperation(string userId, string format, string filePath, bool success, Dictionary<string, object>? metadata = null)
        {
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            
            _logger.Log(logLevel, "Export Operation: User {UserId} exported data to {Format} format at {FilePath} (Success: {Success}) {@Metadata}",
                userId, format, filePath, success, metadata ?? new());

            _logger.LogInformation("UserAction: {Action} in {Feature} by {UserId} {@Parameters}", 
                "Export", "DataExport", userId, 
                new Dictionary<string, object>
                {
                    ["Format"] = format,
                    ["FilePath"] = filePath,
                    ["Success"] = success,
                    ["Metadata"] = metadata ?? new()
                });
        }

        public void LogImportOperation(string userId, string format, int recordsProcessed, int errors, Dictionary<string, object>? metadata = null)
        {
            var success = errors == 0;
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            
            _logger.Log(logLevel, "Import Operation: User {UserId} imported {RecordsProcessed} records from {Format} format with {Errors} errors {@Metadata}",
                userId, recordsProcessed, format, errors, metadata ?? new());

            _logger.LogInformation("UserAction: {Action} in {Feature} by {UserId} {@Parameters}", 
                "Import", "DataImport", userId, 
                new Dictionary<string, object>
                {
                    ["Format"] = format,
                    ["RecordsProcessed"] = recordsProcessed,
                    ["Errors"] = errors,
                    ["Success"] = success,
                    ["Metadata"] = metadata ?? new()
                });
        }

        #endregion

        #region Performance Logging

        public void LogSlowOperation(string operationName, long durationMs, Dictionary<string, object>? context = null)
        {
            _logger.LogInformation("Performance: {Operation} completed in {ElapsedMs}ms {UserId} {@Metrics}", 
                operationName, durationMs, "System", 
                new Dictionary<string, object>
                {
                    ["Category"] = "SlowOperation",
                    ["Context"] = context ?? new()
                });

            _logger.LogWarning("Performance Alert: Slow operation {OperationName} took {DurationMs}ms {@Context}",
                operationName, durationMs, context ?? new());
        }

        public void LogMemoryUsage(string component, long memoryBytes, Dictionary<string, object>? context = null)
        {
            var memoryMB = memoryBytes / (1024.0 * 1024.0);
            
            _logger.LogDebug("Memory Usage: Component {Component} using {MemoryMB:F2} MB ({MemoryBytes} bytes) {@Context}",
                component, memoryMB, memoryBytes, context ?? new());

            // Log warning if memory usage is high
            if (memoryMB > 100) // 100 MB threshold
            {
                _logger.LogWarning("High Memory Usage: Component {Component} using {MemoryMB:F2} MB {@Context}",
                    component, memoryMB, context ?? new());
            }
        }

        #endregion

        #region Error and Exception Logging

        public void LogBusinessError(string operation, string errorMessage, Dictionary<string, object>? context = null)
        {
            _logger.LogWarning("Business Error: Operation {Operation} failed - {ErrorMessage} {@Context}",
                operation, errorMessage, context ?? new());
        }

        public void LogSystemError(Exception exception, string operation, string? userId = null, Dictionary<string, object>? context = null)
        {
            _logger.LogError(exception, "Error: {ErrorType} {ErrorMessage} User: {UserId} Request: {RequestId} {@Context}", 
                exception.GetType().Name, exception.Message, userId ?? "Unknown", "Unknown", 
                new Dictionary<string, object>
                {
                    ["Operation"] = operation,
                    ["Context"] = context ?? new()
                });
        }

        public void LogValidationError(string operation, IEnumerable<string> validationErrors, Dictionary<string, object>? context = null)
        {
            var errors = validationErrors.ToList();
            
            _logger.LogWarning("Validation Error: Operation {Operation} failed validation - {ValidationErrors} {@Context}",
                operation, errors, context ?? new());
        }

        #endregion

        #region Security and Audit Logging

        public void LogSecurityEvent(string eventType, string? userId, bool success, Dictionary<string, object>? details = null)
        {
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            
            _logger.Log(logLevel, "Security Event: {EventType} by {UserId} (Success: {Success}) {@Details}",
                eventType, userId ?? "Unknown", success, details ?? new());
        }

        public void LogDataAccess(string userId, string dataType, string action, string recordId, Dictionary<string, object>? context = null)
        {
            _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                action.ToUpperInvariant(), dataType, recordId, userId, 
                new Dictionary<string, object>
                {
                    ["DataType"] = dataType,
                    ["Action"] = action,
                    ["RecordId"] = recordId,
                    ["Context"] = context ?? new()
                });
        }

        public void LogConfigurationChange(string userId, string setting, object? oldValue, object? newValue, Dictionary<string, object>? context = null)
        {
            _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                "UPDATE", "Configuration", setting, userId, 
                new Dictionary<string, object>
                {
                    ["Setting"] = setting,
                    ["OldValue"] = SafeSerializeValue(oldValue),
                    ["NewValue"] = SafeSerializeValue(newValue),
                    ["Context"] = context ?? new()
                });

            _logger.LogInformation("Configuration Change: {Setting} changed from {OldValue} to {NewValue} by {UserId} {@Context}",
                setting, SafeSerializeValue(oldValue), SafeSerializeValue(newValue), userId, context ?? new());
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Safely serializes values for logging, handling null values and circular references
        /// </summary>
        private static object SafeSerializeValue(object? value)
        {
            if (value == null) return "null";
            
            try
            {
                // Handle primitive types and strings directly
                if (value.GetType().IsPrimitive || value is string or DateTime or decimal or Guid)
                {
                    return value;
                }

                // For complex objects, use JSON serialization with limits
                var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    MaxDepth = 3 // Prevent circular references
                });

                // Limit the length of serialized data
                return json.Length > 200 ? json.Substring(0, 200) + "..." : json;
            }
            catch
            {
                return $"[{value.GetType().Name}]";
            }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for easier application logging integration
    /// </summary>
    public static class ApplicationLoggingExtensions
    {
        /// <summary>
        /// Adds application logging service to the DI container
        /// </summary>
        public static IServiceCollection AddApplicationLogging(this IServiceCollection services)
        {
            services.AddScoped<IApplicationLoggingService, ApplicationLoggingService>();
            return services;
        }

        /// <summary>
        /// Creates a timed operation scope that logs performance metrics
        /// </summary>
        public static IDisposable BeginTimedOperation(this IApplicationLoggingService loggingService, string operationName, Dictionary<string, object>? context = null)
        {
            return new TimedOperationScope(loggingService, operationName, context);
        }

        /// <summary>
        /// Logs method entry with parameters (for debugging)
        /// </summary>
        public static void LogMethodEntry(this ILogger logger, Dictionary<string, object>? parameters = null, [CallerMemberName] string methodName = "")
        {
            logger.LogDebug("Method Entry: {MethodName} {@Parameters}", methodName, parameters ?? new());
        }

        /// <summary>
        /// Logs method exit with result (for debugging)
        /// </summary>
        public static void LogMethodExit(this ILogger logger, object? result = null, [CallerMemberName] string methodName = "")
        {
            logger.LogDebug("Method Exit: {MethodName} {@Result}", methodName, result);
        }
    }

    /// <summary>
    /// Disposable scope for timed operations
    /// </summary>
    internal class TimedOperationScope : IDisposable
    {
        private readonly IApplicationLoggingService _loggingService;
        private readonly string _operationName;
        private readonly Dictionary<string, object>? _context;
        private readonly System.Diagnostics.Stopwatch _stopwatch;

        public TimedOperationScope(IApplicationLoggingService loggingService, string operationName, Dictionary<string, object>? context)
        {
            _loggingService = loggingService;
            _operationName = operationName;
            _context = context;
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var contextWithDuration = new Dictionary<string, object>(_context ?? new());
            contextWithDuration["DurationMs"] = _stopwatch.ElapsedMilliseconds;
            
            if (_stopwatch.ElapsedMilliseconds > 1000) // Log as slow if > 1 second
            {
                _loggingService.LogSlowOperation(_operationName, _stopwatch.ElapsedMilliseconds, contextWithDuration);
            }
        }
    }
}