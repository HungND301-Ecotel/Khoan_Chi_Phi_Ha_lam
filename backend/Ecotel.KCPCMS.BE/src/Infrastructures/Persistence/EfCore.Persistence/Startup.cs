using Application.Common;
using Application.Common.Persistence;
using EfCore.Persistence.ConnectionString;
using EfCore.Persistence.Context;
using EfCore.Persistence.Initialization;
using EfCore.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Serilog;
using Shared.Constants;

#pragma warning disable CA1308

namespace EfCore.Persistence;

public static class Startup
{
    private static readonly ILogger Logger = Log.ForContext(typeof(Startup));

    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddOptions<DatabaseSettings>()
            .BindConfiguration(nameof(DatabaseSettings))
            .PostConfigure(databaseSettings =>
            {
                Logger.Information("Current DB Provider: {DbProvider}", databaseSettings.DbProvider);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services
            .AddDbContext<ApplicationDbContext>((p, m) =>
            {
                var databaseSettings = p.GetRequiredService<IOptions<DatabaseSettings>>().Value;
                m.UseDatabase(databaseSettings.DbProvider, databaseSettings.ConnectionString);
            })

            .AddTransient<IDatabaseInitializer, DatabaseInitializer>()
            .AddTransient<ApplicationDbInitializer>()
            .AddTransient<ApplicationDbSeeder>()
            .AddServices(typeof(ICustomSeeder), ServiceLifetime.Transient)
            .AddTransient<CustomSeederRunner>()

            .AddTransient<IConnectionStringSecurer, ConnectionStringSecurer>()

            .AddRepositories();
    }

    public static DbContextOptionsBuilder UseDatabase(this DbContextOptionsBuilder builder, string dbProvider, string connectionString)
    {
        return dbProvider.ToLowerInvariant() switch
        {
            DbProviderKeys.PostgreSql => builder.UseNpgsql(connectionString, e =>
                e.MigrationsAssembly("Migrators.PostgreSQL")),

            DbProviderKeys.SqlServer => builder.UseSqlServer(connectionString, e =>
                e.MigrationsAssembly("Migrators.MSSQL")),

            DbProviderKeys.MySql => builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), e =>
                                 e.MigrationsAssembly("Migrators.MySQL").SchemaBehavior(MySqlSchemaBehavior.Ignore)),
            _ => throw new InvalidOperationException($"DB Provider {dbProvider} is not supported.")
        };
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IReadRepository<>), typeof(ApplicationDbRepository<>));

        return services;
    }
}