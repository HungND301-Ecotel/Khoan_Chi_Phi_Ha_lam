namespace Domain.Extensions;

public static class StringExtensions
{
    public static bool NotContains(this string str, string value)
    {
        return !str.Contains(value);
    }
}