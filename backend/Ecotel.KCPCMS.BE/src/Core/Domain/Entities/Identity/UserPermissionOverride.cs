using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;

namespace Domain.Entities.Identity;

public class UserPermissionOverride : AuditableEntity<Guid>
{
    public int UserId { get; set; }
    public Guid SubModuleId { get; set; }
    public Guid PermissionId { get; set; }
    public bool IsGranted { get; set; }
    public string? Reason { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("SubModuleId")]
    public virtual SubModule SubModule { get; set; } = null!;

    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; set; } = null!;

    public static UserPermissionOverride Create(int userId, Guid subModuleId, Guid permissionId, bool isGranted, string reason)
    {
        return new UserPermissionOverride
        {
            UserId = userId,
            SubModuleId = subModuleId,
            PermissionId = permissionId,
            IsGranted = isGranted,
            Reason = reason
        };
    }
}
