using Application.Dto.Persistence.Catalog.User;

namespace Application.Dto.Persistence.Catalog.UserClaim;

public class UserClaimDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ClaimType { get; set; } = string.Empty;

    public string ClaimValue { get; set; } = string.Empty;

    public UserDto? User { get; set; }
}