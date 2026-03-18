using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Catalog.MaintainUnitPriceEquipment
{
    public class LongwallMaintainUnitPriceEquipmentExcelDto
    {
        public DefaultIdType Id { get; set; }
        [Display(Name = "Mã thiết bị")]
        public string EquipmentCode { get; set; } = string.Empty;
        [Display(Name = "Tháng bắt đầu (MM/yyyy)")]
        public string StartMonth { get; set; } = string.Empty;
        [Display(Name = "Tháng kết thúc (MM/yyyy)")]
        public string EndMonth { get; set; } = string.Empty;
    }
}
