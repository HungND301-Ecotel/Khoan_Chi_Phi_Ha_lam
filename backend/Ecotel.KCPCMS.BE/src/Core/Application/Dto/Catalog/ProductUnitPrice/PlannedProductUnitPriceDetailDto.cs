using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.ProductUnitPrice;

public class PlannedProductUnitPriceDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; }
    public string ProcessGroupName { get; set; }
    public ProcessGroupType ProcessGroupType { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; }
    public IList<PlannedOutputDto> Outputs { get; set; }
}

public class ActualProductUnitPriceDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; }
    public string ProcessGroupName { get; set; }
    public ProcessGroupType ProcessGroupType { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; }
    public Guid? ProductionOutputId { get; set; }
    public IList<ActualOutputDto> Outputs { get; set; }
}

public class AdjustmentProductUnitPriceDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; }
    public string ProcessGroupName { get; set; }
    public ProcessGroupType ProcessGroupType { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; }
    public IList<AdjustmentPlannedOutputDto> Outputs { get; set; }
    public IList<AdjustmentProductionOutputDto> ProductionOutputs { get; set; }
}

public class AdjustmentProductionOutputDto
{
    public Guid Id { get; set; }
    public Guid? ProductionOutputId { get; set; }
    public DateOnly? StartMonth { get; set; }
    public DateOnly? EndMonth { get; set; }
    public double? ProductionMeters { get; set; }
    public double? StandardProductionMeters { get; set; }
    public double AdjTotalPrice { get; set; }
}
public class AdjustmentPlannedOutputDto
{
    public Guid Id { get; set; }
    public Guid ProductUnitPriceId { get; set; }
    public double ProductionMeters { get; set; }
    public OutputType OutputType { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
}

public class ProductUnitPriceDto : IDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; }
    public string ProcessGroupName { get; set; }
    public ProcessGroupType ProcessGroupType { get; set; }
    public double TotalProductionMeters { get; set; }
    public double PlannedTotalCost { get; set; }
    public double ActualTotalCost { get; set; }
    public double AdjustmentTotalCost { get; set; }
    public DateOnly? StartMonth { get; set; }
    public DateOnly? EndMonth { get; set; }
}

public class CreateProductUnitPriceDto
{
    public Guid ProductId { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public Guid? ProductionOutputId { get; set; }

    public IList<CreateOutputDto> Outputs { get; set; }
}

public class UpdateProductUnitPriceDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; } = Guid.Empty;
    public Guid? UnitOfMeasureId { get; set; }
    public Guid? ProductionOutputId { get; set; }
    public OutputType Type { get; set; }
    public IList<UpdateOutputDto> Outputs { get; set; }
}


public class UpdateAdjustmentProductUnitPriceDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; } = Guid.Empty;
    public Guid? UnitOfMeasureId { get; set; }
    public IDictionary<Guid, double> ProductionOutputs { get; set; } = new Dictionary<Guid, double>();
}