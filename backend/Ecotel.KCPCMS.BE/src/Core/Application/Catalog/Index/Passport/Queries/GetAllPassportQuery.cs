using Application.Catalog.Index.Passport.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.Passport;
using MediatR;

namespace Application.Catalog.Index.Passport.Queries;

public record class GetAllPassportQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<PassportDto>>;

public class GetAllPassportQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.Passport> passportRepository) : IRequestHandler<GetAllPassportQuery, PaginationResponse<PassportDto>>
{
    public async Task<PaginationResponse<PassportDto>> Handle(GetAllPassportQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            // Lấy toàn bộ rồi sắp xếp, cắt trang trong bộ nhớ — xem ToPaginationResponse
            IgnorePagination = true
        };

        var spec = new PassportsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: passportRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result = result.Data.OrderBy(d => d.CreateOn).ToPaginationResponse(request.PageIndex, request.PageSize, request.IgnorePagination);
        return result;
    }
}
