// Test Configuration and Utilities - Centralized Test Settings
// File: BudgetManagement.Tests/Infrastructure/TestConfiguration.cs

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace BudgetManagement.Tests.Infrastructure
{
    /// <summary>
    /// Centralized configuration for test settings and utilities
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        /// Default test timeout in milliseconds
        /// </summary>
        public const int DefaultTimeoutMs = 5000;

        /// <summary>
        /// Long-running test timeout in milliseconds
        /// </summary>
        public const int LongRunningTimeoutMs = 30000;

        /// <summary>
        /// Performance test timeout in milliseconds
        /// </summary>
        public const int PerformanceTestTimeoutMs = 60000;

        /// <summary>
        /// Default page size for pagination tests
        /// </summary>
        public const int DefaultPageSize = 20;

        /// <summary>
        /// Maximum items to generate for large dataset tests
        /// </summary>
        public const int MaxTestDataItems = 1000;

        /// <summary>
        /// Test database connection string template
        /// </summary>
        public const string TestDatabaseTemplate = "Data Source=:memory:;Cache=Shared;";

        /// <summary>
        /// Gets test configuration from appsettings.test.json
        /// </summary>
        /// <returns>Configuration root</returns>
        public static IConfigurationRoot GetTestConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: true)
                .AddEnvironmentVariables("TEST_")
                .Build();
        }

        /// <summary>
        /// Configures services for testing with common test dependencies
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="output">Test output helper</param>
        /// <returns>Configured service collection</returns>
        public static IServiceCollection ConfigureTestServices(
            this IServiceCollection services, 
            ITestOutputHelper output)
        {
            // Add configuration
            var config = GetTestConfiguration();
            services.AddSingleton<IConfiguration>(config);

            // Add logging with test output
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddXUnit(output);
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            return services;
        }

        /// <summary>
        /// Creates a test-specific temporary directory
        /// </summary>
        /// <param name="testName">Name of the test</param>
        /// <returns>Temporary directory path</returns>
        public static string CreateTestDirectory(string testName)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "BudgetManagementTests", testName, Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Creates a test database file path
        /// </summary>
        /// <param name="testName">Name of the test</param>
        /// <returns>Test database file path</returns>
        public static string CreateTestDatabasePath(string testName)
        {
            var testDir = CreateTestDirectory(testName);
            return Path.Combine(testDir, "test.db");
        }

        /// <summary>
        /// Cleans up test directory and files
        /// </summary>
        /// <param name="testDirectory">Test directory to clean up</param>
        public static void CleanupTestDirectory(string testDirectory)
        {
            try
            {
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    /// <summary>
    /// Test categories for organizing tests
    /// </summary>
    public static class TestCategories
    {
        public const string Unit = "Unit";
        public const string Integration = "Integration";
        public const string Performance = "Performance";
        public const string EndToEnd = "E2E";
        public const string Database = "Database";
        public const string UI = "UI";
        public const string Security = "Security";
        public const string Regression = "Regression";
    }

    /// <summary>
    /// Custom test attributes for categorizing tests
    /// </summary>
    public class UnitTestAttribute : Attribute
    {
        public string Category => TestCategories.Unit;
    }

    public class IntegrationTestAttribute : Attribute
    {
        public string Category => TestCategories.Integration;
    }

    public class PerformanceTestAttribute : Attribute
    {
        public string Category => TestCategories.Performance;
    }

    public class DatabaseTestAttribute : Attribute
    {
        public string Category => TestCategories.Database;
    }

    /// <summary>
    /// Test data size categories
    /// </summary>
    public enum TestDataSize
    {
        Small = 10,
        Medium = 100,
        Large = 1000,
        ExtraLarge = 10000
    }

    /// <summary>
    /// Performance test thresholds
    /// </summary>
    public static class PerformanceThresholds
    {
        /// <summary>
        /// Maximum acceptable response time for queries in milliseconds
        /// </summary>
        public const int QueryMaxResponseTimeMs = 1000;

        /// <summary>
        /// Maximum acceptable response time for commands in milliseconds
        /// </summary>
        public const int CommandMaxResponseTimeMs = 2000;

        /// <summary>
        /// Maximum memory usage increase in MB
        /// </summary>
        public const int MaxMemoryIncreaseMB = 50;

        /// <summary>
        /// Minimum acceptable throughput (operations per second)
        /// </summary>
        public const int MinThroughputOps = 100;
    }

    /// <summary>
    /// Utilities for performance testing
    /// </summary>
    public static class PerformanceTestUtilities
    {
        /// <summary>
        /// Measures execution time and memory usage of an operation
        /// </summary>
        /// <param name="operation">Operation to measure</param>
        /// <returns>Performance metrics</returns>
        public static async Task<PerformanceMetrics> MeasureAsync(Func<Task> operation)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await operation();
                return new PerformanceMetrics
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    MemoryUsedBytes = GC.GetTotalMemory(false) - initialMemory,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new PerformanceMetrics
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    MemoryUsedBytes = GC.GetTotalMemory(false) - initialMemory,
                    Success = false,
                    Error = ex
                };
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Measures throughput by running an operation multiple times
        /// </summary>
        /// <param name="operation">Operation to measure</param>
        /// <param name="iterations">Number of iterations</param>
        /// <param name="maxDurationMs">Maximum test duration in milliseconds</param>
        /// <returns>Throughput metrics</returns>
        public static async Task<ThroughputMetrics> MeasureThroughputAsync(
            Func<Task> operation, 
            int iterations = 100,
            int maxDurationMs = 30000)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var completedOperations = 0;
            var errors = new List<Exception>();

            while (completedOperations < iterations && stopwatch.ElapsedMilliseconds < maxDurationMs)
            {
                try
                {
                    await operation();
                    completedOperations++;
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }

            stopwatch.Stop();

            return new ThroughputMetrics
            {
                TotalOperations = completedOperations,
                TotalTimeMs = stopwatch.ElapsedMilliseconds,
                OperationsPerSecond = completedOperations > 0 
                    ? (double)completedOperations / (stopwatch.ElapsedMilliseconds / 1000.0)
                    : 0,
                ErrorCount = errors.Count,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Performance metrics data structure
    /// </summary>
    public class PerformanceMetrics
    {
        public long ExecutionTimeMs { get; set; }
        public long MemoryUsedBytes { get; set; }
        public bool Success { get; set; }
        public Exception? Error { get; set; }
        public double MemoryUsedMB => MemoryUsedBytes / (1024.0 * 1024.0);
    }

    /// <summary>
    /// Throughput metrics data structure
    /// </summary>
    public class ThroughputMetrics
    {
        public int TotalOperations { get; set; }
        public long TotalTimeMs { get; set; }
        public double OperationsPerSecond { get; set; }
        public int ErrorCount { get; set; }
        public List<Exception> Errors { get; set; } = new();
        public double ErrorRate => TotalOperations > 0 ? (double)ErrorCount / TotalOperations : 0;
        public bool MeetsThreshold => OperationsPerSecond >= PerformanceThresholds.MinThroughputOps;
    }

    /// <summary>
    /// Test retry policy for flaky tests
    /// </summary>
    public static class RetryPolicy
    {
        /// <summary>
        /// Retries an operation with exponential backoff
        /// </summary>
        /// <param name="operation">Operation to retry</param>
        /// <param name="maxAttempts">Maximum number of attempts</param>
        /// <param name="baseDelayMs">Base delay between retries</param>
        /// <returns>Operation result</returns>
        public static async Task<T> RetryAsync<T>(
            Func<Task<T>> operation,
            int maxAttempts = 3,
            int baseDelayMs = 100)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    lastException = ex;
                    var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                    await Task.Delay(TimeSpan.FromMilliseconds(delay));
                }
            }

            throw lastException ?? new InvalidOperationException("Retry operation failed");
        }

        /// <summary>
        /// Retries an operation with exponential backoff (non-generic version)
        /// </summary>
        /// <param name="operation">Operation to retry</param>
        /// <param name="maxAttempts">Maximum number of attempts</param>
        /// <param name="baseDelayMs">Base delay between retries</param>
        public static async Task RetryAsync(
            Func<Task> operation,
            int maxAttempts = 3,
            int baseDelayMs = 100)
        {
            await RetryAsync(async () =>
            {
                await operation();
                return true;
            }, maxAttempts, baseDelayMs);
        }
    }

    /// <summary>
    /// Extensions for test assertions with better error messages
    /// </summary>
    public static class TestAssertionExtensions
    {
        /// <summary>
        /// Asserts that an operation completes within the specified time
        /// </summary>
        /// <param name="operation">Operation to time</param>
        /// <param name="maxDurationMs">Maximum allowed duration</param>
        /// <param name="operationName">Name of the operation for error messages</param>
        public static async Task ShouldCompleteWithin(
            this Task operation,
            int maxDurationMs,
            string operationName = "Operation")
        {
            var completedTask = await Task.WhenAny(operation, Task.Delay(maxDurationMs));
            
            if (completedTask != operation)
            {
                throw new TimeoutException($"{operationName} did not complete within {maxDurationMs}ms");
            }

            // If the operation completed, check if it threw an exception
            await operation;
        }

        /// <summary>
        /// Asserts that a performance metric meets the specified threshold
        /// </summary>
        /// <param name="metrics">Performance metrics</param>
        /// <param name="maxExecutionTimeMs">Maximum allowed execution time</param>
        /// <param name="maxMemoryMB">Maximum allowed memory usage</param>
        public static void ShouldMeetPerformanceThresholds(
            this PerformanceMetrics metrics,
            long? maxExecutionTimeMs = null,
            double? maxMemoryMB = null)
        {
            if (!metrics.Success)
            {
                throw new AssertionException($"Operation failed: {metrics.Error?.Message}");
            }

            if (maxExecutionTimeMs.HasValue && metrics.ExecutionTimeMs > maxExecutionTimeMs.Value)
            {
                throw new AssertionException(
                    $"Operation took {metrics.ExecutionTimeMs}ms, which exceeds the threshold of {maxExecutionTimeMs.Value}ms");
            }

            if (maxMemoryMB.HasValue && metrics.MemoryUsedMB > maxMemoryMB.Value)
            {
                throw new AssertionException(
                    $"Operation used {metrics.MemoryUsedMB:F2}MB of memory, which exceeds the threshold of {maxMemoryMB.Value}MB");
            }
        }
    }

    /// <summary>
    /// Custom exception for test assertions
    /// </summary>
    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message)
        {
        }

        public AssertionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}