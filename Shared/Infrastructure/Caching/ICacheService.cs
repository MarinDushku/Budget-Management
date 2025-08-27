// Cache Service Interface - Enterprise Caching Abstraction
// File: Shared/Infrastructure/Caching/ICacheService.cs

using BudgetManagement.Shared.Core;

namespace BudgetManagement.Shared.Infrastructure.Caching
{
    /// <summary>
    /// Generic cache service interface providing consistent caching operations
    /// Supports multiple cache providers and advanced caching scenarios
    /// </summary>
    public interface ICacheService
    {
        // Basic Cache Operations
        Task<Result<T?>> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        Task<Result> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
        Task<Result> RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<Result> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
        Task<Result> ClearAsync(CancellationToken cancellationToken = default);

        // Advanced Cache Operations
        Task<Result<T?>> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
        Task<Result<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default);
        Task<Result<TimeSpan?>> GetTtlAsync(string key, CancellationToken cancellationToken = default);
        Task<Result> RefreshAsync(string key, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        // Batch Operations
        Task<Result<Dictionary<string, T?>>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class;
        Task<Result> SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
        Task<Result> RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        // Cache Statistics
        Task<Result<CacheStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<string>>> GetKeysAsync(string? pattern = null, CancellationToken cancellationToken = default);
        Task<Result<long>> GetCountAsync(CancellationToken cancellationToken = default);

        // Cache Events
        event EventHandler<CacheEventArgs>? ItemAdded;
        event EventHandler<CacheEventArgs>? ItemRemoved;
        event EventHandler<CacheEventArgs>? ItemExpired;
        event EventHandler<CacheEventArgs>? CacheCleared;
    }

