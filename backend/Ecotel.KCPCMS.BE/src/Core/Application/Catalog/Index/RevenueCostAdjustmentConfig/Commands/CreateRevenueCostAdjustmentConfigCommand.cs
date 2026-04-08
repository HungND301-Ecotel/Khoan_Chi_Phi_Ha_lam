using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.RevenueCostAdjustmentConfig;
using MediatR;
using RevenueCostAdjustmentConfigEntity = Domain.Entities.Index.RevenueCostAdjustmentConfig;

namespace Application.Catalog.Index.RevenueCostAdjustmentConfig.Commands;

public record CreateRevenueCostAdjustmentConfigCommand(CreateRevenueCostAdjustmentConfigDto CreateModel) : IRequest<bool>;

public class CreateRevenueCostAdjustmentConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateRevenueCostAdjustmentConfigCommand, bool>
{
    private readonly IWriteRepository<RevenueCostAdjustmentConfigEntity> _revenueCostAdjustmentConfigRepository = unitOfWork.GetRepository<RevenueCostAdjustmentConfigEntity>();

    public async Task<bool> Handle(CreateRevenueCostAdjustmentConfigCommand request, CancellationToken cancellationToken)
    {
        var newRevenueCostAdjustmentConfig = RevenueCostAdjustmentConfigEntity.Create(
            request.CreateModel.ProfitConditionDisplay,
            request.CreateModel.RateDisplay,
            request.CreateModel.Description);

        await _revenueCostAdjustmentConfigRepository.InsertAsync(newRevenueCostAdjustmentConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
