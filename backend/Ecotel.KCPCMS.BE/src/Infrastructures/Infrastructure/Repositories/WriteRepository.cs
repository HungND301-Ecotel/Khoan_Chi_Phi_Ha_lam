using System.Linq.Expressions;
using System.Reflection;
using Application.Common.Repositories;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.

#pragma warning disable CS8603 // Possible null reference return.

#pragma warning disable CS8604 // Possible null reference argument.

namespace Infrastructure.Repositories;

public class WriteRepository<TEntity> : RepositoryBase<TEntity>, IWriteRepository<TEntity>
    where TEntity : class
{
    private readonly DbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    public WriteRepository(DbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = _dbContext.Set<TEntity>();
    }
    public void ChangeTable(string table)
    {
        if (_dbContext.Model.FindEntityType(typeof(TEntity)) is IConventionEntityType relational)
        {
            relational.SetTableName(table);
        }
    }
    public IQueryable<TEntity> GetAll()
    {
        return _dbSet;
    }

    public IQueryable<TEntity> GetAll(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return orderBy(query);
        }

        return query;
    }
    public IQueryable<TResult> GetAll<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return orderBy(query).Select(selector);
        }

        return query.Select(selector);
    }

    public List<TEntity> GetPaging(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int pageIndex = 0,
        int pageSize = 20,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return orderBy(query).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        }

        return query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
    }
    public async Task<List<TEntity>> GetPagingAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int pageIndex = 0,
        int pageSize = 20,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return await orderBy(query).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        }

        return await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }

    public List<TResult> GetPagingSelector<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int pageIndex = 0,
        int pageSize = 20,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
        where TResult : class
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return orderBy(query).Select(selector).ToList();
        }

        return query.Select(selector).ToList();
    }

    public async Task<List<TResult>> GetPagingSelectorAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int pageIndex = 0,
        int pageSize = 20,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return await orderBy(query).Select(selector).ToListAsync(cancellationToken);
        }

        return await query.Select(selector).ToListAsync(cancellationToken);
    }
    public TEntity? GetFirstOrDefault(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return orderBy(query).FirstOrDefault();
        }

        return query.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return await orderBy(query).FirstOrDefaultAsync();
        }

        return await query.FirstOrDefaultAsync();
    }
    public TResult GetFirstOrDefault<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return orderBy(query).Select(selector).FirstOrDefault();
        }

        return query.Select(selector).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<TResult> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return await orderBy(query).Select(selector).FirstOrDefaultAsync();
        }

        return await query.Select(selector).FirstOrDefaultAsync();
    }

    public IQueryable<TEntity> FromSql(string sql, params object[] parameters) => _dbSet.FromSqlRaw(sql, parameters);

    public TEntity Find(params object[] keyValues) => _dbSet.Find(keyValues);

    public ValueTask<TEntity> FindAsync(params object[] keyValues) => _dbSet.FindAsync(keyValues);

    public ValueTask<TEntity> FindAsync(object[] keyValues, CancellationToken cancellationToken)
        => _dbSet.FindAsync(keyValues, cancellationToken);

    public int Count(Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return _dbSet.Count();
        }

        return _dbSet.Count(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _dbSet.CountAsync();
        }

        return await _dbSet.CountAsync(predicate);
    }

    public long LongCount(Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return _dbSet.LongCount();
        }

        return _dbSet.LongCount(predicate);
    }

    public async Task<long> LongCountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _dbSet.LongCountAsync();
        }

        return await _dbSet.LongCountAsync(predicate);
    }

    public T Max<T>(Expression<Func<TEntity, bool>>? predicate = null, Expression<Func<TEntity, T>>? selector = null)
    {
        if (predicate == null)
        {
            return _dbSet.Max(selector);
        }

        return _dbSet.Where(predicate).Max(selector);
    }

    public async Task<T> MaxAsync<T>(Expression<Func<TEntity, bool>>? predicate = null, Expression<Func<TEntity, T>>? selector = null)
    {
        if (predicate == null)
        {
            return await _dbSet.MaxAsync(selector);
        }

        return await _dbSet.Where(predicate).MaxAsync(selector);
    }

    public T Min<T>(Expression<Func<TEntity, bool>>? predicate = null, Expression<Func<TEntity, T>>? selector = null)
    {
        if (predicate == null)
        {
            return _dbSet.Min(selector);
        }

        return _dbSet.Where(predicate).Min(selector);
    }

    public async Task<T> MinAsync<T>(Expression<Func<TEntity, bool>>? predicate = null, Expression<Func<TEntity, T>>? selector = null)
    {
        if (predicate == null)
        {
            return await _dbSet.MinAsync(selector);
        }

        return await _dbSet.Where(predicate).MinAsync(selector);
    }

    public decimal Average(Expression<Func<TEntity, bool>>? predicate = null, Expression<Func<TEntity, decimal>>? selector = null)
    {
        if (predicate == null)
        {
            return _dbSet.Average(selector);
        }

        return _dbSet.Where(predicate).Average(selector);
    }

    public async Task<decimal> AverageAsync(Expression<Func<TEntity, bool>>? predicate = null, Expression<Func<TEntity, decimal>>? selector = null)
    {
        if (predicate == null)
        {
            return await _dbSet.AverageAsync(selector);
        }

        return await _dbSet.Where(predicate).AverageAsync(selector);
    }

    public decimal Sum(Expression<Func<TEntity, bool>>? predicate = null, Expression<Func<TEntity, decimal>>? selector = null)
    {
        if (predicate == null)
        {
            return _dbSet.Sum(selector);
        }

        return _dbSet.Where(predicate).Sum(selector);
    }

    public async Task<decimal> SumAsync(Expression<Func<TEntity, bool>>? predicate = null, Expression<Func<TEntity, decimal>>? selector = null)
    {
        if (predicate == null)
        {
            return await _dbSet.SumAsync(selector);
        }

        return await _dbSet.Where(predicate).SumAsync(selector);
    }

    public bool Exists(Expression<Func<TEntity, bool>>? selector = null)
    {
        if (selector == null)
        {
            return _dbSet.Any();
        }

        return _dbSet.Any(selector);
    }

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>>? selector = null)
    {
        if (selector == null)
        {
            return await _dbSet.AnyAsync();
        }

        return await _dbSet.AnyAsync(selector);
    }

    public TEntity Insert(TEntity entity)
    {
        return _dbSet.Add(entity).Entity;
    }

    public void Insert(params TEntity[] entities) => _dbSet.AddRange(entities);

    public void Insert(IEnumerable<TEntity> entities) => _dbSet.AddRange(entities);

    public ValueTask<EntityEntry<TEntity>> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return _dbSet.AddAsync(entity, cancellationToken);
    }

    public Task InsertAsync(params TEntity[] entities) => _dbSet.AddRangeAsync(entities);

    public Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => _dbSet.AddRangeAsync(entities, cancellationToken);

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Update(params TEntity[] entities) => _dbSet.UpdateRange(entities);

    public void Update(IEnumerable<TEntity> entities) => _dbSet.UpdateRange(entities);

    public void Delete(TEntity entity) => _dbSet.Remove(entity);

    public void Delete(object id)
    {
        // using a stub entity to mark for deletion
        var typeInfo = typeof(TEntity).GetTypeInfo();
        var key = _dbContext.Model.FindEntityType(typeInfo).FindPrimaryKey().Properties.FirstOrDefault();
        var property = typeInfo.GetProperty(key!.Name);
        if (property != null)
        {
            var entity = Activator.CreateInstance<TEntity>();
            property.SetValue(entity, id);
            _dbContext.Entry(entity).State = EntityState.Deleted;
        }
        else
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
            {
                Delete(entity);
            }
        }
    }

    public void Delete(params TEntity[] entities) => _dbSet.RemoveRange(entities);

    public void Delete(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);

    public async Task<IList<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IList<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return await orderBy(query).ToListAsync();
        }

        return await query.ToListAsync().ConfigureAwait(false);
    }

    public async Task<IList<TResult>> GetAllAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (orderBy != null)
        {
            return await orderBy(query).Select(selector).ToListAsync();
        }

        return await query.Select(selector).ToListAsync();
    }
    public void ChangeEntityState(TEntity entity, EntityState state)
    {
        _dbContext.Entry(entity).State = state;
    }

    public bool Any(Expression<Func<TEntity, bool>> predicate) => _dbSet.Any(predicate);

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate) => await _dbSet.AnyAsync(predicate);

    public IQueryable<TEntity> Table()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<List<TResult>> SqlQueryAsync<TResult>(string sql, CancellationToken cancellationToken = default, params object[] parameters)
    {
        return await _dbContext.Database.SqlQueryRaw<TResult>(sql, parameters).ToListAsync(cancellationToken);
    }

    public async Task<TResult?> SqlQueryFirstOrDefaultAsync<TResult>(string sql, CancellationToken cancellationToken = default, params object[] parameters)
    {
        return await _dbContext.Database.SqlQueryRaw<TResult>(sql, parameters).FirstOrDefaultAsync(cancellationToken);
    }

    protected override IQueryable<TResult> ApplySpecification<TResult>(ISpecification<TEntity, TResult> specification) =>
        specification.Selector is not null
       ? base.ApplySpecification(specification)
       : ApplySpecification(specification, false).ProjectToType<TResult>();
}