using System.Reflection;
using Application.Common.Interfaces;
using Application.Dto.Authorization.Permission;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Infrastructure.Auth.Authorization;

public class PermissionDefinitionScanner(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
    : IPermissionDefinitionScanner
{
    public IReadOnlyCollection<PermissionDefinition> Scan()
    {
        var descriptors = actionDescriptorCollectionProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>();

        var result = new List<PermissionDefinition>();

        foreach (var descriptor in descriptors)
        {
            var attribute = descriptor.MethodInfo.GetCustomAttribute<HasPermissionAttribute>()
                ?? descriptor.ControllerTypeInfo.GetCustomAttribute<HasPermissionAttribute>();

            if (attribute is null)
            {
                continue;
            }

            var parts = attribute.Code.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
            {
                continue;
            }

            result.Add(new PermissionDefinition(
                parts[0],
                string.IsNullOrWhiteSpace(attribute.ModuleName) ? parts[0] : attribute.ModuleName,
                parts[1],
                string.IsNullOrWhiteSpace(attribute.SubModuleName) ? parts[1] : attribute.SubModuleName,
                attribute.Code));
        }

        return result
            .GroupBy(x => x.FullCode)
            .Select(g => g.First())
            .ToList();
    }
}