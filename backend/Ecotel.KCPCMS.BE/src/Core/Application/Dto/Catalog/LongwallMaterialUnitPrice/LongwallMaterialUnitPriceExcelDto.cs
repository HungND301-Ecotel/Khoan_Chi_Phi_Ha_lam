using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Catalog.LongwallMaterialUnitPrice
{
    public class LongwallMaterialUnitPriceExcelDto
    {
        public DefaultIdType Id { get; set; }
        [Display(Name = "Mã định mức vật liệu")]
        public string Code { get; set; } = string.Empty;
        [Display(Name = "Công đoạn sản xuất")]
        public string ProcessName { get; set; } = string.Empty;
        [Display(Name = "Công nghệ khai thác")]
        public string TechnologyName { get; set; } = string.Empty;
        [Display(Name = "Thông số lò chợ")]
        public string LongwallParametersName { get; set; } = string.Empty;
        [Display(Name = "Chiều dày lớp khấu (m)")]
        public string CuttingThicknessName { get; set; } = string.Empty;
        [Display(Name = "Mặt vỉa (m)")]
        public string SeamFaceName { get; set; } = string.Empty;
        [Display(Name = "Thời gian bắt đầu")]
        public string StartMonth { get; set; } = string.Empty;
        [Display(Name = "Thời gian kết thúc")]
        public string EndMonth { get; set; } = string.Empty;
        [Display(Name = "Tổng giá")]
        public double TotalPrice { get; set; }
    }
}
