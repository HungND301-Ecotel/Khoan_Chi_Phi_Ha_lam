using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Catalog.SlideUnitPrice
{
    public class SlideUnitPriceExcelDto
    {
        public DefaultIdType Id { get; set; }
        [Display(Name = "Mã định mức máng trượt")]
        public string Code { get; set; } = string.Empty;
        [Display(Name = "Nhóm công đoạn sản xuất")]
        public string ProcessGroupName { get; set; } = string.Empty;
        [Display(Name = "Hộ chiếu, Sđ, Sc")]
        public string PassportName { get; set; } = string.Empty;
        [Display(Name = "Độ kiên cố đá, than (f)")]
        public string HardnessName { get; set; } = string.Empty;
        [Display(Name = "Thời gian bắt đầu")]
        public string StartMonth { get; set; } = string.Empty;
        [Display(Name = "Thời gian kết thúc")]
        public string EndMonth { get; set; } = string.Empty;
    }
}
