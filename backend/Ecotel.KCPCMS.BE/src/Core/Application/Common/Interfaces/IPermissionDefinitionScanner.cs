using Application.Dto.Authorization.Permission;

namespace Application.Common.Interfaces;

public interface IPermissionDefinitionScanner
{
    IReadOnlyCollection<PermissionDefinition> Scan();
}
