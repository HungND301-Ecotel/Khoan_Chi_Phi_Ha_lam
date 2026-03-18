using System.Reflection;
using Application.Catalog.Index.Metrics;
using Application.Helpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class Startup
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<ErrorHelper>();

        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(assembly);
        services.AddMetricMediarCqrs();
        return services;
    }
}