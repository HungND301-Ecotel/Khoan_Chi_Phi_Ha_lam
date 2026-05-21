using System.ComponentModel.DataAnnotations;

namespace Domain.Common.Enums;

public enum RoleType
{
    SystemAdmin = 1,
    User = 2,
}

public enum AdjustmentFactorType
{
    None = 0,
    K1 = 1,
    K2 = 2,
    K3 = 3,
    K4 = 4,
    K5 = 5,
    K6 = 6,
    K7 = 7,
    K8 = 8,
}

public enum FixedKeyType
{
    None = 0,
    DL = 1,
    LC = 2,
    XL = 3,
    K1 = 4,
    K2 = 5,
    K3 = 6,
    K4 = 7,
    K5 = 8,
    K6 = 9,
    K7 = 10,
    K8 = 11
}

public static class FixedKeyTypeExtensions
{
    public static ProcessGroupType ToProcessGroupType(this FixedKeyType type)
    {
        return type switch
        {
            FixedKeyType.DL => ProcessGroupType.DL,
            FixedKeyType.LC => ProcessGroupType.LC,
            FixedKeyType.XL => ProcessGroupType.XL,
            _ => ProcessGroupType.None,
        };
    }

    public static AdjustmentFactorType ToAdjustmentFactorType(this FixedKeyType type)
    {
        return type switch
        {
            FixedKeyType.K1 => AdjustmentFactorType.K1,
            FixedKeyType.K2 => AdjustmentFactorType.K2,
            FixedKeyType.K3 => AdjustmentFactorType.K3,
            FixedKeyType.K4 => AdjustmentFactorType.K4,
            FixedKeyType.K5 => AdjustmentFactorType.K5,
            FixedKeyType.K6 => AdjustmentFactorType.K6,
            FixedKeyType.K7 => AdjustmentFactorType.K7,
            FixedKeyType.K8 => AdjustmentFactorType.K8,
            _ => AdjustmentFactorType.None,
        };
    }

    public static FixedKeyType ToFixedKeyType(this ProcessGroupType type)
    {
        return type switch
        {
            ProcessGroupType.DL => FixedKeyType.DL,
            ProcessGroupType.LC => FixedKeyType.LC,
            ProcessGroupType.XL => FixedKeyType.XL,
            _ => FixedKeyType.None,
        };
    }
}

public enum ProcessGroupType
{
    None = 0,
    DL = 1,
    LC = 2,
    XL = 3
}

public enum CostType
{
    Part = 1,
    Electricity = 2,
    Material = 3,
}
public enum ExecutionType
{
    Maintain = 1,
    Electricity = 2,
    Material = 3
}

public enum PlanCostType
{
    PlanCost = 1,
    AdjustmentPlanCost = 2
}

public enum MaintainUnitPriceType
{
    TunnelExcavation = 1,
    Longwall = 2,
    Trimming = 3
}

public enum LowValuePerishableSupplyType
{
    TunnelExcavation = 1,
    Longwall = 2,
}

public enum OutputType
{
    PlanOutput = 1,
    ActualOutput = 2,
}

public enum ProductUnitPriceScenarioType
{
    Plan = 1,
    Adjustment = 2,
}

public enum MaterialType
{
    [Display(Name = "Vật tư, tài sản")]
    MaterialInContract = 1,

    [Display(Name = "Vật tư, tài sản khác")]
    MaterialOutContract = 2,
}

public enum MaterialUnitPriceType
{
    TunnelExcavation = 1,  // Đào lò
    Longwall = 2,          // Lò chợ
    TunnelSupportAndDrilling = 3,
    Trimming = 4
}


public enum ElectricityUnitPriceType
{
    TunnelExcavation = 1,  // Đào lò
    Longwall = 2,           // Lò chợ
    Trimming = 3           // Xén lò
}

public enum MaterialsIncludedInContractRevenue
{
    None = 1,
    Material = 2,
    Maintain = 3
}

public enum AdditionalCost
{
    None = 1,
    Material = 2,
    Maintain = 3,
    SafeAndWelfare = 4
}

public enum OtherMaterialDetail
{
    None = 1,
    BaoHoLaoDong = 2,
    VatTuPhucVuCongTacAnToan = 3,
}

public enum QuotaBasedMaterial
{
    None = 1,
    MineSupport = 2,
    SupportAccessories = 3,
    MineTimber = 4,
}

public enum QuotaBasedMaterialType
{
    New = 1,
    Reusable = 2
}

public enum LowValuePerishableSupplyInclusion
{
    Exclude = 1,
    Include = 2
}

public enum Asset
{
    None = 1,
    True = 2
}

public enum AcceptanceReportItemType
{
    Material = 1,
    Part = 2
}

public enum ItemType
{
    InContract = 1,
    OutContract = 2,
    SafetyAndWelfare = 3,
    Resource = 4,
    QuotaMaterials = 5
}

public enum PartType
{
    Part = 1,
    OtherPart = 2
}

public enum SteelMeshType
{
    None = 1,
    SingleLayerSteelMesh = 2,
    DoubleLayerSteelMesh = 3
}

public enum IssuedQuantityType
{
    LinhVatTuTraPhieu = 1,
    VayVhuaTraPhieu = 2,
    TraPhieuThangTruoc = 3,
    LinhKhac = 4
}

public enum ShippedQuantityType
{
    XuatChoSanXuat = 1,
    XuatKhac = 2,
    QuyetToanGiaoKhoan = 3
}

public enum TunnelExcavationTrimingUnitPriceType
{
    TunnelExcavation = 1,
    Trimming = 2,
}
