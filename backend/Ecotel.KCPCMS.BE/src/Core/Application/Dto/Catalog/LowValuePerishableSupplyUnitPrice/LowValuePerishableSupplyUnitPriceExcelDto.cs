using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Catalog.LowValuePerishableSupplyUnitPrice;

public class LowValuePerishableSupplyUnitPriceExcelDto
{
    public DefaultIdType Id { get; set; }

    [Display(Name = "Mã đơn vị")]
    public string DepartmentCode { get; set; } = string.Empty;

    [Display(Name = "Mã nhóm công đoạn")]
    public string ProcessGroupCode { get; set; } = string.Empty;

    [Display(Name = "Tháng bắt đầu (MM/yyyy)")]
    public string StartMonth { get; set; } = string.Empty;

    [Display(Name = "Tháng kết thúc (MM/yyyy)")]
    public string EndMonth { get; set; } = string.Empty;

    [Display(Name = "Đơn giá")]
    public double TotalPrice { get; set; }
}