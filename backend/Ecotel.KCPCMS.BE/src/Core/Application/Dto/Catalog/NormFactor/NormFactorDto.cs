using Application.Common.Interfaces;

namespace Application.Dto.Catalog.NormFactor;

public class CreateNormFactorDto
{
    public Guid ProductionProcessId { get; set; }
    public Guid HardnessId { get; set; }
    public Guid StoneClampRatioId { get; set; }
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

    public Guid HardnessId { get; set; }
    public string HardnessName { get; set; }

    public Guid StoneClampRatioId { get; set; }
    public string StoneClampRatioName { get; set; }

    public IList<Guid> AffectAssignmentCodeIds { get; set; }

    public double Value { get; set; }

    public Guid TargetHardnessId { get; set; }
    public string TargetHardnessName { get; set; }
}

public class UpdateNormFactorDto
{
    public Guid Id { get; set; }
    public Guid ProductionProcessId { get; set; }
    public Guid HardnessId { get; set; }
    public Guid StoneClampRatioId { get; set; }
    public double Value { get; set; }
    public IList<Guid> AssignmentCodeIds { get; set; }
    public Guid? TargetHardnessId { get; set; }
}
