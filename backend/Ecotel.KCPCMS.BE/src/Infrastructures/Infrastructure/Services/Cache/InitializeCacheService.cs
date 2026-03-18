using Application.Interfaces;

namespace Infrastructure.Services.Cache;

public class InitializeCacheService : IInitializeCacheService
{
    public Task InitializeCacheAsync()
    {
        return Task.CompletedTask;
    }
}