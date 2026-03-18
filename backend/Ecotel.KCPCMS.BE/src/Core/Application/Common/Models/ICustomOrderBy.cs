using System.Linq.Expressions;

namespace Application.Common.Models;

public interface ICustomOrderBy<T> : IOrderBy
{
    public Dictionary<string, Expression<Func<T, object?>>> CustomOrderBy { get; }
}