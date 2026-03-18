using Application.Catalog.Index.AdjustmentFactor.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.AdjustmentFactor;
using MediatR;

namespace Application.Catalog.Index.AdjustmentFactor.Queries;
public record class GetAllAdjustmentFactorQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<AdjustmentFactorDto>>;

public class GetAllAdjustmentFactorQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.AdjustmentFactor> adjustmentFactorRepository) : IRequestHandler<GetAllAdjustmentFactorQuery, PaginationResponse<AdjustmentFactorDto>>
{
    public async Task<PaginationResponse<AdjustmentFactorDto>> Handle(GetAllAdjustmentFactorQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new AdjustmentFactorsByPaginationSpec(filter, request.Search);

        return await paginationService.PaginatedListAsync(
            repository: adjustmentFactorRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
    }
}
