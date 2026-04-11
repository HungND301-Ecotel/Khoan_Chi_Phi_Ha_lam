using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.PlannedMaintainCost;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.PlannedMaintainCost.Commands;
public record CreatePlannedMaintainCostCommand(CreatePlannedMaintainCostDto CreateModel) : IRequest<bool>;

public class CreatePlannedMaintainCostCommandHandler(
    ICacheService cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreatePlannedMaintainCostCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaintainCost> _plannedMaintainCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaintainCost>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<AdjustmentFactorDescription> _adjustmentFactorDescriptionRepository = unitOfWork.GetRepository<AdjustmentFactorDescription>();
    private readonly IWriteRepository<Domain.Entities.Pricing.MaintainUnitPrice> _maintainUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaintainUnitPrice>();
    public async Task<bool> Handle(CreatePlannedMaintainCostCommand request, CancellationToken cancellationToken)
    {
        var checkProductUnitPrice = await _productUnitPriceRepository.ExistsAsync(p => p.Id == request.CreateModel.ProductUnitPriceId);
        if (!checkProductUnitPrice)
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
        }

        bool checkOutput = await _outputRepository.ExistsAsync(p => p.Id == request.CreateModel.OutputId && p.OutputType == Domain.Common.Enums.OutputType.PlanOutput);
        if (!checkOutput)
        {
            throw new NotFoundException(CustomResponseMessage.OutputNotFound);
        }

        var maintainUnitPriceEquips = request.CreateModel.Costs.Select(c => c.MaintainUnitPriceId).ToList();
        int countMaintainUnitPriceEquip = await _maintainUnitPriceRepository.CountAsync(p => maintainUnitPriceEquips.Contains(p.Id));

        if (countMaintainUnitPriceEquip != maintainUnitPriceEquips.Count)
        {
            throw new NotFoundException(CustomResponseMessage.MaintainUnitPriceNotFound);
        }

        var adjustmentFactorIds = request.CreateModel.Costs
            .SelectMany(c => c.AdjustmentFactorDescriptions)
            .Distinct()
            .ToList();

        if (!adjustmentFactorIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.AdjustmentFactorDescriptionEmpty);
        }

        var adjDescriptions = await _adjustmentFactorDescriptionRepository.GetAllAsync(
            predicate: p => adjustmentFactorIds.Contains(p.Id),
            disableTracking: true);

        var adjMap = adjDescriptions.ToDictionary(a => a.Id, a => a);

        if (adjDescriptions.Count != adjustmentFactorIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AdjustmentFactorDescriptionNotFound);
        }

        var costs = request.CreateModel.Costs.Select(c => PlannedMaintainCostAdjustmentFactor.Create(Guid.Empty, c.MaintainUnitPriceId, c.Quantity, c.K6AdjustmentFactorValue, c.AdjustmentFactorDescriptions.Select(a => adjMap.GetValueOrDefault(a)).ToList())).ToList();

        var newPlannedMaterialCost = Domain.Entities.Pricing.PlannedMaintainCost.Create(
            request.CreateModel.ProductUnitPriceId,
            request.CreateModel.OutputId,
            request.CreateModel.TrimmingCoefficient,
            costs);
        await _plannedMaintainCostRepository.InsertAsync(newPlannedMaterialCost, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
