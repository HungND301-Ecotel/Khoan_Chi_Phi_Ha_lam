using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.StoneClampRatio;

public class StoneClampRatioDto : IDto
{
    public Guid Id { get; set; }
    public string Value { get; set; }
    public double CoefficientValue { get; set; }
    public Guid HardnessId { get; set; }
    public string HardnessValue { get; set; }
    public Guid ProcessId { get; set; }
    public string ProcessName { get; set; }
    public string ProcessCode { get; set; }
}

public class StoneClampRatioExcelDto
{
    public Guid Id { get; set; }
    [Display(Name = "Mã công đoạn sản xuất")]
    public string ProcessCode { get; set; }
    [Display(Name = "Độ kiên cố than, đá")]
    public string HardnessValue { get; set; }
    [Display(Name = "Tỷ lệ đá kẹp")]
    public string Value { get; set; }
    [Display(Name = "Hệ số điều chỉnh định mức")]
    public double CoefficientValue { get; set; }
}

public class CreateStoneClampRatioDto
{
    public string Value { get; set; }
    public double CoefficientValue { get; set; }
    public Guid HardnessId { get; set; }
    public Guid ProcessId { get; set; }
}

public class UpdateStoneClampRatioDto : CreateStoneClampRatioDto
{
    public Guid Id { get; set; }
}
