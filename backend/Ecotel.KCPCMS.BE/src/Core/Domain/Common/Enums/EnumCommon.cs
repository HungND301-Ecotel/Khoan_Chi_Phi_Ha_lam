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
    Longwall = 2
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
}

public enum MaterialUnitPriceType
{
    TunnelExcavation = 1,  // Đào lò
    Longwall = 2           // Lò chợ
}


public enum ElectricityUnitPriceType
{
    TunnelExcavation = 1,  // Đào lò
    Longwall = 2           // Lò chợ
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
    OtherMaterial = 4
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

public enum PartType
{
    Part = 1,
    OtherPart = 2
}