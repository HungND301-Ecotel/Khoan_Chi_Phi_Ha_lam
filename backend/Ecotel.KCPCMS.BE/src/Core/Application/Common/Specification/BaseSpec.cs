using Application.Common.Models;
using Ardalis.Specification;

namespace Application.Common.Specification;

public class BaseSpec<T> : Specification<T>
{
    protected ISpecificationBuilder<T> SpecBuilder => Query;

    protected BaseSpec(ISpecPayload? payload = null, bool? usePagination = null)
    {
        payload?.ApplyOperations(SpecBuilder, usePagination);
    }
}

public class BaseSpec<T, TResult> : Specification<T, TResult>
{
    protected ISpecificationBuilder<T, TResult> SpecBuilder => Query;

    protected BaseSpec(ISpecPayload? payload = null, bool? usePagination = null)
    {
        payload?.ApplyOperations(SpecBuilder, usePagination);
    }
}