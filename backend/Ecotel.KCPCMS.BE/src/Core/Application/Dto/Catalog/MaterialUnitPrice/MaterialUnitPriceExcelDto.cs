using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Catalog.MaterialUnitPrice
{
    public class MaterialUnitPriceExcelDto
    {
        public DefaultIdType Id { get; set; }
        [Display(Name = "Mã định mức vật liệu")]
        public string Code { get; set; } = string.Empty;
        [Display(Name = "Công đoạn sản xuất")]
        public string ProcessName { get; set; } = string.Empty;
        [Display(Name = "Hộ chiếu, Sđ, Sc")]
        public string PassportName { get; set; } = string.Empty;
        [Display(Name = "Độ kiên cố đá, than (f)")]
        public string HardnessName { get; set; } = string.Empty;
        [Display(Name = "Chèn")]
        public string InsertItemName { get; set; } = string.Empty;
        [Display(Name = "Bước chống")]
        public string SupportStepName { get; set; } = string.Empty;
        [Display(Name = "Thời gian bắt đầu")]
        public string StartMonth { get; set; } = string.Empty;
        [Display(Name = "Thời gian kết thúc")]
        public string EndMonth { get; set; } = string.Empty;
        [Display(Name = "Đơn giá vật liệu (đ/m)")]
        public double TotalPrice { get; set; }
    }
}
