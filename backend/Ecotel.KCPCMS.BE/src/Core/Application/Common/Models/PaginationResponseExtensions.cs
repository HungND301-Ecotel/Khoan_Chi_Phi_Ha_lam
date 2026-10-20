using Application.Common.Interfaces;
using Ardalis.Specification;

namespace Application.Common.Models;

public static class PaginationResponseExtensions
{
    public static async Task<PaginationResponse<TDestination>> PaginatedListAsync<T, TDestination>(
        this IReadRepositoryBase<T> repository, ISpecification<T, TDestination> spec, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        where T : class
        where TDestination : class, IDto
    {
        var list = await repository.ListAsync(spec, cancellationToken);
        int count = await repository.CountAsync(spec, cancellationToken);

        return new PaginationResponse<TDestination>(list, count, pageNumber, pageSize);
    }

    public static async Task<PaginationResponse<T>> PaginatedListAsync<T>(
        this IReadRepositoryBase<T> repository, ISpecification<T> spec, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        where T : class
    {
        var list = await repository.ListAsync(spec, cancellationToken);
        int count = await repository.CountAsync(spec, cancellationToken);

        return new PaginationResponse<T>(list, count, pageNumber, pageSize);
    }

    /// <summary>
    /// Cắt trang sau khi đã sắp xếp trong bộ nhớ trên TOÀN BỘ dữ liệu (VD: sắp mã tự nhiên không dịch
    /// được sang SQL). Tránh kiểu cắt trang theo LastModifiedOn rồi mới sắp trong trang — khiến bản ghi
    /// vừa sửa bị đẩy lên trang 1.
    /// </summary>
    public static PaginationResponse<T> ToPaginationResponse<T>(
        this IEnumerable<T> source, int pageNumber, int pageSize, bool ignorePagination)
    {
        var all = source.ToList();
        if (ignorePagination)
        {
            return new PaginationResponse<T>(all, all.Count, pageNumber, 99999);
        }

        var page = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PaginationResponse<T>(page, all.Count, pageNumber, pageSize);
    }
}