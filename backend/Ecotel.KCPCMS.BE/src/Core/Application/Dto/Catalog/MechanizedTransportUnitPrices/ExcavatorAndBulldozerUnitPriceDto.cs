using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.MechanizedTransportUnitPrices;

public class ExcavatorAndBulldozerUnitPriceDetailInputDto
{
    public Guid? HaulDistanceId { get; set; }
    public decimal FuelUnitPrice { get; set; }
    public decimal MaintenanceUnitPrice { get; set; }
}

public class CreateExcavatorAndBulldozerUnitPriceDto
{
    public Guid AssignmentCodeId { get; set; }
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<ExcavatorAndBulldozerUnitPriceDetailInputDto> Details { get; set; } = new();
}

public class UpdateExcavatorAndBulldozerUnitPriceDto
{
    public Guid Id { get; set; }
    public Guid AssignmentCodeId { get; set; }
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<ExcavatorAndBulldozerUnitPriceDetailInputDto> Details { get; set; } = new();
}

public class ExcavatorAndBulldozerUnitPriceDetailDto
{
    public Guid Id { get; set; }
    public Guid? HaulDistanceId { get; set; }
    public string? HaulDistanceValue { get; set; }
    public decimal FuelUnitPrice { get; set; }
    public decimal MaintenanceUnitPrice { get; set; }
}

public class ExcavatorAndBulldozerUnitPriceDto : IDto
{
    public Guid Id { get; set; }
    public Guid AssignmentCodeId { get; set; }
    public string AssignmentCodeName { get; set; } = string.Empty;
    public string EquipmentQuality { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public string ProductionProcessName { get; set; } = string.Empty;
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public List<ExcavatorAndBulldozerUnitPriceDetailDto> Details { get; set; } = new();
}

public class ExcavatorAndBulldozerUnitPriceExcelDto
{
    public Guid Id { get; set; }

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

    [Display(Name = "Đơn giá Nhiên liệu")]
    public decimal FuelUnitPrice { get; set; }

    [Display(Name = "Đơn giá SCTX")]
    public decimal MaintenanceUnitPrice { get; set; }
}