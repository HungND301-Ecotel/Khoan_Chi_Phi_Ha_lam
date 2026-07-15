using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;

namespace Domain.Entities.Identity;

public class Module : AuditableEntity<Guid>
{
    public string Name { get; protected set; } = string.Empty;
    public string Code { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public int SortOrder { get; protected set; } = 0;

    // Navigation Properties
    private IList<SubModule> _subModules = new List<SubModule>();
    public virtual IReadOnlyCollection<SubModule> SubModules => _subModules.AsReadOnly();

    //Constructor
    public static Module Create(string name, string code, string? description, int sortorder)
    {
        return new Module
        {
            Name = name,
            Code = code,
            Description = description,
            SortOrder = sortorder
        };
    }
}
