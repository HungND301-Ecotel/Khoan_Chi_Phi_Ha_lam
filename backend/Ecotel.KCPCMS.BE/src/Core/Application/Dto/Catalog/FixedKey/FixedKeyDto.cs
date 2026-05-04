using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.FixedKey;

public class FixedKeyDto : IDto
{
    public Guid Id { get; set; }
    public string Key { get; set; }
    public string Name { get; set; }
    public ProcessGroupType Type { get; set; }
}

public class FixedKeyExcelDto
{
    public Guid Id { get; set; }

    [Display(Name = "Mã key cố định")]
    public string Key { get; set; }

    [Display(Name = "Tên key cố định")]
    public string Name { get; set; }

    [Display(Name = "Loại nghiệp vụ")]
    public ProcessGroupType Type { get; set; }
}

public class CreateFixedKeyDto
{
    public string Key { get; set; }
    public string Name { get; set; }
    public ProcessGroupType Type { get; set; }
}