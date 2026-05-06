using System.Globalization;
using System.Text.RegularExpressions;
using Domain.Common.Contracts;
using Domain.Common.Enums;

namespace Domain.Entities.Index;

public class AkFactorConfig : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ProcessGroupId { get; protected set; }
    public string? AkDiffOperator { get; protected set; }
    public decimal? AkDiffValue { get; protected set; }
    public decimal? AdjustmentRate { get; protected set; }
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
        var (akDiffOperator, akDiffValue) = ParseAkDiffCondition(akDiffInput);
        var adjustmentRate = ParseAdjustmentRate(adjustmentRateInput);

        return new AkFactorConfig
        {
            ProcessGroupId = processGroupId,
            AkDiffOperator = akDiffOperator,
            AkDiffValue = akDiffValue,
            AdjustmentRate = adjustmentRate,
            AkDiffDisplay = akDiffInput?.Trim(),
            AdjustmentRateDisplay = adjustmentRateInput?.Trim(),
            Description = description
        };
    }

    public void Update(Guid processGroupId, string? akDiffInput, string? adjustmentRateInput, string? description)
    {
        var (akDiffOperator, akDiffValue) = ParseAkDiffCondition(akDiffInput);
        var adjustmentRate = ParseAdjustmentRate(adjustmentRateInput);

        ProcessGroupId = processGroupId;
        AkDiffOperator = akDiffOperator;
        AkDiffValue = akDiffValue;
        AdjustmentRate = adjustmentRate;
        AkDiffDisplay = akDiffInput?.Trim();
        AdjustmentRateDisplay = adjustmentRateInput?.Trim();
        Description = description;
    }

    public bool IsMatch(decimal akDiff)
    {
        if (string.IsNullOrWhiteSpace(AkDiffOperator) || !AkDiffValue.HasValue)
        {
            return false;
        }

        return AkDiffOperator switch
        {
            ">" => akDiff > AkDiffValue.Value,
            ">=" => akDiff >= AkDiffValue.Value,
            "<" => akDiff < AkDiffValue.Value,
            "<=" => akDiff <= AkDiffValue.Value,
            "=" => akDiff == AkDiffValue.Value,
            _ => false
        };
    }

    public decimal ResolveRateByAkDiff(decimal akDiff)
    {
        return IsMatch(akDiff) ? AdjustmentRate ?? 0 : 0;
    }

    public static bool HasValidAkDiffCondition(string? input)
    {
        var (akDiffOperator, akDiffValue) = ParseAkDiffCondition(input);
        return !string.IsNullOrWhiteSpace(akDiffOperator) && akDiffValue.HasValue;
    }

    public static bool HasValidAdjustmentRate(string? input)
    {
        return ParseAdjustmentRate(input).HasValue;
    }

    public static bool SupportsProcessGroupType(FixedKeyType processGroupType)
    {
        return processGroupType != FixedKeyType.None;
    }

    public static decimal ResolveRate(IEnumerable<AkFactorConfig> configs, decimal akDiff)
    {
        return configs
            .Where(config => config.IsMatch(akDiff))
            .OrderBy(config => GetOperatorPriority(config.AkDiffOperator))
            .ThenByDescending(config => IsGreaterOperator(config.AkDiffOperator))
            .ThenByDescending(config => IsGreaterOperator(config.AkDiffOperator) ? config.AkDiffValue : null)
            .ThenBy(config => IsLowerOperator(config.AkDiffOperator) ? config.AkDiffValue : null)
            .Select(config => config.AdjustmentRate ?? 0)
            .FirstOrDefault();
    }

    public static int GetOperatorPriority(string? akDiffOperator)
    {
        return akDiffOperator switch
        {
            "=" => 0,
            ">" => 1,
            ">=" => 2,
            "<" => 3,
            "<=" => 4,
            _ => 5
        };
    }

    private static bool IsGreaterOperator(string? akDiffOperator)
    {
        return akDiffOperator is ">" or ">=";
    }

    private static bool IsLowerOperator(string? akDiffOperator)
    {
        return akDiffOperator is "<" or "<=";
    }

    private static (string? akDiffOperator, decimal? akDiffValue) ParseAkDiffCondition(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (null, null);
        }

        var normalized = input.Trim();
        var conditionMatch = Regex.Match(normalized, @"^(?<operator>>=|<=|>|<|=|≥|≤)\s*(?<value>-?[\d.,]+)$");
        if (conditionMatch.Success)
        {
            var conditionOperator = NormalizeOperator(conditionMatch.Groups["operator"].Value);
            var conditionValue = ParseLocalizedDecimal(conditionMatch.Groups["value"].Value);
            return (conditionOperator, conditionValue);
        }

        try
        {
            var exactValue = ParseLocalizedDecimal(normalized);
            return ("=", exactValue);
        }
        catch
        {
            // ignore parse error and fallback to null condition
        }

        return (null, null);
    }

    private static decimal? ParseAdjustmentRate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var normalized = input.Replace("%", "").Trim();

        try
        {
            return ParseLocalizedDecimal(normalized) / 100;
        }
        catch
        {
            // ignore parse error and fallback to null rate
        }

        return null;
    }

    private static string NormalizeOperator(string value)
    {
        return value switch
        {
            "≥" => ">=",
            "≤" => "<=",
            _ => value
        };
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
