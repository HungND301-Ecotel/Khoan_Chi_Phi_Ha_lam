using System.Text.Json;
using Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Auth.Authorization;

public class PermissionCacheService(IDistributedCache cache, IUserPermissionResolver resolver)
    : IPermissionCacheService
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        SlidingExpiration = TimeSpan.FromMinutes(20)
    };

    private static string BuildKey(int userId) => $"permissions:user:{userId}";

    public async Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(userId);
        var cached = await cache.GetAsync(key, cancellationToken);

        if (cached is not null)
        {
            return JsonSerializer.Deserialize<List<string>>(cached) ?? [];
        }

        var permissions = await resolver.ResolveAsync(userId, cancellationToken);
        var payload = JsonSerializer.SerializeToUtf8Bytes(permissions);

        await cache.SetAsync(key, payload, CacheOptions, cancellationToken);

        return permissions;
    }

    public Task InvalidateUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return cache.RemoveAsync(BuildKey(userId), cancellationToken);
    }

    public async Task InvalidateUsersAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds)
        {
            await cache.RemoveAsync(BuildKey(userId), cancellationToken);
        }
    }
}