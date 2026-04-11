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

public class UpdateDepartmentDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
}
