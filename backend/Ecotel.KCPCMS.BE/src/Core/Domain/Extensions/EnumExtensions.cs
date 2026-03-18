using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Domain.Extensions;

public static class EnumExtensions
{
    public static TEnum ParseDictionaryToFlag<TEnum>(Dictionary<string, bool>? action, TEnum noneFlag)
        where TEnum : struct
    {
        return action!.Aggregate(noneFlag, (acc, curr) =>
        {
            if (curr.Value && Enum.TryParse(curr.Key, out TEnum funcAction))
            {
                return (TEnum)(object)((int)(object)acc | (int)(object)funcAction);
            }

            return acc;
        });
    }

    public static string GetDescription(this Enum enumValue)
    {
        object[] attr = enumValue.GetType().GetField(enumValue.ToString())!
            .GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attr.Length > 0)
        {
            return ((DescriptionAttribute)attr[0]).Description;
        }

        string result = enumValue.ToString();
        result = Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");
        result = Regex.Replace(result, "([A-Za-z])([0-9])", "$1 $2");
        result = Regex.Replace(result, "([0-9])([A-Za-z])", "$1 $2");
        result = Regex.Replace(result, "(?<!^)(?<! )([A-Z][a-z])", " $1");
        return result;
    }

    public static string DayOfWeekToVietnamese(this DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Thứ Hai",
            DayOfWeek.Tuesday => "Thứ Ba",
            DayOfWeek.Wednesday => "Thứ Tư",
            DayOfWeek.Thursday => "Thứ Năm",
            DayOfWeek.Friday => "Thứ Sáu",
            DayOfWeek.Saturday => "Thứ Bảy",
            DayOfWeek.Sunday => "Chủ Nhật",
            _ => "Không xác định"
        };
    }

    public static string GetDisplayName(this Enum enumValue)
    {
        return enumValue.GetType()
                        .GetField(enumValue.ToString())
                        ?.GetCustomAttribute<DisplayAttribute>()
                        ?.Name ?? enumValue.ToString();
    }

    public static T? ParseFromDisplayName<T>(this string displayName) where T : struct, Enum
    {
        foreach (var value in Enum.GetValues(typeof(T)).Cast<T>())
        {
            if (value.GetDisplayName().Equals(displayName?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }
        return null;
    }
}