using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.AkFactorConfig;

public class AkFactorConfigDto : IDto
{
    public Guid Id { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; } = string.Empty;
    public string ProcessGroupName { get; set; } = string.Empty;
    public decimal? MinAkDiff { get; set; }
    public decimal? MaxAkDiff { get; set; }
    public decimal? MinAdjustmentRate { get; set; }
    public decimal? MaxAdjustmentRate { get; set; }
    public string? AkDiffDisplay { get; set; }
    public string? AdjustmentRateDisplay { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreateOn { get; set; }
}

public class AkFactorConfigExcelDto
{
    public Guid Id { get; set; }

    [Display(Name = "Ma nhom cong doan")]
    public string ProcessGroupCode { get; set; } = string.Empty;

    [Display(Name = "Chenh lech Ak")]
    public string? AkDiffDisplay { get; set; }

    [Display(Name = "Ty le dieu chinh doanh thu")]
    public string? AdjustmentRateDisplay { get; set; }

    [Display(Name = "Mo ta")]
    public string? Description { get; set; }
}

public class CreateAkFactorConfigDto
{
    public Guid ProcessGroupId { get; set; }
    public string? AkDiffDisplay { get; set; }
    public string? AdjustmentRateDisplay { get; set; }
    public string? Description { get; set; }
}
