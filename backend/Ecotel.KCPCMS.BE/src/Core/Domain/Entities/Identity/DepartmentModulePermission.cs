using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Identity;

public class DepartmentModulePermission : AuditableEntity<Guid>
{
    public Guid DepartmentId { get; protected set; }
    public Guid ModuleId { get; protected set; }
    public Guid PermissionId { get; protected set; }
    public bool IsGranted { get; protected set; } = true;

    // Navigation Properties
    [ForeignKey("DepartmentId")]
    public virtual Department Department { get; protected set; } = null!;

    [ForeignKey("ModuleId")]
    public virtual Module Module { get; protected set; } = null!;

    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; protected set; } = null!;

    public static DepartmentModulePermission Create(Guid deptId, Guid moduleId, Guid permissionId, bool isGranted)
    {
        return new DepartmentModulePermission
        {
            DepartmentId = deptId,
            ModuleId = moduleId,
            PermissionId = permissionId,
            IsGranted = isGranted
        };
    }
}