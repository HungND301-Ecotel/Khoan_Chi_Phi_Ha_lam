using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Cors;

[ExcludeFromCodeCoverage]
public class CorsSettings
{
    public string AllowedOrigins { get; set; } = string.Empty;
}