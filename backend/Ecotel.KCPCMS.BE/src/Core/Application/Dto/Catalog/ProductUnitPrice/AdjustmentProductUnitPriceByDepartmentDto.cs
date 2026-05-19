using Domain.Common.Enums;

namespace Application.Dto.Catalog.ProductUnitPrice;

public class AdjustmentProductUnitPriceByDepartmentItemDto
{
    public Guid ProductUnitPriceId { get; set; }
    public Guid? PlannedOutputId { get; set; }
    public Guid ProductionOutputId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; } = string.Empty;
    public string ProcessGroupName { get; set; } = string.Empty;
    public ProcessGroupType ProcessGroupType { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; } = string.Empty;
    public double ProductionMeters { get; set; }
    public double StandardProductionMeters { get; set; }
    public double ActualAshContent { get; set; }
    public double AdjustmentTotalCost { get; set; }
    public double AkRate { get; set; }
    public double AkRatePercent { get; set; }
}

public class AdjustmentProductUnitPriceByDepartmentMonthDto
{
    public DateOnly Month { get; set; }
    public IList<AdjustmentProductUnitPriceByDepartmentItemDto> Items { get; set; } =
        new List<AdjustmentProductUnitPriceByDepartmentItemDto>();
}

public class AdjustmentProductUnitPriceByDepartmentDetailDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public IList<AdjustmentProductUnitPriceByDepartmentMonthDto> Months { get; set; } =
        new List<AdjustmentProductUnitPriceByDepartmentMonthDto>();
}
