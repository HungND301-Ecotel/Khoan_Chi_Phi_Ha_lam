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
            IgnorePagination = request.IgnorePagination
        };

        var spec = new PassportsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: passportRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(d => d.CreateOn).ToList();
        return result;
    }
}
