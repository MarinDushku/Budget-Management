// Memory Cache Service Implementation - High-Performance In-Memory Caching
// File: Shared/Infrastructure/Caching/MemoryCacheService.cs

using BudgetManagement.Shared.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace BudgetManagement.Shared.Infrastructure.Caching
{
    /// <summary>
    /// High-performance memory cache service implementation using IMemoryCache
    /// Provides enterprise-grade caching with statistics, events, and advanced features
    /// </summary>
    public class MemoryCacheService : ICacheService, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly MemoryCacheOptions _options;
        private readonly ConcurrentDictionary<string, CacheEntry> _keyTracker;
        private readonly CacheStatistics _statistics;
        private readonly object _statsLock = new();
        private bool _disposed;

        // Cache events
        public event EventHandler<CacheEventArgs>? ItemAdded;
        public event EventHandler<CacheEventArgs>? ItemRemoved;
        public event EventHandler<CacheEventArgs>? ItemExpired;
        public event EventHandler<CacheEventArgs>? CacheCleared;

        public MemoryCacheService(
            IMemoryCache memoryCache,
            ILogger<MemoryCacheService> logger,
            IOptions<MemoryCacheOptions> options)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? new MemoryCacheOptions();
            _keyTracker = new ConcurrentDictionary<string, CacheEntry>();
            _statistics = new CacheStatistics { LastResetTime = DateTime.UtcNow };
        }

        #region Basic Cache Operations

        public async Task<Result<T?>> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return Result<T?>.Failure(Error.Validation(Error.Codes.VALIDATION_ERROR, "Cache key cannot be null or empty"));

                var success = _memoryCache.TryGetValue(key, out var cachedValue);
                
                UpdateStatistics(success ? CacheEventType.CacheHit : CacheEventType.CacheMiss);

                if (success && cachedValue is T typedValue)
                {
                    _logger.LogDebug("Cache hit for key: {CacheKey}", key);
                    return Result<T?>.Success(typedValue);
                }

                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return Result<T?>.Success(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value for key: {CacheKey}", key);
                return Result<T?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache get operation failed", new Dictionary<string, object> { ["Key"] = key }));
            }
        }

        public async Task<Result> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return Result.Failure(Error.Validation(Error.Codes.VALIDATION_ERROR, "Cache key cannot be null or empty"));

                if (value == null)
                    return Result.Failure(Error.Validation(Error.Codes.VALIDATION_ERROR, "Cache value cannot be null"));

                var cacheEntryOptions = CreateCacheEntryOptions(expiration);
                var wasExisting = _keyTracker.ContainsKey(key);

                // Configure eviction callback
                cacheEntryOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    HandleEviction(evictedKey.ToString()!, reason);
                });

                _memoryCache.Set(key, value, cacheEntryOptions);

                // Track the key
                var entry = new CacheEntry
                {
                    Key = key,
                    Type = typeof(T),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null
                };
                
                _keyTracker.AddOrUpdate(key, entry, (k, existing) => entry);

                UpdateStatistics(CacheEventType.ItemAdded);

                var eventType = wasExisting ? CacheEventType.ItemUpdated : CacheEventType.ItemAdded;
                OnItemAdded(new CacheEventArgs
                {
                    Key = key,
                    ValueType = typeof(T),
                    EventType = eventType,
                    Metadata = { ["Expiration"] = expiration?.ToString() ?? "None" }
                });

                _logger.LogDebug("Cache set for key: {CacheKey}, Type: {ValueType}, Expiration: {Expiration}", 
                    key, typeof(T).Name, expiration?.ToString() ?? "None");

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {CacheKey}", key);
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache set operation failed", new Dictionary<string, object> { ["Key"] = key }));
            }
        }

        public async Task<Result> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return Result.Failure(Error.Validation(Error.Codes.VALIDATION_ERROR, "Cache key cannot be null or empty"));

                var wasPresent = _keyTracker.ContainsKey(key);
                _memoryCache.Remove(key);
                _keyTracker.TryRemove(key, out _);

                if (wasPresent)
                {
                    UpdateStatistics(CacheEventType.ItemRemoved);
                    OnItemRemoved(new CacheEventArgs
                    {
                        Key = key,
                        EventType = CacheEventType.ItemRemoved,
                        Reason = "Manual removal"
                    });

                    _logger.LogDebug("Cache removed for key: {CacheKey}", key);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache value for key: {CacheKey}", key);
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache remove operation failed", new Dictionary<string, object> { ["Key"] = key }));
            }
        }

        public async Task<Result> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(pattern))
                    return Result.Failure(Error.Validation(Error.Codes.VALIDATION_ERROR, "Pattern cannot be null or empty"));

                var regex = new Regex(WildcardToRegex(pattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var keysToRemove = _keyTracker.Keys.Where(key => regex.IsMatch(key)).ToList();

                var removeResults = new List<Result>();
                foreach (var key in keysToRemove)
                {
                    var result = await RemoveAsync(key, cancellationToken);
                    removeResults.Add(result);
                }

                var failedRemovals = removeResults.Count(r => r.IsFailure);
                if (failedRemovals > 0)
                {
                    _logger.LogWarning("Failed to remove {FailedCount} out of {TotalCount} cache entries matching pattern: {Pattern}", 
                        failedRemovals, keysToRemove.Count, pattern);
                }

                _logger.LogInformation("Removed {SuccessCount} cache entries matching pattern: {Pattern}", 
                    keysToRemove.Count - failedRemovals, pattern);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache values by pattern: {Pattern}", pattern);
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache pattern removal failed", new Dictionary<string, object> { ["Pattern"] = pattern }));
            }
        }

        public async Task<Result> ClearAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var keyCount = _keyTracker.Count;
                
                // Remove all tracked keys
                var keys = _keyTracker.Keys.ToList();
                foreach (var key in keys)
                {
                    _memoryCache.Remove(key);
                }
                
                _keyTracker.Clear();
                ResetStatistics();

                OnCacheCleared(new CacheEventArgs
                {
                    Key = "*",
                    EventType = CacheEventType.CacheCleared,
                    Metadata = { ["ClearedCount"] = keyCount }
                });

                _logger.LogInformation("Cache cleared, removed {KeyCount} entries", keyCount);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache clear operation failed"));
            }
        }

        #endregion

        #region Advanced Cache Operations

        public async Task<Result<T?>> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                // Try to get from cache first
                var getResult = await GetAsync<T>(key, cancellationToken);
                if (getResult.IsFailure)
                    return getResult;

                if (getResult.Value != null)
                {
                    return getResult;
                }

                // Cache miss, execute factory function
                _logger.LogDebug("Cache miss for key {CacheKey}, executing factory function", key);
                
                using var timedOperation = new TimedOperation(_logger, $"Factory function for {key}");
                var factoryResult = await factory(cancellationToken);
                
                if (factoryResult != null)
                {
                    var setResult = await SetAsync(key, factoryResult, expiration, cancellationToken);
                    if (setResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to cache factory result for key {CacheKey}: {Error}", key, setResult.Error);
                    }
                }

                return Result<T?>.Success(factoryResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrSet for key: {CacheKey}", key);
                return Result<T?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache GetOrSet operation failed", new Dictionary<string, object> { ["Key"] = key }));
            }
        }

        public async Task<Result<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var exists = _keyTracker.ContainsKey(key) && _memoryCache.TryGetValue(key, out _);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {CacheKey}", key);
                return Result<bool>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache exists check failed"));
            }
        }

        public async Task<Result<TimeSpan?>> GetTtlAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_keyTracker.TryGetValue(key, out var entry) && entry.ExpiresAt.HasValue)
                {
                    var ttl = entry.ExpiresAt.Value - DateTime.UtcNow;
                    return Result<TimeSpan?>.Success(ttl > TimeSpan.Zero ? ttl : null);
                }

                return Result<TimeSpan?>.Success(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TTL for key: {CacheKey}", key);
                return Result<TimeSpan?>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache TTL check failed"));
            }
        }

        public async Task<Result> RefreshAsync(string key, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_memoryCache.TryGetValue(key, out var value))
                {
                    return Result.Failure(Error.NotFound(Error.Codes.NOT_FOUND, "Cache key not found"));
                }

                // Reset the expiration
                var cacheEntryOptions = CreateCacheEntryOptions(expiration);
                _memoryCache.Set(key, value, cacheEntryOptions);

                // Update tracking
                if (_keyTracker.TryGetValue(key, out var entry))
                {
                    entry.ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null;
                }

                _logger.LogDebug("Cache refreshed for key: {CacheKey}", key);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing cache for key: {CacheKey}", key);
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Cache refresh failed"));
            }
        }

        #endregion

        #region Batch Operations

        public async Task<Result<Dictionary<string, T?>>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var result = new Dictionary<string, T?>();
                var tasks = keys.Select(async key =>
                {
                    var getResult = await GetAsync<T>(key, cancellationToken);
                    return new { Key = key, Value = getResult.IsSuccess ? getResult.Value : null };
                });

                var results = await Task.WhenAll(tasks);
                foreach (var item in results)
                {
                    result[item.Key] = item.Value;
                }

                return Result<Dictionary<string, T?>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple cache values");
                return Result<Dictionary<string, T?>>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Batch get operation failed"));
            }
        }

        public async Task<Result> SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var tasks = keyValuePairs.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken));
                var results = await Task.WhenAll(tasks);

                var failedCount = results.Count(r => r.IsFailure);
                if (failedCount > 0)
                {
                    _logger.LogWarning("Failed to set {FailedCount} out of {TotalCount} cache entries in batch operation", 
                        failedCount, keyValuePairs.Count);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting multiple cache values");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Batch set operation failed"));
            }
        }

        public async Task<Result> RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            try
            {
                var tasks = keys.Select(key => RemoveAsync(key, cancellationToken));
                var results = await Task.WhenAll(tasks);

                var failedCount = results.Count(r => r.IsFailure);
                if (failedCount > 0)
                {
                    _logger.LogWarning("Failed to remove {FailedCount} cache entries in batch operation", failedCount);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing multiple cache values");
                return Result.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Batch remove operation failed"));
            }
        }

        #endregion

        #region Cache Statistics and Monitoring

        public async Task<Result<CacheStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                lock (_statsLock)
                {
                    var stats = new CacheStatistics
                    {
                        TotalItems = _keyTracker.Count,
                        HitCount = _statistics.HitCount,
                        MissCount = _statistics.MissCount,
                        EvictionCount = _statistics.EvictionCount,
                        ExpirationCount = _statistics.ExpirationCount,
                        LastResetTime = _statistics.LastResetTime,
                        ProviderSpecificStats = new Dictionary<string, object>
                        {
                            ["Provider"] = "MemoryCache",
                            ["TrackedKeys"] = _keyTracker.Count,
                            ["Options"] = new
                            {
                                _options.SizeLimit,
                                _options.CompactionPercentage,
                                _options.ExpirationScanFrequency
                            }
                        }
                    };

                    return Result<CacheStatistics>.Success(stats);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return Result<CacheStatistics>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to get cache statistics"));
            }
        }

        public async Task<Result<IEnumerable<string>>> GetKeysAsync(string? pattern = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = _keyTracker.Keys.AsEnumerable();

                if (!string.IsNullOrEmpty(pattern))
                {
                    var regex = new Regex(WildcardToRegex(pattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    keys = keys.Where(key => regex.IsMatch(key));
                }

                return Result<IEnumerable<string>>.Success(keys.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache keys with pattern: {Pattern}", pattern);
                return Result<IEnumerable<string>>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to get cache keys"));
            }
        }

        public async Task<Result<long>> GetCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return Result<long>.Success(_keyTracker.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache count");
                return Result<long>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Failed to get cache count"));
            }
        }

        #endregion

        #region Helper Methods

        private MemoryCacheEntryOptions CreateCacheEntryOptions(TimeSpan? expiration)
        {
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else if (_options.ExpirationScanFrequency != TimeSpan.Zero)
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(30); // Default sliding expiration
            }

            options.Priority = CacheItemPriority.Normal;
            return options;
        }

        private void HandleEviction(string key, EvictionReason reason)
        {
            _keyTracker.TryRemove(key, out _);

            var eventType = reason switch
            {
                EvictionReason.Expired => CacheEventType.ItemExpired,
                EvictionReason.Capacity => CacheEventType.ItemEvicted,
                EvictionReason.TokenExpired => CacheEventType.ItemExpired,
                _ => CacheEventType.ItemRemoved
            };

            UpdateStatistics(eventType);

            var eventArgs = new CacheEventArgs
            {
                Key = key,
                EventType = eventType,
                Reason = reason.ToString(),
                Metadata = { ["EvictionReason"] = reason.ToString() }
            };

            if (eventType == CacheEventType.ItemExpired)
            {
                OnItemExpired(eventArgs);
            }
            else
            {
                OnItemRemoved(eventArgs);
            }

            _logger.LogDebug("Cache entry evicted: {CacheKey}, Reason: {EvictionReason}", key, reason);
        }

        private void UpdateStatistics(CacheEventType eventType)
        {
            lock (_statsLock)
            {
                switch (eventType)
                {
                    case CacheEventType.CacheHit:
                        _statistics.HitCount++;
                        break;
                    case CacheEventType.CacheMiss:
                        _statistics.MissCount++;
                        break;
                    case CacheEventType.ItemEvicted:
                        _statistics.EvictionCount++;
                        break;
                    case CacheEventType.ItemExpired:
                        _statistics.ExpirationCount++;
                        break;
                }
            }
        }

        private void ResetStatistics()
        {
            lock (_statsLock)
            {
                _statistics.HitCount = 0;
                _statistics.MissCount = 0;
                _statistics.EvictionCount = 0;
                _statistics.ExpirationCount = 0;
                _statistics.LastResetTime = DateTime.UtcNow;
            }
        }

        private static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }

        #endregion

        #region Event Handlers

        protected virtual void OnItemAdded(CacheEventArgs e) => ItemAdded?.Invoke(this, e);
        protected virtual void OnItemRemoved(CacheEventArgs e) => ItemRemoved?.Invoke(this, e);
        protected virtual void OnItemExpired(CacheEventArgs e) => ItemExpired?.Invoke(this, e);
        protected virtual void OnCacheCleared(CacheEventArgs e) => CacheCleared?.Invoke(this, e);

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _keyTracker.Clear();
                _disposed = true;
            }
        }

        #endregion

        #region Internal Classes

        private class CacheEntry
        {
            public string Key { get; set; } = string.Empty;
            public Type? Type { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        private class TimedOperation : IDisposable
        {
            private readonly ILogger _logger;
            private readonly string _operationName;
            private readonly System.Diagnostics.Stopwatch _stopwatch;

            public TimedOperation(ILogger logger, string operationName)
            {
                _logger = logger;
                _operationName = operationName;
                _stopwatch = System.Diagnostics.Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                if (_stopwatch.ElapsedMilliseconds > 100) // Log if operation takes more than 100ms
                {
                    _logger.LogWarning("Slow cache operation: {OperationName} took {ElapsedMs}ms", 
                        _operationName, _stopwatch.ElapsedMilliseconds);
                }
            }
        }

        #endregion
    }
}