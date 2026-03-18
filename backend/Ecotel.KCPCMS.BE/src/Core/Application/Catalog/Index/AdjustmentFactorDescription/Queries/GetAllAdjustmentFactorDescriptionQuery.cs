using Application.Catalog.Index.AdjustmentFactorDescription.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using MediatR;

namespace Application.Catalog.Index.AdjustmentFactorDescription.Queries;
public record class GetAllAdjustmentFactorDescriptionQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<AdjustmentFactorDescriptionDto>>;

public class GetAllAdjustmentFactorDescriptionQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.AdjustmentFactorDescription> adjustmentFactorDescription) : IRequestHandler<GetAllAdjustmentFactorDescriptionQuery, PaginationResponse<AdjustmentFactorDescriptionDto>>

{
    public async Task<PaginationResponse<AdjustmentFactorDescriptionDto>> Handle(GetAllAdjustmentFactorDescriptionQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new AdjustmentFactorDescriptionsByPaginationSpec(filter, request.Search);

        return await paginationService.PaginatedListAsync(
            repository: adjustmentFactorDescription,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
    }
}
