using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.ProductionOrder;

public class CreateProductionOrderDto
{
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
}

public class ProductionOrderDto : IDto
{
    public Guid Id { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
}

public class ProductionOrderExcelDto
{
    public Guid Id { get; set; }
    [Display(Name = "Thời gian bắt đầu")]
    public DateOnly StartMonth { get; set; }
    [Display(Name = "Thời gian kết thúc")]
    public DateOnly EndMonth { get; set; }
    [Display(Name = "Số quyết định, lệnh sản xuất")]
    public string Code { get; set; }
    [Display(Name = "Tên quyết định, lệnh sản xuất")]
    public string Name { get; set; }
}
