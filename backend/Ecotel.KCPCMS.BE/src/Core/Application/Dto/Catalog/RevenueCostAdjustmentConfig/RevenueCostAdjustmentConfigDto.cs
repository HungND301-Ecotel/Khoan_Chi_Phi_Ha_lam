using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.RevenueCostAdjustmentConfig;

public class RevenueCostAdjustmentConfigDto : IDto
{
    public Guid Id { get; set; }
    public string ProfitConditionDisplay { get; set; } = default!;
    public decimal? MinProfit { get; set; }
    public decimal? MaxProfit { get; set; }
    public string RateDisplay { get; set; } = default!;
    public decimal Rate { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreateOn { get; set; }
}

public class RevenueCostAdjustmentConfigExcelDto
{
    public Guid Id { get; set; }

    [Display(Name = "Dieu kien loi nhuan")]
    public string ProfitConditionDisplay { get; set; } = default!;

    [Display(Name = "Ty le dieu chinh")]
    public string RateDisplay { get; set; } = default!;

    [Display(Name = "Mo ta")]
    public string? Description { get; set; }
}

public class CreateRevenueCostAdjustmentConfigDto
{
    public string ProfitConditionDisplay { get; set; } = default!;
    public string RateDisplay { get; set; } = default!;
    public string? Description { get; set; }
}
