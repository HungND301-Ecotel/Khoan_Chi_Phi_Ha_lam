using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.TransportUnitPrice
{
    public class TransportUnitPriceDto : IDto
    {
        public Guid Id { get; set; }
        public Guid ProductionProcessId { get; set; }
        public string? ProductionProcessName { get; set; }
        public Guid? TransportRouteId { get; set; }
        public string? TransportRouteName { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public Guid? EquipmentId { get; set; }
        public string? EquipmentName { get; set; }
        public string? EquipmentQuality { get; set; }
        public decimal? MaterialFuelUnitPrice { get; set; }
        public decimal? PowerUnitPrice { get; set; }
        public decimal? MaintenanceUnitPrice { get; set; }
        public decimal? Quantity { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public bool IsLowVolumeCase { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
    }

    public class CreateTransportUnitPriceDto
    {
        public Guid ProductionProcessId { get; set; }
        public Guid? TransportRouteId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? EquipmentId { get; set; }
        public string? EquipmentQuality { get; set; }
        public decimal? MaterialFuelUnitPrice { get; set; }
        public decimal? PowerUnitPrice { get; set; }
        public decimal? MaintenanceUnitPrice { get; set; }
        public decimal? Quantity { get; set; }
        public bool IsLowVolumeCase { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
    }

    public class TransportUnitPriceExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Thời gian bắt đầu")]
        public string StartMonth { get; set; } = string.Empty;

        [Display(Name = "Thời gian kết thúc")]
        public string EndMonth { get; set; } = string.Empty;

        [Display(Name = "Công đoạn sản xuất")]
        public string ProductionProcessCode { get; set; } = string.Empty;

        [Display(Name = "Tuyến vận tải")]
        public string? TransportRouteCode { get; set; }

        [Display(Name = "Đơn vị")]
        public string? DepartmentCode { get; set; }

        [Display(Name = "Nhóm vật tư, tài sản")]
        public string? EquipmentCode { get; set; }

        [Display(Name = "Chất lượng thiết bị")]
        public string? EquipmentQuality { get; set; }

        [Display(Name = "Đơn giá Vật liệu, nhiên liệu")]
        public decimal? MaterialFuelUnitPrice { get; set; }

        [Display(Name = "Đơn giá Động lực")]
        public decimal? PowerUnitPrice { get; set; }

        [Display(Name = "Đơn giá SCTX")]
        public decimal? MaintenanceUnitPrice { get; set; }

        [Display(Name = "Định mức")]
        public decimal? Quantity { get; set; }

        [Display(Name = "Áp dụng giá SL thấp")]
        public bool IsLowVolumeCase { get; set; }
    }
}