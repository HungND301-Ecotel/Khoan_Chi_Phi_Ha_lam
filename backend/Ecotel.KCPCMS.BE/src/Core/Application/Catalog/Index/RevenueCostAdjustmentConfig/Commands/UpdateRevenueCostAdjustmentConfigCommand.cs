using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.RevenueCostAdjustmentConfig;
using MediatR;
using Shared.Constants;
using RevenueCostAdjustmentConfigEntity = Domain.Entities.Index.RevenueCostAdjustmentConfig;

namespace Application.Catalog.Index.RevenueCostAdjustmentConfig.Commands;

public record UpdateRevenueCostAdjustmentConfigCommand(RevenueCostAdjustmentConfigDto UpdateModel) : IRequest<bool>;

public class UpdateRevenueCostAdjustmentConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateRevenueCostAdjustmentConfigCommand, bool>
{
    private readonly IWriteRepository<RevenueCostAdjustmentConfigEntity> _revenueCostAdjustmentConfigRepository = unitOfWork.GetRepository<RevenueCostAdjustmentConfigEntity>();

    public async Task<bool> Handle(UpdateRevenueCostAdjustmentConfigCommand request, CancellationToken cancellationToken)
    {
        var existRevenueCostAdjustmentConfig = await _revenueCostAdjustmentConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        existRevenueCostAdjustmentConfig.Update(
            request.UpdateModel.ProfitConditionDisplay,
            request.UpdateModel.RateDisplay,
            request.UpdateModel.Description);

        _revenueCostAdjustmentConfigRepository.Update(existRevenueCostAdjustmentConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
