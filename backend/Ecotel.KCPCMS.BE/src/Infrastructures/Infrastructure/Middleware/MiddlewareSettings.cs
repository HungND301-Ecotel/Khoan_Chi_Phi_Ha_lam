using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Middleware;

[ExcludeFromCodeCoverage]
public class MiddlewareSettings
{
    public bool EnableHttpsLogging { get; set; }
    public bool EnableLocalization { get; set; }
}