using Domain.Common.Enums;

namespace Application.Dto.Authorization.Role;

public class ShortRoleDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public RoleType RoleType { get; set; }
}