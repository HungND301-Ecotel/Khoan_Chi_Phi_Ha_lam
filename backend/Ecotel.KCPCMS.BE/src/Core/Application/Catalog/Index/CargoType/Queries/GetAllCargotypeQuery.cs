using Application.Catalog.Index.CargoType.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.CargoType;
using MediatR;

namespace Application.Catalog.Index.CargoType.Queries;

public record GetAllCargoTypeQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<CargoTypeDto>>;

public class GetAllCargoTypeQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.CargoType> cargoTypeRepository) : IRequestHandler<GetAllCargoTypeQuery, PaginationResponse<CargoTypeDto>>
{
    public async Task<PaginationResponse<CargoTypeDto>> Handle(GetAllCargoTypeQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination,
        };
        var spec = new CargoTypeByPaginationSpec(filter, request.Search);

        var rawList = await paginationService.PaginatedListAsync(
            repository: cargoTypeRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
        rawList.Data = rawList.Data.OrderBy(d => d.Name).ToList();

        return rawList;
    }
}