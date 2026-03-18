using System.Globalization;
using System.Text;

namespace Infrastructure.Common;
public static class EmailGenerator
{
    private static string RemoveDiacritics(string text)
    {
        text = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string MakePrefix(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        string firstName = parts[^1]; // Tên
        string initials = string.Concat(parts.Take(parts.Length - 1).Select(p => p[0])); // Chữ cái đầu của họ và tên đệm

        string raw = $"{firstName}{initials}".ToLowerInvariant();
        raw = RemoveDiacritics(raw); // Bỏ dấu tiếng Việt
        return new string(raw.Where(char.IsLetterOrDigit).ToArray());
    }

    public static string GetUniqueEmail(string fullName, List<string> existingEmails)
    {
        string prefix = MakePrefix(fullName);
        var prefixes = existingEmails
            .Where(e => e.EndsWith("@dabs.com"))
            .Select(e => e.Substring(0, e.IndexOf("@")));
        int count = prefixes.Count(p => p == prefix || p.StartsWith(prefix));
        string unique = count == 0 ? prefix : $"{prefix}{count}";
        string email = $"{unique}@dabs.com";
        existingEmails.Add(email);
        return email;
    }
}
