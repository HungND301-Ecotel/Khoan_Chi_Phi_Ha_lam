using Domain.Common.Enums;

namespace Application.Dto.Catalog.ProductUnitPrice;

public class PlannedProductUnitPriceByDepartmentItemDto
{
    public Guid? ProductUnitPriceId { get; set; }
    public Guid? OutputId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; } = string.Empty;
    public string ProcessGroupName { get; set; } = string.Empty;
    public ProcessGroupType ProcessGroupType { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; } = string.Empty;
    public double ProductionMeters { get; set; }
    public double PlannedTotalCost { get; set; }
    public double? PlanAshContent { get; set; }
}

public class PlannedProductUnitPriceByDepartmentMonthDto
{
    public DateOnly Month { get; set; }
    public IList<PlannedProductUnitPriceByDepartmentItemDto> Items { get; set; } =
        new List<PlannedProductUnitPriceByDepartmentItemDto>();
}

public class PlannedProductUnitPriceByDepartmentDetailDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public IList<PlannedProductUnitPriceByDepartmentMonthDto> Months { get; set; } =
        new List<PlannedProductUnitPriceByDepartmentMonthDto>();
}

public class CreatePlannedProductUnitPriceByDepartmentDto
{
    public Guid DepartmentId { get; set; }
    public IList<CreatePlannedProductUnitPriceByDepartmentMonthDto> Months { get; set; } =
        new List<CreatePlannedProductUnitPriceByDepartmentMonthDto>();
}

public class CreatePlannedProductUnitPriceByDepartmentMonthDto
{
    public DateOnly Month { get; set; }
    public IList<CreatePlannedProductUnitPriceByDepartmentItemDto> Items { get; set; } =
        new List<CreatePlannedProductUnitPriceByDepartmentItemDto>();
}

public class CreatePlannedProductUnitPriceByDepartmentItemDto
{
    public Guid ProductId { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public double ProductionMeters { get; set; }
    public double? PlanAshContent { get; set; }
}

public class UpdatePlannedProductUnitPriceByDepartmentDto
{
    public Guid DepartmentId { get; set; }
    public IList<UpdatePlannedProductUnitPriceByDepartmentMonthDto> Months { get; set; } =
        new List<UpdatePlannedProductUnitPriceByDepartmentMonthDto>();
}

public class UpdatePlannedProductUnitPriceByDepartmentMonthDto
{
    public DateOnly Month { get; set; }
    public IList<UpdatePlannedProductUnitPriceByDepartmentItemDto> Items { get; set; } =
        new List<UpdatePlannedProductUnitPriceByDepartmentItemDto>();
}

public class UpdatePlannedProductUnitPriceByDepartmentItemDto
{
    public Guid? ProductUnitPriceId { get; set; }
    public Guid? OutputId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public double ProductionMeters { get; set; }
    public double? PlanAshContent { get; set; }
}
