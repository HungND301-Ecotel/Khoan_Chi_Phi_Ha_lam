using Domain.Common.Contracts;

namespace Domain.Entities.Identity;

public class UserClaim : AuditableEntity<int>, IAggregateRoot
{
    public int UserId { get; set; }

    public string ClaimType { get; set; } = string.Empty;

    public string ClaimValue { get; set; } = string.Empty;

    public virtual User? User { get; set; }
}