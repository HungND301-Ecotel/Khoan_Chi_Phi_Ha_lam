using Application.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Infrastructure.Jobs;

internal static class Startup
{
    internal static IServiceCollection AddQuartz(this IServiceCollection services, IConfiguration configuration)
    {
        var quartzConfig = configuration.GetSection("Quartz").Get<QuartzConfiguration>() ?? new QuartzConfiguration();

        if (quartzConfig.Enabled)
        {
            services.AddQuartz(q =>
            {
                q.UseSimpleTypeLoader();
                q.UseInMemoryStore();
                q.UseDefaultThreadPool(tp =>
                {
                    tp.MaxConcurrency = 10;
                });
            })
            .AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });
        }

        return services;
    }
}