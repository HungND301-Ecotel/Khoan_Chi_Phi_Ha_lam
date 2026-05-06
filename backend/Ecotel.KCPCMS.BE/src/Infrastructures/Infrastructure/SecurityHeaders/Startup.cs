using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.SecurityHeaders;

internal static class Startup
{
    internal static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IConfiguration config)
    {
        var settings = config.GetSection(nameof(SecurityHeaderSettings)).Get<SecurityHeaderSettings>();

        if (settings?.Enable is true)
        {
            app.Use(async (context, next) =>
                {
                    if (!context.Response.HasStarted)
                    {
                        if (!string.IsNullOrWhiteSpace(settings.Headers.XFrameOptions))
                        {
                            context.Response.Headers.TryAdd(HeaderNames.Xframeoptions, settings.Headers.XFrameOptions);
                        }

                        if (!string.IsNullOrWhiteSpace(settings.Headers.XContentTypeOptions))
                        {
                            context.Response.Headers.TryAdd(HeaderNames.Xcontenttypeoptions, settings.Headers.XContentTypeOptions);
                        }

                        if (!string.IsNullOrWhiteSpace(settings.Headers.ReferrerPolicy))
                        {
                            context.Response.Headers.TryAdd(HeaderNames.Referrerpolicy, settings.Headers.ReferrerPolicy);
                        }

                        if (!string.IsNullOrWhiteSpace(settings.Headers.PermissionsPolicy))
                        {
                            context.Response.Headers.TryAdd(HeaderNames.Permissionspolicy, settings.Headers.PermissionsPolicy);
                        }

                        if (!string.IsNullOrWhiteSpace(settings.Headers.SameSite))
                        {
                            context.Response.Headers.TryAdd(HeaderNames.Samesite, settings.Headers.SameSite);
                        }

                        if (!string.IsNullOrWhiteSpace(settings.Headers.XxssProtection))
                        {
                            context.Response.Headers.TryAdd(HeaderNames.Xxssprotection, settings.Headers.XxssProtection);
                        }
                    }

                    await next();
                });
        }

        return app;
    }
}