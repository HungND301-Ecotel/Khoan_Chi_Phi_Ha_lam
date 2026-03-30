using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.Metric
{
    public class MetricDto : IDto
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
    }

    public class HardnessExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Độ kiên cố than đá")]
        public string Value { get; set; }
    }

    public class PowerExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Công suất")]
        public string Value { get; set; }
    }

    public class SupportStepExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Bước chống")]
        public string Value { get; set; }
    }

    public class InsertItemExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Chèn")]
        public string Value { get; set; }
    }

    public class ProductioOrderExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Quyết định, lệnh sản xuất")]
        public string Value { get; set; }
    }

    public class TechnologyExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Công nghệ khai thác")]
        public string Value { get; set; }
    }

    public class SeamFaceExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mặt vỉa (M)")]
        public string Value { get; set; }
    }

    public class CreateMetricDto
    {
        public string Value { get; set; }
    }
}
