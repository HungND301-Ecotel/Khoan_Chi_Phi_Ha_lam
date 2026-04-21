using Application.Catalog.Pricing.Common;
using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Queries;

public record GetAdjustmentProductUnitPriceByIdQuery(DefaultIdType Id) : IRequest<AdjustmentProductUnitPriceDetailDto>;

public class GetAdjustmentProductUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<GetAdjustmentProductUnitPriceByIdQuery, AdjustmentProductUnitPriceDetailDto>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaterialCost> _plannedMaterialCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaterialCost>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<PlannedMaintainCostAdjustmentFactor> _plannedMaintainFactorRepository = unitOfWork.GetRepository<PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<PlannedElectricityCostAdjustmentFactor> _plannedElectricityFactorRepository = unitOfWork.GetRepository<PlannedElectricityCostAdjustmentFactor>();
    private readonly IWriteRepository<AkFactorConfig> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfig>();

    public async Task<AdjustmentProductUnitPriceDetailDto> Handle(GetAdjustmentProductUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheSignalKey}:Adjustment:{request.Id}";

        var cachedResult = await cacheService.GetAsync<AdjustmentProductUnitPriceDetailDto>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        // STEP 1: Get base adjustment ProductUnitPrice info with ProductionOutputs from many-to-many relationship
        var baseData = await _productUnitPriceRepository.GetAll()
            .Where(e => e.Id == request.Id && e.ScenarioType == ProductUnitPriceScenarioType.Adjustment)
            .Select(p => new
            {
                Id = p.Id,
                ProductId = p.ProductId,
                ProductName = p.Product!.Name,
                ProductCode = p.Product.Code.Value,
                UnitOfMeasureId = p.UnitOfMeasureId,
                UnitOfMeasureName = p.UnitOfMeasure!.Name,
                DepartmentId = p.DepartmentId,
                DepartmentCode = p.Department != null && p.Department.Code != null ? p.Department.Code.Value : null,
                DepartmentName = p.Department != null ? p.Department.Name : null,
                ProcessGroupId = p.Product.ProcessGroupId,
                ProcessGroupCode = p.Product.ProcessGroup!.Code.Value,
                ProcessGroupName = p.Product.ProcessGroup.Name,
                ProcessGroupType = p.Product.ProcessGroup.Type,
                ProductionOutputs = p.ProductUnitPriceProductionOutputs
                    .Select(po => new
                    {
                        Id = po.ProductionOutputId,
                        ProductionOutputId = po.ProductionOutputId,
                        StartMonth = po.ProductionOutput!.StartMonth,
                        EndMonth = po.ProductionOutput.EndMonth,
                        ProductionMeters = po.ProductionMeters,
                        StandardProductionMeters = po.ProductionOutput.StandardProductionMeters,
                        ActualAshContent = po.ProductionOutput.ProductionOutputProcessGroups
                            .SelectMany(g => g.ProductionOutputProducts)
                            .Where(pp => pp.ProductId == p.ProductId)
                            .Select(pp => (double?)pp.ActualAshContent)
                            .FirstOrDefault() ?? 0
                    })
                    .ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (baseData == null)
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
        }

        var planMeta = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ProductId == baseData.ProductId
                && p.ScenarioType == ProductUnitPriceScenarioType.Plan
                && p.DepartmentId == baseData.DepartmentId)
            .Select(p => new
            {
                p.UnitOfMeasureId,
                UnitOfMeasureName = p.UnitOfMeasure != null ? p.UnitOfMeasure.Name : null,
                p.DepartmentId,
                DepartmentCode = p.Department != null && p.Department.Code != null ? p.Department.Code.Value : null,
                DepartmentName = p.Department != null ? p.Department.Name : null
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var effectiveUnitOfMeasureId = baseData.UnitOfMeasureId ?? planMeta?.UnitOfMeasureId;
        var effectiveUnitOfMeasureName = baseData.UnitOfMeasureName ?? planMeta?.UnitOfMeasureName ?? string.Empty;
        var effectiveDepartmentId = baseData.DepartmentId ?? planMeta?.DepartmentId;
        var effectiveDepartmentCode = baseData.DepartmentCode ?? planMeta?.DepartmentCode ?? string.Empty;
        var effectiveDepartmentName = baseData.DepartmentName ?? planMeta?.DepartmentName ?? string.Empty;

        // STEP 2: Get planned outputs with cost data from plan ProductUnitPrice
        var plannedOutputsRaw = await _outputRepository.GetAll()
            .Where(o => o.OutputType == OutputType.PlanOutput
                && o.ProductUnitPrice!.ProductId == baseData.ProductId
                && o.ProductUnitPrice.ScenarioType == ProductUnitPriceScenarioType.Plan
                && o.ProductUnitPrice.DepartmentId == baseData.DepartmentId)
            .Select(o => new OutputRawData
            {
                Id = o.Id,
                ProductUnitPriceId = o.ProductUnitPriceId,
                StartMonth = o.StartMonth,
                EndMonth = o.EndMonth,
                ProductionMeters = o.ProductionMeters,
                PlanAshContent = o.PlanAshContent,
                PlannedMaterialCostId = o.PlannedMaterialCost != null ? o.PlannedMaterialCost.Id : (Guid?)null,
                PlannedMaintainCostId = o.PlannedMaintainCost != null ? o.PlannedMaintainCost.Id : (Guid?)null,
                PlannedElectricityCostId = o.PlannedElectricityCost != null ? o.PlannedElectricityCost.Id : (Guid?)null
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var plannedOutputs = plannedOutputsRaw.Select(o => new AdjustmentPlannedOutputDto
        {
            Id = o.Id,
            ProductUnitPriceId = o.ProductUnitPriceId,
            OutputType = OutputType.PlanOutput,
            StartMonth = o.StartMonth,
            EndMonth = o.EndMonth,
            ProductionMeters = o.ProductionMeters,
            PlanAshContent = o.PlanAshContent
        }).ToList();

        // STEP 3: Load planned cost data
        var plannedMaintainCostIds = plannedOutputsRaw.Where(o => o.PlannedMaintainCostId.HasValue).Select(o => o.PlannedMaintainCostId!.Value).ToList();
        var plannedElectricityCostIds = plannedOutputsRaw.Where(o => o.PlannedElectricityCostId.HasValue).Select(o => o.PlannedElectricityCostId!.Value).ToList();
        var plannedMaterialCostIds = plannedOutputsRaw.Where(o => o.PlannedMaterialCostId.HasValue).Select(o => o.PlannedMaterialCostId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialCosts = await LoadPlannedMaterialCosts(plannedMaterialCostIds, cancellationToken);
        var akConfigs = await _akFactorConfigRepository.GetAll()
            .Where(x => x.ProcessGroupId == baseData.ProcessGroupId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // STEP 4: Calculate AdjTotalPrice for each ProductionOutput
        var productionOutputsWithAdj = baseData.ProductionOutputs.Select(po =>
        {
            var adjTotalPrice = 0.0;
            var akRateValue = 0.0;

            var plannedOutput = plannedOutputsRaw
                .Where(o => o.StartMonth <= po.StartMonth && o.EndMonth >= po.EndMonth)
                .OrderBy(o => o.StartMonth)
                .ThenBy(o => o.EndMonth)
                .FirstOrDefault();

            if (plannedOutput != null)
            {
                // Calculate unit costs from planned output
                var plannedMaterialCost = CalculatePlannedMaterialCost(plannedOutput, plannedMaterialCosts);
                var plannedMaintainCost = CalculateMaintainCost(plannedOutput.PlannedMaintainCostId, plannedMaintainFactors);
                var plannedElectricityCost = CalculatePlannedElectricityCost(plannedOutput.PlannedElectricityCostId, plannedElectricityFactors);
                var akDiff = (decimal)(po.ActualAshContent - plannedOutput.PlanAshContent);
                var akRate = ResolveAkRate(akConfigs, akDiff);
                akRateValue = (double)akRate;
                adjTotalPrice = po.ProductionMeters * (plannedMaterialCost * (1 + (double)akRate) + plannedMaintainCost + plannedElectricityCost);
            }

            return new AdjustmentProductionOutputDto
            {
                Id = po.Id,
                ProductionOutputId = po.ProductionOutputId,
                StartMonth = po.StartMonth,
                EndMonth = po.EndMonth,
                ProductionMeters = po.ProductionMeters,
                StandardProductionMeters = po.StandardProductionMeters,
                ActualAshContent = po.ActualAshContent,
                AkRate = akRateValue,
                AkRatePercent = akRateValue * 100,
                AdjTotalPrice = adjTotalPrice
            };
        }).ToList();

        var result = new AdjustmentProductUnitPriceDetailDto
        {
            Id = baseData.Id,
            ProductId = baseData.ProductId,
            ProductName = baseData.ProductName,
            ProductCode = baseData.ProductCode,
            UnitOfMeasureId = effectiveUnitOfMeasureId,
            UnitOfMeasureName = effectiveUnitOfMeasureName,
            DepartmentId = effectiveDepartmentId,
            DepartmentCode = effectiveDepartmentCode,
            DepartmentName = effectiveDepartmentName,
            ProcessGroupId = baseData.ProcessGroupId,
            ProcessGroupCode = baseData.ProcessGroupCode,
            ProcessGroupName = baseData.ProcessGroupName,
            ProcessGroupType = baseData.ProcessGroupType,
            ProductionOutputs = productionOutputsWithAdj,
            Outputs = plannedOutputs.OrderByDescending(o => o.StartMonth).ToList()
        };

        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);

        return result;
    }

    #region Helper Methods

    private static decimal ResolveAkRate(IEnumerable<AkFactorConfig> configs, decimal akDiff)
    {
        foreach (var config in configs)
        {
            var minMatched = !config.MinAkDiff.HasValue || akDiff >= config.MinAkDiff.Value;
            var maxMatched = !config.MaxAkDiff.HasValue || akDiff <= config.MaxAkDiff.Value;
            if (!minMatched || !maxMatched)
            {
                continue;
            }

            if (config.MinAdjustmentRate.HasValue && config.MaxAdjustmentRate.HasValue)
            {
                if (config.MinAdjustmentRate == config.MaxAdjustmentRate)
                {
                    return config.MinAdjustmentRate.Value;
                }
            }
            else
            {
                return config.MinAdjustmentRate ?? config.MaxAdjustmentRate ?? 0;
            }
        }

        return 0;
    }

    private async Task<Dictionary<Guid, List<MaintainFactorData>>> LoadPlannedMaintainFactors(
        List<Guid> costIds, CancellationToken cancellationToken)
    {
        if (!costIds.Any())
        {
            return new Dictionary<Guid, List<MaintainFactorData>>();
        }

        var data = await _plannedMaintainFactorRepository.GetAll()
            .Where(f => costIds.Contains(f.PlannedMaintainCostId))
            .Select(f => new
            {
                f.PlannedMaintainCostId,
                TrimmingCoefficient = f.PlannedMaintainCost.TrimmingCoefficient,
                f.Quantity,
                f.K6AdjustmentFactorValue,
                OtherMaterialValue = f.MaintainUnitPrice.OtherMaterialValue,
                MaintainStartMonth = f.MaintainUnitPrice.StartMonth,
                Equipments = f.MaintainUnitPrice.MaintainUnitPriceEquipments.Select(m => new
                {
                    m.Quantity,
                    m.ReplacementTimeStandard,
                    m.AverageMonthlyTunnelProduction,
                    PartCosts = m.Part.Costs.Select(c => new { c.StartMonth, c.EndMonth, c.Amount }).ToList()
                }).ToList(),
                AdjustmentValues = f.PlannedMaintainCostAdjustmentFactorDescriptions
                    .Select(d => d.AdjustmentFactorDescription.MaintenanceAdjustmentValue ?? 1.0).ToList()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return data.GroupBy(f => f.PlannedMaintainCostId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => new MaintainFactorData
                {
                    Quantity = (double)f.Quantity,
                    TrimmingCoefficient = f.TrimmingCoefficient,
                    K6AdjustmentFactorValue = f.K6AdjustmentFactorValue,
                    OtherMaterialValue = f.OtherMaterialValue,
                    EquipmentCost = f.Equipments.Sum(m =>
                    {
                        var partCost = m.PartCosts.FirstOrDefault(c => c.StartMonth <= f.MaintainStartMonth && c.EndMonth >= f.MaintainStartMonth)?.Amount ?? 0;
                        return partCost * (m.Quantity / (double)(m.ReplacementTimeStandard * m.AverageMonthlyTunnelProduction));
                    }),
                    AdjustmentFactor = f.AdjustmentValues.Any() ? f.AdjustmentValues.Aggregate(1.0, (acc, val) => acc * val) : 1.0
                }).ToList());
    }

    private async Task<Dictionary<Guid, List<PlannedElectricityFactorData>>> LoadPlannedElectricityFactors(
        List<Guid> costIds, CancellationToken cancellationToken)
    {
        if (!costIds.Any())
        {
            return new Dictionary<Guid, List<PlannedElectricityFactorData>>();
        }

        var data = await _plannedElectricityFactorRepository.GetAll()
            .Include(f => f.ElectricityUnitPriceEquipment)
                .ThenInclude(e => e.Equipment)
                    .ThenInclude(eq => eq.Costs)
            .Where(f => costIds.Contains(f.PlannedElectricityCostId))
            .Select(f => new
            {
                f.PlannedElectricityCostId,
                TrimmingCoefficient = f.PlannedElectricityCost.TrimmingCoefficient,
                f.Quantity,
                f.ElectricityUnitPriceEquipment,
                AdjustmentValues = f.PlannedElectricityCostAdjustmentFactorDescriptions
                    .Select(d => d.AdjustmentFactorDescription.MaintenanceAdjustmentValue ?? 1.0).ToList()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return data.GroupBy(f => f.PlannedElectricityCostId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f =>
                {
                    var costPerMetre = f.ElectricityUnitPriceEquipment.GetElectricityCostPerMetres();

                    return new PlannedElectricityFactorData
                    {
                        Quantity = (double)f.Quantity,
                        TrimmingCoefficient = f.TrimmingCoefficient,
                        CostPerMetre = costPerMetre,
                        AdjustmentFactor = f.AdjustmentValues.Any() ? f.AdjustmentValues.Aggregate(1.0, (acc, val) => acc * val) : 1.0
                    };
                }).ToList());
    }

    private async Task<Dictionary<Guid, double>> LoadPlannedMaterialCosts(
        List<Guid> plannedMaterialCostIds, CancellationToken cancellationToken)
    {
        if (!plannedMaterialCostIds.Any())
        {
            return new Dictionary<Guid, double>();
        }

        var plannedMaterialCosts = await _plannedMaterialCostRepository.GetAll()
            .Where(c => plannedMaterialCostIds.Contains(c.Id))
            .Include(c => c.Output)
            .Include(c => c.SlideUnitPriceAssignmentCode)
            .Include(c => c.NormFactor).ThenInclude(n => n.NormFactorAssignmentCodes)
            .Include(c => c.MaterialUnitPrice).ThenInclude(m => m.MaterialUnitPriceAssignmentCodes)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var currentTunnelMaterials = plannedMaterialCosts
            .Where(c => c.NormFactor != null
                && c.NormFactor.NormFactorAssignmentCodes.Any(nfa => nfa.TargetHardnessId.HasValue)
                && c.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice)
            .Select(c => (TunnelExcavationMaterialUnitPrice)c.MaterialUnitPrice!)
            .ToList();

        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterials = new List<TunnelExcavationMaterialUnitPrice>();
        if (currentTunnelMaterials.Any())
        {
            var targetHardnessIds = plannedMaterialCosts
                .Where(c => c.NormFactor != null)
                .SelectMany(c => c.NormFactor!.NormFactorAssignmentCodes
                    .Where(nfa => nfa.TargetHardnessId.HasValue)
                    .Select(nfa => nfa.TargetHardnessId!.Value))
                .Distinct()
                .ToList();
            var processIds = currentTunnelMaterials.Select(x => x.ProcessId).Distinct().ToList();
            var passportIds = currentTunnelMaterials.Select(x => x.PassportId).Distinct().ToList();
            var insertItemIds = currentTunnelMaterials.Select(x => x.InsertItemId).Distinct().ToList();
            var supportStepIds = currentTunnelMaterials.Select(x => x.SupportStepId).Distinct().ToList();

            tunnelMaterials = await _tunnelMaterialUnitPriceRepository.GetAll()
                .Where(x => targetHardnessIds.Contains(x.HardnessId)
                    && processIds.Contains(x.ProcessId)
                    && passportIds.Contains(x.PassportId)
                    && insertItemIds.Contains(x.InsertItemId)
                    && supportStepIds.Contains(x.SupportStepId))
                .Include(x => x.MaterialUnitPriceAssignmentCodes)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        return PlannedMaterialCostCalculator.CalculateUnitPricesByCostId(plannedMaterialCosts, tunnelMaterials);
    }

    private static double CalculatePlannedMaterialCost(
        OutputRawData output,
        Dictionary<Guid, double> plannedMaterialCosts)
    {
        if (!output.PlannedMaterialCostId.HasValue)
        {
            return 0;
        }

        return plannedMaterialCosts.GetValueOrDefault(output.PlannedMaterialCostId.Value, 0);
    }

    private static double CalculateMaintainCost(
        Guid? costId,
        Dictionary<Guid, List<MaintainFactorData>> maintainFactors)
    {
        if (!costId.HasValue || !maintainFactors.TryGetValue(costId.Value, out var factors))
        {
            return 0;
        }

        var baseCost = factors.Sum(f => f.Quantity * f.EquipmentCost * (1 + (f.OtherMaterialValue ?? 0) / 100.0) * f.K6AdjustmentFactorValue * f.AdjustmentFactor);
        var trimmingCoefficient = factors.FirstOrDefault()?.TrimmingCoefficient ?? 1;
        return baseCost * NormalizeTrimmingCoefficient(trimmingCoefficient);
    }

    private static double CalculatePlannedElectricityCost(
        Guid? costId,
        Dictionary<Guid, List<PlannedElectricityFactorData>> electricityFactors)
    {
        if (!costId.HasValue || !electricityFactors.TryGetValue(costId.Value, out var factors))
        {
            return 0;
        }

        var baseCost = factors.Sum(f => f.Quantity * f.CostPerMetre * f.AdjustmentFactor);
        var trimmingCoefficient = factors.FirstOrDefault()?.TrimmingCoefficient ?? 1;
        return baseCost * NormalizeTrimmingCoefficient(trimmingCoefficient);
    }

    #endregion

    #region Helper Classes

    private class OutputRawData
    {
        public Guid Id { get; set; }
        public Guid ProductUnitPriceId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double ProductionMeters { get; set; }
        public double PlanAshContent { get; set; }
        public Guid? PlannedMaterialCostId { get; set; }
        public Guid? PlannedMaintainCostId { get; set; }
        public Guid? PlannedElectricityCostId { get; set; }
    }

    private class MaintainFactorData
    {
        public double Quantity { get; set; }
        public double TrimmingCoefficient { get; set; }
        public double K6AdjustmentFactorValue { get; set; }
        public double? OtherMaterialValue { get; set; }
        public double EquipmentCost { get; set; }
        public double AdjustmentFactor { get; set; }
    }

    private class ProductionOutputRawData
    {
        public Guid Id { get; set; }
        public Guid? ProductionOutputId { get; set; }
        public DateOnly? StartMonth { get; set; }
        public DateOnly? EndMonth { get; set; }
        public double? ProductionMeters { get; set; }
        public double? StandardProductionMeters { get; set; }
        public double ActualAshContent { get; set; }
    }

    private class PlannedElectricityFactorData
    {
        public double Quantity { get; set; }
        public double TrimmingCoefficient { get; set; }
        public double CostPerMetre { get; set; }
        public double AdjustmentFactor { get; set; }
    }

    private static double NormalizeTrimmingCoefficient(double trimmingCoefficient)
    {
        if (trimmingCoefficient <= 0)
        {
            return 1;
        }

        return trimmingCoefficient > 1 ? trimmingCoefficient / 100 : trimmingCoefficient;
    }

    #endregion
}

