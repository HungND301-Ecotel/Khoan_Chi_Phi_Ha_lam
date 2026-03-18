using System.ComponentModel.DataAnnotations;
using Domain.Common.Contracts;
using Domain.Common.Enums;

namespace Domain.Entities.Identity;

public class Role : AuditableEntity<int>, IAggregateRoot
{
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public RoleType RoleType { get; set; }

    [MaxLength(256)]
    public string NormalizedName { get; set; } = string.Empty;

    private IList<UserRole> _userRoles = new List<UserRole>();
    public virtual IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static Role Create(RoleType roleType, string roleName)
    {
        return new Role
        {
            RoleType = roleType,
            Name = roleName
        };
    }
}