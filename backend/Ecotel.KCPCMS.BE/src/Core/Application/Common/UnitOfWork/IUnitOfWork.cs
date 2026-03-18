using System.Data;
using Application.Common.Repositories;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Common.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    void ChangeDatabase(string database);

    IWriteRepository<TEntity> GetRepository<TEntity>(bool hasCustomRepository = false)
        where TEntity : class;
    int SaveChanges(bool ensureAutoHistory = false);
    Task<int> SaveChangesAsync(bool ensureAutoHistory = false);
    int ExecuteSqlCommand(string sql, params object[] parameters);
    IQueryable<TEntity> FromSql<TEntity>(string sql, params object[] parameters)
        where TEntity : class;
    void TrackGraph(object rootEntity, Action<EntityEntryGraphNode> callback);

    IDbContextTransaction BeginTransaction();

    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}