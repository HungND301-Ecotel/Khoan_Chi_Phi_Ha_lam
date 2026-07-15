using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;

namespace Domain.Entities.Identity;

public class SubModule : AuditableEntity<Guid>
{
    public Guid ModuleId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Code { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public int SortOrder { get; protected set; } = 0;

    // Navigation Properties
    [ForeignKey("ModuleId")]
    public virtual Module Module { get; protected set; } = null!;

    public static SubModule Create(Guid moduleId, string name, string code, string description, int sortOrder)
    {
        return new SubModule
        {
            ModuleId = moduleId,
            Name = name,
            Code = code,
            Description = description,
            SortOrder = sortOrder
        };
    }
}
