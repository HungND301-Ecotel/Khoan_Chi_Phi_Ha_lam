using Application.Catalog.Permissions;
using Application.Common.Interfaces;
using Infrastructure.Auth.Authorization;
using Infrastructure.Auth.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Auth;

internal static class Startup
{
    internal static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddCurrentUser();
        services.Configure<SecuritySettings>(config.GetSection(nameof(SecuritySettings)));
        services.AddJwtAuth();
        services.AddPermissionAuthorization();
        return services;
    }

    internal static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentUserMiddleware>();

    private static void AddCurrentUser(this IServiceCollection services)
    {
        services
            .AddScoped<CurrentUserMiddleware>()
            .AddScoped<ICurrentUser, CurrentUser>()
            .AddScoped(sp => (ICurrentUserInitializer)sp.GetRequiredService<ICurrentUser>());
    }

    private static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        // Distributed cache (in-memory fallback — swap with Redis in production)
        services.AddDistributedMemoryCache();

        // Permission pipeline services
        services
            .AddScoped<IPermissionCacheService, PermissionCacheService>()
            .AddScoped<IUserPermissionResolver, UserPermissionResolver>()
            .AddScoped<IPermissionEnumSeeder, PermissionEnumSeeder>()
            .AddScoped<IPermissionCatalogSynchronizer, PermissionCatalogSynchronizer>()
            .AddScoped<IPermissionDefinitionScanner, PermissionDefinitionScanner>();

        // ASP.NET Core Authorization integration
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}