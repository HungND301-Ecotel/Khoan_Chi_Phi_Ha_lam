using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Identity;

public class PositionSubmodulePermission : AuditableEntity<Guid>
{
    public int PositionId { get; protected set; }
    public Guid SubModuleId { get; protected set; }
    public Guid PermissionId { get; protected set; }
    public bool IsGranted { get; protected set; } = true;

    // Navigation Properties
    [ForeignKey("PositionId")]
    public virtual Position Position { get; protected set; } = null!;

    [ForeignKey("SubModuleId")]
    public virtual SubModule SubModule { get; protected set; } = null!;

    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; protected set; } = null!;

    public static PositionSubmodulePermission Create(int posId, Guid smId, Guid permId, bool isGranted)
    {
        return new PositionSubmodulePermission
        {
            PositionId = posId,
            SubModuleId = smId,
            PermissionId = permId,
            IsGranted = isGranted,  // Fix: param trước bị typo 'isGraned' và không được gán
        };
    }
}
