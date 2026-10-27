namespace Shared.Authorization;

/// <summary>
/// Tên hiển thị của một module trong <see cref="AppPermissions"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PermissionModuleAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

/// <summary>
/// Tên hiển thị của một resource (SubModule) trong <see cref="AppPermissions"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PermissionResourceAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
