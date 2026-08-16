using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;

namespace Application.Catalog.Pricing.TransportPlanLine.Commands;

public record DeleteTransportPlanLineCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteTransportPlanLineCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : IRequestHandler<DeleteTransportPlanLineCommand, bool>
{
    private const string CacheSignalKey = "TransportPlanLine";
    private readonly IWriteRepository<TransportPlanLineEntity> _repository =
        unitOfWork.GetRepository<TransportPlanLineEntity>();

    public async Task<bool> Handle(DeleteTransportPlanLineCommand request, CancellationToken cancellationToken)
    {

        var existing = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.DeleteId,
            include: x => x
                .Include(x => x.PlannedTransportCost)
                    .ThenInclude(c => c!.AdjustmentFactors),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.TransportPlanLineNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _repository.Delete(existing);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            cacheService.InvalidateGroup(CacheSignalKey);

            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
