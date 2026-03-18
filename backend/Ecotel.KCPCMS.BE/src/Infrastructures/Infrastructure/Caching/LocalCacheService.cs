using System.Collections.Concurrent;
using Application.Common.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Infrastructure.Caching;

public class LocalCacheService(IMemoryCache cache, ILogger<LocalCacheService> logger) : ICacheService
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _signals = new();

    public void SetWithSignal<T>(string key, T value, string signalKey)
    {
        // Lấy hoặc tạo mới Token cho SignalKey này
        var cts = _signals.GetOrAdd(signalKey, _ => new CancellationTokenSource());

        var options = new MemoryCacheEntryOptions()
            .AddExpirationToken(new CancellationChangeToken(cts.Token))
            .SetSlidingExpiration(TimeSpan.FromMinutes(4))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(8));

        cache.Set(key, value, options);
    }

    public void InvalidateGroup(string signalKey)
    {
        if (_signals.TryRemove(signalKey, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public T? Get<T>(string key) =>
        cache.Get<T>(key);

    public Task<T?> GetAsync<T>(string key, CancellationToken token = default) =>
        Task.FromResult(Get<T>(key));

    public void Refresh(string key) =>
        cache.TryGetValue(key, out _);

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        Refresh(key);
        return Task.CompletedTask;
    }

    public void Remove(string key) =>
        cache.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Set<T>(string key, T value, TimeSpan? slidingExpiration = null)
    {
        slidingExpiration ??= TimeSpan.FromMinutes(10); // Default expiration time of 10 minutes.

        cache.Set(key, value, new MemoryCacheEntryOptions { SlidingExpiration = slidingExpiration });
        logger.LogDebug("Added to Cache : {CacheKey}", key);
    }

    public Task SetAsync<T>(string key, T value)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.MaxValue
        };

        cache.Set(key, value, options);

        logger.LogDebug("Added to Cache : {CacheKey}", key);
        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan slidingExpiration)
    {
        Set(key, value, slidingExpiration);
        return Task.CompletedTask;
    }
}