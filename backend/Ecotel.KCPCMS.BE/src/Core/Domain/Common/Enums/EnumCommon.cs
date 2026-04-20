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
    K4 = 3,
    K5 = 5,
    K6 = 6,
    K7 = 7,
    K8 = 8,
}

public enum ProcessGroupType
{
    None = 0,
    DL = 1,
    LC = 2,
    XL = 3
}

public enum FixedKeyType
{
    None = 0,
    ProcessGroup = 1,
    MaterialsIncludedInContractRevenue = 2,
    AdditionalCost = 3,
    OtherMaterialDetail = 4,
    QuotaBasedMaterial = 5,
    QuotaBasedMaterialType = 6,
    Asset = 7,
    IssuedQuantityType = 8,
    ShippedQuantityType = 9,
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
    [Display(Name = "Trong khoán")]
    MaterialInContract = 1,

    [Display(Name = "Ngoài khoán")]
    MaterialOutContract = 2,

    [Display(Name = "Vật tư theo chế độ người lao động, phòng cháy chữa cháy, phòng chống mưa bão")]
    SafetyAndWelfareMaterials = 3,

    [Display(Name = "Tài sản")]
    Resource = 4,

    [Display(Name = "Vật tư theo hạn mức")]
    QuotaMaterials = 5,
}

public enum MaterialUnitPriceType
{
    TunnelExcavation = 1,  // Đào lò
    Longwall = 2,          // Lò chợ
    TunnelSupportAndDrilling = 3
}


public enum ElectricityUnitPriceType
{
    TunnelExcavation = 1,  // Đào lò
    Longwall = 2,           // Lò chợ
    Trimming = 3           // Lò chợ
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
