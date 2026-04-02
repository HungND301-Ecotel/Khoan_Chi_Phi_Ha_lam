using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.NormFactor;

public class CreateNormFactorDto
{
    public Guid ProductionProcessId { get; set; }
    public Guid? HardnessId { get; set; }
    public Guid StoneClampRatioId { get; set; }
    public SteelMeshType SteelMeshType { get; set; }
    public double Value { get; set; }
    public IList<Guid> AssignmentCodeIds { get; set; }
    public Guid? TargetHardnessId { get; set; }
}

public class NormFactorDto : IDto
{
    public Guid Id { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; }
    public string ProcessGroupName { get; set; }
    public Guid ProductionProcessId { get; set; }
    public string ProductionProcessCode { get; set; }
    public string ProductionProcessName { get; set; }

    public Guid? HardnessId { get; set; }
    public string HardnessName { get; set; }

    public Guid StoneClampRatioId { get; set; }
    public SteelMeshType SteelMeshType { get; set; }
    public string StoneClampRatioName { get; set; }

    public IList<AssignmentCode.ShortAssignmentCodeDto> AffectAssignmentCodes { get; set; }

    public double Value { get; set; }

    public Guid TargetHardnessId { get; set; }
    public string TargetHardnessName { get; set; }
}

public class UpdateNormFactorDto
{
    public Guid Id { get; set; }
    public Guid ProductionProcessId { get; set; }
    public Guid? HardnessId { get; set; }
    public Guid StoneClampRatioId { get; set; }
    public SteelMeshType SteelMeshType { get; set; }
    public double Value { get; set; }
    public IList<Guid> AssignmentCodeIds { get; set; }
    public Guid? TargetHardnessId { get; set; }
}

public class NormFactorExcelDto
{
    public Guid Id { get; set; }

    [Display(Name = "Công đoạn sản xuất")]
    public string ProductionProcessName { get; set; } = string.Empty;

    [Display(Name = "Độ kiên cố than đá (f)")]
    public string HardnessName { get; set; } = string.Empty;

    [Display(Name = "Lớp lưới thép")]
    public string SteelMeshTypeName { get; set; } = string.Empty;

    [Display(Name = "Tỷ lệ đá kẹp (Ckẹp)")]
    public string StoneClampRatioName { get; set; } = string.Empty;

    [Display(Name = "Thành phần điều chỉnh định mức")]
    public string AffectAssignmentCodes { get; set; } = string.Empty;

    [Display(Name = "Hệ số điều chỉnh định mức")]
    public double Value { get; set; }

    [Display(Name = "Định mức tham chiếu")]
    public string TargetHardnessName { get; set; } = string.Empty;
}
