using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentElectricityCost;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.AdjustmentElectricityCost.Queries;

public record GetAdjustmentElectricityCostByOutputIdQuery(DefaultIdType Id) : IRequest<AdjustmentElectricityCostDetailDto>;

public class GetPlannedElectricityCostByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdjustmentElectricityCostByOutputIdQuery, AdjustmentElectricityCostDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    public async Task<AdjustmentElectricityCostDetailDto> Handle(GetAdjustmentElectricityCostByOutputIdQuery request, CancellationToken cancellationToken)
    {
        var plannedOutput = await _outputRepository.GetFirstOrDefaultAsync(
            predicate: o => o.Id == request.Id,
            include: o => o
                .Include(o => o.ProductUnitPrice)
                .Include(o => o.PlannedElectricityCost).ThenInclude(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.ElectricityUnitPriceEquipment).ThenInclude(e => e.Equipment).ThenInclude(e => e.Costs)
                .Include(o => o.PlannedElectricityCost).ThenInclude(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.ElectricityUnitPriceEquipment).ThenInclude(e => e.Equipment).ThenInclude(e => e.Code)
                .Include(o => o.PlannedElectricityCost).ThenInclude(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.PlannedElectricityCostAdjustmentFactorDescriptions).ThenInclude(p => p.AdjustmentFactorDescription).ThenInclude(p => p.AdjustmentFactor).ThenInclude(p => p.Code!), disableTracking: true
            ) ?? throw new NotFoundException(CustomResponseMessage.PlannedOutputNotFound);

        var adjustmentProductionMeters = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ScenarioType == ProductUnitPriceScenarioType.Adjustment &&
                        p.ProductId == plannedOutput.ProductUnitPrice!.ProductId)
            .SelectMany(p => p.ProductUnitPriceProductionOutputs)
            .Where(p => p.ProductionOutput!.StartMonth == plannedOutput.StartMonth &&
                        p.ProductionOutput.EndMonth == plannedOutput.EndMonth)
            .Select(p => p.ProductionMeters)
            .FirstOrDefaultAsync(cancellationToken);

        if (adjustmentProductionMeters <= 0)
        {
            throw new ConflictException(CustomResponseMessage.PleaseProvideTheActualOutputProductionMeters);
        }

        var plannedElectricityCost = plannedOutput.PlannedElectricityCost
            ?? throw new NotFoundException(CustomResponseMessage.PlannedElectricityCostNotFound);

        var mCost = new AdjustmentElectricityCostDetailDto
        {
            Id = plannedElectricityCost.Id,
            ProductUnitPriceId = plannedElectricityCost.ProductUnitPriceId,
            OutputId = plannedElectricityCost.OutputId,
            Costs = plannedElectricityCost.PlannedElectricityCostAdjustmentFactors.Select(p =>
            {
                return new AdjustmentElectricityCostAdjDto
                {
                    EquipmentId = p.ElectricityUnitPriceEquipment!.EquipmentId,
                    EquipmentCode = p.ElectricityUnitPriceEquipment.Equipment?.Code?.Value ?? string.Empty,
                    EquipmentName = p.ElectricityUnitPriceEquipment.Equipment?.Name ?? string.Empty,
                    Quantity = p.Quantity,
                    ElectricityUnitPriceEquipmentId = p.ElectricityUnitPriceId,
                    ElectricityUnitPrice = p.ElectricityUnitPriceEquipment.GetElectricityCostPerMetres(),
                    TotalPrice = p.GetCurrentElectricityCost(),
                    AdjustmentFactorDescriptions = p.PlannedElectricityCostAdjustmentFactorDescriptions.Select(a => new ElectricityAjustmentFactorDescriptionDto
                    {
                        Id = a?.AdjustmentFactorDescription?.Id ?? Guid.Empty,
                        AdjustmentFactorId = a.AdjustmentFactorDescription?.AdjustmentFactorId ?? Guid.Empty,
                        AdjustmentFactorCode = a.AdjustmentFactorDescription?.AdjustmentFactor?.Code?.Value ?? string.Empty,
                        AdjustmentFactorName = a.AdjustmentFactorDescription?.AdjustmentFactor?.Name ?? string.Empty,
                        Description = a.AdjustmentFactorDescription?.Description ?? "",
                        ElectricityAdjustmentValue = a.AdjustmentFactorDescription?.ElectricityAdjustmentValue ?? 0
                    }).ToList()
                };
            }).ToList()
        };
        return mCost;
    }
}