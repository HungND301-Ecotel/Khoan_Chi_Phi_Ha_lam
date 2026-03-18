namespace Infrastructure.Common.Extensions;

public static class StringExtensions
{
    public static string ToLowerFirstCharInvariant(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        char[] chars = input.ToCharArray();
        chars[0] = char.ToLowerInvariant(chars[0]);
        return new string(chars);
    }
}