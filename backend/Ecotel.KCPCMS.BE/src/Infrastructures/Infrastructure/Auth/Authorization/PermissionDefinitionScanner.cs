using System.Reflection;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Infrastructure.Auth.Authorization;

public class PermissionDefinitionScanner(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
    : IPermissionDefinitionScanner
{
    public IReadOnlyCollection<string> ScanEnforcedCodes()
    {
        return actionDescriptorCollectionProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Select(descriptor => descriptor.MethodInfo.GetCustomAttribute<HasPermissionAttribute>()
                ?? descriptor.ControllerTypeInfo.GetCustomAttribute<HasPermissionAttribute>())
            .Where(attribute => attribute is not null)
            .Select(attribute => attribute!.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
