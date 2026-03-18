using Application.Common.Repositories;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using EfCore.Persistence.Context;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.CustomRepositories;

public class UserRoleCustomRepository(ApplicationDbContext context) : WriteRepository<UserRole>(context), IUserRoleCustomRepository
{
    public async Task<bool> CheckUserHasRole(long userId, RoleType roleType)
    {
        return await GetAll().AnyAsync(o => o.UserId == userId && o.RoleType == roleType);
    }

    public async Task<RoleType?> GetUserRoleType(long userId)
    {
        return await GetFirstOrDefaultAsync(
            predicate: o => o.UserId == userId,
            selector: o => o.Role!.RoleType);
    }
}