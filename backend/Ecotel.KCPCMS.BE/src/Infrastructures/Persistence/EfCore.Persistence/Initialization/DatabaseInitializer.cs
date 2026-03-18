using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EfCore.Persistence.Initialization;

internal class DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
    : IDatabaseInitializer
{
    public async Task InitializeDatabasesAsync(CancellationToken cancellationToken)
    {
        // First create a new scope
        using var scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbInitializer>()
            .InitializeAsync(cancellationToken);

        logger.LogInformation("INITIALIZE_DATABASES_ASYNC DONE");
    }
}