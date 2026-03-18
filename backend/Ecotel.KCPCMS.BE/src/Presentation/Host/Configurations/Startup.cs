using System.Diagnostics.CodeAnalysis;
using Application.Configurations;
using Infrastructure.Cors;

namespace Host.Configurations;

[ExcludeFromCodeCoverage]
internal static class Startup
{
    internal static WebApplicationBuilder AddConfigurations(this WebApplicationBuilder builder)
    {
        const string configurationsDirectory = "Configurations";
        var env = builder.Environment;
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/quartz.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

        #region Loading Configurations

        var mailServerConfiguration = new MailServerConfiguration();
        builder.Configuration.Bind("MailServer", mailServerConfiguration);
        builder.Services.AddSingleton(mailServerConfiguration);

        var awsConfiguration = new AwsS3Configuration();
        builder.Configuration.Bind("AwsS3Configuration", awsConfiguration);
        builder.Services.AddSingleton(awsConfiguration);

        var corsConfig = new CorsSettings();
        builder.Configuration.Bind("CorsSettings", corsConfig);
        builder.Services.AddSingleton(corsConfig);

        var quartzConfiguration = new QuartzConfiguration();
        builder.Configuration.Bind("Quartz", quartzConfiguration);
        builder.Services.AddSingleton(quartzConfiguration);

        var excelConfiguration = new ExcelConfiguration();
        builder.Configuration.Bind("ExcelConfiguration", excelConfiguration);
        builder.Services.AddSingleton(excelConfiguration);

        #endregion Loading Configurations

        return builder;
    }
}