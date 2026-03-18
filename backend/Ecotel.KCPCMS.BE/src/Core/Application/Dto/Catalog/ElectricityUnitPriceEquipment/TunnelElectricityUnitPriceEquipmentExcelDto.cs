using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Catalog.ElectricityUnitPriceEquipment
{
    public class TunnelElectricityUnitPriceEquipmentExcelDto
    {
        public DefaultIdType Id { get; set; }
        [Display(Name = "Mã thiết bị")]
        public string EquipmentCode { get; set; } = string.Empty;
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasureName { get; set; } = string.Empty;
        [Display(Name = "Điện năng tiêu thụ/tháng (kWh)")]
        public double MonthlyElectricityCost { get; set; }
        [Display(Name = "Sản lượng mét lò bình quân (m)")]
        public decimal AverageMonthlyTunnelProduction { get; set; }
        [Display(Name = "Thời gian bắt đầu")]
        public string StartMonth { get; set; } = string.Empty;
        [Display(Name = "Thời gian kết thúc")]
        public string EndMonth { get; set; } = string.Empty;
    }
}
