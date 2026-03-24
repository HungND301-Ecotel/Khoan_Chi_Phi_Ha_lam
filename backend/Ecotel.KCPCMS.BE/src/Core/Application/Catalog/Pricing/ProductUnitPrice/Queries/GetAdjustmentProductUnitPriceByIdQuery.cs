using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.Pricing.Common;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
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
                        StandardProductionMeters = po.ProductionOutput.StandardProductionMeters
                    })
                    .ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (baseData == null)
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
        }

        // STEP 2: Get planned outputs with cost data from plan ProductUnitPrice
        var plannedOutputsRaw = await _outputRepository.GetAll()
            .Where(o => o.OutputType == OutputType.PlanOutput
                && o.ProductUnitPrice!.ProductId == baseData.ProductId
                && o.ProductUnitPrice.ScenarioType == ProductUnitPriceScenarioType.Plan)
            .Select(o => new OutputRawData
            {
                Id = o.Id,
                ProductUnitPriceId = o.ProductUnitPriceId,
                StartMonth = o.StartMonth,
                EndMonth = o.EndMonth,
                ProductionMeters = o.ProductionMeters,
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
        }).ToList();

        var plannedOutputsByPeriod = plannedOutputsRaw
            .GroupBy(o => (o.StartMonth, o.EndMonth))
            .ToDictionary(g => g.Key, g => g.ToList());

        // STEP 3: Load planned cost data
        var plannedMaintainCostIds = plannedOutputsRaw.Where(o => o.PlannedMaintainCostId.HasValue).Select(o => o.PlannedMaintainCostId!.Value).ToList();
        var plannedElectricityCostIds = plannedOutputsRaw.Where(o => o.PlannedElectricityCostId.HasValue).Select(o => o.PlannedElectricityCostId!.Value).ToList();
        var plannedMaterialCostIds = plannedOutputsRaw.Where(o => o.PlannedMaterialCostId.HasValue).Select(o => o.PlannedMaterialCostId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialCosts = await LoadPlannedMaterialCosts(plannedMaterialCostIds, cancellationToken);

        // STEP 4: Calculate AdjTotalPrice for each ProductionOutput
        var productionOutputsWithAdj = baseData.ProductionOutputs.Select(po =>
        {
            var adjTotalPrice = 0.0;

            var plannedOutput = plannedOutputsByPeriod.TryGetValue((po.StartMonth, po.EndMonth), out var candidates)
                ? candidates.FirstOrDefault()
                : null;

            if (plannedOutput != null)
            {
                // Calculate unit costs from planned output
                var plannedMaterialCost = CalculatePlannedMaterialCost(plannedOutput, plannedMaterialCosts);
                var plannedMaintainCost = CalculateMaintainCost(plannedOutput.PlannedMaintainCostId, plannedMaintainFactors);
                var plannedElectricityCost = CalculatePlannedElectricityCost(plannedOutput.PlannedElectricityCostId, plannedElectricityFactors);

                adjTotalPrice = po.ProductionMeters * (plannedMaterialCost + plannedMaintainCost + plannedElectricityCost);
            }

            return new AdjustmentProductionOutputDto
            {
                Id = po.Id,
                ProductionOutputId = po.ProductionOutputId,
                StartMonth = po.StartMonth,
                EndMonth = po.EndMonth,
                ProductionMeters = po.ProductionMeters,
                StandardProductionMeters = po.StandardProductionMeters,
                AdjTotalPrice = adjTotalPrice
            };
        }).ToList();

        var result = new AdjustmentProductUnitPriceDetailDto
        {
            Id = baseData.Id,
            ProductId = baseData.ProductId,
            ProductName = baseData.ProductName,
            ProductCode = baseData.ProductCode,
            UnitOfMeasureId = baseData.UnitOfMeasureId,
            UnitOfMeasureName = baseData.UnitOfMeasureName,
            ProcessGroupId = baseData.ProcessGroupId,
            ProcessGroupCode = baseData.ProcessGroupCode,
            ProcessGroupName = baseData.ProcessGroupName,
            ProcessGroupType = baseData.ProcessGroupType,
            ProductionOutputs = productionOutputsWithAdj,
            Outputs = plannedOutputs
        };

        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);

        return result;
    }

    #region Helper Methods

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
                f.Quantity,
                f.K6AdjustmentFactorValue,
                OtherMaterialValue = f.MaintainUnitPrice.OtherMaterialValue,
                MaintainStartMonth = f.MaintainUnitPrice.StartMonth,
                Equipments = f.MaintainUnitPrice.MaintainUnitPriceEquipments.Select(m => new
                {
                    m.Quantity,
                    m.Part.ReplacementTimeStandard,
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
            .Where(c => c.NormFactor?.TargetHardnessId.HasValue == true && c.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice)
            .Select(c => (TunnelExcavationMaterialUnitPrice)c.MaterialUnitPrice!)
            .ToList();

        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterials = new List<TunnelExcavationMaterialUnitPrice>();
        if (currentTunnelMaterials.Any())
        {
            var targetHardnessIds = plannedMaterialCosts
                .Where(c => c.NormFactor?.TargetHardnessId.HasValue == true)
                .Select(c => c.NormFactor!.TargetHardnessId!.Value)
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

        return factors.Sum(f => f.Quantity * f.EquipmentCost * (1 + (f.OtherMaterialValue ?? 0) / 100.0) * f.K6AdjustmentFactorValue * f.AdjustmentFactor);
    }

    private static double CalculatePlannedElectricityCost(
        Guid? costId,
        Dictionary<Guid, List<PlannedElectricityFactorData>> electricityFactors)
    {
        if (!costId.HasValue || !electricityFactors.TryGetValue(costId.Value, out var factors))
        {
            return 0;
        }

        return factors.Sum(f => f.Quantity * f.CostPerMetre * f.AdjustmentFactor);
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
        public Guid? PlannedMaterialCostId { get; set; }
        public Guid? PlannedMaintainCostId { get; set; }
        public Guid? PlannedElectricityCostId { get; set; }
    }

    private class MaintainFactorData
    {
        public double Quantity { get; set; }
        public double K6AdjustmentFactorValue { get; set; }
        public double? OtherMaterialValue { get; set; }
        public double EquipmentCost { get; set; }
        public double AdjustmentFactor { get; set; }
    }

    private class PlannedElectricityFactorData
    {
        public double Quantity { get; set; }
        public double CostPerMetre { get; set; }
        public double AdjustmentFactor { get; set; }
    }

    #endregion
}
