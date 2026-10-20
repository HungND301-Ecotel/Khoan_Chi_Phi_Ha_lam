using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Domain.Common.Enums;
using Domain.Extensions;

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
    public string LocationTypeName => LocationType == LocationType.None ? string.Empty : LocationType.GetDisplayName();
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

    // Ghi bằng tên hiển thị (Vị trí nhận tải / Vị trí đổ tải)
    [Display(Name = "Phân loại")]
    public string? LocationType { get; set; }
}