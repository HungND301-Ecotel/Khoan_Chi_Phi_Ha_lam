using System.Globalization;
using System.Text.RegularExpressions;
using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class SavingsRateConfig : AuditableEntity<Guid>, IAggregateRoot
{
    public decimal? MinRevenue { get; protected set; }
    public decimal? MaxRevenue { get; protected set; }
    public decimal? MinSavingsRate { get; protected set; }
    public decimal? MaxSavingsRate { get; protected set; }
    public string? RevenueDisplay { get; protected set; }    // lưu string gốc "≥ 300000"
    public string? SavingsRateDisplay { get; protected set; } // lưu string gốc "≥ 8%"
    public string? Description { get; protected set; }

    public static SavingsRateConfig Create(string? revenueInput, string? savingsRateInput, string? description)
    {
        var (minRevenue, maxRevenue) = ParseRangeValue(revenueInput);
        var (minRate, maxRate) = ParseRangeValue(savingsRateInput, isPercent: true);

        return new SavingsRateConfig
        {
            MinRevenue = minRevenue,
            MaxRevenue = maxRevenue,
            MinSavingsRate = minRate,
            MaxSavingsRate = maxRate,
            RevenueDisplay = revenueInput?.Trim(),
            SavingsRateDisplay = savingsRateInput?.Trim(),
            Description = description
        };
    }

    public void Update(string? revenueInput, string? savingsRateInput, string? description)
    {
        var (minRevenue, maxRevenue) = ParseRangeValue(revenueInput);
        var (minRate, maxRate) = ParseRangeValue(savingsRateInput, isPercent: true);

        MinRevenue = minRevenue;
        MaxRevenue = maxRevenue;
        MinSavingsRate = minRate;
        MaxSavingsRate = maxRate;
        RevenueDisplay = revenueInput?.Trim();
        SavingsRateDisplay = savingsRateInput?.Trim();
        Description = description;
    }

    /// <summary>
    /// Parse các dạng: "≥ 300000", "≤ 500000", "> 100", "< 200", "300000 - 500000", "≥ 8%"
    /// </summary>
    private static (decimal? min, decimal? max) ParseRangeValue(string? input, bool isPercent = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (null, null);
        }

        var normalized = input
            .Replace("%", "")
            .Replace(",", "")
            .Trim();

        // Dạng range: "300000 - 500000"
        var rangeMatch = Regex.Match(normalized, @"^([\d.]+)\s*[-–]\s*([\d.]+)$");
        if (rangeMatch.Success)
        {
            var min = decimal.Parse(rangeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var max = decimal.Parse(rangeMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            return isPercent ? (min / 100, max / 100) : (min, max);
        }

        // Dạng ≥ hoặc >
        var gteMatch = Regex.Match(normalized, @"^[≥>]=?\s*([\d.]+)$");
        if (gteMatch.Success)
        {
            var value = decimal.Parse(gteMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            return isPercent ? (value / 100, null) : (value, null);
        }

        // Dạng ≤ hoặc 
        var lteMatch = Regex.Match(normalized, @"^[≤<]=?\s*([\d.]+)$");
        if (lteMatch.Success)
        {
            var value = decimal.Parse(lteMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            return isPercent ? (null, value / 100) : (null, value);
        }

        // Dạng số thuần
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var plain))
        {
            return isPercent ? (plain / 100, plain / 100) : (plain, plain);
        }

        return (null, null);
    }
}