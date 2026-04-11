using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.PlannedElectricityCost;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Shared.Constants;

namespace Application.Catalog.Pricing.PlannedElectricityCost.Commands;

public record UpdatePlannedElectricityCostCommand(UpdatePlannedElectricityCostDto UpdateModel) : IRequest<bool>;

public class UpdatePlannedElectricityCostCommandHandler(
    ICacheService cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdatePlannedElectricityCostCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedElectricityCost> _plannedElectricityCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedElectricityCost>();
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedElectricityCostAdjustmentFactor> _plannedElectricityCostAdjustmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedElectricityCostAdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<AdjustmentFactorDescription> _adjustmentFactorDescriptionRepository = unitOfWork.GetRepository<AdjustmentFactorDescription>();
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _electricityUnitPriceEquipmentRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();

    private const string CacheSignalKey = "ProductUnitPrice";

    public async Task<bool> Handle(UpdatePlannedElectricityCostCommand request, CancellationToken cancellationToken)
    {
        var checkProductUnitPrice = await _productUnitPriceRepository.ExistsAsync(p => p.Id == request.UpdateModel.ProductUnitPriceId);
        if (!checkProductUnitPrice)
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
        }

        var plannedElectricityCost = await _plannedElectricityCostRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.UpdateModel.Id,
            include: p => p.Include(p => p.PlannedElectricityCostAdjustmentFactors),
            disableTracking: true
            ) ?? throw new NotFoundException(CustomResponseMessage.PlannedElectricityCostNotFound);

        bool checkOutput = await _outputRepository.ExistsAsync(p => p.Id == request.UpdateModel.OutputId && p.OutputType == Domain.Common.Enums.OutputType.PlanOutput);
        if (!checkOutput)
        {
            throw new NotFoundException(CustomResponseMessage.OutputNotFound);
        }

        var electricityUnitPriceEquipmentIds = request.UpdateModel.Costs.Select(c => c.ElectricityUnitPriceEquipmentId).ToList();
        int countElectricityUnitPriceEquip = await _electricityUnitPriceEquipmentRepository.CountAsync(p => electricityUnitPriceEquipmentIds.Contains(p.Id));

        if (countElectricityUnitPriceEquip != electricityUnitPriceEquipmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.ElectricityUnitPriceEquipmentNotFound);
        }

        var adjustmentFactorIds = request.UpdateModel.Costs
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

        var costs = request.UpdateModel.Costs.Select(c => PlannedElectricityCostAdjustmentFactor.Create(Guid.Empty, c.ElectricityUnitPriceEquipmentId, c.Quantity, c.AdjustmentFactorDescriptions.Select(a => adjMap.GetValueOrDefault(a)).ToList())).ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _plannedElectricityCostAdjustmentFactorRepository.Delete(plannedElectricityCost.PlannedElectricityCostAdjustmentFactors);

            plannedElectricityCost.ClearPlannedElectricityCostAdjustmentFactors();
            plannedElectricityCost.Update(
                request.UpdateModel.ProductUnitPriceId,
                request.UpdateModel.OutputId,
                request.UpdateModel.TrimmingCoefficient);
            plannedElectricityCost.AddPlannedElectricityCostAdjustmentFactors(costs);

            _plannedElectricityCostRepository.Update(plannedElectricityCost);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);
        return true;
    }
}
