using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Metrics.Queries;
public record GetMetricByIdQuery<TEntity>(DefaultIdType Id) : IRequest<MetricDto>
    where TEntity : class;

public class GetMetricByIdQueryHandler<TEntity>(IUnitOfWork unitOfWork) : IRequestHandler<GetMetricByIdQuery<TEntity>, MetricDto>
    where TEntity : class
{
    private readonly IWriteRepository<TEntity> _metricRepository = unitOfWork.GetRepository<TEntity>();
    public async Task<MetricDto> Handle(GetMetricByIdQuery<TEntity> request, CancellationToken cancellationToken)
    {
        var entity = await _metricRepository.GetFirstOrDefaultAsync(
            predicate: e => EF.Property<DefaultIdType>(e, "Id") == request.Id,
            disableTracking: true
        ) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return entity.Adapt<MetricDto>();
    }
}
