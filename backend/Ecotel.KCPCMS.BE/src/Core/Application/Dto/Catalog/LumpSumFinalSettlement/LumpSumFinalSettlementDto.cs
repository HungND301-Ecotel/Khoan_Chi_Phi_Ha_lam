namespace Application.Dto.Catalog.LumpSumFinalSettlement;

public class LumpSumFinalSettlementDto
{
    public Guid Id { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; } = string.Empty;
    public string ProcessGroupName { get; set; } = string.Empty;
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

public class LumpSumFinalSettlementQuarterResponseDto
{
    public List<LumpSumFinalSettlementDto> Items { get; set; } = new();
    public List<LumpSumQuarterRevenueByMonthDto> RevenuesByMonth { get; set; } = new();
    public LumpSumQuarterTransferredCostDto TransferredCost { get; set; } = new();
}

public class LumpSumQuarterRevenueByMonthDto
{
    public int Month { get; set; }
    public LumpSumCostDetailDto Materials { get; set; } = new();
    public LumpSumCostDetailDto Maintains { get; set; } = new();
    public LumpSumCostDetailDto Electricities { get; set; } = new();
    public double TotalAmount { get; set; }
}

public class LumpSumQuarterTransferredCostDto
{
    public int Month { get; set; }
    public LumpSumCostDetailDto Materials { get; set; } = new();
    public LumpSumCostDetailDto Maintains { get; set; } = new();
    public LumpSumCostDetailDto Electricities { get; set; } = new();
    public double TotalAmount { get; set; }
}
