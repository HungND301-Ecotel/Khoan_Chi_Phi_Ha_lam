using System.Text.RegularExpressions;

namespace Application.Common.Models;

public static class CodeNaturalSortExtensions
{
    public static IOrderedEnumerable<TSource> OrderByCodeNatural<TSource>(this IEnumerable<TSource> source, Func<TSource, string?> keySelector)
    {
        return source.OrderBy(keySelector, CodeNaturalSortComparer.Instance);
    }

    public static IOrderedEnumerable<TSource> ThenByCodeNatural<TSource>(this IOrderedEnumerable<TSource> source, Func<TSource, string?> keySelector)
    {
        return source.ThenBy(keySelector, CodeNaturalSortComparer.Instance);
    }

    private sealed class CodeNaturalSortComparer : IComparer<string?>
    {
        private static readonly Regex TrailingNumberRegex = new(@"^(?<text>.*?)(?<number>\d+)$", RegexOptions.Compiled);

        public static CodeNaturalSortComparer Instance { get; } = new();

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var left = ParseCode(x);
            var right = ParseCode(y);

            var textCompare = string.Compare(left.TextPart, right.TextPart, StringComparison.OrdinalIgnoreCase);
            if (textCompare != 0)
            {
                return textCompare;
            }

            if (left.NumberPart.HasValue && right.NumberPart.HasValue)
            {
                var numberCompare = left.NumberPart.Value.CompareTo(right.NumberPart.Value);
                if (numberCompare != 0)
                {
                    return numberCompare;
                }
            }
            else if (left.NumberPart.HasValue != right.NumberPart.HasValue)
            {
                return left.NumberPart.HasValue ? -1 : 1;
            }

            var originalCompare = string.Compare(left.OriginalValue, right.OriginalValue, StringComparison.OrdinalIgnoreCase);
            if (originalCompare != 0)
            {
                return originalCompare;
            }

            return string.Compare(left.OriginalValue, right.OriginalValue, StringComparison.Ordinal);
        }

        private static ParsedCode ParseCode(string value)
        {
            var trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                return new ParsedCode(string.Empty, null, string.Empty);
            }

            var match = TrailingNumberRegex.Match(trimmed);
            if (!match.Success)
            {
                return new ParsedCode(trimmed, null, trimmed);
            }

            var textPart = match.Groups["text"].Value;
            var numberText = match.Groups["number"].Value;
            var parsed = long.TryParse(numberText, out var numberPart) ? numberPart : (long?)null;

            return new ParsedCode(textPart, parsed, trimmed);
        }

        private readonly record struct ParsedCode(string TextPart, long? NumberPart, string OriginalValue);
    }
}
