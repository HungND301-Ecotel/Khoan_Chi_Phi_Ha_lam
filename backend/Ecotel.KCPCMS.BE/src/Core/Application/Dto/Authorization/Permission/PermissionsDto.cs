using System;
using System.Collections.Generic;
using Domain.Common.Enums;

namespace Application.Dto.Authorization.Permission;

public class UserPermissionsDto
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string Fullname { get; set; }
    public int? EmployeeId { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public List<string> Permissions { get; set; }
}

public class ModulePermissionDetailDto
{
    public Guid ModuleId { get; set; }
    public string ModuleCode { get; set; }
    public string ModuleName { get; set; }
    public List<SubModulePermissionDetailDto> SubModules { get; set; }
}

public class SubModulePermissionDetailDto
{
    public Guid SubModuleId { get; set; }
    public string SubModuleCode { get; set; }
    public string SubModuleName { get; set; }
    public List<string> Permissions { get; set; }
    public List<Guid> AllowedDepartmentIds { get; set; }
}

public class ComputedSubModulePermissionDto
{
    public Guid SubModuleId { get; set; }
    public string SubModuleCode { get; set; }
    public string SubModuleName { get; set; }
    public Guid ModuleId { get; set; }
    public string ModuleCode { get; set; }
    public string ModuleName { get; set; }
    public List<PermissionCode> Permissions { get; set; }
    public List<Guid> AllowedDepartmentIds { get; set; }
}

public class SubModulePermissionDto
{
    public Guid SubModuleId { get; set; }
    public string SubModuleCode { get; set; }
    public string SubModuleName { get; set; }
    public Guid ModuleId { get; set; }
    public string ModuleCode { get; set; }
    public string ModuleName { get; set; }
    public List<PermissionCode> Permissions { get; set; }
}

public class DepartmentPermissionDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; }
    public List<ModulePermissionDto> Modules { get; set; }
}

public class ModulePermissionDto
{
    public Guid ModuleId { get; set; }
    public string ModuleCode { get; set; }
    public string ModuleName { get; set; }
    public List<PermissionCode> Permissions { get; set; }
}

public class PositionPermissionDto
{
    public int PositionId { get; set; }
    public string PositionName { get; set; }
    public List<SubModulePermissionDto> SubModules { get; set; }
}

public class UserPermissionOverrideDto
{
    public Guid SubModuleId { get; set; }
    public string SubModuleName { get; set; }
    public Guid PermissionId { get; set; }
    public PermissionCode PermissionCode { get; set; }
    public bool IsGranted { get; set; }
    public string? Reason { get; set; }
}

public class UpdateDepartmentPermissionsDto
{
    public Guid DepartmentId { get; set; }
    public List<DepartmentModulePermissionInputDto> Permissions { get; set; } = new();
}

public class DepartmentModulePermissionInputDto
{
    public Guid ModuleId { get; set; }
    public Guid PermissionId { get; set; }
    public bool IsGranted { get; set; } = true;
}

public class UpdatePositionPermissionsDto
{
    public int PositionId { get; set; }
    public List<PositionSubmodulePermissionInputDto> Permissions { get; set; } = new();
}

public class PositionSubmodulePermissionInputDto
{
    public Guid SubModuleId { get; set; }
    public Guid PermissionId { get; set; }
    public bool IsGranted { get; set; } = true;
}

public class UpdateUserOverridePermissionsDto
{
    public int UserId { get; set; }
    public List<UserPermissionOverrideInputDto> Overrides { get; set; } = new();
}

public class UserPermissionOverrideInputDto
{
    public Guid SubModuleId { get; set; }
    public Guid PermissionId { get; set; }
    public bool IsGranted { get; set; }
    public string? Reason { get; set; }
}

public class PermissionCatalogDto
{
    public List<ModuleCatalogDto> Modules { get; set; } = new();
    public List<PermissionItemDto> GlobalPermissions { get; set; } = new();
}

public class ModuleCatalogDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<SubModuleCatalogDto> SubModules { get; set; } = new();
}

public class SubModuleCatalogDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<PermissionCode> AllowedPermissions { get; set; } = new();
}

public class PermissionItemDto
{
    public Guid Id { get; set; }
    public PermissionCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
}

public record PermissionDefinition(
    string ModuleCode,
    string ModuleName,
    string SubModuleCode,
    string SubModuleName,
    string FullCode);