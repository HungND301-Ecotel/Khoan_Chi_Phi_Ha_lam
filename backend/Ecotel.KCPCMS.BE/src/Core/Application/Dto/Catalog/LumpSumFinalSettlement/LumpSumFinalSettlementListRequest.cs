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
