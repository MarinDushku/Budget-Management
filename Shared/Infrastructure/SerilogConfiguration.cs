// Serilog Configuration - Enterprise Structured Logging Setup
// File: Shared/Infrastructure/SerilogConfiguration.cs

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;
using System.Diagnostics;
using System.IO;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// Comprehensive Serilog configuration for enterprise-level structured logging
    /// </summary>
    public static class SerilogConfiguration
    {
        /// <summary>
        /// Configures Serilog with comprehensive enterprise logging setup
        /// </summary>
        /// <param name="context">Host builder context</param>
        /// <param name="configuration">Logger configuration</param>
        /// <param name="appName">Application name for logging context</param>
        public static void ConfigureEnterpriseLogging(
            HostBuilderContext context, 
            LoggerConfiguration configuration,
            string appName = "BudgetManagement")
        {
            var environment = context.HostingEnvironment.EnvironmentName;
            var config = context.Configuration;

            // Base configuration
            configuration
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .Enrich.WithProperty("ProcessName", System.Diagnostics.Process.GetCurrentProcess().ProcessName)
                .Enrich.WithExceptionDetails()
                .Enrich.With(new CorrelationIdEnricher())
                .Enrich.With(new ApplicationContextEnricher(appName, environment));

            // Configure minimum log levels based on environment
            ConfigureLogLevels(configuration, environment);

            // Configure sinks based on environment
            ConfigureSinks(configuration, environment, config);

            // Configure filtering
            ConfigureFiltering(configuration);

            // Configure destructuring for complex objects
            ConfigureDestructuring(configuration);
        }

        /// <summary>
        /// Configures log levels based on environment
        /// </summary>
        private static void ConfigureLogLevels(LoggerConfiguration configuration, string environment)
        {
            var minimumLevel = environment.ToLowerInvariant() switch
            {
                "development" => LogEventLevel.Debug,
                "testing" => LogEventLevel.Information,
                "staging" => LogEventLevel.Information,
                "production" => LogEventLevel.Warning,
                _ => LogEventLevel.Information
            };

            configuration
                .MinimumLevel.Is(minimumLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);

            // Enable debug logging for our application namespaces in development
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                configuration
                    .MinimumLevel.Override("BudgetManagement", LogEventLevel.Debug)
                    .MinimumLevel.Override("BudgetManagement.Features", LogEventLevel.Debug)
                    .MinimumLevel.Override("BudgetManagement.Shared", LogEventLevel.Debug);
            }
        }

        /// <summary>
        /// Configures logging sinks based on environment
        /// </summary>
        private static void ConfigureSinks(LoggerConfiguration configuration, string environment, IConfiguration config)
        {
            var baseLogPath = GetLogPath(config);
            
            // Console sink - always enabled for development
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                configuration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
            }

            // File sinks - structured logging to files
            configuration
                .WriteTo.File(
                    path: Path.Combine(baseLogPath, "logs", "app-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] [{CorrelationId}] [{ThreadId}] {Message:lj}{NewLine}{Exception}")
                
                // Separate error log for critical issues
                .WriteTo.Logger(errorLogger => errorLogger
                    .Filter.ByIncludingOnly(evt => evt.Level >= LogEventLevel.Error)
                    .WriteTo.File(
                        path: Path.Combine(baseLogPath, "logs", "errors", "error-.txt"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 90, // Keep error logs longer
                        shared: true))
                
                // Performance log for tracking slow operations
                .WriteTo.Logger(perfLogger => perfLogger
                    .Filter.ByIncludingOnly(evt => evt.MessageTemplate.Text.Contains("Performance:"))
                    .WriteTo.File(
                        path: Path.Combine(baseLogPath, "logs", "performance", "perf-.txt"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        shared: true))
                
                // Audit log for user actions and data changes
                .WriteTo.Logger(auditLogger => auditLogger
                    .Filter.ByIncludingOnly(evt => evt.MessageTemplate.Text.Contains("Audit:"))
                    .WriteTo.File(
                        path: Path.Combine(baseLogPath, "logs", "audit", "audit-.txt"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 365, // Keep audit logs for a year
                        shared: true,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{UserId}] [{Action}] {Message:lj}{NewLine}"));

            // JSON structured log for advanced processing
            configuration.WriteTo.File(
                path: Path.Combine(baseLogPath, "logs", "structured", "app-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                formatter: new Serilog.Formatting.Json.JsonFormatter());

            // Production-specific sinks
            if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                // Add application insights, elasticsearch, or other production sinks here
                ConfigureProductionSinks(configuration, config);
            }
        }

        /// <summary>
        /// Configures production-specific logging sinks
        /// </summary>
        private static void ConfigureProductionSinks(LoggerConfiguration configuration, IConfiguration config)
        {
            // Example: Application Insights (uncomment and configure as needed)
            /*
            var instrumentationKey = config["ApplicationInsights:InstrumentationKey"];
            if (!string.IsNullOrEmpty(instrumentationKey))
            {
                configuration.WriteTo.ApplicationInsights(instrumentationKey, TelemetryConverter.Traces);
            }
            */

            // Example: Elasticsearch (uncomment and configure as needed)
            /*
            var elasticsearchUrl = config["Elasticsearch:Url"];
            if (!string.IsNullOrEmpty(elasticsearchUrl))
            {
                configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
                {
                    IndexFormat = "budgetmanagement-logs-{0:yyyy.MM.dd}",
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7
                });
            }
            */
        }

        /// <summary>
        /// Configures log filtering to reduce noise
        /// </summary>
        private static void ConfigureFiltering(LoggerConfiguration configuration)
        {
            configuration
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Mvc.Infrastructure"))
                .Filter.ByExcluding(evt => 
                    evt.MessageTemplate.Text.Contains("Request starting") ||
                    evt.MessageTemplate.Text.Contains("Request finished") ||
                    evt.MessageTemplate.Text.Contains("Route matched"));
        }

        /// <summary>
        /// Configures destructuring for complex objects
        /// </summary>
        private static void ConfigureDestructuring(LoggerConfiguration configuration)
        {
            configuration
                .Destructure.ByTransforming<Exception>(ex => new
                {
                    Type = ex.GetType().Name,
                    ex.Message,
                    ex.StackTrace,
                    InnerException = ex.InnerException?.Message
                });
        }

        /// <summary>
        /// Gets the base log path from configuration or defaults to application data folder
        /// </summary>
        private static string GetLogPath(IConfiguration config)
        {
            var configuredPath = config["Logging:LogPath"];
            if (!string.IsNullOrEmpty(configuredPath) && Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            // Default to application data folder
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appDataPath, "BudgetManagement");
        }
    }

    /// <summary>
    /// Enricher that adds correlation ID to log events for tracking requests
    /// </summary>
    public class CorrelationIdEnricher : ILogEventEnricher
    {
        private const string CorrelationIdProperty = "CorrelationId";
        private static readonly AsyncLocal<string?> _correlationId = new();

        public static string? Current
        {
            get => _correlationId.Value;
            set => _correlationId.Value = value;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var correlationId = Current ?? Activity.Current?.Id ?? Guid.NewGuid().ToString("N")[..8];
            var property = propertyFactory.CreateProperty(CorrelationIdProperty, correlationId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }

    /// <summary>
    /// Enricher that adds application context information to log events
    /// </summary>
    public class ApplicationContextEnricher : ILogEventEnricher
    {
        private readonly LogEventProperty _applicationProperty;
        private readonly LogEventProperty _environmentProperty;
        private readonly LogEventProperty _versionProperty;

        public ApplicationContextEnricher(string applicationName, string environment)
        {
            _applicationProperty = new LogEventProperty("Application", new ScalarValue(applicationName));
            _environmentProperty = new LogEventProperty("Environment", new ScalarValue(environment));
            
            var version = typeof(ApplicationContextEnricher).Assembly.GetName().Version?.ToString() ?? "1.0.0";
            _versionProperty = new LogEventProperty("Version", new ScalarValue(version));
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(_applicationProperty);
            logEvent.AddPropertyIfAbsent(_environmentProperty);
            logEvent.AddPropertyIfAbsent(_versionProperty);
        }
    }

    /// <summary>
    /// Structured logging models for consistent log data
    /// </summary>
    public static class LoggingModels
    {
        public record PerformanceLogEntry(
            string Operation,
            long ElapsedMilliseconds,
            string? UserId = null,
            Dictionary<string, object>? Metrics = null)
        {
            public static void LogPerformance(Microsoft.Extensions.Logging.ILogger logger, string operation, long elapsedMs, string? userId = null, Dictionary<string, object>? metrics = null)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(logger, "Performance: {Operation} completed in {ElapsedMs}ms {UserId} {@Metrics}", 
                    operation, elapsedMs, userId ?? "Unknown", metrics ?? new Dictionary<string, object>());
            }
        }

        public record AuditLogEntry(
            string Action,
            string EntityType,
            string EntityId,
            string? UserId = null,
            Dictionary<string, object>? Changes = null)
        {
            public static void LogAudit(Microsoft.Extensions.Logging.ILogger logger, string action, string entityType, string entityId, string? userId = null, Dictionary<string, object>? changes = null)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(logger, "Audit: {Action} {EntityType} {EntityId} by {UserId} {@Changes}", 
                    action, entityType, entityId, userId ?? "System", changes ?? new Dictionary<string, object>());
            }
        }

        public record ErrorLogEntry(
            string ErrorType,
            string ErrorMessage,
            string? UserId = null,
            string? RequestId = null,
            Dictionary<string, object>? Context = null)
        {
            public static void LogError(Microsoft.Extensions.Logging.ILogger logger, Exception exception, string? userId = null, string? requestId = null, Dictionary<string, object>? context = null)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(logger, exception, "Error: {ErrorType} {ErrorMessage} User: {UserId} Request: {RequestId} {@Context}", 
                    exception.GetType().Name, exception.Message, userId ?? "Unknown", requestId ?? "Unknown", context ?? new Dictionary<string, object>());
            }
        }

        public record UserActionLogEntry(
            string Action,
            string Feature,
            string? UserId = null,
            Dictionary<string, object>? Parameters = null)
        {
            public static void LogUserAction(Microsoft.Extensions.Logging.ILogger logger, string action, string feature, string? userId = null, Dictionary<string, object>? parameters = null)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(logger, "UserAction: {Action} in {Feature} by {UserId} {@Parameters}", 
                    action, feature, userId ?? "Anonymous", parameters ?? new Dictionary<string, object>());
            }
        }
    }

    /// <summary>
    /// Extensions for easier structured logging
    /// </summary>
    public static class StructuredLoggingExtensions
    {
        /// <summary>
        /// Logs performance metrics in a structured way
        /// </summary>
        public static void LogPerformance(this Microsoft.Extensions.Logging.ILogger logger, string operation, long elapsedMilliseconds, string? userId = null, params (string key, object value)[] metrics)
        {
            var metricsDict = metrics?.ToDictionary(m => m.key, m => m.value);
            LoggingModels.PerformanceLogEntry.LogPerformance(logger, operation, elapsedMilliseconds, userId, metricsDict);
        }

        /// <summary>
        /// Logs audit events in a structured way
        /// </summary>
        public static void LogAudit(this Microsoft.Extensions.Logging.ILogger logger, string action, string entityType, string entityId, string? userId = null, params (string key, object value)[] changes)
        {
            var changesDict = changes?.ToDictionary(c => c.key, c => c.value);
            LoggingModels.AuditLogEntry.LogAudit(logger, action, entityType, entityId, userId, changesDict);
        }

        /// <summary>
        /// Logs errors in a structured way
        /// </summary>
        public static void LogStructuredError(this Microsoft.Extensions.Logging.ILogger logger, Exception exception, string? userId = null, string? requestId = null, params (string key, object value)[] context)
        {
            var contextDict = context?.ToDictionary(c => c.key, c => c.value);
            LoggingModels.ErrorLogEntry.LogError(logger, exception, userId, requestId, contextDict);
        }

        /// <summary>
        /// Logs user actions in a structured way
        /// </summary>
        public static void LogUserAction(this Microsoft.Extensions.Logging.ILogger logger, string action, string feature, string? userId = null, params (string key, object value)[] parameters)
        {
            var parametersDict = parameters?.ToDictionary(p => p.key, p => p.value);
            LoggingModels.UserActionLogEntry.LogUserAction(logger, action, feature, userId, parametersDict);
        }
    }

    /// <summary>
    /// Correlation ID management for request tracking
    /// </summary>
    public static class CorrelationIdManager
    {
        /// <summary>
        /// Starts a new correlation context
        /// </summary>
        /// <param name="correlationId">Optional correlation ID, generates new one if not provided</param>
        /// <returns>Disposable correlation context</returns>
        public static IDisposable BeginCorrelationContext(string? correlationId = null)
        {
            var id = correlationId ?? Guid.NewGuid().ToString("N")[..8];
            return new CorrelationContext(id);
        }

        private class CorrelationContext : IDisposable
        {
            private readonly string? _previousCorrelationId;

            public CorrelationContext(string correlationId)
            {
                _previousCorrelationId = CorrelationIdEnricher.Current;
                CorrelationIdEnricher.Current = correlationId;
            }

            public void Dispose()
            {
                CorrelationIdEnricher.Current = _previousCorrelationId;
            }
        }
    }
}