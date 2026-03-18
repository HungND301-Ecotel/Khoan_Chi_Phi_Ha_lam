namespace Application.Dto.Catalog.LumpSumFinalSettlement;

public class LumpSumFinalSettlementDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public Guid UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; } = string.Empty;
    public double PlannedQuantity { get; set; }
    public double ActualQuantity { get; set; }
    
    public LumpSumCostDetailDto Materials { get; set; } = new();
    public LumpSumCostDetailDto Maintains { get; set; } = new();
    public LumpSumCostDetailDto Electricities { get; set; } = new();
    
    public double TotalAmount { get; set; }
}

public class LumpSumCostDetailDto
{
    public double UnitPrice { get; set; }
    public double TotalAmount { get; set; }
}
