using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.MechanizedTransportUnitPrices;

public class ScaniaTruckUnitPriceDetailInputDto
{
    public Guid? HaulDistanceId { get; set; }
    public decimal FuelUnitPrice { get; set; }
    public decimal PowerUnitPrice { get; set; }
    public decimal MaintenanceUnitPrice { get; set; }
}

public class CreateScaniaTruckUnitPriceDto
{
    public Guid AssignmentCodeId { get; set; }
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public Guid CargoTypeId { get; set; }
    public Guid? ReceivingLocationId { get; set; }
    public Guid? DumpingLocationId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<ScaniaTruckUnitPriceDetailInputDto> Details { get; set; } = new();
}

public class UpdateScaniaTruckUnitPriceDto
{
    public Guid Id { get; set; }
    public Guid AssignmentCodeId { get; set; }
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public Guid CargoTypeId { get; set; }
    public Guid? ReceivingLocationId { get; set; }
    public Guid? DumpingLocationId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<ScaniaTruckUnitPriceDetailInputDto> Details { get; set; } = new();
}

public class ScaniaTruckUnitPriceDetailDto
{
    public Guid Id { get; set; }
    public Guid? HaulDistanceId { get; set; }
    public string? HaulDistanceValue { get; set; }
    public decimal FuelUnitPrice { get; set; }
    public decimal? PowerUnitPrice { get; set; }
    public decimal MaintenanceUnitPrice { get; set; }
}

public class ScaniaTruckUnitPriceDto : IDto
{
    public Guid Id { get; set; }
    public Guid AssignmentCodeId { get; set; }
    public string AssignmentCodeName { get; set; } = string.Empty;
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public string ProductionProcessName { get; set; } = string.Empty;
    public Guid CargoTypeId { get; set; }
    public string CargoTypeName { get; set; } = string.Empty;
    public Guid? ReceivingLocationId { get; set; }
    public string? ReceivingLocationName { get; set; }
    public Guid? DumpingLocationId { get; set; }
    public string? DumpingLocationName { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<ScaniaTruckUnitPriceDetailDto> Details { get; set; } = new();
}

public class ScaniaTruckUnitPriceExcelDto
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

    [Display(Name = "Chủng loại hàng")]
    public string CargoTypeCode { get; set; } = string.Empty;

    [Display(Name = "Vị trí nhận")]
    public string? ReceivingLocationCode { get; set; }

    [Display(Name = "Vị trí đổ")]
    public string? DumpingLocationCode { get; set; }

    [Display(Name = "Cung độ vận tải")]
    public string HaulDistanceValue { get; set; } = string.Empty;

    [Display(Name = "Đơn giá Nhiên liệu")]
    public decimal FuelUnitPrice { get; set; }

    [Display(Name = "Đơn giá Động lực")]
    public decimal PowerUnitPrice { get; set; }

    [Display(Name = "Đơn giá SCTX")]
    public decimal MaintenanceUnitPrice { get; set; }
}

// phần gộp chung cho GetAll, import,export

public class MechanizedTransportUnitPriceRowDto
{
    public Guid HeaderId { get; set; }
    public Guid DetailId { get; set; }
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid? HaulDistanceId { get; set; }
    public string? HaulDistanceValue { get; set; }
    public decimal FuelUnitPrice { get; set; }
    public decimal? PowerUnitPrice { get; set; }
    public decimal MaintenanceUnitPrice { get; set; }
}

public class MechanizedTransportUnitPriceSectionDto
{
    public MechanizedTransportUnitPriceType VehicleType { get; set; }
    public Guid ProductionProcessId { get; set; }
    public string ProductionProcessName { get; set; } = string.Empty;

    // Scania-specific fields
    public Guid? CargoTypeId { get; set; }
    public string? CargoTypeName { get; set; }
    public Guid? ReceivingLocationId { get; set; }
    public string? ReceivingLocationName { get; set; }
    public Guid? DumpingLocationId { get; set; }
    public string? DumpingLocationName { get; set; }

    public List<MechanizedTransportUnitPriceRowDto> Rows { get; set; } = new();
}

// Gộp theo Nhóm vật tư, tài sản (AssignmentCode) + khoảng thời gian, không phụ thuộc vào loại xe/công đoạn bên trong
public class MechanizedTransportUnitPriceGroupDto : IDto
{
    public Guid AssignmentCodeId { get; set; }
    public string AssignmentCodeName { get; set; } = string.Empty;
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<MechanizedTransportUnitPriceSectionDto> Sections { get; set; } = new();
}