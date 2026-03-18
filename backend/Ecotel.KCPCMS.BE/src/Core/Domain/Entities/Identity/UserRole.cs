using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common.Contracts;
using Domain.Common.Enums;

namespace Domain.Entities.Identity;

[Table("UserRole")]
public class UserRole : AuditableEntity<long>
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public RoleType RoleType { get; set; }

    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
}