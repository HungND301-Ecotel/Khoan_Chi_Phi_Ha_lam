using Application.Common.Exceptions;
using Domain.Common.Enums;
using Domain.Entities.MasterData;

namespace Application.Catalog.MasterData.FixedKeys;

internal static class FixedKeyCodeMapper
{
    public static ProcessGroupType ToProcessGroupType(FixedKey fixedKey)
        => ToEnum<ProcessGroupType>(fixedKey, FixedKeyType.ProcessGroup);

    public static ProcessGroupType ResolveProcessGroupType(FixedKey? fixedKey)
        => fixedKey == null ? ProcessGroupType.None : ToProcessGroupType(fixedKey);

    public static MaterialsIncludedInContractRevenue ToMaterialsIncludedInContractRevenue(FixedKey fixedKey)
        => ToEnum<MaterialsIncludedInContractRevenue>(fixedKey, FixedKeyType.MaterialsIncludedInContractRevenue);

    public static MaterialsIncludedInContractRevenue ResolveMaterialsIncludedInContractRevenue(FixedKey? fixedKey)
        => fixedKey == null ? MaterialsIncludedInContractRevenue.None : ToMaterialsIncludedInContractRevenue(fixedKey);

    public static AdditionalCost ToAdditionalCost(FixedKey fixedKey)
        => ToEnum<AdditionalCost>(fixedKey, FixedKeyType.AdditionalCost);

    public static AdditionalCost ResolveAdditionalCost(FixedKey? fixedKey)
        => fixedKey == null ? AdditionalCost.None : ToAdditionalCost(fixedKey);

    public static OtherMaterialDetail ToOtherMaterialDetail(FixedKey fixedKey)
        => ToEnum<OtherMaterialDetail>(fixedKey, FixedKeyType.OtherMaterialDetail);

    public static OtherMaterialDetail ResolveOtherMaterialDetail(FixedKey? fixedKey)
        => fixedKey == null ? OtherMaterialDetail.None : ToOtherMaterialDetail(fixedKey);

    public static QuotaBasedMaterial ToQuotaBasedMaterial(FixedKey fixedKey)
        => ToEnum<QuotaBasedMaterial>(fixedKey, FixedKeyType.QuotaBasedMaterial);

    public static QuotaBasedMaterial ResolveQuotaBasedMaterial(FixedKey? fixedKey)
        => fixedKey == null ? QuotaBasedMaterial.None : ToQuotaBasedMaterial(fixedKey);

    public static QuotaBasedMaterialType ToQuotaBasedMaterialType(FixedKey fixedKey)
        => ToEnum<QuotaBasedMaterialType>(fixedKey, FixedKeyType.QuotaBasedMaterialType);

    public static QuotaBasedMaterialType ResolveQuotaBasedMaterialType(FixedKey? fixedKey)
        => fixedKey == null
            ? throw new BadRequestException("Fixed key is required for quota-based material type.")
            : ToQuotaBasedMaterialType(fixedKey);

    public static Asset ToAsset(FixedKey fixedKey)
        => ToEnum<Asset>(fixedKey, FixedKeyType.Asset);

    public static Asset ResolveAsset(FixedKey? fixedKey)
        => fixedKey == null ? Asset.None : ToAsset(fixedKey);

    public static IssuedQuantityType ToIssuedQuantityType(FixedKey fixedKey)
        => ToEnum<IssuedQuantityType>(fixedKey, FixedKeyType.IssuedQuantityType);

    public static IssuedQuantityType ResolveIssuedQuantityType(FixedKey? fixedKey)
        => fixedKey == null
            ? throw new BadRequestException("Fixed key is required for issued quantity type.")
            : ToIssuedQuantityType(fixedKey);

    public static ShippedQuantityType ToShippedQuantityType(FixedKey fixedKey)
        => ToEnum<ShippedQuantityType>(fixedKey, FixedKeyType.ShippedQuantityType);

    public static ShippedQuantityType ResolveShippedQuantityType(FixedKey? fixedKey)
        => fixedKey == null
            ? throw new BadRequestException("Fixed key is required for shipped quantity type.")
            : ToShippedQuantityType(fixedKey);

    private static TEnum ToEnum<TEnum>(FixedKey fixedKey, FixedKeyType expectedType)
        where TEnum : struct, Enum
    {
        if (fixedKey.Type != expectedType)
        {
            throw new BadRequestException($"Fixed key '{fixedKey.Id}' must have type '{expectedType}'.");
        }

        var normalizedCode = Normalize(fixedKey.Code);
        var match = Enum.GetValues<TEnum>()
            .Select(item => new { Value = item, Key = Normalize(item.ToString()) })
            .FirstOrDefault(item => item.Key == normalizedCode);

        if (match == null)
        {
            throw new BadRequestException(
                $"Fixed key code '{fixedKey.Code}' is not mapped to enum '{typeof(TEnum).Name}'.");
        }

        return match.Value;
    }

    private static string Normalize(string value)
        => new((value ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
}