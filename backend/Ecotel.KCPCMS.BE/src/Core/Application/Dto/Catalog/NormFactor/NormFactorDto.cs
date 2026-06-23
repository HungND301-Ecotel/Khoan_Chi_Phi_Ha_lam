using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.NormFactor;

public class CreateNormFactorDto
{
    public Guid ProductionProcessId { get; set; }
    public Guid? HardnessId { get; set; }
    public Guid? StoneClampRatioId { get; set; }
    public SteelMeshType SteelMeshType { get; set; }
    public IList<NormFactorAssignmentCodeUpsertDto> AssignmentCodes { get; set; } = [];
}

public class UpdateNormFactorDto
{
    public Guid Id { get; set; }
    public Guid ProductionProcessId { get; set; }
    public Guid? HardnessId { get; set; }
    public Guid? StoneClampRatioId { get; set; }
    public SteelMeshType SteelMeshType { get; set; }
    public IList<NormFactorAssignmentCodeUpsertDto> AssignmentCodes { get; set; } = [];
}

public class NormFactorAssignmentCodeUpsertDto
{
    public Guid AssignmentCodeId { get; set; }
    public Guid MaterialId { get; set; }
    public double Value { get; set; }
    public Guid? TargetHardnessId { get; set; }
}

public class NormFactorDto : IDto
{
    public Guid Id { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; } = string.Empty;
    public string ProcessGroupName { get; set; } = string.Empty;
    public Guid ProductionProcessId { get; set; }
    public string ProductionProcessCode { get; set; } = string.Empty;
    public string ProductionProcessName { get; set; } = string.Empty;
    public Guid? HardnessId { get; set; }
    public string HardnessName { get; set; } = string.Empty;
    public Guid? StoneClampRatioId { get; set; }
    public string StoneClampRatioName { get; set; } = string.Empty;
    public SteelMeshType SteelMeshType { get; set; }
    public IList<AssignmentCode.ShortAssignmentCodeDto> AffectAssignmentCodes { get; set; } = [];
    public IList<NormFactorAssignmentCodeDto> AssignmentCodes { get; set; } = [];
}

public class NormFactorAssignmentCodeDto
{
    public Guid AssignmentCodeId { get; set; }
    public string AssignmentCode { get; set; } = string.Empty;
    public string AssignmentCodeName { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public double Value { get; set; }
    public Guid? TargetHardnessId { get; set; }
    public string TargetHardnessName { get; set; } = string.Empty;
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

    [Display(Name = "Nhóm vật tư, tài sản")]
    public string AssignmentCode { get; set; } = string.Empty;

    [Display(Name = "Mã - Tên vật tư")]
    public string MaterialCode { get; set; } = string.Empty;

    [Display(Name = "Hệ số điều chỉnh định mức")]
    public double Value { get; set; }

    [Display(Name = "Định mức tham chiếu")]
    public string TargetHardnessName { get; set; } = string.Empty;
}