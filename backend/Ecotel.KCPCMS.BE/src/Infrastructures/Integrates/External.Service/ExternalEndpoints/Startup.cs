using Application.Interfaces.Infrastructures.Integrates.Externals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace External.Service.ExternalEndpoints;
public static class Startup
{
    public static void AddExternalEndpoint(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<IExternalEndpointClient, ExternalEndpointClient>();
    }
}
