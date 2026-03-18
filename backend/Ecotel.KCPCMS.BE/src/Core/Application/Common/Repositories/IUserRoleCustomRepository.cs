using Domain.Common.Enums;
using Domain.Entities.Identity;

namespace Application.Common.Repositories;

public interface IUserRoleCustomRepository : IWriteRepository<UserRole>
{
    Task<bool> CheckUserHasRole(long userId, RoleType roleType);

    Task<RoleType?> GetUserRoleType(long userId);
}