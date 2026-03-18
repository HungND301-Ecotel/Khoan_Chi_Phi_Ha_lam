using System.Reflection;

namespace Domain.Common.Extensions;

public static class ClassExtensions
{
    public static IList<T> GetAllPropsValue<T>(this Type type)
    {
        var values = type
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi is { IsLiteral: true, IsInitOnly: false })
            .Select(x => (T)x.GetRawConstantValue()!)
            .ToList();
        return values;
    }
}