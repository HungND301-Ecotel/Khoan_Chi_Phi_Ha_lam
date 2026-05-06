namespace Application.Catalog.Pricing.LumpSumFinalSettlement;

public static class LumpSumFinalSettlementSpecialQuantityKeys
{
    public const string CoalExcavation = "__SPECIAL_COAL_EXCAVATION__";
    public const string CoalCrosscut = "__SPECIAL_COAL_CROSSCUT__";
    public const string SavingCarryForward = "__SPECIAL_SAVING_CARRY_FORWARD__";

    public static bool IsSpecialQuantityKey(string? value)
    {
        return string.Equals(value, CoalExcavation, StringComparison.Ordinal)
            || string.Equals(value, CoalCrosscut, StringComparison.Ordinal)
            || string.Equals(value, SavingCarryForward, StringComparison.Ordinal);
    }
}
