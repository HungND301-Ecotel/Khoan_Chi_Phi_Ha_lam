namespace Application.Dto.Catalog.LumpSumFinalSettlement;

public class LumpSumFinalSettlementListRequest
{
    public string Month { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string ProcessGroupId { get; set; } = string.Empty;
}

public class LumpSumFinalSettlementQuarterListRequest
{
    public string Quarter { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string ProcessGroupId { get; set; } = string.Empty;
}

public class LumpSumQuarterCustomCostListRequest
{
    public string Quarter { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string ProcessGroupId { get; set; } = string.Empty;
}

public class CreateLumpSumQuarterCustomCostRequest
{
    public string Month { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string ProcessGroupId { get; set; } = string.Empty;
    public string CustomName { get; set; } = string.Empty;
    public double ActualQuantity { get; set; }
    public double MaterialUnitPrice { get; set; }
    public double MaintainUnitPrice { get; set; }
    public double ElectricityUnitPrice { get; set; }
}

public class UpdateLumpSumQuarterCustomCostRequest
{
    public Guid Id { get; set; }
    public string Month { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string ProcessGroupId { get; set; } = string.Empty;
    public string CustomName { get; set; } = string.Empty;
    public double ActualQuantity { get; set; }
    public double MaterialUnitPrice { get; set; }
    public double MaintainUnitPrice { get; set; }
    public double ElectricityUnitPrice { get; set; }
}
