using System.Globalization;
using System.Text.RegularExpressions;
using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class AkFactorConfig : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ProcessGroupId { get; protected set; }
    public decimal? MinAkDiff { get; protected set; }
    public decimal? MaxAkDiff { get; protected set; }
    public decimal? MinAdjustmentRate { get; protected set; }
    public decimal? MaxAdjustmentRate { get; protected set; }
    public string? AkDiffDisplay { get; protected set; }
    public string? AdjustmentRateDisplay { get; protected set; }
    public string? Description { get; protected set; }

    public ProcessGroup? ProcessGroup { get; protected set; }

    public static AkFactorConfig Create(
        Guid processGroupId,
        string? akDiffInput,
        string? adjustmentRateInput,
        string? description)
    {
        var (minAkDiff, maxAkDiff) = ParseRangeValue(akDiffInput);
        var (minRate, maxRate) = ParseRangeValue(adjustmentRateInput, isPercent: true);

        return new AkFactorConfig
        {
            ProcessGroupId = processGroupId,
            MinAkDiff = minAkDiff,
            MaxAkDiff = maxAkDiff,
            MinAdjustmentRate = minRate,
            MaxAdjustmentRate = maxRate,
            AkDiffDisplay = akDiffInput?.Trim(),
            AdjustmentRateDisplay = adjustmentRateInput?.Trim(),
            Description = description
        };
    }

    public void Update(Guid processGroupId, string? akDiffInput, string? adjustmentRateInput, string? description)
    {
        var (minAkDiff, maxAkDiff) = ParseRangeValue(akDiffInput);
        var (minRate, maxRate) = ParseRangeValue(adjustmentRateInput, isPercent: true);

        ProcessGroupId = processGroupId;
        MinAkDiff = minAkDiff;
        MaxAkDiff = maxAkDiff;
        MinAdjustmentRate = minRate;
        MaxAdjustmentRate = maxRate;
        AkDiffDisplay = akDiffInput?.Trim();
        AdjustmentRateDisplay = adjustmentRateInput?.Trim();
        Description = description;
    }

    public decimal ResolveRateByAkDiff(decimal akDiff)
    {
        var minMatched = !MinAkDiff.HasValue || akDiff >= MinAkDiff.Value;
        var maxMatched = !MaxAkDiff.HasValue || akDiff <= MaxAkDiff.Value;
        if (!minMatched || !maxMatched)
        {
            return 0;
        }

        if (MinAdjustmentRate.HasValue && MaxAdjustmentRate.HasValue)
        {
            return MinAdjustmentRate == MaxAdjustmentRate ? MinAdjustmentRate.Value : 0;
        }

        return MinAdjustmentRate ?? MaxAdjustmentRate ?? 0;
    }

    private static (decimal? min, decimal? max) ParseRangeValue(string? input, bool isPercent = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (null, null);
        }

        var normalized = input
            .Replace("%", "")
            .Trim();

        var rangeMatch = Regex.Match(normalized, @"^(-?[\d.,]+)\s*[-–]\s*(-?[\d.,]+)$");
        if (rangeMatch.Success)
        {
            var min = ParseLocalizedDecimal(rangeMatch.Groups[1].Value);
            var max = ParseLocalizedDecimal(rangeMatch.Groups[2].Value);
            return isPercent ? (min / 100, max / 100) : (min, max);
        }

        var gteMatch = Regex.Match(normalized, @"^[≥>]=?\s*(-?[\d.,]+)$");
        if (gteMatch.Success)
        {
            var value = ParseLocalizedDecimal(gteMatch.Groups[1].Value);
            return isPercent ? (value / 100, null) : (value, null);
        }

        var lteMatch = Regex.Match(normalized, @"^[≤<]=?\s*(-?[\d.,]+)$");
        if (lteMatch.Success)
        {
            var value = ParseLocalizedDecimal(lteMatch.Groups[1].Value);
            return isPercent ? (null, value / 100) : (null, value);
        }

        try
        {
            var plain = ParseLocalizedDecimal(normalized);
            return isPercent ? (plain / 100, plain / 100) : (plain, plain);
        }
        catch
        {
            // ignore parse error and fallback to null range
        }

        return (null, null);
    }

    private static decimal ParseLocalizedDecimal(string input)
    {
        var value = input.Trim().Replace(" ", "");

        if (value.Contains(',') && value.Contains('.'))
        {
            var lastComma = value.LastIndexOf(',');
            var lastDot = value.LastIndexOf('.');

            if (lastComma > lastDot)
            {
                value = value.Replace(".", string.Empty).Replace(',', '.');
            }
            else
            {
                value = value.Replace(",", string.Empty);
            }
        }
        else if (value.Contains(','))
        {
            value = value.Replace(',', '.');
        }

        return decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
    }
}
