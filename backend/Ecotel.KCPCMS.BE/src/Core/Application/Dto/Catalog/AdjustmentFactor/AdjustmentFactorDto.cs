using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.AdjustmentFactor;

public class AdjustmentFactorDto : IDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid? FixedKeyId { get; set; }
    public string FixedKeyKey { get; set; }
    public FixedKeyType FixedKeyType { get; set; }
    public string Name { get; set; }
    public AdjustmentFactorType Type { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; }
    public string ProcessGroupName { get; set; }
}

public class AdjustmentFactorExcelDto
{
    public Guid Id { get; set; }
    public int Type { get; set; }
    [Display(Name = "Mã nhóm công đoạn sản xuất")]
    public string ProcessGroupCode { get; set; }
    [Display(Name = "Mã hệ số điều chỉnh")]
    public string Code { get; set; }
    [Display(Name = "Tên hệ số điều chỉnh")]
    public string Name { get; set; }
}

public class AdjustmentFactorDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid? FixedKeyId { get; set; }
    public string FixedKeyKey { get; set; }
    public FixedKeyType FixedKeyType { get; set; }
    public string Name { get; set; }
    public AdjustmentFactorType Type { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupName { get; set; }
    public IList<ShortAdjustmentFactorDescriptionDto> AdjustmentFactorDescriptions { get; set; } = new List<ShortAdjustmentFactorDescriptionDto>();
}

public class UpdateAdjustmentFactorDto
{
    public Guid Id { get; set; }
    public Guid FixedKeyId { get; set; }
    public string Name { get; set; }
    public Guid ProcessGroupId { get; set; }
}

public class CreateAdjustmentFactorDto
{
    public Guid FixedKeyId { get; set; }
    public string Name { get; set; }
    public Guid ProcessGroupId { get; set; }
}
