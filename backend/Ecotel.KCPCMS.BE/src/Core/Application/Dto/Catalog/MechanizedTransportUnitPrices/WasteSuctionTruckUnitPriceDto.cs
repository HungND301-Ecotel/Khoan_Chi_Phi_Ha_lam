using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.MechanizedTransportUnitPrices;

public class WasteSuctionTruckUnitPriceDetailInputDto
{
    public Guid? HaulDistanceId { get; set; }
    public decimal FuelUnitPrice { get; set; }
    public decimal MaintenanceUnitPrice { get; set; }
}

public class CreateWasteSuctionTruckUnitPriceDto
{
    public Guid AssignmentCodeId { get; set; }
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<WasteSuctionTruckUnitPriceDetailInputDto> Details { get; set; } = new();
}

public class UpdateWasteSuctionTruckUnitPriceDto
{
    public Guid Id { get; set; }
    public Guid AssignmentCodeId { get; set; }
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<WasteSuctionTruckUnitPriceDetailInputDto> Details { get; set; } = new();
}

public class WasteSuctionTruckUnitPriceDetailDto
{
    public Guid Id { get; set; }
    public Guid? HaulDistanceId { get; set; }
    public string? HaulDistanceValue { get; set; }
    public decimal FuelUnitPrice { get; set; }
    public decimal MaintenanceUnitPrice { get; set; }
}

public class WasteSuctionTruckUnitPriceDto : IDto
{
    public Guid Id { get; set; }
    public Guid AssignmentCodeId { get; set; }
    public string AssignmentCodeName { get; set; } = string.Empty;
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public string ProductionProcessName { get; set; } = string.Empty;
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<WasteSuctionTruckUnitPriceDetailDto> Details { get; set; } = new();
}

public class WasteSuctionTruckUnitPriceExcelDto
{
    public Guid HeaderId { get; set; }

    [Display(Name = "Thời gian bắt đầu")]
    public string StartMonth { get; set; } = string.Empty;

    [Display(Name = "Thời gian kết thúc")]
    public string EndMonth { get; set; } = string.Empty;

    [Display(Name = "Nhóm vật tư, tài sản")]
    public string AssignmentCodeCode { get; set; } = string.Empty;

    [Display(Name = "Chất lượng thiết bị")]
    public string EquipmentQuality { get; set; } = string.Empty;

    [Display(Name = "Công đoạn sản xuất")]
    public string ProductionProcessCode { get; set; } = string.Empty;

    [Display(Name = "Cung độ vận tải")]
    public string? HaulDistanceValue { get; set; }

    [Display(Name = "Đơn giá Nhiên liệu")]
    public decimal FuelUnitPrice { get; set; }

    [Display(Name = "Đơn giá SCTX")]
    public decimal MaintenanceUnitPrice { get; set; }
}