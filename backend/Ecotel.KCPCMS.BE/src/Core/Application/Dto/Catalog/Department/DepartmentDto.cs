using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.Department;

public class DepartmentDto : IDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
}

public class CreateDepartmentDto
{
    public string Code { get; set; }
    public string Name { get; set; }
}

public class DepartmentExcelDto
{
    public Guid Id { get; set; }

    [Display(Name = "Mã đơn vị")]
    public string Code { get; set; }

    [Display(Name = "Tên đơn vị")]
    public string Name { get; set; }
}

public class UpdateDepartmentDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
}
