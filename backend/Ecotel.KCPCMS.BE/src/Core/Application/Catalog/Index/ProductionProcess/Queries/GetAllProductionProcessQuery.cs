using Application.Catalog.Index.ProductionProcess.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.ProductionProcess;
using MediatR;

namespace Application.Catalog.Index.ProductionProcess.Queries;
public record class GetAllProductionProcessQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<ProductionProcessDto>>;

public class GetAllProductionProcessQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.ProductionProcess> processGroupRepository) : IRequestHandler<GetAllProductionProcessQuery, PaginationResponse<ProductionProcessDto>>
{
    public async Task<PaginationResponse<ProductionProcessDto>> Handle(GetAllProductionProcessQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };
        var spec = new ProductionProcessesByPaginationSpec(filter, request.Search);

        return await paginationService.PaginatedListAsync(
            repository: processGroupRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
    }
}

