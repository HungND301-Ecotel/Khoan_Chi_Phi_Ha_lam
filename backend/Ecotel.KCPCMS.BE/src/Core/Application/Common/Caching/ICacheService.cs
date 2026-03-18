namespace Application.Common.Caching;

public interface ICacheService
{
    void SetWithSignal<T>(string key, T value, string signalKey);
    void InvalidateGroup(string signalKey);
    T? Get<T>(string key);

    Task<T?> GetAsync<T>(string key, CancellationToken token = default);

    void Refresh(string key);

    Task RefreshAsync(string key, CancellationToken token = default);

    void Remove(string key);

    Task RemoveAsync(string key, CancellationToken token = default);

    void Set<T>(string key, T value, TimeSpan? slidingExpiration = null);

    Task SetAsync<T>(string key, T value, TimeSpan slidingExpiration);

    Task SetAsync<T>(string key, T value);
}