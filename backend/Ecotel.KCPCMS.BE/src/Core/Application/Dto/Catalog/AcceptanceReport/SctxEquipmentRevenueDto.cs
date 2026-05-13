namespace Application.Dto.Catalog.AcceptanceReport;

public class GetSctxEquipmentRevenueRequest
{
    public string? FromMonth { get; set; }
    public string? ToMonth { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid? DepartmentId { get; set; }
}

public class SctxEquipmentRevenueByMonthDto
{
    public int Month { get; set; }
    public double UnitPrice { get; set; }
    public double PlannedOutput { get; set; }
    public double ActualOutput { get; set; }
    public double InitialRevenue { get; set; }
    public double AdjustedRevenue { get; set; }
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
