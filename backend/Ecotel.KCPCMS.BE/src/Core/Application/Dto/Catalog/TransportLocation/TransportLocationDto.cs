using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.TransportLocation;

public class CreateTransportLocationDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public LocationType LocationType { get; set; }
}

public class UpdateTransportLocationDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public LocationType LocationType { get; set; }
}

public class TransportLocationDto : IDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public LocationType LocationType { get; set; }
}

public class TransportLocationExcelDto
{
    public Guid? Id { get; set; }

    [Display(Name = "Mã vị trí")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Tên vị trí")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [Display(Name = "Loại vị trí")]
    public LocationType LocationType { get; set; }
}