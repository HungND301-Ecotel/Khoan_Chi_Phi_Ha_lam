using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;
using RevenueCostAdjustmentConfigEntity = Domain.Entities.Index.RevenueCostAdjustmentConfig;

namespace Application.Catalog.Index.RevenueCostAdjustmentConfig.Commands;

public record DeleteRevenueCostAdjustmentConfigCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteRevenueCostAdjustmentConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteRevenueCostAdjustmentConfigCommand, bool>
{
    private readonly IWriteRepository<RevenueCostAdjustmentConfigEntity> _revenueCostAdjustmentConfigRepository = unitOfWork.GetRepository<RevenueCostAdjustmentConfigEntity>();

    public async Task<bool> Handle(DeleteRevenueCostAdjustmentConfigCommand request, CancellationToken cancellationToken)
    {
        var existRevenueCostAdjustmentConfig = await _revenueCostAdjustmentConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        _revenueCostAdjustmentConfigRepository.Delete(existRevenueCostAdjustmentConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
