using Application.Common.Repositories;
using Domain.Entities.Identity;
using EfCore.Persistence.Context;
using Infrastructure.Repositories;

namespace Infrastructure.CustomRepositories;

public class UserCustomRepository(ApplicationDbContext context) : WriteRepository<User>(context), IUserCustomRepository
{
    // TODO: Implement the method to retrieve user ID based on userId. Will change after
    public async Task<int?> GetUserDesignationId(long userId)
    {
        return await GetFirstOrDefaultAsync(
            predicate: o => o.Id == userId,
            selector: o => o.Id);
    }
}