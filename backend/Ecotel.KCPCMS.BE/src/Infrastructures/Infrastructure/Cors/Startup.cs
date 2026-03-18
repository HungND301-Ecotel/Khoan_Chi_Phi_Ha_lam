using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Cors;

internal static class Startup
{
    private const string CorsPolicy = nameof(CorsPolicy);

    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var corsSettings = config.GetSection(nameof(CorsSettings)).Get<CorsSettings>();
        if (corsSettings == null || string.IsNullOrWhiteSpace(corsSettings.AllowedOrigins))
        {
            return services.AddCors(opt =>
                    opt.AddPolicy(CorsPolicy, policy =>
                        policy.AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .AllowAnyOrigin()));
        }

        string[] origins = corsSettings.AllowedOrigins.Split(";");
        return services.AddCors(x => x.AddPolicy(CorsPolicy, policy =>
                    policy.WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .AllowAnyHeader()));
    }

    internal static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app) =>
        app.UseCors(CorsPolicy);
}