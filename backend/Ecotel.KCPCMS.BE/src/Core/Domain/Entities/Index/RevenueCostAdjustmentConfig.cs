using System.Globalization;
using System.Text.RegularExpressions;
using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class RevenueCostAdjustmentConfig : AuditableEntity<Guid>, IAggregateRoot
{
    public string ProfitConditionDisplay { get; protected set; } = default!;
    public decimal? MinProfit { get; protected set; }
    public decimal? MaxProfit { get; protected set; }

    public string RateDisplay { get; protected set; } = default!;
    public decimal Rate { get; protected set; }

    public string? Description { get; protected set; }

    public static RevenueCostAdjustmentConfig Create(
        string profitConditionInput,
        string rateInput,
        string? description = null)
    {
        var (minProfit, maxProfit) = ParseRangeValue(profitConditionInput);
        var rate = ParseRate(rateInput);

        return new RevenueCostAdjustmentConfig
        {
            ProfitConditionDisplay = profitConditionInput.Trim(),
            MinProfit = minProfit,
            MaxProfit = maxProfit,
            RateDisplay = rateInput.Trim(),
            Rate = rate,
            Description = description,
        };
    }

    public void Update(
        string profitConditionInput,
        string rateInput,
        string? description)
    {
        var (minProfit, maxProfit) = ParseRangeValue(profitConditionInput);
        var rate = ParseRate(rateInput);

        ProfitConditionDisplay = profitConditionInput.Trim();
        MinProfit = minProfit;
        MaxProfit = maxProfit;
        RateDisplay = rateInput.Trim();
        Rate = rate;
        Description = description;
    }

    /// <summary>
    /// Kiểm tra config này có áp dụng cho giá trị lợi nhuận đầu vào không.
    /// </summary>
    public bool Matches(decimal profit)
    {
        var aboveMin = MinProfit is null || profit >= MinProfit.Value;
        var belowMax = MaxProfit is null || profit <= MaxProfit.Value;
        return aboveMin && belowMax;
    }

    /// <summary>
    /// Tính delta thu nhập theo config này.
    /// </summary>
    public decimal CalcIncomeDelta(decimal savingsOrOverrun)
        => Rate * savingsOrOverrun;

    /// <summary>
    /// Parse: "≥ 0", "≤ 0", "> 100000", "< 0", "0 - 500000"
    /// </summary>
    private static (decimal? min, decimal? max) ParseRangeValue(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (null, null);
        }

        var normalized = input.Replace(",", "").Trim();

        // Range: "100000 - 500000"
        var rangeMatch = Regex.Match(normalized, @"^(-?[\d.]+)\s*[-–]\s*(-?[\d.]+)$");
        if (rangeMatch.Success)
        {
            var min = decimal.Parse(rangeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var max = decimal.Parse(rangeMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            return (min, max);
        }

        // ≥ hoặc >
        var gteMatch = Regex.Match(normalized, @"^[≥>]=?\s*(-?[\d.]+)$");
        if (gteMatch.Success)
        {
            var value = decimal.Parse(gteMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            return (value, null);
        }

        // ≤ hoặc 
        var lteMatch = Regex.Match(normalized, @"^[≤<]=?\s*(-?[\d.]+)$");
        if (lteMatch.Success)
        {
            var value = decimal.Parse(lteMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            return (null, value);
        }

        // Số thuần
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var plain))
        {
            return (plain, plain);
        }

        return (null, null);
    }

    /// <summary>
    /// Parse tỷ lệ: "60%", "-100%", "0.6", "-1"
    /// </summary>
    private static decimal ParseRate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return 0;
        }

        var normalized = input.Trim();
        var isPercent = normalized.EndsWith("%");
        var cleaned = normalized.Replace("%", "").Trim();

        return !decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? 0
            : isPercent ? value / 100 : value;
    }
}