using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Dto.Catalog.AdjustmentMaintainCost;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.AdjustmnetMaintainCost.Queries;

public record GetAdjustmentMaintainCostByOutputIdQuery(DefaultIdType Id) : IRequest<AdjustmentMaintainCostDetailDto>;

public class GetAdjustmentMaintainCostByOutputIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdjustmentMaintainCostByOutputIdQuery, AdjustmentMaintainCostDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<AkFactorConfig> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfig>();
    public async Task<AdjustmentMaintainCostDetailDto> Handle(GetAdjustmentMaintainCostByOutputIdQuery request, CancellationToken cancellationToken)
    {
        var plannedOutput = await _outputRepository.GetFirstOrDefaultAsync(
            predicate: o => o.Id == request.Id,
            include: o => o
                .Include(o => o.ProductUnitPrice).ThenInclude(p => p.Product)
                .Include(o => o.PlannedMaintainCost).ThenInclude(o => o.PlannedMaintainCostAdjustmentFactors).ThenInclude(o => o.MaintainUnitPrice).ThenInclude(m => m.MaintainUnitPriceEquipments).ThenInclude(m => m.Part).ThenInclude(m => m.Costs)
                .Include(o => o.PlannedMaintainCost).ThenInclude(o => o.PlannedMaintainCostAdjustmentFactors).ThenInclude(o => o.MaintainUnitPrice).ThenInclude(muac => muac.MaintainUnitPriceEquipments).ThenInclude(m => m.Part).ThenInclude(p => p.Code)
                .Include(o => o.PlannedMaintainCost).ThenInclude(m => m.PlannedMaintainCostAdjustmentFactors).ThenInclude(p => p.MaintainUnitPrice).ThenInclude(m => m.Equipment).ThenInclude(e => e.Code!)
                .Include(o => o.PlannedMaintainCost).ThenInclude(m => m.PlannedMaintainCostAdjustmentFactors).ThenInclude(p => p.PlannedMaintainCostAdjustmentFactorDescriptions).ThenInclude(p => p.AdjustmentFactorDescription).ThenInclude(p => p.AdjustmentFactor).ThenInclude(p => p.Code!)
                .Include(o => o.PlannedMaintainCost).ThenInclude(m => m.PlannedMaintainCostAdjustmentFactors).ThenInclude(p => p.PlannedMaintainCostAdjustmentFactorDescriptions).ThenInclude(p => p.AdjustmentFactor).ThenInclude(p => p.Code!),
            disableTracking: true
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
        var adjustmentMultiplier = hasAkConfigs
            ? 1 + (double)(akDiff * akRate)
            : 1;

        var plannedMaintainCost = plannedOutput.PlannedMaintainCost
            ?? throw new NotFoundException(CustomResponseMessage.PlannedMaintainCostNotFound);

        var mCost = new AdjustmentMaintainCostDetailDto
        {
            Id = plannedMaintainCost.Id,
            ProductUnitPriceId = plannedMaintainCost.ProductUnitPriceId,
            OutputId = plannedMaintainCost.OutputId,
            AkRate = (double)akRate,
            AkRatePercent = (double)akRate * 100,
            Costs = plannedMaintainCost.PlannedMaintainCostAdjustmentFactors.Select(p =>
            {
                return new AdjustmentMaintainCostAdjDto
                {
                    EquipmentId = p.MaintainUnitPrice!.EquipmentId,
                    EquipmentCode = p.MaintainUnitPrice.Equipment?.Code?.Value ?? string.Empty,
                    EquipmentName = p.MaintainUnitPrice.Equipment?.Name ?? string.Empty,
                    Quantity = p.Quantity,
                    MaintainUnitPriceId = p.MaintainUnitPriceId,
                    MaintainUnitPrice = p.MaintainUnitPrice.GetMaintainTotalPrice(),
                    TotalPrice = p.GetCurrentMaintainCost() * adjustmentMultiplier,
                    K6AdjustmentFactorValue = p.K6AdjustmentFactorValue,
                    AdjustmentFactorDescriptions = p.PlannedMaintainCostAdjustmentFactorDescriptions.Select(a => new MaintainAjustmentFactorDescriptionDto
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
                        MaintenanceAdjustmentValue = a.AdjustmentFactorDescription?.MaintenanceAdjustmentValue,
                        CustomValue = a.CustomValue,
                        EffectiveValue = a.EffectiveValue
                    }).OrderBy(a => a.AdjustmentFactorCode).ToList()
                };
            }).ToList()
        };
        return mCost;
    }
}
