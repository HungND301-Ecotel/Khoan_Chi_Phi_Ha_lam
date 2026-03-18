using Application.Catalog.Index.Part.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.Part;
using MediatR;

namespace Application.Catalog.Index.Part.Queries;

public record GetAllPartQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, DateTime Date) : IRequest<PaginationResponse<PartDto>>;

public class GetAllPartQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.Part> partRepository) : IRequestHandler<GetAllPartQuery, PaginationResponse<PartDto>>
{
    public async Task<PaginationResponse<PartDto>> Handle(GetAllPartQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination,
        };

        var spec = new PartsByPaginationSpec(filter, request.Search, request.Date);

        var rawList = await paginationService.PaginatedListAsync(
                    repository: partRepository,
                    spec: spec,
                    pageNumber: filter.PageNumber,
                    pageSize: filter.PageSize,
                    ignorePagination: filter.IgnorePagination,
                    cancellationToken: cancellationToken);
        rawList.Data = rawList.Data.OrderBy(a => a.EquipmentCode).ThenBy(a => a.Name).ToList();
        return rawList;
    }
}
