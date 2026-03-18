using Application.Common.Persistence;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Domain.Common.Contracts;
using EfCore.Persistence.Context;
using Mapster;

namespace EfCore.Persistence.Repository;

// Inherited from Ardalis.Specification's RepositoryBase<T>
public class ApplicationDbRepository<T>(ApplicationDbContext dbContext)
    : RepositoryBase<T>(dbContext), IReadRepository<T>
    where T : class, IAggregateRoot
{
    // We override the default behavior when mapping to a dto.
    // We're using Mapster's ProjectToType here to immediately map the result from the database.
    // This is only done when no Selector is defined, so regular specifications with a selector also still work.
    protected override IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification) =>
        specification.Selector is not null
            ? base.ApplySpecification(specification)
            : ApplySpecification(specification, false)
                .ProjectToType<TResult>();
}