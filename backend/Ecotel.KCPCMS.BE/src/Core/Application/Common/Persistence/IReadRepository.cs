using Ardalis.Specification;
using Domain.Common.Contracts;

namespace Application.Common.Persistence;

public interface IReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot;