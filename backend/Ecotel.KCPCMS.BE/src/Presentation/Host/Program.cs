using System.Text.Json.Serialization;
using Application;
using Application.Configurations;
using FluentValidation;
using Host.Configurations;
using Host.Controllers.Base;
using Infrastructure;
using Infrastructure.Common;
using Infrastructure.Common.Extensions;
using Logger.Logging.Serilog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Serilog;

[assembly: ApiConventionType(typeof(ApiConventions))]
StaticLogger.EnsureInitialized();
Log.Information("Server Booting Up...");
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddConfigurations().RegisterSerilog();
    // Configure services
    ConfigureServices(builder);

    var app = builder.Build();
    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

    if (!Directory.Exists(uploadsPath))
    {
        Directory.CreateDirectory(uploadsPath);
    }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads",
        DefaultContentType = "application/octet-stream"
    });

    // Configure SQL Server timestamp behavior
    //AppContext.SetSwitch("SqlServer.EnableLegacyTimestampBehavior", true);
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    // Configure and run application
    await ConfigureApplication(app, builder.Configuration, builder.Environment);
}
catch (Exception ex) when (ex.GetType() != typeof(HostAbortedException))
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Server Shutting down...");
    await Log.CloseAndFlushAsync();
}

// Extract service configuration to a separate method
static void ConfigureServices(WebApplicationBuilder builder)
{
    builder.RegisterSerilog();

    builder.Services.AddControllers(option =>
    {
        option.Conventions.Add(new ControllerNameAttributeConvention());
    }).AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

    // FluentValidation: validation message: properties as camelCase
    ValidatorOptions.Global.PropertyNameResolver = (_, member, _) => member?.Name.ToLowerFirstCharInvariant();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
}

// Extract application configuration to a separate method
static async Task ConfigureApplication(WebApplication app, IConfiguration configuration, IWebHostEnvironment environment)
{
    await app.Services.InitializeDatabasesAsync();
    app.UseInfrastructure(configuration, environment);
    await app.Services.InitializeCacheAsync();
    app.MapEndpoints();
    await app.RunAsync();
}