    /// <summary>
    /// Typed cache service for specific data types
    /// Provides strongly-typed caching with domain-specific operations
    /// </summary>
    /// <typeparam name="T">Type of data to cache</typeparam>
    public interface ITypedCacheService<T> where T : class
    {
        Task<Result<T?>> GetAsync(string key, CancellationToken cancellationToken = default);
        Task<Result> SetAsync(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task<Result<T?>> GetOrSetAsync(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task<Result> RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<Result> InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache configuration interface
    /// </summary>
    public interface ICacheConfiguration
    {
        TimeSpan DefaultExpiration { get; }
        int MaxItems { get; }
        bool EnableStatistics { get; }
        CacheEvictionPolicy EvictionPolicy { get; }
        Dictionary<string, object> ProviderSettings { get; }
    }

    /// <summary>
    /// Cache statistics data structure
    /// </summary>
    public class CacheStatistics
    {
        public long TotalItems { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
        public long TotalRequests => HitCount + MissCount;
        public long EvictionCount { get; set; }
        public long ExpirationCount { get; set; }
        public DateTime LastResetTime { get; set; }
        public TimeSpan Uptime => DateTime.UtcNow - LastResetTime;
        public Dictionary<string, object> ProviderSpecificStats { get; set; } = new();
    }

    /// <summary>
    /// Cache event arguments
    /// </summary>
    public class CacheEventArgs : EventArgs
    {
        public string Key { get; init; } = string.Empty;
        public Type? ValueType { get; init; }
        public CacheEventType EventType { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string? Reason { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Cache event types
    /// </summary>
    public enum CacheEventType
    {
        ItemAdded,
        ItemUpdated,
        ItemRemoved,
        ItemExpired,
        ItemEvicted,
        CacheCleared,
        CacheMiss,
        CacheHit
    }

    /// <summary>
    /// Cache eviction policies
    /// </summary>
    public enum CacheEvictionPolicy
    {
        LeastRecentlyUsed,
        LeastFrequentlyUsed,
        FirstInFirstOut,
        Random,
        None
    }

    /// <summary>
    /// Cache invalidation strategies
    /// </summary>
    public enum CacheInvalidationStrategy
    {
        TimeBasedExpiration,
        TagBasedInvalidation,
        EventBasedInvalidation,
        ManualInvalidation,
        DependencyBasedInvalidation
    }

    /// <summary>
    /// Cache key builder interface for consistent key generation
    /// </summary>
    public interface ICacheKeyBuilder
    {
        string BuildKey(params object[] keyParts);
        string BuildKey(string prefix, params object[] keyParts);
        string BuildKey(Type type, params object[] keyParts);
        string BuildTaggedKey(string key, params string[] tags);
        IEnumerable<string> ExtractTags(string key);
    }

    /// <summary>
    /// Cache serialization interface
    /// </summary>
    public interface ICacheSerializer
    {
        Task<byte[]> SerializeAsync<T>(T value, CancellationToken cancellationToken = default);
        Task<T?> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default);
        Task<object?> DeserializeAsync(byte[] data, Type type, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache warming interface for preloading frequently used data
    /// </summary>
    public interface ICacheWarmer
    {
        Task<Result> WarmupAsync(CancellationToken cancellationToken = default);
        Task<Result> WarmupAsync(string category, CancellationToken cancellationToken = default);
        Task<Result> WarmupAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
        Task<Result<CacheWarmupResult>> GetWarmupStatusAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache warmup result
    /// </summary>
    public class CacheWarmupResult
    {
        public int TotalItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool IsSuccess => FailedItems == 0;
        public double SuccessRate => TotalItems > 0 ? (double)SuccessfulItems / TotalItems : 1.0;
    }

    /// <summary>
    /// Cache health check interface
    /// </summary>
    public interface ICacheHealthCheck
    {
        Task<Result<CacheHealthResult>> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache health result
    /// </summary>
    public class CacheHealthResult
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = "Unknown";
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> Diagnostics { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Extensions for easier cache usage
    /// </summary>
    public static class CacheServiceExtensions
    {
        /// <summary>
        /// Gets or sets a value with automatic key generation based on method parameters
        /// </summary>
        public static async Task<Result<T?>> GetOrSetAsync<T>(
            this ICacheService cacheService,
            Func<CancellationToken, Task<T?>> factory,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            params object[] parameters) where T : class
        {
            var keyBuilder = new DefaultCacheKeyBuilder();
            var key = keyBuilder.BuildKey(typeof(T), memberName, sourceFilePath, parameters);
            return await cacheService.GetOrSetAsync(key, factory, expiration, cancellationToken);
        }

        /// <summary>
        /// Removes cache entries by type
        /// </summary>
        public static async Task<Result> RemoveByTypeAsync<T>(
            this ICacheService cacheService,
            CancellationToken cancellationToken = default)
        {
            var pattern = $"{typeof(T).FullName}:*";
            return await cacheService.RemoveByPatternAsync(pattern, cancellationToken);
        }

        /// <summary>
        /// Sets a value with tags for group invalidation
        /// </summary>
        public static async Task<Result> SetWithTagsAsync<T>(
            this ICacheService cacheService,
            string key,
            T value,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default,
            params string[] tags) where T : class
        {
            var keyBuilder = new DefaultCacheKeyBuilder();
            var taggedKey = keyBuilder.BuildTaggedKey(key, tags);
            return await cacheService.SetAsync(taggedKey, value, expiration, cancellationToken);
        }
    }

    /// <summary>
    /// Default cache key builder implementation
    /// </summary>
    public class DefaultCacheKeyBuilder : ICacheKeyBuilder
    {
        private const char KeySeparator = ':';
        private const char TagSeparator = '|';
        private const string TagPrefix = "#tags=";

        public string BuildKey(params object[] keyParts)
        {
            return string.Join(KeySeparator, keyParts.Select(FormatKeyPart));
        }

        public string BuildKey(string prefix, params object[] keyParts)
        {
            var allParts = new object[] { prefix }.Concat(keyParts);
            return BuildKey(allParts.ToArray());
        }

        public string BuildKey(Type type, params object[] keyParts)
        {
            var allParts = new object[] { type.FullName }.Concat(keyParts);
            return BuildKey(allParts.ToArray());
        }

        public string BuildTaggedKey(string key, params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return key;

            var tagString = string.Join(TagSeparator.ToString(), tags);
            return $"{key}{KeySeparator}{TagPrefix}{tagString}";
        }

        public IEnumerable<string> ExtractTags(string key)
        {
            var tagIndex = key.IndexOf(TagPrefix, StringComparison.Ordinal);
            if (tagIndex == -1)
                return Enumerable.Empty<string>();

            var tagsPart = key.Substring(tagIndex + TagPrefix.Length);
            return tagsPart.Split(TagSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string FormatKeyPart(object keyPart)
        {
            return keyPart switch
            {
                null => "null",
                string str => str,
                DateTime dt => dt.ToString("yyyy-MM-dd-HH-mm-ss"),
                Guid guid => guid.ToString("N"),
                _ => keyPart.ToString() ?? string.Empty
            };
        }
    }
}