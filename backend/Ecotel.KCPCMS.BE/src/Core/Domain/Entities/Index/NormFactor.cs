using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Pricing;

namespace Domain.Entities.Index;

public class NormFactor : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ProductionProcessId { get; protected set; }
    public Guid? HardnessId { get; protected set; }
    public Guid? StoneClampRatioId { get; protected set; }
    public SteelMeshType SteelMeshType { get; protected set; } = SteelMeshType.None;
    public string? Note { get; protected set; }

    public virtual ProductionProcess? ProductionProcess { get; protected set; }
    public virtual Hardness? Hardness { get; protected set; }
    public virtual StoneClampRatio? StoneClampRatio { get; protected set; }

    private IList<PlannedMaterialCost> _plannedMaterialCosts = new List<PlannedMaterialCost>();
    public virtual IReadOnlyCollection<PlannedMaterialCost> PlannedMaterialCosts => _plannedMaterialCosts.AsReadOnly();

    private IList<NormFactorAssignmentCode> _normFactorAssignmentCodes = new List<NormFactorAssignmentCode>();
    public IReadOnlyList<NormFactorAssignmentCode> NormFactorAssignmentCodes => _normFactorAssignmentCodes.ToList();

    public static NormFactor Create(
        Guid productionProcessId,
        Guid? hardnessId,
        Guid? stoneClampRatioId,
        SteelMeshType steelMeshType = SteelMeshType.None,
        string? note = null)
    {
        return new NormFactor
        {
            ProductionProcessId = productionProcessId,
            HardnessId = hardnessId,
            StoneClampRatioId = stoneClampRatioId,
            SteelMeshType = steelMeshType,
            Note = NormalizeNote(note)
        };
    }

    public void AddNormFactorAssignmentCode(IList<NormFactorAssignmentCode> list)
    {
        _normFactorAssignmentCodes.Clear();
        foreach (var item in list)
        {
            _normFactorAssignmentCodes.Add(item);
        }
    }

    public void Update(
        Guid productionProcessId,
        Guid? hardnessId,
        Guid? stoneClampRatioId,
        SteelMeshType steelMeshType = SteelMeshType.None,
        string? note = null)
    {
        ProductionProcessId = productionProcessId;
        HardnessId = hardnessId;
        StoneClampRatioId = stoneClampRatioId;
        SteelMeshType = steelMeshType;
        Note = NormalizeNote(note);
    }

    public static string? NormalizeNote(string? note)
    {
        return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}