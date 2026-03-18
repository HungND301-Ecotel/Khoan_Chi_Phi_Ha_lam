using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Catalog.AcceptanceReport;

public record AcceptanceReportExcelTemplateDto
{
    public Guid? Id { get; init; }
    [Display(Name = "Mã vật tư")]
    public required string MaterialCode { get; init; }
    [Display(Name = "Số lượng lĩnh")]
    public required double IssuedQuantity { get; init; }
    [Display(Name = "Số lượng xuất")]
    public required double ShippedQuantity { get; init; }
}
