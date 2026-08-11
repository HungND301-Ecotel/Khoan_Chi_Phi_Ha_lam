using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.CargoType;

public class CreateCargoTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class UpdateCargoTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CargoTypeDto : IDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CargoTypeExcelDto
{
    public Guid? Id { get; set; }

    [Display(Name = "Mã chủng loại hàng")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Chủng loại hàng")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}