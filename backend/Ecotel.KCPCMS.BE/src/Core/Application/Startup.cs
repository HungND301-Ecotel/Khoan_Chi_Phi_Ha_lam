using System.Reflection;
using Application.Behaviors;
using Application.Catalog.Index.Metrics;
using Application.Catalog.Permissions;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Services;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class Startup
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<ErrorHelper>();

        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AvatarUrlBehavior<,>));
        services.AddMetricMediarCqrs();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        return services;
    }
}