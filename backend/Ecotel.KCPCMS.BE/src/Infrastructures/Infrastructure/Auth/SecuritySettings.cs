using Infrastructure.Auth.Jwt;

namespace Infrastructure.Auth;

public class SecuritySettings
{
    public string? Provider { get; set; }
    public bool RequireConfirmedAccount { get; set; }

    public JwtSettings JwtSettings { get; set; } = new();
}