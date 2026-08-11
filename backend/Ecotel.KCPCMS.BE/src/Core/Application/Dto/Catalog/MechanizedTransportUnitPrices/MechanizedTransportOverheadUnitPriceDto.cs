using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.MechanizedTransportUnitPrices;

public class CreateMechanizedTransportOverheadUnitPriceDto
{
    public Guid ProcessGroupId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public decimal LowValuePerishableSupplyUnitPrice { get; set; }
    public decimal? ElectricityUnitPrice { get; set; }
}

public class UpdateMechanizedTransportOverheadUnitPriceDto
{
    public Guid Id { get; set; }
    public Guid ProcessGroupId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public decimal LowValuePerishableSupplyUnitPrice { get; set; }
    public decimal? ElectricityUnitPrice { get; set; }
}

public class MechanizedTransportOverheadUnitPriceDto : IDto
{
    public Guid Id { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupName { get; set; } = string.Empty;
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public decimal LowValuePerishableSupplyUnitPrice { get; set; }
    public decimal? ElectricityUnitPrice { get; set; }
}

public class MechanizedTransportOverheadUnitPriceExcelDto
{
    public Guid Id { get; set; }

    [Display(Name = "Thời gian bắt đầu")]
    public string StartMonth { get; set; } = string.Empty;

    [Display(Name = "Thời gian kết thúc")]
    public string EndMonth { get; set; } = string.Empty;

    [Display(Name = "Nhóm công đoạn sản xuất")]
    public string ProcessGroupCode { get; set; } = string.Empty;

    [Display(Name = "Đơn giá vật tư mau hỏng rẻ tiền")]
    public decimal LowValuePerishableSupplyUnitPrice { get; set; }

    [Display(Name = "Đơn giá điện năng")]
    public decimal? ElectricityUnitPrice { get; set; }
}