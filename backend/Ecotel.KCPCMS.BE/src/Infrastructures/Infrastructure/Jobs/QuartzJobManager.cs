using Application.Configurations;
using Quartz;

namespace Infrastructure.Jobs;

public static class QuartzJobManager
{
    public static IServiceCollectionQuartzConfigurator AddJob<T>(
        this IServiceCollectionQuartzConfigurator quartzConfigurator,
        QuartzSetting configuration) where T : IJob
    {
        if (!configuration.Enabled)
        {
            return quartzConfigurator;
        }

        string jobName = typeof(T).Name;
        var jobKey = new JobKey(jobName);
        string cronExpression = configuration.Scheduled;
        quartzConfigurator.AddJob<T>(opts => opts.WithIdentity(jobKey));

        quartzConfigurator.AddTrigger(t => t
            .ForJob(jobKey)
            .WithIdentity($"{jobName}Trigger")
            .WithCronSchedule(cronExpression)
            .WithDescription($"Trigger for [{jobName}] job"));

        return quartzConfigurator;
    }
}
