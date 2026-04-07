using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.SavingsRateConfig;

public class SavingsRateConfigDto : IDto
{
    public Guid Id { get; set; }
    public decimal? MinRevenue { get; set; }
    public decimal? MaxRevenue { get; set; }
    public decimal? MinSavingsRate { get; set; }
    public decimal? MaxSavingsRate { get; set; }
    public string? RevenueDisplay { get; set; }
    public string? SavingsRateDisplay { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreateOn { get; set; }
}

public class SavingsRateConfigExcelDto
{
    public Guid Id { get; set; }

    [Display(Name = "Doanh thu")]
    public string? RevenueDisplay { get; set; }

    [Display(Name = "Ty le tiet kiem")]
    public string? SavingsRateDisplay { get; set; }

    // Backward-compatible columns for old import templates.
    public decimal? MaxRevenue { get; set; }
    public decimal? MaxSavingsRate { get; set; }

    [Display(Name = "Mo ta")]
    public string? Description { get; set; }
}

public class CreateSavingsRateConfigDto
{
    public string? RevenueDisplay { get; set; }
    public string? SavingsRateDisplay { get; set; }
    public decimal? MaxRevenue { get; set; }
    public decimal? MaxSavingsRate { get; set; }
    public string? Description { get; set; }
}
