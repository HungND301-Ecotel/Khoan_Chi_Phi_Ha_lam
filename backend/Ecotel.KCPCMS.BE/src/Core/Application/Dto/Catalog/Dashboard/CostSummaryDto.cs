namespace Application.Dto.Catalog.Dashboard;

public class MonthlyCostDto
{
    public int Month { get; set; }
    public double TunnelQuantity { get; set; }
    public double LongwallQuantity { get; set; }
    public double PlannedCost { get; set; }
    public double ActualCost { get; set; }
}

public class CostSummaryDto
{
    public double TotalTunnelQuantity { get; set; }
    public double TotalLongwallQuantity { get; set; }
    public double TotalOtherQuantity { get; set; }
    public double TotalPlannedCost { get; set; }
    public double TotalActualCost { get; set; }
    public IList<MonthlyCostDto> MonthlyData { get; set; } = new List<MonthlyCostDto>();
}
