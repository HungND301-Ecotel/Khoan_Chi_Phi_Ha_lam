using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Auth.Jwt;

public class JwtSettings : IValidatableObject
{
    public string Key { get; set; } = string.Empty;

    public int TokenExpirationInMinutes { get; set; }

    public int RefreshTokenExpirationInDays { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(Key ?? string.Empty) < 32)
        {
            yield return new ValidationResult("JWT_SECRET_KEY must contain at least 32 UTF-8 bytes", [nameof(Key)]);
        }

        if (TokenExpirationInMinutes is < 15 or > 30)
        {
            yield return new ValidationResult("Access token lifetime must be 15 to 30 minutes", [nameof(TokenExpirationInMinutes)]);
        }

        if (RefreshTokenExpirationInDays < 1)
        {
            yield return new ValidationResult("Refresh token lifetime must be at least one day", [nameof(RefreshTokenExpirationInDays)]);
        }
    }
}
