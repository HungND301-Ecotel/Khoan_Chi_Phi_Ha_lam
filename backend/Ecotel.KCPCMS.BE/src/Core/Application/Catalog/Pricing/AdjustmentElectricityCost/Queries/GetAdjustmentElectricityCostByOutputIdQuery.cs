using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentElectricityCost;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Domain.Common.Enums;
using Domain.Entities.Index;
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
    private readonly IWriteRepository<AkFactorConfig> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfig>();
    public async Task<AdjustmentElectricityCostDetailDto> Handle(GetAdjustmentElectricityCostByOutputIdQuery request, CancellationToken cancellationToken)
    {
        var plannedOutput = await _outputRepository.GetFirstOrDefaultAsync(
            predicate: o => o.Id == request.Id,
            include: o => o
                .Include(o => o.ProductUnitPrice).ThenInclude(p => p.Product)
                .Include(o => o.PlannedElectricityCost).ThenInclude(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.ElectricityUnitPriceEquipment).ThenInclude(e => e.Equipment).ThenInclude(e => e.Costs)
                .Include(o => o.PlannedElectricityCost).ThenInclude(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.ElectricityUnitPriceEquipment).ThenInclude(e => e.Equipment).ThenInclude(e => e.Code)
                .Include(o => o.PlannedElectricityCost).ThenInclude(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.PlannedElectricityCostAdjustmentFactorDescriptions).ThenInclude(p => p.AdjustmentFactorDescription).ThenInclude(p => p.AdjustmentFactor).ThenInclude(p => p.Code!)
                .Include(o => o.PlannedElectricityCost).ThenInclude(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.PlannedElectricityCostAdjustmentFactorDescriptions).ThenInclude(p => p.AdjustmentFactor).ThenInclude(p => p.Code!), disableTracking: true
            ) ?? throw new NotFoundException(CustomResponseMessage.PlannedOutputNotFound);

        var adjustmentOutputInfo = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ScenarioType == ProductUnitPriceScenarioType.Adjustment &&
                        p.ProductId == plannedOutput.ProductUnitPrice!.ProductId &&
                        p.DepartmentId == plannedOutput.ProductUnitPrice.DepartmentId)
            .SelectMany(p => p.ProductUnitPriceProductionOutputs)
            .Where(p => p.ProductionOutput!.StartMonth == plannedOutput.StartMonth &&
                        p.ProductionOutput.EndMonth == plannedOutput.EndMonth)
            .Select(p => new
            {
                p.ProductionMeters,
                ActualAshContent = p.ProductionOutput!.ProductionOutputProcessGroups
                    .SelectMany(g => g.ProductionOutputProducts)
                    .Where(pp => pp.ProductId == plannedOutput.ProductUnitPrice!.ProductId)
                    .Select(pp => (double?)pp.ActualAshContent)
                    .FirstOrDefault() ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (adjustmentOutputInfo == null || adjustmentOutputInfo.ProductionMeters <= 0)
        {
            throw new ConflictException(CustomResponseMessage.PleaseProvideTheActualOutputProductionMeters);
        }

        var akConfigs = await _akFactorConfigRepository.GetAll()
            .Where(x => x.ProcessGroupId == plannedOutput.ProductUnitPrice!.Product.ProcessGroupId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var hasAkConfigs = akConfigs.Any();
        var akDiff = hasAkConfigs
            ? (decimal)(plannedOutput.PlanAshContent - adjustmentOutputInfo.ActualAshContent)
            : 0;
        var akRate = hasAkConfigs ? AkFactorConfig.ResolveRate(akConfigs, akDiff) : 0;

        var plannedElectricityCost = plannedOutput.PlannedElectricityCost
            ?? throw new NotFoundException(CustomResponseMessage.PlannedElectricityCostNotFound);

        var mCost = new AdjustmentElectricityCostDetailDto
        {
            Id = plannedElectricityCost.Id,
            ProductUnitPriceId = plannedElectricityCost.ProductUnitPriceId,
            OutputId = plannedElectricityCost.OutputId,
            AkRate = (double)akRate,
            AkRatePercent = (double)akRate * 100,
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
                        Id = a.Id,
                        AdjustmentFactorDescriptionId = a.AdjustmentFactorDescriptionId,
                        AdjustmentFactorId = a.AdjustmentFactorDescription?.AdjustmentFactorId
                            ?? a.AdjustmentFactorId
                            ?? Guid.Empty,
                        AdjustmentFactorCode = a.AdjustmentFactorDescription?.AdjustmentFactor?.Code?.Value
                            ?? a.AdjustmentFactor?.Code?.Value
                            ?? string.Empty,
                        AdjustmentFactorName = a.AdjustmentFactorDescription?.AdjustmentFactor?.Name
                            ?? a.AdjustmentFactor?.Name
                            ?? string.Empty,
                        Description = a.AdjustmentFactorDescription?.Description ?? string.Empty,
                        ElectricityAdjustmentValue = a.AdjustmentFactorDescription?.ElectricityAdjustmentValue,
                        CustomValue = a.CustomValue,
                        EffectiveValue = a.EffectiveValue
                    }).ToList()
                };
            }).ToList()
        };
        return mCost;
    }
}
