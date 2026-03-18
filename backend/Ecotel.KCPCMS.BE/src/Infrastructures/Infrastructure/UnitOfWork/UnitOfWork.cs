using System.Data;
using System.Text.RegularExpressions;
using System.Transactions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using IsolationLevel = System.Data.IsolationLevel;

namespace Infrastructure.UnitOfWork;

public class UnitOfWork<TContext> : IWriteRepositoryFactory, IUnitOfWork<TContext>
    where TContext : DbContext
{
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;
    private bool _disposed;
    public UnitOfWork(TContext context)
    {
        DbContext = context ?? throw new ArgumentNullException(nameof(context));
        _repositories = new Dictionary<Type, object>();
    }

    public TContext DbContext { get; }

    public void ChangeDatabase(string database)
    {
        var connection = DbContext.Database.GetDbConnection();
        if (connection.State.HasFlag(ConnectionState.Open))
        {
            connection.ChangeDatabase(database);
        }
        else
        {
            string connectionString = Regex.Replace(connection.ConnectionString.Replace(" ", string.Empty), @"(?<=[Dd]atabase=)\w+(?=;)", database, RegexOptions.Singleline);
            connection.ConnectionString = connectionString;
        }

        // Following code only working for mysql.
        var items = DbContext.Model.GetEntityTypes();
        foreach (var item in items)
        {
            if (item is IConventionEntityType entityType)
            {
                entityType.SetSchema(database);
            }
        }
    }

    public IWriteRepository<TEntity> GetRepository<TEntity>(bool hasCustomRepository = false)
        where TEntity : class
    {
        // what's the best way to support custom repository?
        if (hasCustomRepository)
        {
            var customRepo = DbContext.GetService<IWriteRepository<TEntity>>();
            if (customRepo != null)
            {
                return customRepo;
            }
        }

        var type = typeof(TEntity);
        if (_repositories.TryGetValue(type, out object? value))
        {
            return (IWriteRepository<TEntity>)value;
        }

        value = new WriteRepository<TEntity>(DbContext);
        _repositories[type] = value;

        return (IWriteRepository<TEntity>)value;
    }
    public int ExecuteSqlCommand(string sql, params object[] parameters) => DbContext.Database.ExecuteSqlRaw(sql, parameters);

    public IQueryable<TEntity> FromSql<TEntity>(string sql, params object[] parameters)
        where TEntity : class
        => DbContext.Set<TEntity>().FromSqlRaw(sql, parameters);

    public int SaveChanges(bool ensureAutoHistory = false)
    {
        if (ensureAutoHistory)
        {
            DbContext.EnsureAutoHistory();
        }

        return DbContext.SaveChanges();
    }

    public async Task<int> SaveChangesAsync(bool ensureAutoHistory = false)
    {
        if (ensureAutoHistory)
        {
            DbContext.EnsureAutoHistory();
        }

        return await DbContext.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync(bool ensureAutoHistory = false, params IUnitOfWork[] unitOfWorks)
    {
        using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        int count = 0;
        foreach (var unitOfWork in unitOfWorks)
        {
            count += await unitOfWork.SaveChangesAsync(ensureAutoHistory).ConfigureAwait(false);
        }

        count += await SaveChangesAsync(ensureAutoHistory);

        ts.Complete();

        return count;
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _repositories.Clear();
                DbContext.Dispose();
            }
        }

        _disposed = true;
    }

    public void TrackGraph(object rootEntity, Action<EntityEntryGraphNode> callback)
    {
        DbContext.ChangeTracker.TrackGraph(rootEntity, callback);
    }

    public IDbContextTransaction BeginTransaction()
    {
        return DbContext.Database.BeginTransaction();
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        _transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}