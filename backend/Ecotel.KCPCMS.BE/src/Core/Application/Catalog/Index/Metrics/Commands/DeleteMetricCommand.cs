using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Metrics.Commands;
public record DeleteMetricCommand<TEntity>(DefaultIdType DeleteId) : IRequest<bool>
    where TEntity : class;

public class DeleteMetricCommandHandler<TEntity>(IUnitOfWork unitOfWork) : IRequestHandler<DeleteMetricCommand<TEntity>, bool>
    where TEntity : class
{
    private readonly IWriteRepository<TEntity> _metricRepository = unitOfWork.GetRepository<TEntity>();

    public async Task<bool> Handle(DeleteMetricCommand<TEntity> request, CancellationToken cancellationToken)
    {
        var existing = await _metricRepository.GetFirstOrDefaultAsync(
            predicate: e => EF.Property<DefaultIdType>(e, "Id") == request.DeleteId,
            disableTracking: true
        ) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        _metricRepository.Delete(existing);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
