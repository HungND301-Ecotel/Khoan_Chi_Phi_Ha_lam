namespace Application.Dto.Catalog.AcceptanceReport;

public class GetSctxEquipmentRevenueRequest
{
    public int FromYear { get; set; }
    public int ToYear { get; set; }
    public Guid EquipmentId { get; set; }
}

public class SctxEquipmentRevenueByMonthDto
{
    public int Month { get; set; }
    public decimal UnitPrice { get; set; }
    public double PlannedOutput { get; set; }
    public double ActualOutput { get; set; }
    public decimal InitialRevenue { get; set; }
    public decimal AdjustedRevenue { get; set; }
}

public class SctxEquipmentRevenueResponseDto
{
    public Guid EquipmentId { get; set; }
    public IList<SctxEquipmentRevenueByYearDto> Years { get; set; } = new List<SctxEquipmentRevenueByYearDto>();
}

public class SctxEquipmentRevenueByYearDto
{
    public int Year { get; set; }
    public IList<SctxEquipmentRevenueByMonthDto> Months { get; set; } = new List<SctxEquipmentRevenueByMonthDto>();
}
