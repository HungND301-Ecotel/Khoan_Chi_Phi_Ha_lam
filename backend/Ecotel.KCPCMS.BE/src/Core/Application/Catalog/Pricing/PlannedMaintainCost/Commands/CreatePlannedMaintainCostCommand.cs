using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.PlannedMaintainCost;
using Domain.Common.Enums;
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
    private readonly IWriteRepository<AdjustmentFactor> _adjustmentFactorRepository = unitOfWork.GetRepository<AdjustmentFactor>();
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

        var adjustmentFactorDescriptionIds = request.CreateModel.Costs
            .SelectMany(c => c.AdjustmentFactorDescriptions)
            .Select(c => c.AdjustmentFactorDescriptionId)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .Distinct()
            .ToList();
        var customAdjustmentFactorIds = request.CreateModel.Costs
            .SelectMany(c => c.AdjustmentFactorDescriptions)
            .Select(c => c.AdjustmentFactorId)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .Distinct()
            .ToList();

        if (!request.CreateModel.Costs.SelectMany(c => c.AdjustmentFactorDescriptions).Any())
        {
            throw new BadRequestException(CustomResponseMessage.AdjustmentFactorDescriptionEmpty);
        }

        ValidateAdjustmentFactorInputs(request.CreateModel.Costs.SelectMany(c => c.AdjustmentFactorDescriptions));

        var adjDescriptions = await _adjustmentFactorDescriptionRepository.GetAllAsync(
            predicate: p => adjustmentFactorDescriptionIds.Contains(p.Id),
            disableTracking: true);

        var adjMap = adjDescriptions.ToDictionary(a => a.Id, a => a);

        if (adjDescriptions.Count != adjustmentFactorDescriptionIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AdjustmentFactorDescriptionNotFound);
        }

        int countCustomAdjustmentFactors = await _adjustmentFactorRepository.CountAsync(
            p => customAdjustmentFactorIds.Contains(p.Id) && p.FixedKey != null && p.FixedKey.Type != FixedKeyType.K6);

        if (countCustomAdjustmentFactors != customAdjustmentFactorIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AdjustmentFactorNotFound);
        }

        var costs = request.CreateModel.Costs.Select(c => PlannedMaintainCostAdjustmentFactor.Create(
            Guid.Empty,
            c.MaintainUnitPriceId,
            c.Quantity,
            c.K6AdjustmentFactorValue,
            c.AdjustmentFactorDescriptions.Select(a => new PlannedMaintainAdjustmentFactorInput(
                a.AdjustmentFactorDescriptionId,
                a.AdjustmentFactorId,
                a.CustomValue,
                a.AdjustmentFactorDescriptionId.HasValue
                    ? adjMap.GetValueOrDefault(a.AdjustmentFactorDescriptionId.Value)
                    : null)).ToList())).ToList();

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

    private static void ValidateAdjustmentFactorInputs(IEnumerable<PlannedMaintainCostAdjustmentFactorValueDto> adjustmentFactors)
    {
        foreach (var adjustmentFactor in adjustmentFactors)
        {
            var hasAdjustmentFactorDescription = adjustmentFactor.AdjustmentFactorDescriptionId.HasValue;
            var hasCustomValue = adjustmentFactor.AdjustmentFactorId.HasValue || adjustmentFactor.CustomValue.HasValue;

            if (hasAdjustmentFactorDescription == hasCustomValue)
            {
                throw new BadRequestException(CustomResponseMessage.InvalidParams);
            }

            if (hasCustomValue && (!adjustmentFactor.AdjustmentFactorId.HasValue || !adjustmentFactor.CustomValue.HasValue))
            {
                throw new BadRequestException(CustomResponseMessage.InvalidParams);
            }
        }
    }
}
