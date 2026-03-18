namespace Application.Common.Repositories;

public interface IWriteRepositoryFactory
{
    IWriteRepository<TEntity> GetRepository<TEntity>(bool hasCustomRepository = false)
        where TEntity : class;
}