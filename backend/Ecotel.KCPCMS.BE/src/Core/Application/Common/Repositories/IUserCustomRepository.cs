using Domain.Entities.Identity;

namespace Application.Common.Repositories;

public interface IUserCustomRepository : IWriteRepository<User>
{
    Task<int?> GetUserDesignationId(long userId);
}