// Application Health Check - Comprehensive System Health Monitoring
// File: Shared/Infrastructure/ApplicationHealthCheck.cs

using BudgetManagement.Shared.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// Comprehensive application health check that monitors system resources,
    /// memory usage, performance metrics, and overall application health
    /// </summary>
    public class ApplicationHealthCheck : IHealthCheck
    {
        private readonly ILogger<ApplicationHealthCheck> _logger;
        private readonly ICacheService? _cacheService;

        // Health thresholds
        private const int MaxMemoryUsageMB = 500;
        private const double MaxCpuUsagePercent = 80.0;
        private const int MaxResponseTimeMs = 5000;
        private const int MaxDiskUsagePercent = 90;

        public ApplicationHealthCheck(
            ILogger<ApplicationHealthCheck> logger,
            ICacheService? cacheService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            const string healthCheckName = "Application";
            var healthData = new Dictionary<string, object>();
            var issues = new List<string>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("Starting comprehensive application health check");

                // Check 1: Memory Usage
                var memoryHealthy = CheckMemoryUsage(healthData, issues);
                
                // Check 2: Application Performance
                var performanceHealthy = CheckApplicationPerformance(healthData, issues);
                
                // Check 3: System Resources
                var systemHealthy = await CheckSystemResourcesAsync(healthData, issues, cancellationToken);
                
                // Check 4: Cache Performance (if available)
                var cacheHealthy = await CheckCachePerformanceAsync(healthData, issues, cancellationToken);
                
                // Check 5: Application Metrics
                var metricsHealthy = CheckApplicationMetrics(healthData, issues);

                stopwatch.Stop();
                healthData["health_check_duration_ms"] = stopwatch.ElapsedMilliseconds;
                healthData["last_checked"] = DateTime.UtcNow;
                healthData["check_version"] = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";

                // Determine overall health status
                var criticalIssues = issues.Where(i => i.Contains("CRITICAL")).ToList();
                var warningIssues = issues.Where(i => i.Contains("WARNING")).ToList();

                if (criticalIssues.Any())
                {
                    _logger.LogError("Application health check failed with critical issues: {Issues}", 
                        string.Join(", ", criticalIssues));
                    
                    healthData["critical_issues"] = criticalIssues;
                    healthData["warning_issues"] = warningIssues;
                    
                    return HealthCheckResult.Unhealthy("Application has critical issues", null, healthData);
                }

                if (warningIssues.Any())
                {
                    _logger.LogWarning("Application health check degraded with warnings: {Issues}", 
                        string.Join(", ", warningIssues));
                    
                    healthData["warning_issues"] = warningIssues;
                    
                    return HealthCheckResult.Degraded("Application has performance warnings", null, healthData);
                }

                _logger.LogDebug("Application health check passed successfully");
                return HealthCheckResult.Healthy("Application is functioning optimally", healthData);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Application health check was cancelled");
                return HealthCheckResult.Unhealthy("Health check was cancelled", null, healthData);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Application health check failed with exception");
                
                healthData["error"] = ex.Message;
                healthData["health_check_duration_ms"] = stopwatch.ElapsedMilliseconds;
                
                return HealthCheckResult.Unhealthy("Application health check failed", ex, healthData);
            }
        }

        private bool CheckMemoryUsage(Dictionary<string, object> healthData, List<string> issues)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var workingSetMB = process.WorkingSet64 / (1024 * 1024);
                var privateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024);
                var gcTotalMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);

                healthData["memory_working_set_mb"] = workingSetMB;
                healthData["memory_private_mb"] = privateMemoryMB;
                healthData["memory_gc_total_mb"] = gcTotalMemoryMB;
                healthData["memory_threshold_mb"] = MaxMemoryUsageMB;

                // Check GC generations
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);

                healthData["gc_gen0_collections"] = gen0Collections;
                healthData["gc_gen1_collections"] = gen1Collections;
                healthData["gc_gen2_collections"] = gen2Collections;

                if (workingSetMB > MaxMemoryUsageMB)
                {
                    issues.Add($"CRITICAL: High memory usage - {workingSetMB}MB exceeds {MaxMemoryUsageMB}MB threshold");
                    return false;
                }

                if (workingSetMB > MaxMemoryUsageMB * 0.8)
                {
                    issues.Add($"WARNING: Elevated memory usage - {workingSetMB}MB approaching {MaxMemoryUsageMB}MB threshold");
                }

                if (gen2Collections > 10) // Arbitrary threshold
                {
                    issues.Add($"WARNING: High Gen2 GC pressure - {gen2Collections} collections detected");
                }

                return true;
            }
            catch (Exception ex)
            {
                issues.Add($"WARNING: Could not check memory usage - {ex.Message}");
                return true; // Don't fail health check for this
            }
        }

        private bool CheckApplicationPerformance(Dictionary<string, object> healthData, List<string> issues)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var totalProcessorTime = process.TotalProcessorTime;
                var startTime = process.StartTime;
                var uptime = DateTime.Now - startTime;

                healthData["application_uptime_seconds"] = uptime.TotalSeconds;
                healthData["total_processor_time_ms"] = totalProcessorTime.TotalMilliseconds;
                healthData["application_threads"] = process.Threads.Count;

                // Check if application has been running too long (potential memory leaks)
                if (uptime.TotalHours > 24)
                {
                    issues.Add($"WARNING: Application has been running for {uptime.TotalHours:F1} hours");
                }

                // Check thread count
                if (process.Threads.Count > 100)
                {
                    issues.Add($"WARNING: High thread count - {process.Threads.Count} threads active");
                }

                return true;
            }
            catch (Exception ex)
            {
                issues.Add($"WARNING: Could not check application performance - {ex.Message}");
                return true;
            }
        }

        private async Task<bool> CheckSystemResourcesAsync(Dictionary<string, object> healthData, List<string> issues, CancellationToken cancellationToken)
        {
            try
            {
                // Check available disk space
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                var systemDrive = drives.FirstOrDefault(d => d.Name.Contains(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:"));

                if (systemDrive != null)
                {
                    var totalSpaceGB = systemDrive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    var availableSpaceGB = systemDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    var usagePercent = ((totalSpaceGB - availableSpaceGB) / totalSpaceGB) * 100;

                    healthData["disk_total_gb"] = Math.Round(totalSpaceGB, 2);
                    healthData["disk_available_gb"] = Math.Round(availableSpaceGB, 2);
                    healthData["disk_usage_percent"] = Math.Round(usagePercent, 1);

                    if (usagePercent > MaxDiskUsagePercent)
                    {
                        issues.Add($"CRITICAL: Low disk space - {usagePercent:F1}% used");
                        return false;
                    }

                    if (usagePercent > MaxDiskUsagePercent * 0.9)
                    {
                        issues.Add($"WARNING: Disk space low - {usagePercent:F1}% used");
                    }
                }

                // Check system information
                healthData["processor_count"] = Environment.ProcessorCount;
                healthData["os_version"] = Environment.OSVersion.ToString();
                healthData["machine_name"] = Environment.MachineName;
                healthData["user_domain"] = Environment.UserDomainName;

                return true;
            }
            catch (Exception ex)
            {
                issues.Add($"WARNING: Could not check system resources - {ex.Message}");
                return true;
            }
        }

        private async Task<bool> CheckCachePerformanceAsync(Dictionary<string, object> healthData, List<string> issues, CancellationToken cancellationToken)
        {
            if (_cacheService == null)
            {
                healthData["cache_service"] = "Not Available";
                return true;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var statsResult = await _cacheService.GetStatisticsAsync(cancellationToken);
                stopwatch.Stop();

                healthData["cache_response_time_ms"] = stopwatch.ElapsedMilliseconds;

                if (statsResult.IsSuccess)
                {
                    var stats = statsResult.Value!;
                    healthData["cache_total_items"] = stats.TotalItems;
                    healthData["cache_hit_ratio"] = Math.Round(stats.HitRatio * 100, 2);
                    healthData["cache_total_requests"] = stats.TotalRequests;
                    healthData["cache_hit_count"] = stats.HitCount;
                    healthData["cache_miss_count"] = stats.MissCount;

                    // Check cache performance
                    if (stats.HitRatio < 0.5 && stats.TotalRequests > 100)
                    {
                        issues.Add($"WARNING: Low cache hit ratio - {stats.HitRatio * 100:F1}%");
                    }

                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        issues.Add($"WARNING: Slow cache response - {stopwatch.ElapsedMilliseconds}ms");
                    }

                    healthData["cache_service"] = "Healthy";
                }
                else
                {
                    issues.Add($"WARNING: Cache statistics unavailable - {statsResult.Error?.Message}");
                    healthData["cache_service"] = "Degraded";
                }

                return true;
            }
            catch (Exception ex)
            {
                issues.Add($"WARNING: Cache health check failed - {ex.Message}");
                healthData["cache_service"] = "Error";
                return true;
            }
        }

        private bool CheckApplicationMetrics(Dictionary<string, object> healthData, List<string> issues)
        {
            try
            {
                // Application version and build information
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "Unknown";
                var buildDate = GetBuildDate(assembly);

                healthData["application_version"] = version;
                healthData["build_date"] = buildDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";

                // Runtime information
                healthData["dotnet_version"] = Environment.Version.ToString();
                healthData["framework_description"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                healthData["os_description"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                healthData["process_architecture"] = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

                // Check if running in debug mode (not recommended for production)
#if DEBUG
                issues.Add("WARNING: Application is running in DEBUG mode");
                healthData["debug_mode"] = true;
#else
                healthData["debug_mode"] = false;
#endif

                return true;
            }
            catch (Exception ex)
            {
                issues.Add($"WARNING: Could not retrieve application metrics - {ex.Message}");
                return true;
            }
        }

        private static DateTime? GetBuildDate(Assembly assembly)
        {
            try
            {
                var attribute = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>();
                if (attribute != null && DateTime.TryParse(attribute.InformationalVersion, out var buildDate))
                {
                    return buildDate;
                }

                // Fallback to file creation time
                var location = assembly.Location;
                if (!string.IsNullOrEmpty(location) && File.Exists(location))
                {
                    return File.GetCreationTime(location);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Health check service for managing and aggregating health check results
    /// </summary>
    public interface IHealthCheckService
    {
        Task<ApplicationHealthResult> GetApplicationHealthAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<string, HealthCheckResult>> GetAllHealthChecksAsync(CancellationToken cancellationToken = default);
        Task<bool> IsApplicationHealthyAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of health check service
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService _microsoftHealthCheckService;
        private readonly ILogger<HealthCheckService> _logger;

        public HealthCheckService(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService,
            ILogger<HealthCheckService> logger)
        {
            _microsoftHealthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApplicationHealthResult> GetApplicationHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var report = await _microsoftHealthCheckService.CheckHealthAsync(cancellationToken);
                
                return new ApplicationHealthResult
                {
                    Status = report.Status,
                    TotalDuration = report.TotalDuration,
                    Entries = report.Entries.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new HealthCheckEntry
                        {
                            Status = kvp.Value.Status,
                            Description = kvp.Value.Description,
                            Duration = kvp.Value.Duration,
                            Exception = kvp.Value.Exception,
                            Data = kvp.Value.Data
                        }),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application health");
                return new ApplicationHealthResult
                {
                    Status = HealthStatus.Unhealthy,
                    Entries = new Dictionary<string, HealthCheckEntry>(),
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message
                };
            }
        }

        public async Task<Dictionary<string, HealthCheckResult>> GetAllHealthChecksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var report = await _microsoftHealthCheckService.CheckHealthAsync(cancellationToken);
                return report.Entries.ToDictionary(kvp => kvp.Key, kvp => 
                    new HealthCheckResult(kvp.Value.Status, kvp.Value.Description, kvp.Value.Exception, kvp.Value.Data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving health checks");
                return new Dictionary<string, HealthCheckResult>();
            }
        }

        public async Task<bool> IsApplicationHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var report = await _microsoftHealthCheckService.CheckHealthAsync(cancellationToken);
                return report.Status == HealthStatus.Healthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking application health");
                return false;
            }
        }
    }

    /// <summary>
    /// Application health result data structure
    /// </summary>
    public class ApplicationHealthResult
    {
        public HealthStatus Status { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public Dictionary<string, HealthCheckEntry> Entries { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string? Error { get; set; }

        public bool IsHealthy => Status == HealthStatus.Healthy;
        public bool IsDegraded => Status == HealthStatus.Degraded;
        public bool IsUnhealthy => Status == HealthStatus.Unhealthy;
        public int HealthyCount => Entries.Values.Count(e => e.Status == HealthStatus.Healthy);
        public int DegradedCount => Entries.Values.Count(e => e.Status == HealthStatus.Degraded);
        public int UnhealthyCount => Entries.Values.Count(e => e.Status == HealthStatus.Unhealthy);
        public double HealthPercentage => Entries.Any() ? (double)HealthyCount / Entries.Count * 100 : 100;
    }

    /// <summary>
    /// Health check entry data structure
    /// </summary>
    public class HealthCheckEntry
    {
        public HealthStatus Status { get; set; }
        public string? Description { get; set; }
        public TimeSpan Duration { get; set; }
        public Exception? Exception { get; set; }
        public IReadOnlyDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Extensions for health check registration
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Adds comprehensive application health checks
        /// </summary>
        public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<ApplicationHealthCheck>("application")
                .AddCheck<BudgetServiceHealthCheck>("budget-service")
                .AddCheck<ThemeServiceHealthCheck>("theme-service");

            services.AddScoped<IHealthCheckService, HealthCheckService>();
            return services;
        }
    }
}