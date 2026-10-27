using System.Reflection;

namespace Shared.Authorization;

public sealed record PermissionDescriptor(
    string ModuleCode,
    string ModuleName,
    string ResourceCode,
    string ResourceName,
    string Action,
    string Code);

/// <summary>
/// Đọc <see cref="AppPermissions"/> một lần và cung cấp danh sách quyền cho seed Module/SubModule và API catalog.
/// </summary>
public static class PermissionRegistry
{
    private static readonly Lazy<IReadOnlyList<PermissionDescriptor>> Definitions = new(Build);

    private static readonly Lazy<HashSet<string>> Codes =
        new(() => All.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<PermissionDescriptor> All => Definitions.Value;

    public static bool Contains(string code) => Codes.Value.Contains(code);

    private static IReadOnlyList<PermissionDescriptor> Build()
    {
        var result = new List<PermissionDescriptor>();

        foreach (var moduleType in typeof(AppPermissions).GetNestedTypes())
        {
            var module = moduleType.GetCustomAttribute<PermissionModuleAttribute>()
                ?? throw new InvalidOperationException($"{moduleType.Name} thiếu [PermissionModule].");

            foreach (var resourceType in moduleType.GetNestedTypes())
            {
                var resource = resourceType.GetCustomAttribute<PermissionResourceAttribute>()
                    ?? throw new InvalidOperationException($"{moduleType.Name}.{resourceType.Name} thiếu [PermissionResource].");

                var fields = resourceType
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f is { IsLiteral: true } && f.FieldType == typeof(string));

                foreach (var field in fields)
                {
                    var code = (string)field.GetRawConstantValue()!;
                    var parts = code.Split('.');

                    if (parts.Length != 3 || !string.Equals(parts[2], field.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Mã quyền '{code}' ({moduleType.Name}.{resourceType.Name}.{field.Name}) phải có dạng module.resource.{field.Name.ToLowerInvariant()}.");
                    }

                    result.Add(new PermissionDescriptor(parts[0], module.Name, parts[1], resource.Name, parts[2], code));
                }
            }

            // Mọi resource trong cùng một class module phải chung mã module, và một mã module chỉ thuộc một class.
            var moduleCodes = result.Where(x => x.ModuleName == module.Name).Select(x => x.ModuleCode).Distinct().ToList();

            if (moduleCodes.Count > 1)
            {
                throw new InvalidOperationException($"{moduleType.Name} chứa nhiều mã module: {string.Join(", ", moduleCodes)}.");
            }
        }

        var splitModule = result
            .GroupBy(x => x.ModuleCode)
            .FirstOrDefault(g => g.Select(x => x.ModuleName).Distinct().Count() > 1);

        if (splitModule is not null)
        {
            throw new InvalidOperationException($"Mã module '{splitModule.Key}' được khai báo ở nhiều class.");
        }

        var splitResource = result
            .GroupBy(x => (x.ModuleCode, x.ResourceCode))
            .FirstOrDefault(g => g.Select(x => x.ResourceName).Distinct().Count() > 1);

        if (splitResource is not null)
        {
            throw new InvalidOperationException(
                $"Resource '{splitResource.Key.ModuleCode}.{splitResource.Key.ResourceCode}' được khai báo ở nhiều class.");
        }

        var duplicate = result.GroupBy(x => x.Code).FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Mã quyền '{duplicate.Key}' bị khai báo trùng.");
        }

        return result;
    }
}
