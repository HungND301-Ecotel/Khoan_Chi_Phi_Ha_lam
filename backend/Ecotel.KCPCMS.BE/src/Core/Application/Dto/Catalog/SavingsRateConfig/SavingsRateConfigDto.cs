using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.SavingsRateConfig;

public class SavingsRateConfigDto : IDto
{
    public Guid Id { get; set; }
    public decimal? MaxRevenue { get; set; }
    public decimal? MaxSavingsRate { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreateOn { get; set; }
}

public class SavingsRateConfigExcelDto
{
    public Guid Id { get; set; }

    [Display(Name = "Doanh thu toi da")]
    public decimal? MaxRevenue { get; set; }

    [Display(Name = "Ty le tiet kiem toi da")]
    public decimal? MaxSavingsRate { get; set; }

    [Display(Name = "Mo ta")]
    public string? Description { get; set; }
}

public class CreateSavingsRateConfigDto
{
    public decimal? MaxRevenue { get; set; }
    public decimal? MaxSavingsRate { get; set; }
    public string? Description { get; set; }
}
