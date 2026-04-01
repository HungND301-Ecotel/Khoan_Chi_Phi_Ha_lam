namespace Application.Dto.Catalog.PlannedMaterialCost;

public class PlannedMaterialCostDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductUnitPriceId { get; set; }
    public Guid MaterialUnitPriceId { get; set; }
    public Guid? SlideUnitPriceAssignmentCodeId { get; set; }
    public Guid? NormFactorId { get; set; }
    public Guid? StoneClampRatioReferenceId { get; set; }
    public Guid? MaterialReferenceId { get; set; }
    public Guid OutputId { get; set; }
    public double? OtherMaterialValue { get; set; }
    public double TotalPlannedMaterialPrice { get; set; }
    public double MaterialCost { get; set; }
    public double SlideUnitPriceCost { get; set; }
    public string NormFactorValue { get; set; } = string.Empty;
    public IList<PlannedMaterialCostAssignmentCode> PlannedMaterialCostAssignmentCodes { get; set; } = new List<PlannedMaterialCostAssignmentCode>();
}

public class PlannedMaterialCostAssignmentCode
{
    public DefaultIdType? AssignmentCodeId { get; set; }
    public string AssignmentCode { get; set; } = string.Empty;
    public string AssignmentCodeName { get; set; } = string.Empty;
    public IList<PlannedMaterialCostDto> Costs { get; set; } = new List<PlannedMaterialCostDto>();
}

public class PlannedMaterialCostDto
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

public class CreatePlannedMaterialCostDto
{
    public Guid ProductUnitPriceId { get; set; }
    public Guid MaterialUnitPriceId { get; set; }
    public Guid? SlideUnitPriceAssignmentCodeId { get; set; }
    public Guid? NormFactorId { get; set; }
    public Guid? StoneClampRatioReferenceId { get; set; }
    public Guid? MaterialReferenceId { get; set; }
    public Guid OutputId { get; set; }

}

public class UpdatePlannedMaterialCostDto
{
    public Guid Id { get; set; }
    public Guid MaterialUnitPriceId { get; set; }
    public Guid? SlideUnitPriceAssignmentCodeId { get; set; }
    public Guid? NormFactorId { get; set; }
    public Guid? StoneClampRatioReferenceId { get; set; }
    public Guid? MaterialReferenceId { get; set; }
    public Guid OutputId { get; set; }
}
