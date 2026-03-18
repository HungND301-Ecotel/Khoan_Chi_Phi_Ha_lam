using Application.Common.Models;

namespace Application.Common.Specification;

public class EntitiesByPaginationFilterSpec<T, TResult>(PaginationFilter payload)
    : BaseSpec<T, TResult>(payload);

public class EntitiesByPaginationFilterSpec<T>(PaginationFilter payload) : BaseSpec<T>(payload);