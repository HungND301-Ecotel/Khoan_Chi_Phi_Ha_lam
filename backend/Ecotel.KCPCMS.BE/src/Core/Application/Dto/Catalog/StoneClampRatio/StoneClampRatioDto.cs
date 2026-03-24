using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.StoneClampRatio;

public class StoneClampRatioDto : IDto
{
    public Guid Id { get; set; }
    public string Value { get; set; }
}

public class StoneClampRatioExcelDto
{
    public Guid Id { get; set; }
    [Display(Name = "Tỷ lệ đá kẹp")]
    public string Value { get; set; }
}

public class CreateStoneClampRatioDto
{
    public string Value { get; set; }
}

public class UpdateStoneClampRatioDto : CreateStoneClampRatioDto
{
    public Guid Id { get; set; }
}
