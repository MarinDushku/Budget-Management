// MediatR Logging Behavior - Cross-Cutting Concern Implementation
// File: Shared/Infrastructure/LoggingBehavior.cs

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using BudgetManagement.Shared.Core;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// MediatR behavior for logging request/response information
    /// Implements cross-cutting logging concerns for all commands and queries
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Handle(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var responseName = typeof(TResponse).Name;
            var requestId = GetRequestId(request);

            // Start correlation context for this request
            using var correlationContext = CorrelationIdManager.BeginCorrelationContext(requestId);

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestName"] = requestName,
                ["RequestId"] = requestId,
                ["ResponseName"] = responseName,
                ["RequestType"] = typeof(TRequest).FullName,
                ["ResponseType"] = typeof(TResponse).FullName
            });

            // Log request start with structured data
            _logger.LogInformation("MediatR Request Started: {RequestName} [{RequestId}] {@RequestData}", 
                requestName, requestId, GetRequestData(request));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await next();
                
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Log performance metrics using structured logging
                _logger.LogInformation("Performance: {Operation} completed in {ElapsedMs}ms {UserId} {@Metrics}", 
                    $"MediatR.{requestName}", elapsedMs, "System", 
                    new Dictionary<string, object>
                    {
                        ["RequestType"] = requestName,
                        ["ResponseType"] = responseName,
                        ["RequestId"] = requestId
                    });

                // Log success with structured data
                _logger.LogInformation("MediatR Request Completed: {RequestName} [{RequestId}] in {ElapsedMs}ms", 
                    requestName, requestId, elapsedMs);

                // Log additional details for Results with structured logging
                if (response is IResult result)
                {
                    if (result.IsFailure)
                    {
                        _logger.LogWarning("MediatR Request Failed: {RequestName} [{RequestId}] with error {ErrorType}: {ErrorMessage} {@ErrorDetails}", 
                            requestName, requestId, result.Error?.Type, result.Error?.Message, result.Error);
                    }
                    else
                    {
                        _logger.LogDebug("MediatR Request Success: {RequestName} [{RequestId}] completed successfully", 
                            requestName, requestId);
                    }
                }

                // Log performance warnings for slow requests with structured data
                if (elapsedMs > 1000) // 1 second
                {
                    _logger.LogWarning("Slow MediatR Request: {RequestName} [{RequestId}] took {ElapsedMs}ms {@PerformanceContext}", 
                        requestName, requestId, elapsedMs, new { Threshold = 1000, Category = "Performance" });
                }

                // Log critical performance issues
                if (elapsedMs > 5000) // 5 seconds
                {
                    _logger.LogError("Critical Performance Issue: {RequestName} [{RequestId}] took {ElapsedMs}ms {@CriticalContext}", 
                        requestName, requestId, elapsedMs, new { Threshold = 5000, Category = "CriticalPerformance", Action = "RequiresInvestigation" });
                }

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Log error with structured data and correlation context
                _logger.LogError(ex, "Error: {ErrorType} {ErrorMessage} User: {UserId} Request: {RequestId} {@Context}", 
                    ex.GetType().Name, ex.Message, "Unknown", requestId, 
                    new Dictionary<string, object>
                    {
                        ["RequestName"] = requestName,
                        ["ElapsedMs"] = elapsedMs,
                        ["RequestData"] = GetRequestData(request)
                    });

                // Also log the error in the traditional way for backwards compatibility
                _logger.LogError(ex, "MediatR Request Error: {RequestName} [{RequestId}] failed after {ElapsedMs}ms", 
                    requestName, requestId, elapsedMs);

                throw;
            }
        }

        /// <summary>
        /// Extracts a unique identifier from the request for correlation
        /// </summary>
        /// <param name="request">The request object</param>
        /// <returns>Request identifier</returns>
        private static string GetRequestId(TRequest request)
        {
            // Try to get ID from command/query interfaces
            if (request is ICommand command)
                return command.CommandId.ToString();

            if (request is IQuery<object> query)
                return query.QueryId.ToString();

            // Fallback to hash code
            return request.GetHashCode().ToString();
        }

        /// <summary>
        /// Extracts relevant data from the request for logging (without sensitive information)
        /// </summary>
        /// <param name="request">The request object</param>
        /// <returns>Safe request data for logging</returns>
        private static object GetRequestData(TRequest request)
        {
            try
            {
                // For security, only log basic information about the request
                var requestType = request.GetType();
                var properties = new Dictionary<string, object?>();

                // Get public properties but exclude sensitive data
                var publicProperties = requestType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                foreach (var prop in publicProperties)
                {
                    // Skip potentially sensitive properties
                    if (IsSensitiveProperty(prop.Name))
                        continue;

                    try
                    {
                        var value = prop.GetValue(request);
                        
                        // Limit string length for logging
                        if (value is string stringValue && stringValue.Length > 100)
                        {
                            value = stringValue.Substring(0, 100) + "...";
                        }
                        
                        properties[prop.Name] = value;
                    }
                    catch
                    {
                        // Ignore properties that can't be accessed
                        properties[prop.Name] = "[AccessError]";
                    }
                }

                return properties;
            }
            catch
            {
                // If anything goes wrong, return minimal data
                return new { RequestType = request.GetType().Name };
            }
        }

        /// <summary>
        /// Determines if a property name contains sensitive information that shouldn't be logged
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>True if the property is sensitive</returns>
        private static bool IsSensitiveProperty(string propertyName)
        {
            var sensitivePatterns = new[]
            {
                "password", "pwd", "secret", "token", "key", "credential", 
                "ssn", "social", "credit", "card", "account", "pin"
            };

            var lowerName = propertyName.ToLowerInvariant();
            return sensitivePatterns.Any(pattern => lowerName.Contains(pattern));
        }
    }

    /// <summary>
    /// Extension methods for logging behavior
    /// </summary>
    public static class LoggingBehaviorExtensions
    {
        /// <summary>
        /// Adds detailed request logging with structured data
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="request">Request object</param>
        /// <param name="additionalProperties">Additional properties to log</param>
        public static void LogRequestDetails<T>(this ILogger logger, T request, IDictionary<string, object>? additionalProperties = null)
        {
            var properties = new Dictionary<string, object>
            {
                ["RequestType"] = typeof(T).Name,
                ["Request"] = request ?? throw new ArgumentNullException(nameof(request))
            };

            if (additionalProperties != null)
            {
                foreach (var prop in additionalProperties)
                {
                    properties[prop.Key] = prop.Value;
                }
            }

            using var scope = logger.BeginScope(properties);
            logger.LogDebug("Request details logged for {RequestType}", typeof(T).Name);
        }

        /// <summary>
        /// Logs performance metrics for requests
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="requestName">Name of the request</param>
        /// <param name="elapsedMilliseconds">Elapsed time in milliseconds</param>
        /// <param name="isSuccess">Whether the request was successful</param>
        public static void LogPerformanceMetrics(this ILogger logger, string requestName, long elapsedMilliseconds, bool isSuccess = true)
        {
            var logLevel = elapsedMilliseconds switch
            {
                > 5000 => LogLevel.Warning,  // > 5 seconds
                > 1000 => LogLevel.Information,  // > 1 second
                _ => LogLevel.Debug
            };

            logger.Log(logLevel, "Performance: {RequestName} completed in {ElapsedMs}ms (Success: {IsSuccess})",
                requestName, elapsedMilliseconds, isSuccess);
        }
    }
}