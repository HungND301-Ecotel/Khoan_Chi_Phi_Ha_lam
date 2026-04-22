using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.LowValuePerishableSupplyUnitPrice;

public class LowValuePerishableSupplyUnitPriceDto : IDto
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; } = string.Empty;
    public string ProcessGroupName { get; set; } = string.Empty;
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public LowValuePerishableSupplyType Type { get; set; }
    public double TotalPrice { get; set; }
}

public class CreateLowValuePerishableSupplyUnitPriceDto
{
    public Guid DepartmentId { get; set; }
    public Guid ProcessGroupId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public LowValuePerishableSupplyType Type { get; set; } = LowValuePerishableSupplyType.TunnelExcavation;
    public double TotalPrice { get; set; }
}

public class UpdateLowValuePerishableSupplyUnitPriceDto
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid ProcessGroupId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public LowValuePerishableSupplyType Type { get; set; } = LowValuePerishableSupplyType.TunnelExcavation;
    public double TotalPrice { get; set; }
}