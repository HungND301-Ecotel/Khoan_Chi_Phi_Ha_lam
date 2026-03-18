using Application.Catalog.Production.ProductionOutputs.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.ProductionOutput;
using Domain.Entities.Production;
using MediatR;

namespace Application.Catalog.Production.ProductionOutputs.Queries;

public record GetAllProductionOutputsQuery(int PageIndex, int PageSize, bool IgnorePagination) : IRequest<PaginationResponse<ProductionOutputDto>>;

public class GetAllProductionOutputsQueryHandler(IPaginationService paginationService, IReadRepository<ProductionOutput> productionOutputRepository) : IRequestHandler<GetAllProductionOutputsQuery, PaginationResponse<ProductionOutputDto>>
{
    public async Task<PaginationResponse<ProductionOutputDto>> Handle(GetAllProductionOutputsQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new ProductionOutputsByPaginationSpec(filter);

        return await paginationService.PaginatedListAsync(
            repository: productionOutputRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
    }
}
