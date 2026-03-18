using Application.Configurations;
using Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;
using External.Service.AWS;
using External.Service.ExternalEndpoints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace External.Service;

public static class ServiceRegistration
{
    public static IServiceCollection AddExternalServiceInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<AwsS3Configuration>()
            .BindConfiguration($"{nameof(AwsS3Configuration)}")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddCloudServiceInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddExternalEndpoint(config);
        services.AddTransient<IAwsS3Service, AwsS3Service>();
        return services;
    }
}