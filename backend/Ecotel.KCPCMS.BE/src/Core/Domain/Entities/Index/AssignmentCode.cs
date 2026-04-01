using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index;

public class AssignmentCode : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid CodeId { get; protected set; }
    public string Name { get; protected set; }
    public Guid? UnitOfMeasureId { get; protected set; }
    public bool IsSlideAssignmentCode { get; protected set; } = false;

    // Navigation properties
    public virtual UnitOfMeasure? UnitOfMeasure { get; protected set; }
    public virtual Code? Code { get; protected set; }

    private IList<Material> _materials = new List<Material>();
    public virtual IReadOnlyCollection<Material> Materials => _materials.AsReadOnly();
    private IList<MaterialUnitPriceAssignmentCode> _materialUnitPriceAssignmentCodes = new List<MaterialUnitPriceAssignmentCode>();
    public virtual IReadOnlyCollection<MaterialUnitPriceAssignmentCode> MaterialUnitPriceAssignmentCodes => _materialUnitPriceAssignmentCodes.AsReadOnly();

    private IList<NormFactorAssignmentCode> _normFactorAssignmentCodes = new List<NormFactorAssignmentCode>();
    public IReadOnlyList<NormFactorAssignmentCode> NormFactorAssignmentCodes => _normFactorAssignmentCodes.ToList();

    //constructor
    public static AssignmentCode Create(string name, string code, Guid? unitOfMeasureId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        return new AssignmentCode
        {
            Name = name,
            Code = new Code(code.ToUpper()),
            UnitOfMeasureId = unitOfMeasureId
        };
    }

    public static AssignmentCode Create(Guid id, string name, string code, Guid? unitOfMeasureId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        return new AssignmentCode
        {
            Id = id,
            Name = name,
            Code = new Code(code.ToUpper()),
            UnitOfMeasureId = unitOfMeasureId
        };
    }

    public void Update(string name, string code, Guid? unitOfMeasureId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        Name = name;
        if (Code != null)
        {
            Code.Value = code.ToUpper();
        }

        UnitOfMeasureId = unitOfMeasureId;
    }

    public bool CheckChange(AssignmentCode dto)
    {
        return !(Code?.Value == dto.Code?.Value && Name == dto.Name && UnitOfMeasureId == dto.UnitOfMeasureId);
    }
}
