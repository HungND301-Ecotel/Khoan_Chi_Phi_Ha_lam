using Application.Common.Specification;
using Ardalis.Specification;

#pragma warning disable S3060

namespace Application.Common.Models;

public interface ISpecPayload
{
    public void ApplyOperations<T>(ISpecificationBuilder<T> specBuilder, bool? usePagination)
    {
        if (this is IAdvancedFilter advancedFilter)
        {
            specBuilder.SearchBy(advancedFilter);
        }

        if (this is IPagination pagination)
        {
            if (usePagination is null or true && !pagination.IgnorePagination)
            {
                specBuilder.PaginateBy(pagination);
            }
        }

        ApplyOrderBy(specBuilder);
    }

    public void ApplyOperations<T, TResult>(ISpecificationBuilder<T, TResult> specBuilder, bool? usePagination)
    {
        if (this is IAdvancedFilter advancedFilter)
        {
            specBuilder.SearchBy(advancedFilter);
        }

        if (this is IPagination pagination)
        {
            if (usePagination is null or true && !pagination.IgnorePagination)
            {
                specBuilder.PaginateBy(pagination);
            }
        }

        ApplyOrderBy(specBuilder);
    }

    public void ApplyOrderBy<T>(ISpecificationBuilder<T> specBuilder)
    {
        if (this is IOrderBy orderBy)
        {
            var defaultOrderBy = orderBy.ForceUseCustomOrderBy
                ? new List<string>()
                : GetDefaultOrderBy<T>();
            specBuilder.OrderBy(orderBy, defaultOrderBy);
        }
    }

    public void ApplyOrderBy<T, TResult>(ISpecificationBuilder<T, TResult> specBuilder)
    {
        if (this is IOrderBy orderBy)
        {
            var defaultOrderBy = GetDefaultOrderBy<T>();
            specBuilder.OrderBy(orderBy, defaultOrderBy);
        }
    }

    private static List<string> GetDefaultOrderBy<T>()
    {
        List<string> defaultOrderBy = [];
        if (typeof(T).GetProperty("LastModifiedOn") != null)
        {
            defaultOrderBy.Add("LastModifiedOn desc");
        }

        if (typeof(T).GetProperty("Id") != null)
        {
            defaultOrderBy.Add("Id");
        }

        return defaultOrderBy;
    }
}