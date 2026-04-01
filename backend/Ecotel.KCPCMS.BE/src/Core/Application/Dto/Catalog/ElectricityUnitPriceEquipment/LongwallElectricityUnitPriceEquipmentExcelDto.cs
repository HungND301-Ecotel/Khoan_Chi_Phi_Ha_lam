using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Catalog.ElectricityUnitPriceEquipment
{
    public class LongwallElectricityUnitPriceEquipmentExcelDto
    {
        public DefaultIdType Id { get; set; }
        [Display(Name = "Mã thiết bị")]
        public string EquipmentCode { get; set; } = string.Empty;
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasureName { get; set; } = string.Empty;
        [Display(Name = "Số lượng")]
        public decimal Quantity { get; set; }
        [Display(Name = "Pđm (kW)")]
        public decimal Pdm { get; set; }
        [Display(Name = "Kyc ")]
        public double Kyc { get; set; }
        [Display(Name = "Kdt")]
        public double Kdt { get; set; }
        [Display(Name = "Thời gian (h)")]
        public double WorkingHour { get; set; }
        [Display(Name = "Ngày hoạt động")]
        public decimal WorkingDate { get; set; }
        [Display(Name = "Sản lượng than bình quân tháng (1000 tấn)")]
        public decimal AverageMonthlyTunnelProduction { get; set; }
        [Display(Name = "Thời gian bắt đầu")]
        public string StartMonth { get; set; } = string.Empty;
        [Display(Name = "Thời gian kết thúc")]
        public string EndMonth { get; set; } = string.Empty;
    }
}
