using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;
using Domain.Common.Enums;
using Microsoft.Graph.Models;

namespace Domain.Entities.Identity;

public class Permission : AuditableEntity<Guid>, IAggregateRoot
{
    public PermissionCode Code { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }

    //Constructor
    public static Permission Create(PermissionCode code, string name, string? description)
    {
        return new Permission
        {
            Code = code,
            Name = name,
            Description = description
        };
    }
}
