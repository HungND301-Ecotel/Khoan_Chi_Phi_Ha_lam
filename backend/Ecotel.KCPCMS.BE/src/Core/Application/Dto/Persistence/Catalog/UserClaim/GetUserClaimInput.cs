using Application.Common.Pagination;

namespace Application.Dto.Persistence.Catalog.UserClaim;

public class GetUserClaimInput : PagedInputDto
{
    public string KeySearch { get; set; } = string.Empty;
}