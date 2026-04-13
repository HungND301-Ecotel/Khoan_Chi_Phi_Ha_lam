namespace Application.Catalog.Pricing.LumpSumFinalSettlement;

public static class LumpSumFinalSettlementSpecialQuantityKeys
{
    public const string CoalExcavation = "__SPECIAL_COAL_EXCAVATION__";
    public const string CoalCrosscut = "__SPECIAL_COAL_CROSSCUT__";

    public static bool IsSpecialQuantityKey(string? value)
    {
        return string.Equals(value, CoalExcavation, StringComparison.Ordinal)
            || string.Equals(value, CoalCrosscut, StringComparison.Ordinal);
    }
}
