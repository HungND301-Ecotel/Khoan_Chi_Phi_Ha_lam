using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.PlannedElectricityCost;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.PlannedElectricityCost.Commands;

public record CreatePlannedElectricityCostCommand(CreatePlannedElectricityCostDto CreateModel) : IRequest<bool>;

public class CreatePlannedElectricityCostCommandHandler(
ICacheService cacheService,
IUnitOfWork unitOfWork) : IRequestHandler<CreatePlannedElectricityCostCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedElectricityCost> _plannedElectricityCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedElectricityCost>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<AdjustmentFactorDescription> _adjustmentFactorDescriptionRepository = unitOfWork.GetRepository<AdjustmentFactorDescription>();
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _electricityUnitPriceEquipmentRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();

    private const string CacheSignalKey = "ProductUnitPrice";

    public async Task<bool> Handle(CreatePlannedElectricityCostCommand request, CancellationToken cancellationToken)
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

        var electricityUnitPriceEquips = request.CreateModel.Costs.Select(c => c.ElectricityUnitPriceEquipmentId).ToList();
        int countElectricityUnitPriceEquip = await _electricityUnitPriceEquipmentRepository.CountAsync(p => electricityUnitPriceEquips.Contains(p.Id));

        if (countElectricityUnitPriceEquip != electricityUnitPriceEquips.Count)
        {
            throw new NotFoundException(CustomResponseMessage.ElectricityUnitPriceEquipmentNotFound);
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

        var costs = request.CreateModel.Costs.Select(c => PlannedElectricityCostAdjustmentFactor.Create(Guid.Empty, c.ElectricityUnitPriceEquipmentId, c.Quantity, c.AdjustmentFactorDescriptions.Select(a => adjMap.GetValueOrDefault(a)).ToList())).ToList();

        var newPlannedMaterialCost = Domain.Entities.Pricing.PlannedElectricityCost.Create(
            request.CreateModel.ProductUnitPriceId,
            request.CreateModel.OutputId,
            request.CreateModel.TrimmingCoefficient,
            costs);
        await _plannedElectricityCostRepository.InsertAsync(newPlannedMaterialCost, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
