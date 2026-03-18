namespace Application.Dto.Catalog.AdjustmnetMaterialCost;

public class AdjustmentMaterialCostDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductUnitPriceId { get; set; }
    public Guid MaterialUnitPriceId { get; set; }
    public Guid? SlideUnitPriceAssignmentCodeId { get; set; }
    public double? OtherMaterialValue { get; set; }
    public Guid? StoneClampRatioId { get; set; }
    public Guid OutputId { get; set; }
    public double TotalPlannedMaterialPrice { get; set; }
    public IList<AdjustmentMaterialCostAssignmentCode> AdjustmentMaterialCostAssignmentCodes { get; set; } = new List<AdjustmentMaterialCostAssignmentCode>();
}

public class AdjustmentMaterialCostAssignmentCode
{
    public DefaultIdType? AssignmentCodeId { get; set; }
    public string AssignmentCode { get; set; } = string.Empty;
    public string AssignmentCodeName { get; set; } = string.Empty;
    public IList<AdjustmentMaterialCostDto> Costs { get; set; } = new List<AdjustmentMaterialCostDto>();
}

public class AdjustmentMaterialCostDto
{
    public DefaultIdType MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string UnitOfMeasureName { get; set; } = string.Empty;
    public double MaterialCost { get; set; }
    public double MaterialUnitPriceCost { get; set; }
    public double OriginalQuantity { get; set; }
    public double CoefficientValue { get; set; }
    public double FinalQuantity { get; set; }
    public double TotalPrice { get; set; }
}