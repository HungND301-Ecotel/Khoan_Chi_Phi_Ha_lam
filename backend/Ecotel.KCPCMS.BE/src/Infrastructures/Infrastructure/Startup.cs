using System.Reflection;
using System.Runtime.CompilerServices;
using Application.Common;
using Application.Configurations;
using Application.Interfaces;
using EfCore.Persistence;
using EfCore.Persistence.Context;
using EfCore.Persistence.Initialization;
using External.Service;
using Infrastructure.Auth;
using Infrastructure.Caching;
using Infrastructure.Cors;
using Infrastructure.Jobs;
using Infrastructure.Localization;
using Infrastructure.Middleware;
using Infrastructure.OpenApi;
using Infrastructure.SecurityHeaders;
using Infrastructure.Services;
using Infrastructure.UnitOfWork;
using Infrastructure.Validations;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Infrastructure.Test")]

namespace Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        return services
            .AddApiVersioning(config)
            .AddSettings()
            .AddAuth(config)
            .AddCaching(config)
            .AddUnitOfWork<ApplicationDbContext>()
            .AddCustomRepository()
            .AddCorsPolicy(config)
            .AddExceptionMiddleware()
            .AddBehaviours()
            .AddHealthCheck()
            .AddPoLocalization(config)
            .AddMediatR(Assembly.GetExecutingAssembly())
            .AddOpenApiDocumentation(config)
            .AddPersistence()
            .AddRequestLogging(config)
            .AddRouting(options => options.LowercaseUrls = true)
            .AddExternalServiceInfrastructure(config)
            .AddCloudServiceInfrastructure(config)
            .AddServices()
            .AddRegisterService()
            .AddQuartz(config);
    }

    private static IServiceCollection AddApiVersioning(this IServiceCollection services, IConfiguration cfg) =>
        services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(int.Parse(cfg["SwaggerSettings:Versions:MajorApiVersion"] ?? "1"), int.Parse(cfg["SwaggerSettings:Versions:MinorApiVersion"] ?? "0"));
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
        });

    private static IServiceCollection AddSettings(this IServiceCollection services)
    {
        services.AddOptions<MailServerConfiguration>()
            .BindConfiguration($"{nameof(MailServerConfiguration)}");

        services.AddOptions<AppSettings>()
            .BindConfiguration($"{nameof(AppSettings)}");

        services.AddOptions<VerificationSettings>()
            .BindConfiguration($"{nameof(VerificationSettings)}");

        return services;
    }

    private static IServiceCollection AddHealthCheck(this IServiceCollection services) =>
        services.AddHealthChecks().Services;

    public static async Task InitializeDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // Create a new scope to retrieve scoped services
        using var scope = services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>()
            .InitializeDatabasesAsync(cancellationToken);
    }

    public static async Task InitializeCacheAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IInitializeCacheService>().InitializeCacheAsync();
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder builder, IConfiguration config, IWebHostEnvironment environment) =>
        builder
            .UseRequestLocalization()
            .UseStaticFiles()
            .UseSecurityHeaders(config)
            .UseExceptionMiddleware()
            .UseRouting()
            .UseCorsPolicy()
            .UseAuthentication()
            .UseCurrentUser()
            .UseAuthorization()
            .UseRequestLogging(config)
            .UseOpenApiDocumentation(config, environment);

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers();
        builder.MapHealthCheck();
        return builder;
    }

    private static IEndpointConventionBuilder MapHealthCheck(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHealthChecks("/api/health");
}