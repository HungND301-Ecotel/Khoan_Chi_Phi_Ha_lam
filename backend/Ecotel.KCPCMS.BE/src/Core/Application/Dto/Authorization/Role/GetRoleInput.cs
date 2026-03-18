using Application.Common.Pagination;

namespace Application.Dto.Authorization.Role;

public class GetRoleInput : PagedInputDto
{
    public string? KeySearch { get; set; } = string.Empty;
}